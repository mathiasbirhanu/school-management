using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scholaris.Application.Interfaces;
using Scholaris.Domain.Exceptions;

namespace Scholaris.Infrastructure.Payments;

/// <summary>
/// Chooses the active payment path. Payments:Mode=Mock (default) exposes the four mock
/// Ethiopian methods; Payments:Mode=Live switches to real adapters once keys exist.
/// </summary>
public class PaymentProviderResolver(IConfiguration config, IServiceProvider services) : IPaymentProviderResolver
{
    private readonly bool _live =
        string.Equals(config["Payments:Mode"], "Live", StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<PaymentProviderInfo> ListMethods()
    {
        if (_live)
        {
            var chapa = services.GetRequiredService<ChapaProvider>();
            return [chapa.Info];
        }
        return
        [
            new MockTeleBirrProvider().Info,
            new MockCbeBirrProvider().Info,
            new MockMPesaProvider().Info,
            new MockCardProvider().Info
        ];
    }

    public IPaymentProvider Resolve(string code)
    {
        if (_live)
        {
            var chapa = services.GetRequiredService<ChapaProvider>();
            if (string.Equals(code, chapa.Info.Code, StringComparison.OrdinalIgnoreCase)) return chapa;
            throw new DomainException($"Payment method '{code}' is not available in Live mode.");
        }

        return code.ToLowerInvariant() switch
        {
            "telebirr_mock" => new MockTeleBirrProvider(),
            "cbebirr_mock" => new MockCbeBirrProvider(),
            "mpesa_mock" => new MockMPesaProvider(),
            "chapa_card_mock" => new MockCardProvider(),
            _ => throw new DomainException($"Unknown payment method '{code}'.")
        };
    }
}
