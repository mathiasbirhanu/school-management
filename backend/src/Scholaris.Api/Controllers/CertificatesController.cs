using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scholaris.Application.Dtos;
using Scholaris.Application.Services;

namespace Scholaris.Api.Controllers;

[ApiController]
[Route("api/certificates")]
public class CertificatesController(CertificateService certificates) : ControllerBase
{
    [Authorize]
    [HttpGet("mine")]
    public async Task<ActionResult<List<CertificateViewDto>>> Mine() => await certificates.MineAsync();

    /// <summary>Public verification: anyone with the serial can view the rendered certificate.</summary>
    [AllowAnonymous]
    [HttpGet("{serial}")]
    public async Task<ActionResult<CertificateViewDto>> BySerial(string serial) =>
        await certificates.GetBySerialAsync(serial);
}
