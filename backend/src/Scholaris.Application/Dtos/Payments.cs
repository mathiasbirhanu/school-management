namespace Scholaris.Application.Dtos;

public record PaymentMethodDto(string Code, string Name, string Description, bool IsMock);
public record CheckoutRequest(Guid CourseId, string MethodCode);
public record CheckoutResponse(Guid OrderId, string ProviderRef, string Instructions, decimal Amount, string Currency);
public record ConfirmRequest(Guid OrderId);
public record ConfirmResponse(Guid OrderId, string Status, bool Enrolled);
