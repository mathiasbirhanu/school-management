using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scholaris.Application.Dtos;
using Scholaris.Application.Services;

namespace Scholaris.Api.Controllers;

/// <summary>
/// Admin back office: platform income (commission) rate, course catalog with price and
/// discount control, certificate template wording, and the revenue dashboard with CSV export.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(
    SettingsService settings,
    AdminCourseService courses,
    RevenueService revenue) : ControllerBase
{
    // ── Platform settings (income rate) ──
    [HttpGet("settings")]
    public async Task<ActionResult<PlatformSettingsDto>> GetSettings() => await settings.GetAsync();

    [HttpPut("settings")]
    public async Task<ActionResult<PlatformSettingsDto>> UpdateSettings(UpdateSettingsRequest req) =>
        await settings.UpdateAsync(req);

    // ── Certificate template ──
    [HttpGet("certificate-template")]
    public async Task<ActionResult<CertificateTemplateDto>> GetTemplate() => await settings.GetTemplateDtoAsync();

    [HttpPut("certificate-template")]
    public async Task<ActionResult<CertificateTemplateDto>> UpdateTemplate(CertificateTemplateDto req) =>
        await settings.UpdateTemplateAsync(req);

    // ── Course management (price / discount / publish / delete) ──
    [HttpGet("courses")]
    public async Task<ActionResult<List<AdminCourseDto>>> Courses() => await courses.ListAsync();

    [HttpGet("teachers")]
    public async Task<ActionResult<List<TeacherOptionDto>>> Teachers() => await courses.TeachersAsync();

    [HttpPost("courses")]
    public async Task<ActionResult<AdminCourseDto>> CreateCourse(AdminCourseCreateRequest req) =>
        await courses.CreateAsync(req);

    [HttpPut("courses/{id:guid}")]
    public async Task<ActionResult<AdminCourseDto>> UpdateCourse(Guid id, AdminCourseUpdateRequest req) =>
        await courses.UpdateAsync(id, req);

    [HttpDelete("courses/{id:guid}")]
    public async Task<IActionResult> DeleteCourse(Guid id)
    {
        await courses.DeleteAsync(id);
        return NoContent();
    }

    // ── Revenue dashboard + CSV export ──
    [HttpGet("revenue")]
    public async Task<ActionResult<List<RevenueRowDto>>> Revenue([FromQuery] int? year) =>
        await revenue.AdminRevenueAsync(year ?? DateTime.UtcNow.Year);

    [HttpGet("revenue/csv")]
    public async Task<IActionResult> RevenueCsv([FromQuery] int? year)
    {
        var y = year ?? DateTime.UtcNow.Year;
        var csv = RevenueService.ToCsv(await revenue.AdminRevenueAsync(y));
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"scholaris-revenue-{y}.csv");
    }
}
