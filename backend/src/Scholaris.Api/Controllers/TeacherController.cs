using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scholaris.Application.Common;
using Scholaris.Application.Dtos;
using Scholaris.Application.Services;

namespace Scholaris.Api.Controllers;

/// <summary>Teacher-facing income dashboard: own monthly gross / service commission / net, with CSV export.</summary>
[ApiController]
[Route("api/teacher")]
[Authorize(Roles = "Teacher")]
public class TeacherController(RevenueService revenue, AdminCourseService courses, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("courses")]
    public async Task<ActionResult<List<AdminCourseDto>>> Courses()
    {
        if (currentUser.UserId is not { } uid) return Unauthorized();
        return await courses.ListByTeacherAsync(uid);
    }

    [HttpGet("revenue")]
    public async Task<ActionResult<List<RevenueRowDto>>> Revenue([FromQuery] int? year)
    {
        if (currentUser.UserId is not { } uid) return Unauthorized();
        return await revenue.TeacherRevenueAsync(uid, year ?? DateTime.UtcNow.Year);
    }

    [HttpGet("revenue/csv")]
    public async Task<IActionResult> RevenueCsv([FromQuery] int? year)
    {
        if (currentUser.UserId is not { } uid) return Unauthorized();
        var y = year ?? DateTime.UtcNow.Year;
        var csv = RevenueService.ToCsv(await revenue.TeacherRevenueAsync(uid, y));
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"scholaris-teacher-income-{y}.csv");
    }
}
