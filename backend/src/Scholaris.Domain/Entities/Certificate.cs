namespace Scholaris.Domain.Entities;

/// <summary>
/// Issued automatically when a student completes every lesson of a course.
/// Design/wording comes from CertificateTemplate (admin-adjusted); this row only
/// records the fact of completion. AI exam results are never stored anywhere.
/// </summary>
public class Certificate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SerialCode { get; set; } = default!;
    public Guid StudentId { get; set; }
    public User Student { get; set; } = default!;
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = default!;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}
