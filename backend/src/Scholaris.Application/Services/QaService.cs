using Microsoft.EntityFrameworkCore;
using Scholaris.Application.Common;
using Scholaris.Application.Dtos;
using Scholaris.Domain.Entities;
using Scholaris.Domain.Enums;
using Scholaris.Domain.Exceptions;

namespace Scholaris.Application.Services;

/// <summary>Teacher Q&A: students ask; the course's teacher answers. (AI answers come separately and are ephemeral.)</summary>
public class QaService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<QaThreadDto> CreateAsync(QaCreateRequest req)
    {
        var uid = RequireUser();
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == req.CourseId)
                     ?? throw new NotFoundException("Course not found.");
        if (!await db.Enrollments.AnyAsync(e => e.StudentId == uid && e.CourseId == course.Id))
            throw new ForbiddenException("Enroll in the course to ask the teacher a question.");
        if (string.IsNullOrWhiteSpace(req.Question))
            throw new DomainException("Please type your question.");

        var thread = new QaThread
        {
            CourseId = course.Id,
            LessonId = req.LessonId,
            StudentId = uid,
            Question = req.Question.Trim()
        };
        db.QaThreads.Add(thread);
        await db.SaveChangesAsync();
        return await MapAsync(thread.Id) ?? throw new DomainException("Could not create the question.");
    }

    public async Task<List<QaThreadDto>> ListMineAsync()
    {
        var uid = RequireUser();
        var threads = await db.QaThreads.AsNoTracking()
            .Include(t => t.Course)
            .Where(t => t.StudentId == uid)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        return threads.Select(Map).ToList();
    }

    public async Task<List<QaThreadDto>> ListByCourseAsync(Guid courseId)
    {
        var uid = RequireUser();
        var isTeacher = await db.Courses.AnyAsync(c => c.Id == courseId && c.TeacherId == uid);
        if (!isTeacher && !currentUser.IsInRole("Admin"))
            throw new ForbiddenException("Only the course teacher can view incoming questions.");

        var threads = await db.QaThreads.AsNoTracking()
            .Include(t => t.Course).Include(t => t.Student)
            .Where(t => t.CourseId == courseId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        return threads.Select(t => MapWithStudent(t)).ToList();
    }

    public async Task<QaThreadDto> AnswerAsync(Guid threadId, QaAnswerRequest req)
    {
        var uid = RequireUser();
        var thread = await db.QaThreads.Include(t => t.Course).FirstOrDefaultAsync(t => t.Id == threadId)
                     ?? throw new NotFoundException("Question not found.");
        var owns = await db.Courses.AnyAsync(c => c.Id == thread.CourseId && c.TeacherId == uid);
        if (!owns && !currentUser.IsInRole("Admin"))
            throw new ForbiddenException("Only the course teacher can answer this question.");
        if (string.IsNullOrWhiteSpace(req.Answer))
            throw new DomainException("Answer cannot be empty.");

        thread.Answer = req.Answer.Trim();
        thread.AnsweredByTeacherId = uid;
        thread.AnsweredAt = DateTime.UtcNow;
        thread.Status = QaStatus.Answered;
        await db.SaveChangesAsync();
        return await MapAsync(thread.Id) ?? throw new DomainException("Could not load the answer.");
    }

    private async Task<QaThreadDto?> MapAsync(Guid id) =>
        MapWithStudent(await db.QaThreads.AsNoTracking()
            .Include(t => t.Course).Include(t => t.Student)
            .FirstOrDefaultAsync(t => t.Id == id));

    private static QaThreadDto Map(QaThread t) => new(
        t.Id, t.CourseId, t.Course.Title, t.LessonId, t.Question, t.Status.ToString(), t.CreatedAt, t.Answer, t.AnsweredAt);

    private static QaThreadDto MapWithStudent(QaThread? t) =>
        t == null
            ? null!
            : new QaThreadDto(t.Id, t.CourseId, t.Course.Title, t.LessonId, t.Question, t.Status.ToString(), t.CreatedAt, t.Answer, t.AnsweredAt);

    private Guid RequireUser() =>
        currentUser.UserId ?? throw new ForbiddenException("Please sign in to continue.");
}
