namespace Scholaris.Application.Dtos;

// ── Admin: platform settings (commission / income rate) ──
public record PlatformSettingsDto(decimal CommissionRatePercent, string SiteName, DateTime UpdatedAt);
public record UpdateSettingsRequest(decimal CommissionRatePercent, string SiteName);

// ── Admin: course management (create nested, edit price/discount, delete) ──
public record AdminLessonCreate(string Title, string Type, string? Content, string? VideoYoutubeId, int DurationMinutes, int OrderIndex);
public record AdminTopicCreate(string Title, string Concepts, int OrderIndex, List<AdminLessonCreate>? Lessons);
public record AdminModuleCreate(string Title, int OrderIndex, List<AdminTopicCreate>? Topics);
public record AdminCourseCreateRequest(
    string Title, string Subject, string Level, string Summary,
    decimal Price, decimal DiscountPercent, Guid TeacherId, int CoverHue,
    List<AdminModuleCreate>? Modules);

public record AdminCourseUpdateRequest(
    string Title, string Subject, string Level, string Summary,
    decimal Price, decimal DiscountPercent, string Status, Guid TeacherId, int CoverHue);

public record AdminCourseDto(
    Guid Id, string Slug, string Title, string Subject, string Level, string Summary,
    decimal Price, decimal DiscountPercent, decimal FinalPrice, string Status,
    Guid TeacherId, string TeacherName, int CoverHue, int LessonCount, int StudentCount, DateTime CreatedAt);

public record TeacherOptionDto(Guid Id, string FullName, string Email);

// ── Admin: certificate template ──
public record CertificateTemplateDto(
    string Title, string Subtitle, string BodyTemplate, string FooterNote,
    string SignatoryName, string SignatoryTitle, string AccentHex);

// ── Revenue reporting (admin sees all teachers, teacher sees own) ──
public record RevenueRowDto(string TeacherName, string Month, decimal Gross, decimal Commission, decimal Net, int Orders);

// ── Certificate rendering ──
public record CertificateViewDto(string Serial, string StudentName, string CourseTitle, DateTime IssuedAt, CertificateTemplateDto Template);
