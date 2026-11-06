using Scholaris.Domain.Enums;

namespace Scholaris.Domain.Entities;

/// <summary>
/// A purchasable course. Price AND discount are administered by the Admin
/// (never by teachers) — see FR-PAY-03/04 in the SRS addendum.
/// DiscountPercent 0..100; FinalPrice is computed = Price * (1 - DiscountPercent/100).
/// </summary>
public class Course
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string Summary { get; set; } = "";
    public string Subject { get; set; } = default!;
    /// <summary>Level band, e.g. "Grade 8", "Grade 12", "College Year 1", "University Year 2".</summary>
    public string Level { get; set; } = default!;
    /// <summary>ETB list price before discount.</summary>
    public decimal Price { get; set; }
    /// <summary>Admin-adjusted discount, percent (0-100). Shown as a tag and in the discount balloon.</summary>
    public decimal DiscountPercent { get; set; }
    /// <summary>Hue used to generate the SVG cover art (course card).</summary>
    public int CoverHue { get; set; } = 258;
    public CourseStatus Status { get; set; } = CourseStatus.Draft;
    public Guid TeacherId { get; set; }
    public User Teacher { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Module> Modules { get; set; } = new List<Module>();

    public decimal FinalPrice() => Math.Round(Price * (1 - DiscountPercent / 100m), 2, MidpointRounding.AwayFromZero);
}

public class Module
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = default!;
    public string Title { get; set; } = default!;
    public int OrderIndex { get; set; }

    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
}

/// <summary>A topic anchors the AI-generated exam: its Concepts feed the generator prompt.</summary>
public class Topic
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ModuleId { get; set; }
    public Module Module { get; set; } = default!;
    public string Title { get; set; } = default!;
    /// <summary>Key concepts this topic teaches (newline/comma separated). Anchors AI exam generation.</summary>
    public string Concepts { get; set; } = "";
    public int OrderIndex { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}

public class Lesson
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TopicId { get; set; }
    public Topic Topic { get; set; } = default!;
    public string Title { get; set; } = default!;
    public LessonType Type { get; set; } = LessonType.Note;
    /// <summary>Teacher's note content (markdown). Also the source material for AI summaries/exams.</summary>
    public string Content { get; set; } = "";
    /// <summary>Unlisted YouTube video id, always played inside Scholaris' branded player.</summary>
    public string? VideoYoutubeId { get; set; }
    public int DurationMinutes { get; set; }
    public int OrderIndex { get; set; }
}
