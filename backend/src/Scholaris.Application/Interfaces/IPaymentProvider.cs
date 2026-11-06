using Scholaris.Domain.Entities;

namespace Scholaris.Application.Interfaces;

public record PaymentInitiation(string ProviderRef, string Instructions);

/// <summary>What the checkout page shows as a selectable method.</summary>
public record PaymentProviderInfo(string Code, string Name, string Description, bool IsMock);

/// <summary>
/// Payment adapter (Strategy pattern). Mock implementations ship by default; live
/// Tele Birr / Chapa adapters plug in behind the same interface without touching
/// the checkout flow (ADR-013). Money amounts on the order are final ETB values.
/// </summary>
public interface IPaymentProvider
{
    PaymentProviderInfo Info { get; }
    Task<PaymentInitiation> InitiateAsync(Order order, CancellationToken ct = default);
}

public interface IPaymentProviderResolver
{
    IReadOnlyList<PaymentProviderInfo> ListMethods();
    IPaymentProvider Resolve(string code);
}
