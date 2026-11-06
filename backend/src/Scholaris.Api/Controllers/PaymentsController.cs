using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scholaris.Application.Dtos;
using Scholaris.Application.Services;

namespace Scholaris.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(PaymentService payments) : ControllerBase
{
    /// <summary>Selectable payment methods for the checkout page (payer's choice).</summary>
    [Authorize]
    [HttpGet("methods")]
    public IActionResult Methods() => Ok(payments.GetMethods());

    /// <summary>Creates a Pending order with the commission rate snapshotted at this moment.</summary>
    [Authorize]
    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutResponse>> Checkout(CheckoutRequest req) => await payments.CheckoutAsync(req);

    /// <summary>Confirms payment. Mock path: the UI calls this after checkout. Live path: the provider webhook confirms instead.</summary>
    [Authorize]
    [HttpPost("confirm")]
    public async Task<ActionResult<ConfirmResponse>> Confirm(ConfirmRequest req) => await payments.ConfirmAsync(req);

    /// <summary>
    /// ── REAL PROVIDER WEBHOOK INTEGRATION POINT (ADR-013) ──
    /// Configure each provider's dashboard to POST here. In Live mode: verify the provider
    /// signature, load the matching order, verify with the provider (ChapaProvider.VerifyAsync),
    /// then mark Paid exactly as PaymentService.ConfirmAsync does.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook/{provider}")]
    public IActionResult Webhook(string provider) => Ok(new
    {
        received = true,
        provider,
        note = "Webhook accepted. Signature verification + order confirmation activate when Payments:Mode=Live."
    });
}
