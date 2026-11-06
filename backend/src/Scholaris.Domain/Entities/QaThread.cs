using Scholaris.Domain.Enums;

namespace Scholaris.Domain.Entities;

/// <summary>Student question routed to the teacher of the course (AI answers separately, ephemerally).</summary>
public class QaThread
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = default!;
    public Guid? LessonId { get; set; }
    public Guid StudentId { get; set; }
    public User Student { get; set; } = default!;
    public string Question { get; set; } = default!;
    public string? Answer { get; set; }
    public Guid? AnsweredByTeacherId { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public QaStatus Status { get; set; } = QaStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
