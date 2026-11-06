namespace Scholaris.Application.Dtos;

public record CourseListItem(
    Guid Id, string Slug, string Title, string Summary, string Subject, string Level,
    string TeacherName, decimal Price, decimal DiscountPercent, decimal FinalPrice,
    int CoverHue, int LessonCount, int StudentCount);

public record LessonDto(Guid Id, string Title, string Type, int DurationMinutes, int OrderIndex);
public record TopicDto(Guid Id, string Title, string Concepts, int OrderIndex, List<LessonDto> Lessons);
public record ModuleDto(Guid Id, string Title, int OrderIndex, List<TopicDto> Topics);

public record CourseDetailDto(
    Guid Id, string Slug, string Title, string Summary, string Subject, string Level,
    string TeacherName, decimal Price, decimal DiscountPercent, decimal FinalPrice,
    int CoverHue, int LessonCount, int StudentCount, bool Enrolled, List<ModuleDto> Modules);
