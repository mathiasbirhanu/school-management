using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scholaris.Application.Common;
using Scholaris.Application.Dtos;
using Scholaris.Application.Services;

namespace Scholaris.Api.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController(CatalogService catalog) : ControllerBase
{
    /// <summary>Published catalog for the public home/course pages (includes admin-set discount tags).</summary>
    [HttpGet]
    public async Task<ActionResult<List<CourseListItem>>> List() => await catalog.GetPublishedAsync();

    /// <summary>Courses currently discounted — powers the balloon announcement.</summary>
    [HttpGet("discounted")]
    public async Task<ActionResult<List<CourseListItem>>> Discounted() => await catalog.GetDiscountedAsync();

    /// <summary>Course detail with curriculum; lesson bodies stay gated until enrollment.</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<CourseDetailDto>> Detail(string slug) => await catalog.GetDetailAsync(slug);
}
