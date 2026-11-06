namespace Scholaris.Application.Dtos;

public record QaCreateRequest(Guid CourseId, Guid? LessonId, string Question);
public record QaThreadDto(Guid Id, Guid CourseId, string CourseTitle, Guid? LessonId, string Question, string Status, DateTime CreatedAt, string? Answer, DateTime? AnsweredAt);
public record QaAnswerRequest(string Answer);
