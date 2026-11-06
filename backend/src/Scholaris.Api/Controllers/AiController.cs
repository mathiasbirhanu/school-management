using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scholaris.Application.Dtos;
using Scholaris.Application.Services;

namespace Scholaris.Api.Controllers;

/// <summary>
/// AI endpoints. Exams are generated per student/per attempt from the topic's concepts,
/// evaluated by AI, and NEVER persisted (nothing is written to the database). Note
/// summaries are returned ephemerally as well.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class AiController(AiExamAppService exam, AiNotesAppService notes) : ControllerBase
{
    [HttpPost("ai-exam/generate")]
    public async Task<ActionResult<ExamPaperDto>> GenerateExam(GenerateExamRequest req) =>
        await exam.GenerateAsync(req.TopicId);

    [HttpPost("ai-exam/evaluate")]
    public async Task<ActionResult<ExamEvaluationDto>> EvaluateExam(ExamEvaluateRequest req) =>
        await exam.EvaluateAsync(req);

    [HttpPost("ai/notes/summarize")]
    public async Task<ActionResult<LessonSummaryDto>> SummarizeNotes(NotesSummarizeRequest req) =>
        await notes.SummarizeAsync(req.LessonId);

    public record GenerateExamRequest(Guid TopicId);
}
