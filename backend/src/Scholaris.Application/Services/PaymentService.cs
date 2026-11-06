using Microsoft.EntityFrameworkCore;
using Scholaris.Application.Common;
using Scholaris.Application.Dtos;
using Scholaris.Application.Interfaces;
using Scholaris.Domain.Entities;
using Scholaris.Domain.Enums;
using Scholaris.Domain.Exceptions;

namespace Scholaris.Application.Services;

/// <summary>
/// Checkout flow: student picks a payment method (Tele Birr mock, CBE Birr mock, M-Pesa mock,
/// card via Chapa placeholder), an Order is created with the commission rate snapshotted at
/// payment time, then confirmed. Until real provider credentials are configured, every method
/// follows the mock path (ADR-013).
/// </summary>
public class PaymentService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IPaymentProviderResolver resolver,
    SettingsService settings)
{
    public IReadOnlyList<PaymentProviderInfo> GetMethods() => resolver.ListMethods();

    public async Task<CheckoutResponse> CheckoutAsync(CheckoutRequest req)
    {
        var uid = RequireUser();
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == req.CourseId && c.Status == CourseStatus.Published)
                     ?? throw new NotFoundException("Course not found or not open for enrollment.");
        if (await db.Enrollments.AnyAsync(e => e.StudentId == uid && e.CourseId == course.Id))
            throw new DomainException("You already own this course.");

        var provider = resolver.Resolve(req.MethodCode)
                       ?? throw new DomainException("Unknown payment method.");

        var final = course.FinalPrice();
        var rate = (await settings.GetSettingAsync()).CommissionRatePercent;

        var order = new Order
        {
            StudentId = uid,
            CourseId = course.Id,
            CourseTitle = course.Title,
            ProviderCode = provider.Info.Code,
            GrossAmount = course.Price,
            DiscountAmount = course.Price - final,
            FinalAmount = final,
            CommissionRatePercent = rate, // snapshot — later rate changes never rewrite history
            Status = OrderStatus.Pending,
            ProviderRef = ""
        };
        order.CommissionAmount = Math.Round(final * rate / 100m, 2, MidpointRounding.AwayFromZero);
        order.NetAmount = final - order.CommissionAmount;

        var initiation = await provider.InitiateAsync(order);
        order.ProviderRef = initiation.ProviderRef;

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return new CheckoutResponse(order.Id, initiation.ProviderRef, initiation.Instructions, final, order.Currency);
    }

    /// <summary>
    /// Confirms the order. In Mock mode the UI calls this right after checkout to simulate the
    /// provider callback; the webhook endpoint (/api/payments/webhook/{"provider"}) is the
    /// integration point for live providers.
    /// </summary>
    public async Task<ConfirmResponse> ConfirmAsync(ConfirmRequest req)
    {
        var uid = RequireUser();
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == req.OrderId && o.StudentId == uid)
                    ?? throw new NotFoundException("Order not found.");
        if (order.Status == OrderStatus.Paid)
            return new ConfirmResponse(order.Id, nameof(OrderStatus.Paid), true);

        order.Status = OrderStatus.Paid;
        order.PaidAt = DateTime.UtcNow;

        if (!await db.Enrollments.AnyAsync(e => e.StudentId == uid && e.CourseId == order.CourseId))
            db.Enrollments.Add(new Enrollment { StudentId = uid, CourseId = order.CourseId });

        await db.SaveChangesAsync();
        return new ConfirmResponse(order.Id, nameof(OrderStatus.Paid), true);
    }

    private Guid RequireUser() =>
        currentUser.UserId ?? throw new ForbiddenException("Please sign in to continue.");
}
