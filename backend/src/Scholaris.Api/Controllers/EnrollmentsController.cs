using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scholaris.Application.Dtos;
using Scholaris.Application.Services;

namespace Scholaris.Api.Controllers;

[ApiController]
[Route("api/enrollments")]
[Authorize]
public class EnrollmentsController(LearnService learn) : ControllerBase
{
    /// <summary>The signed-in student's enrollments with progress and certificate serials.</summary>
    [HttpGet]
    public async Task<ActionResult<List<EnrollmentDto>>> Mine() => await learn.MineAsync();
}
