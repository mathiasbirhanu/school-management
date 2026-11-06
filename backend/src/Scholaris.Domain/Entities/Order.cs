using Scholaris.Domain.Enums;

namespace Scholaris.Domain.Entities;

/// <summary>
/// One course purchase. Money is ETB. At checkout the platform commission rate is
/// SNAPSHOTTED from PlatformSetting so later rate changes never rewrite history.
/// Gross = FinalAmount (what the student paid after discount), Commission = platform
/// service income, Net = teacher income.
/// </summary>
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public User Student { get; set; } = default!;
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = default!;
    public string CourseTitle { get; set; } = default!;
    public string ProviderCode { get; set; } = default!;
    public string ProviderRef { get; set; } = default!;
    public string Currency { get; set; } = "ETB";
    /// <summary>List price before discount.</summary>
    public decimal GrossAmount { get; set; }
    /// <summary>Amount taken off by the admin-set discount.</summary>
    public decimal DiscountAmount { get; set; }
    /// <summary>Amount actually paid = Gross - Discount.</summary>
    public decimal FinalAmount { get; set; }
    /// <summary>Commission rate snapshot (%) taken at payment time.</summary>
    public decimal CommissionRatePercent { get; set; }
    /// <summary>Platform service income = round(Final * Rate / 100).</summary>
    public decimal CommissionAmount { get; set; }
    /// <summary>Teacher income = Final - Commission.</summary>
    public decimal NetAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
}
