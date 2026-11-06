namespace Scholaris.Application.Interfaces;

// ────────────────────────── AI-GENERATED EXAMS (ephemeral by design) ──────────────────────────
// Per product decision: exams are generated per student and per attempt from the topic's
// concepts, evaluated by AI, and NEVER persisted. The paper lives only in an in-memory
// session store with a TTL and is destroyed right after evaluation (ADR-012).

public record ExamGenerationContext(
    string TopicTitle,
    string Concepts,
    string NotesExcerpt,
    string Subject,
    string Level,
    string VariationSeed);

public record ExamQuestion(Guid Id, string Prompt, int MaxScore, string Rubric);

public record ExamPaper(Guid ExamId, Guid TopicId, string TopicTitle, List<ExamQuestion> Questions);

public record ExamAnswer(Guid QuestionId, string Text);

public record ExamQuestionResult(Guid QuestionId, string Verdict, int Score, int MaxScore, string Feedback);

public record ExamEvaluation(double ScorePercent, string OverallFeedback, List<ExamQuestionResult> Results);

public interface IAiExamService
{
    Task<ExamPaper> GenerateAsync(ExamGenerationContext ctx, CancellationToken ct = default);
    Task<ExamEvaluation> EvaluateAsync(ExamPaper paper, IReadOnlyList<ExamAnswer> answers, CancellationToken ct = default);
}

/// <summary>Ephemeral, in-memory holder for AI exam papers. Implementations must not touch the database.</summary>
public interface IExamSessionStore
{
    void Save(ExamPaper paper, TimeSpan ttl);
    /// <summary>Returns the paper and removes it (one-shot evaluation).</summary>
    ExamPaper? Take(Guid examId);
}

// ────────────────────────── AI NOTES (summary / elaboration) ──────────────────────────

public record LessonNotesContext(string CourseTitle, string LessonTitle, string NotesContent);

public record LessonSummary(string Summary, List<string> KeyPoints, string Elaborated);

public interface IAiNotesService
{
    Task<LessonSummary> SummarizeAsync(LessonNotesContext ctx, CancellationToken ct = default);
}
