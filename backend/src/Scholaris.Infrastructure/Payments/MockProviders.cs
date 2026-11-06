using Scholaris.Application.Interfaces;
using Scholaris.Domain.Entities;
using Scholaris.Domain.Exceptions;

namespace Scholaris.Infrastructure.Payments;

/// <summary>
/// Mock payment adapters — the DEFAULT path until real provider credentials are wired in
/// (Payments:Mode=Live + ADR-013 integration points). Each one imitates its real Ethiopian
/// provider's flow so the checkout experience is already shaped correctly.
/// </summary>
public abstract class MockPaymentProviderBase : IPaymentProvider
{
    public abstract PaymentProviderInfo Info { get; }

    public Task<PaymentInitiation> InitiateAsync(Order order, CancellationToken ct = default)
    {
        var reference = $"MOCK-{Info.Code.ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}";
        return Task.FromResult(new PaymentInitiation(reference,
            $"[Mock] In production you would approve {order.FinalAmount:0.00} ETB inside {Info.Name}. " +
            "Mock mode simply waits for your confirmation on Scholaris, exactly where the real provider callback will land."));
    }
}

public sealed class MockTeleBirrProvider : MockPaymentProviderBase
{
    public override PaymentProviderInfo Info { get; } = new(
        "telebirr_mock", "Tele Birr", "Ethio Telecom mobile wallet — USSD/app approval", IsMock: true);
}

public sealed class MockCbeBirrProvider : MockPaymentProviderBase
{
    public override PaymentProviderInfo Info { get; } = new(
        "cbebirr_mock", "CBE Birr", "Commercial Bank of Ethiopia mobile banking", IsMock: true);
}

public sealed class MockMPesaProvider : MockPaymentProviderBase
{
    public override PaymentProviderInfo Info { get; } = new(
        "mpesa_mock", "M-Pesa", "Safaricom Ethiopia mobile money", IsMock: true);
}

public sealed class MockCardProvider : MockPaymentProviderBase
{
    public override PaymentProviderInfo Info { get; } = new(
        "chapa_card_mock", "Card / Bank (Chapa)", "Cards and bank checkout through Chapa", IsMock: true);
}
