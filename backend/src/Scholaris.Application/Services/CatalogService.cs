using Microsoft.EntityFrameworkCore;
using Scholaris.Application.Common;
using Scholaris.Application.Dtos;
using Scholaris.Domain.Entities;
using Scholaris.Domain.Enums;
using Scholaris.Domain.Exceptions;

namespace Scholaris.Application.Services;

public class CatalogService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<List<CourseListItem>> GetPublishedAsync()
    {
        var courses = await db.Courses.AsNoTracking()
            .Include(c => c.Teacher)
            .Include(c => c.Modules).ThenInclude(m => m.Topics).ThenInclude(t => t.Lessons)
            .Where(c => c.Status == CourseStatus.Published)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
        var students = await StudentCountsAsync();
        return courses.Select(c => MapItem(c, students.GetValueOrDefault(c.Id))).ToList();
    }

    /// <summary>Courses with an admin-set discount — powers the balloon announcement.</summary>
    public async Task<List<CourseListItem>> GetDiscountedAsync()
    {
        var all = await GetPublishedAsync();
        return all.Where(c => c.DiscountPercent > 0).ToList();
    }

    public async Task<CourseDetailDto> GetDetailAsync(string slug)
    {
        var course = await db.Courses.AsNoTracking()
            .Include(c => c.Teacher)
            .Include(c => c.Modules).ThenInclude(m => m.Topics).ThenInclude(t => t.Lessons)
            .FirstOrDefaultAsync(c => c.Slug == slug)
            ?? throw new NotFoundException("Course not found.");

        var students = await StudentCountsAsync();
        var uid = currentUser.UserId;
        var enrolled = uid != null && await db.Enrollments.AnyAsync(e => e.StudentId == uid && e.CourseId == course.Id);

        return new CourseDetailDto(
            course.Id, course.Slug, course.Title, course.Summary, course.Subject, course.Level,
            course.Teacher.FullName, course.Price, course.DiscountPercent, course.FinalPrice(),
            course.CoverHue, course.Modules.SelectMany(m => m.Topics).SelectMany(t => t.Lessons).Count(),
            students.GetValueOrDefault(course.Id), enrolled,
            course.Modules.OrderBy(m => m.OrderIndex).Select(m => new ModuleDto(
                m.Id, m.Title, m.OrderIndex,
                m.Topics.OrderBy(t => t.OrderIndex).Select(t => new TopicDto(
                    t.Id, t.Title, t.Concepts, t.OrderIndex,
                    t.Lessons.OrderBy(l => l.OrderIndex).Select(l => new LessonDto(
                        l.Id, l.Title, l.Type.ToString(), l.DurationMinutes, l.OrderIndex)).ToList())).ToList())).ToList());
    }

    private static CourseListItem MapItem(Course c, int students) => new(
        c.Id, c.Slug, c.Title, c.Summary, c.Subject, c.Level,
        c.Teacher.FullName, c.Price, c.DiscountPercent, c.FinalPrice(),
        c.CoverHue, c.Modules.SelectMany(m => m.Topics).SelectMany(t => t.Lessons).Count(), students);

    private async Task<Dictionary<Guid, int>> StudentCountsAsync() =>
        await db.Enrollments.AsNoTracking()
            .GroupBy(e => e.CourseId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
}
