using Microsoft.EntityFrameworkCore;
using Scholaris.Application.Common;
using Scholaris.Application.Dtos;
using Scholaris.Domain.Entities;
using Scholaris.Domain.Exceptions;

namespace Scholaris.Application.Services;

/// <summary>
/// Paid-access learning area. Every method enforces enrollment; unenrolled callers get
/// HTTP 402 with pricing so the UI can route them to checkout. When the last lesson of a
/// course is completed, a Certificate is issued automatically from the admin template.
/// </summary>
public class LearnService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<LearnCourseDto> GetCourseAsync(Guid courseId)
    {
        var uid = RequireUser();
        var course = await db.Courses.AsNoTracking()
            .Include(c => c.Teacher)
            .Include(c => c.Modules).ThenInclude(m => m.Topics).ThenInclude(t => t.Lessons)
            .FirstOrDefaultAsync(c => c.Id == courseId)
            ?? throw new NotFoundException("Course not found.");

        await EnsureEnrolledAsync(uid, course);

        var lessons = course.Modules.SelectMany(m => m.Topics).SelectMany(t => t.Lessons).ToList();
        var lessonIds = lessons.Select(l => l.Id).ToList();
        var completed = await db.LessonCompletions.AsNoTracking()
            .Where(lc => lc.StudentId == uid && lessonIds.Contains(lc.LessonId))
            .Select(lc => lc.LessonId)
            .ToListAsync();

        var progress = lessons.Count == 0 ? 0 : Math.Round(completed.Count * 100.0 / lessons.Count, 1);
        var serial = await db.Certificates.AsNoTracking()
            .Where(c => c.StudentId == uid && c.CourseId == courseId)
            .Select(c => (string?)c.SerialCode)
            .FirstOrDefaultAsync();

        return new LearnCourseDto(
            course.Id, course.Title, course.Subject, course.Level, course.Teacher.FullName,
            course.Modules.OrderBy(m => m.OrderIndex).Select(m => new LearnModuleDto(
                m.Id, m.Title, m.OrderIndex,
                m.Topics.OrderBy(t => t.OrderIndex).Select(t => new LearnTopicDto(
                    t.Id, t.Title, t.OrderIndex,
                    t.Lessons.OrderBy(l => l.OrderIndex).Select(l => new LearnLessonDto(
                        l.Id, l.Title, l.Type.ToString(), l.DurationMinutes, l.OrderIndex,
                        l.Content, l.VideoYoutubeId)).ToList())).ToList())).ToList(),
            completed,
            progress,
            serial);
    }

    public async Task<List<EnrollmentDto>> MineAsync()
    {
        var uid = RequireUser();
        var enrollments = await db.Enrollments.AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.StudentId == uid)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();

        var result = new List<EnrollmentDto>();
        foreach (var e in enrollments)
        {
            var lessons = await db.Lessons.AsNoTracking()
                .Where(l => l.Topic.Module.CourseId == e.CourseId)
                .Select(l => l.Id)
                .ToListAsync();
            var completed = lessons.Count == 0
                ? 0
                : await db.LessonCompletions.AsNoTracking()
                    .CountAsync(lc => lc.StudentId == uid && lessons.Contains(lc.LessonId));
            var progress = lessons.Count == 0 ? 0 : Math.Round(completed * 100.0 / lessons.Count, 1);
            var serial = await db.Certificates.AsNoTracking()
                .Where(c => c.StudentId == uid && c.CourseId == e.CourseId)
                .Select(c => (string?)c.SerialCode)
                .FirstOrDefaultAsync();

            result.Add(new EnrollmentDto(e.CourseId, e.Course.Slug, e.Course.Title, progress, e.CompletedAt, serial));
        }
        return result;
    }

    public async Task<CompleteLessonResponse> CompleteLessonAsync(Guid lessonId)
    {
        var uid = RequireUser();
        var lesson = await db.Lessons
            .Include(l => l.Topic).ThenInclude(t => t.Module).ThenInclude(m => m.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId)
            ?? throw new NotFoundException("Lesson not found.");

        var course = lesson.Topic.Module.Course;
        await EnsureEnrolledAsync(uid, course);

        if (!await db.LessonCompletions.AnyAsync(lc => lc.StudentId == uid && lc.LessonId == lesson.Id))
            db.LessonCompletions.Add(new LessonCompletion { StudentId = uid, LessonId = lesson.Id });

        var lessons = await db.Lessons.AsNoTracking()
            .Where(l => l.Topic.Module.CourseId == course.Id)
            .Select(l => l.Id)
            .ToListAsync();
        var completedCount = await db.LessonCompletions.AsNoTracking()
            .CountAsync(lc => lc.StudentId == uid && lessons.Contains(lc.LessonId));
        var progress = lessons.Count == 0 ? 0 : Math.Round(completedCount * 100.0 / lessons.Count, 1);

        string? serial = null;
        var courseCompleted = lessons.Count > 0 && completedCount >= lessons.Count;
        if (courseCompleted)
        {
            var enrollment = await db.Enrollments.FirstAsync(e => e.StudentId == uid && e.CourseId == course.Id);
            enrollment.CompletedAt ??= DateTime.UtcNow;

            var cert = await db.Certificates.FirstOrDefaultAsync(c => c.StudentId == uid && c.CourseId == course.Id);
            if (cert == null)
            {
                cert = new Certificate
                {
                    StudentId = uid,
                    CourseId = course.Id,
                    SerialCode = $"SCH-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}"
                };
                db.Certificates.Add(cert);
            }
            serial = cert.SerialCode;
        }

        await db.SaveChangesAsync();
        return new CompleteLessonResponse(progress, courseCompleted, serial);
    }

    private async Task EnsureEnrolledAsync(Guid uid, Course course)
    {
        if (!await db.Enrollments.AnyAsync(e => e.StudentId == uid && e.CourseId == course.Id))
            throw new PaymentRequiredException(course.Id, course.Price, course.DiscountPercent, course.FinalPrice());
    }

    private Guid RequireUser() =>
        currentUser.UserId ?? throw new ForbiddenException("Please sign in to continue.");
}
