namespace Scholaris.Domain.Entities;

public class Enrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public User Student { get; set; } = default!;
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = default!;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public ICollection<LessonCompletion> LessonCompletions { get; set; } = new List<LessonCompletion>();
}

public class LessonCompletion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public Guid LessonId { get; set; }
    public Lesson Lesson { get; set; } = default!;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
