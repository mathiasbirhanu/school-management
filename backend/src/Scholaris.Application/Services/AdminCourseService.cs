using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Scholaris.Application.Common;
using Scholaris.Application.Dtos;
using Scholaris.Domain.Entities;
using Scholaris.Domain.Enums;
using Scholaris.Domain.Exceptions;

namespace Scholaris.Application.Services;

/// <summary>
/// Admin course management: admin (never teachers) creates/deletes courses, adjusts price
/// and discount, publishes/archives. Creating a course can include the full nested
/// curriculum (modules → topics → lessons) in one call.
/// </summary>
public partial class AdminCourseService(IAppDbContext db)
{
    public async Task<List<AdminCourseDto>> ListAsync()
    {
        var courses = await db.Courses.AsNoTracking()
            .Include(c => c.Teacher)
            .Include(c => c.Modules).ThenInclude(m => m.Topics).ThenInclude(t => t.Lessons)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        var students = await db.Enrollments.AsNoTracking()
            .GroupBy(e => e.CourseId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        return courses.Select(c => new AdminCourseDto(
            c.Id, c.Slug, c.Title, c.Subject, c.Level, c.Summary,
            c.Price, c.DiscountPercent, c.FinalPrice(), c.Status.ToString(),
            c.TeacherId, c.Teacher.FullName, c.CoverHue,
            c.Modules.SelectMany(m => m.Topics).SelectMany(t => t.Lessons).Count(),
            students.GetValueOrDefault(c.Id), c.CreatedAt)).ToList();
    }

    public async Task<List<TeacherOptionDto>> TeachersAsync() =>
        await db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Teacher)
            .OrderBy(u => u.FullName)
            .Select(u => new TeacherOptionDto(u.Id, u.FullName, u.Email))
            .ToListAsync();

    public async Task<List<AdminCourseDto>> ListByTeacherAsync(Guid teacherId) =>
        (await ListAsync()).Where(c => c.TeacherId == teacherId).ToList();

    public async Task<AdminCourseDto> CreateAsync(AdminCourseCreateRequest req)
    {
        var teacher = await db.Users.FirstOrDefaultAsync(u => u.Id == req.TeacherId && u.Role == UserRole.Teacher)
                      ?? throw new DomainException("Selected teacher account was not found.");
        if (req.Price < 0) throw new DomainException("Price cannot be negative.");
        if (req.DiscountPercent is < 0 or > 100) throw new DomainException("Discount must be between 0 and 100 percent.");

        var course = new Course
        {
            Title = req.Title.Trim(),
            Slug = await UniqueSlugAsync(req.Title),
            Summary = req.Summary?.Trim() ?? "",
            Subject = req.Subject.Trim(),
            Level = req.Level.Trim(),
            Price = req.Price,
            DiscountPercent = req.DiscountPercent,
            CoverHue = req.CoverHue,
            TeacherId = teacher.Id,
            Status = CourseStatus.Draft
        };

        foreach (var m in req.Modules ?? [])
        {
            var module = new Module { Title = m.Title, OrderIndex = m.OrderIndex, Course = course };
            foreach (var t in m.Topics ?? [])
            {
                var topic = new Topic { Title = t.Title, Concepts = t.Concepts ?? "", OrderIndex = t.OrderIndex, Module = module };
                foreach (var l in t.Lessons ?? [])
                {
                    var type = Enum.TryParse<LessonType>(l.Type, ignoreCase: true, out var parsed) ? parsed : LessonType.Note;
                    topic.Lessons.Add(new Lesson
                    {
                        Title = l.Title,
                        Type = type,
                        Content = l.Content ?? "",
                        VideoYoutubeId = type == LessonType.Video ? l.VideoYoutubeId : null,
                        DurationMinutes = l.DurationMinutes,
                        OrderIndex = l.OrderIndex
                    });
                }
                module.Topics.Add(topic);
            }
            course.Modules.Add(module);
        }

        if (course.Modules.SelectMany(m => m.Topics).SelectMany(t => t.Lessons).Any())
            course.Status = CourseStatus.Published; // a course with content goes live; can be re-set to Draft

        db.Courses.Add(course);
        await db.SaveChangesAsync();
        return (await ListAsync()).First(c => c.Id == course.Id);
    }

    public async Task<AdminCourseDto> UpdateAsync(Guid id, AdminCourseUpdateRequest req)
    {
        var course = await db.Courses.Include(c => c.Teacher).FirstOrDefaultAsync(c => c.Id == id)
                     ?? throw new NotFoundException("Course not found.");
        if (!Enum.TryParse<CourseStatus>(req.Status, ignoreCase: true, out var status))
            throw new DomainException("Status must be Draft, Published or Archived.");
        if (req.Price < 0) throw new DomainException("Price cannot be negative.");
        if (req.DiscountPercent is < 0 or > 100) throw new DomainException("Discount must be between 0 and 100 percent.");

        course.Title = req.Title.Trim();
        course.Subject = req.Subject.Trim();
        course.Level = req.Level.Trim();
        course.Summary = req.Summary?.Trim() ?? "";
        course.Price = req.Price;
        course.DiscountPercent = req.DiscountPercent;
        course.Status = status;
        course.TeacherId = req.TeacherId;
        course.CoverHue = req.CoverHue;
        await db.SaveChangesAsync();
        return (await ListAsync()).First(c => c.Id == id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == id)
                     ?? throw new NotFoundException("Course not found.");
        if (await db.Orders.AnyAsync(o => o.CourseId == id))
            throw new DomainException("This course has purchase history. Archive it instead of deleting, so revenue records stay intact.");

        await db.SaveChangesAsync();
        db.Courses.Remove(course);
        await db.SaveChangesAsync();
    }

    private async Task<string> UniqueSlugAsync(string title)
    {
        var baseSlug = SlugRegex().Replace(title.ToLowerInvariant(), "-").Trim('-');
        if (baseSlug.Length > 60) baseSlug = baseSlug[..60];
        if (baseSlug.Length == 0) baseSlug = "course";
        var slug = baseSlug;
        while (await db.Courses.AnyAsync(c => c.Slug == slug))
            slug = $"{baseSlug}-{Guid.NewGuid().ToString("N")[..5]}";
        return slug;
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex SlugRegex();
}
