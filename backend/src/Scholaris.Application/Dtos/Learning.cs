namespace Scholaris.Application.Dtos;

public record LearnLessonDto(Guid Id, string Title, string Type, int DurationMinutes, int OrderIndex, string? Content, string? VideoYoutubeId);
public record LearnTopicDto(Guid Id, string Title, int OrderIndex, List<LearnLessonDto> Lessons);
public record LearnModuleDto(Guid Id, string Title, int OrderIndex, List<LearnTopicDto> Topics);

public record LearnCourseDto(
    Guid CourseId, string Title, string Subject, string Level, string TeacherName,
    List<LearnModuleDto> Modules, List<Guid> CompletedLessonIds, double ProgressPercent, string? CertificateSerial);

public record CompleteLessonResponse(double ProgressPercent, bool CourseCompleted, string? CertificateSerial);

public record EnrollmentDto(Guid CourseId, string Slug, string Title, double ProgressPercent, DateTime? CompletedAt, string? CertificateSerial);
