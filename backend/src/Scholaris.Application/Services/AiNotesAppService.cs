using Microsoft.EntityFrameworkCore;
using Scholaris.Application.Common;
using Scholaris.Application.Dtos;
using Scholaris.Application.Interfaces;
using Scholaris.Domain.Exceptions;

namespace Scholaris.Application.Services;

/// <summary>AI summaries/elaborations of the teacher's notes — returned ephemerally, never stored.</summary>
public class AiNotesAppService(IAppDbContext db, ICurrentUser currentUser, IAiNotesService ai)
{
    public async Task<LessonSummaryDto> SummarizeAsync(Guid lessonId)
    {
        var uid = currentUser.UserId ?? throw new ForbiddenException("Please sign in to continue.");
        var lesson = await db.Lessons.AsNoTracking()
            .Include(l => l.Topic).ThenInclude(t => t.Module).ThenInclude(m => m.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId)
            ?? throw new NotFoundException("Lesson not found.");

        var course = lesson.Topic.Module.Course;
        if (!await db.Enrollments.AnyAsync(e => e.StudentId == uid && e.CourseId == course.Id))
            throw new PaymentRequiredException(course.Id, course.Price, course.DiscountPercent, course.FinalPrice());

        if (string.IsNullOrWhiteSpace(lesson.Content))
            throw new DomainException("This lesson has no notes to summarize yet.");

        var summary = await ai.SummarizeAsync(new LessonNotesContext(course.Title, lesson.Title, lesson.Content));
        return new LessonSummaryDto(summary.Summary, summary.KeyPoints, summary.Elaborated);
    }
}
