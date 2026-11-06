using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scholaris.Application.Dtos;
using Scholaris.Application.Services;

namespace Scholaris.Api.Controllers;

[ApiController]
[Route("api/learn")]
[Authorize]
public class LearnController(LearnService learn) : ControllerBase
{
    /// <summary>Full paid-access course view: lesson bodies, video ids, progress, certificate serial.</summary>
    [HttpGet("{courseId:guid}")]
    public async Task<ActionResult<LearnCourseDto>> Course(Guid courseId) => await learn.GetCourseAsync(courseId);

    /// <summary>Marks a lesson complete; auto-issues a certificate when the course finishes.</summary>
    [HttpPost("lessons/{lessonId:guid}/complete")]
    public async Task<ActionResult<CompleteLessonResponse>> Complete(Guid lessonId) =>
        await learn.CompleteLessonAsync(lessonId);
}
