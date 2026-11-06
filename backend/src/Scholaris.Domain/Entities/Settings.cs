namespace Scholaris.Domain.Entities;

/// <summary>Singleton row (Id=1). Admin adjusts the platform income (commission) rate here.</summary>
public class PlatformSetting
{
    public int Id { get; set; } = 1;
    /// <summary>Platform service commission on every sale, percent. Default 22% per business decision.</summary>
    public decimal CommissionRatePercent { get; set; } = 22.00m;
    public string SiteName { get; set; } = "Scholaris";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Singleton row (Id=1). Admin edits certificate wording/design here — the student-facing
/// certificate is rendered from this template with placeholders filled at view time.
/// Supported placeholders: {{StudentName}} {{CourseTitle}} {{Date}} {{Serial}}.
/// </summary>
public class CertificateTemplate
{
    public int Id { get; set; } = 1;
    public string Title { get; set; } = "Certificate of Completion";
    public string Subtitle { get; set; } = "Scholaris Online Academy";
    public string BodyTemplate { get; set; } =
        "This is to certify that\n\n{{StudentName}}\n\nhas successfully completed all topics of the course\n\n{{CourseTitle}}\n\non {{Date}}.";
    public string FooterNote { get; set; } = "Verify authenticity at scholaris.et/verify with serial {{Serial}}.";
    public string SignatoryName { get; set; } = "Dr. Meron Tadesse";
    public string SignatoryTitle { get; set; } = "Academic Director";
    public string AccentHex { get; set; } = "#3F3D8F";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
