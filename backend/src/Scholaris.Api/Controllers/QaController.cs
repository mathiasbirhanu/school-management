using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scholaris.Application.Common;
using Scholaris.Application.Dtos;
using Scholaris.Application.Services;

namespace Scholaris.Api.Controllers;

[ApiController]
[Route("api/qa")]
[Authorize]
public class QaController(QaService qa) : ControllerBase
{
    /// <summary>Student asks the teacher (course teacher sees it; the AI answers separately, ephemerally).</summary>
    [HttpPost("threads")]
    public async Task<ActionResult<QaThreadDto>> Create(QaCreateRequest req) => await qa.CreateAsync(req);

    /// <summary>Without courseId: my own questions. With courseId (teacher only): all questions for that course.</summary>
    [HttpGet("threads")]
    public async Task<ActionResult<List<QaThreadDto>>> List([FromQuery] Guid? courseId) =>
        courseId == null ? await qa.ListMineAsync() : await qa.ListByCourseAsync(courseId.Value);

    [HttpPost("threads/{id:guid}/answer")]
    public async Task<ActionResult<QaThreadDto>> Answer(Guid id, QaAnswerRequest req) =>
        await qa.AnswerAsync(id, req);
}
