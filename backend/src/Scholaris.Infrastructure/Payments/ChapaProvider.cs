using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Scholaris.Application.Interfaces;
using Scholaris.Domain.Entities;
using Scholaris.Domain.Exceptions;

namespace Scholaris.Infrastructure.Payments;

/// <summary>
/// ───────────────── REAL PAYMENT INTEGRATION POINT (ADR-013) ─────────────────
/// Chapa aggregates Tele Birr, CBE Birr, M-Pesa, cards and bank transfer behind one API.
/// Until Payments:Chapa:ApiKey is configured this adapter refuses to run and the platform
/// stays on the mock path. When you obtain production keys from chapa.co:
///   1. Set Payments:Mode=Live and Payments:Chapa:ApiKey in configuration.
///   2. Point your Chapa dashboard webhook to POST /api/payments/webhook/chapa.
/// The HTTP calls below follow Chapa's documented Initialize + Verify flow.
/// </summary>
public class ChapaProvider(IConfiguration config, IHttpClientFactory httpClientFactory) : IPaymentProvider
{
    public PaymentProviderInfo Info { get; } = new(
        "chapa_live", "Chapa (Tele Birr, CBE Birr, cards…)", "Live checkout through the Chapa aggregator", IsMock: false);

    public async Task<PaymentInitiation> InitiateAsync(Order order, CancellationToken ct = default)
    {
        var apiKey = config["Payments:Chapa:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new DomainException(
                "Live payments are not configured yet. Set Payments:Chapa:ApiKey (or keep Payments:Mode=Mock).");

        var baseUrl = config["Payments:Chapa:BaseUrl"] ?? "https://api.chapa.co/v1";
        var client = httpClientFactory.CreateClient("chapa");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            amount = order.FinalAmount.ToString("0.00"),
            currency = "ETB",
            tx_ref = order.Id.ToString(),
            callback_url = $"{config["Payments:PublicBaseUrl"] ?? ""}/api/payments/webhook/chapa",
            return_url = $"{config["Payments:PublicBaseUrl"] ?? ""}/checkout/success?order={order.Id}",
            customization = new { title = "Scholaris", description = $"Course: {order.CourseTitle}" }
        };

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync($"{baseUrl.TrimEnd('/')}/transaction/initialize", content, ct);
        if (!response.IsSuccessStatusCode)
            throw new DomainException($"Chapa rejected the payment ({(int)response.StatusCode}). Check your API keys.");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var checkoutUrl = doc.RootElement.GetProperty("data").GetProperty("checkout_url").GetString()
                          ?? throw new DomainException("Chapa returned no checkout URL.");
        return new PaymentInitiation(order.Id.ToString(), $"Redirect the student to {checkoutUrl}");
    }

    /// <summary>Server-side verification used by the webhook before granting access.</summary>
    public async Task<bool> VerifyAsync(string txRef, CancellationToken ct = default)
    {
        var apiKey = config["Payments:Chapa:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return false;

        var baseUrl = config["Payments:Chapa:BaseUrl"] ?? "https://api.chapa.co/v1";
        var client = httpClientFactory.CreateClient("chapa");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await client.GetAsync($"{baseUrl.TrimEnd('/')}/transaction/verify/{txRef}", ct);
        if (!response.IsSuccessStatusCode) return false;

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("data").GetProperty("status").GetString() == "success";
    }
}
