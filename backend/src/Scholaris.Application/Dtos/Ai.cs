namespace Scholaris.Application.Dtos;

// AI-generated exams (per student / per attempt, never persisted)
public record ExamQuestionDto(Guid Id, string Prompt, int MaxScore);
public record ExamPaperDto(Guid ExamId, Guid TopicId, string TopicTitle, List<ExamQuestionDto> Questions);
public record ExamAnswerDto(Guid QuestionId, string Text);
public record ExamEvaluateRequest(Guid ExamId, List<ExamAnswerDto> Answers);
public record ExamResultDto(Guid QuestionId, string Verdict, int Score, int MaxScore, string Feedback);
public record ExamEvaluationDto(double ScorePercent, string OverallFeedback, List<ExamResultDto> Results);

// AI notes (summary + elaboration, ephemeral)
public record NotesSummarizeRequest(Guid LessonId);
public record LessonSummaryDto(string Summary, List<string> KeyPoints, string Elaborated);
