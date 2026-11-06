using Microsoft.EntityFrameworkCore;
using Scholaris.Application.Common;
using Scholaris.Application.Dtos;
using Scholaris.Application.Interfaces;
using Scholaris.Domain.Enums;
using Scholaris.Domain.Exceptions;

namespace Scholaris.Application.Services;

/// <summary>
/// Orchestrates AI-generated exams. The paper is created fresh for every student and every
/// attempt, anchored on the topic's key concepts and the teacher's notes, held ONLY in an
/// in-memory session store (TTL 90 minutes) and destroyed immediately after evaluation.
/// Nothing about exams is written to the database — by explicit product decision.
/// </summary>
public class AiExamAppService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IAiExamService ai,
    IExamSessionStore store)
{
    public async Task<ExamPaperDto> GenerateAsync(Guid topicId)
    {
        var uid = RequireUser();
        var topic = await db.Topics.AsNoTracking()
            .Include(t => t.Module).ThenInclude(m => m.Course)
            .Include(t => t.Lessons)
            .FirstOrDefaultAsync(t => t.Id == topicId)
            ?? throw new NotFoundException("Topic not found.");

        var course = topic.Module.Course;
        if (!await db.Enrollments.AnyAsync(e => e.StudentId == uid && e.CourseId == course.Id))
            throw new PaymentRequiredException(course.Id, course.Price, course.DiscountPercent, course.FinalPrice());

        var notes = string.Join("\n\n",
            topic.Lessons.Where(l => l.Type == LessonType.Note).Select(l => l.Content));
        if (string.IsNullOrWhiteSpace(notes)) notes = course.Summary;

        // Variation seed mixes student + timestamp so two students (or the same student
        // at another moment) get differently angled questions on the same concepts.
        var seed = $"{uid:N}:{DateTime.UtcNow:yyyyMMddHHmmssfff}:{Guid.NewGuid():N}"[..48];

        var paper = await ai.GenerateAsync(new ExamGenerationContext(
            topic.Title, topic.Concepts, Truncate(notes, 4000), course.Subject, course.Level, seed));

        store.Save(paper, TimeSpan.FromMinutes(90));
        return new ExamPaperDto(paper.ExamId, topicId, topic.Title,
            paper.Questions.Select(q => new ExamQuestionDto(q.Id, q.Prompt, q.MaxScore)).ToList());
    }

    public async Task<ExamEvaluationDto> EvaluateAsync(ExamEvaluateRequest req)
    {
        RequireUser();
        var paper = store.Take(req.ExamId)
                    ?? throw new DomainException("This exam session has expired or was already submitted. Generate a fresh one.");
        if (req.Answers.Count == 0)
            throw new DomainException("Please answer at least one question before submitting.");

        var evaluation = await ai.EvaluateAsync(paper,
            req.Answers.Select(a => new ExamAnswer(a.QuestionId, a.Text)).ToList());

        return new ExamEvaluationDto(
            evaluation.ScorePercent,
            evaluation.OverallFeedback,
            evaluation.Results.Select(r => new ExamResultDto(r.QuestionId, r.Verdict, r.Score, r.MaxScore, r.Feedback)).ToList());
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    private Guid RequireUser() =>
        currentUser.UserId ?? throw new ForbiddenException("Please sign in to continue.");
}
