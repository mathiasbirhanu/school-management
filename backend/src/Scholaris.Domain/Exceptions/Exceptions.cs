namespace Scholaris.Domain.Exceptions;

/// <summary>Base for expected, user-facing failures → HTTP 400 by default.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message) { }
}

public class ForbiddenException : DomainException
{
    public ForbiddenException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a paid action is attempted without enrollment. The API maps this to
/// HTTP 402 with pricing info so the UI can route the user to checkout.
/// </summary>
public class PaymentRequiredException : DomainException
{
    public Guid CourseId { get; }
    public decimal Price { get; }
    public decimal DiscountPercent { get; }
    public decimal FinalPrice { get; }

    public PaymentRequiredException(Guid courseId, decimal price, decimal discountPercent, decimal finalPrice)
        : base("This course requires payment before access.")
    {
        CourseId = courseId;
        Price = price;
        DiscountPercent = discountPercent;
        FinalPrice = finalPrice;
    }
}
