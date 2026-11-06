using Scholaris.Domain.Exceptions;

namespace Scholaris.Api.Middleware;

/// <summary>Maps domain exceptions to clean HTTP responses (402 pricing payload for paywalled content).</summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, body) = ex switch
        {
            PaymentRequiredException p => (StatusCodes.Status402PaymentRequired, (object)new
            {
                message = p.Message,
                courseId = p.CourseId,
                price = p.Price,
                discountPercent = p.DiscountPercent,
                finalPrice = p.FinalPrice
            }),
            NotFoundException => (StatusCodes.Status404NotFound, (object)new { message = ex.Message }),
            ForbiddenException => (StatusCodes.Status403Forbidden, (object)new { message = ex.Message }),
            DomainException => (StatusCodes.Status400BadRequest, (object)new { message = ex.Message }),
            _ => (StatusCodes.Status500InternalServerError, (object)new { message = "Unexpected server error. Please try again." })
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(ex, "Unhandled exception while processing {Path}", context.Request.Path);

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(body);
    }
}
