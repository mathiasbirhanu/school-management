using System.Text.Json;
using Scholaris.Application.Interfaces;
using Scholaris.Domain.Exceptions;

namespace Scholaris.Infrastructure.Ai;

/// <summary>
/// AI exam engine on top of a real model. Every generation is seeded with student + timestamp
/// variation so the paper differs per student and per attempt, while every question stays
/// anchored on the topic's declared key concepts.
/// </summary>
public class AiExamEngine(IAiChat chat) : IAiExamService
{
    private const int QuestionCount = 5;

    public async Task<ExamPaper> GenerateAsync(ExamGenerationContext ctx, CancellationToken ct = default)
    {
        const string system = "You are an experienced exam writer. Output ONLY strict JSON with no commentary and no markdown fences.";

        var user = $$"""
            Create {{QuestionCount}} short-answer exam questions for a {{ctx.Level}} {{ctx.Subject}} topic.

            Topic: {{ctx.TopicTitle}}
            Key concepts the questions MUST test: {{ctx.Concepts}}
            Lesson notes (may be truncated): {{ctx.NotesExcerpt}}

            Rules:
            - Every question must be answerable in 2-6 sentences by one student.
            - Vary the angle: explain, apply, compare, spot-the-mistake, real-world example.
            - Each question is worth 10 points.
            - Randomize wording and angle with this seed so no two students see the same paper: {{ctx.VariationSeed}}

            Return JSON exactly shaped as:
            {"questions":[{"prompt":"...","rubric":"what a good answer must mention","maxScore":10}]}
            """;

        var parsed = AiJson.Parse<GeneratedExam>(await chat.ChatAsync(system, user, ct));
        var questions = (parsed.Questions ?? [])
            .Where(q => !string.IsNullOrWhiteSpace(q.Prompt))
            .Take(QuestionCount)
            .Select(q => new ExamQuestion(
                Guid.NewGuid(),
                q.Prompt!.Trim(),
                q.MaxScore is > 0 and <= 100 ? q.MaxScore.Value : 10,
                string.IsNullOrWhiteSpace(q.Rubric)
                    ? "A strong answer names the key concepts, uses correct terminology and reasons clearly."
                    : q.Rubric!.Trim()))
            .ToList();

        if (questions.Count == 0)
            throw new DomainException("The AI did not return any questions. Please try again.");

        return new ExamPaper(Guid.NewGuid(), Guid.Empty, ctx.TopicTitle, questions);
    }

    public async Task<ExamEvaluation> EvaluateAsync(ExamPaper paper, IReadOnlyList<ExamAnswer> answers, CancellationToken ct = default)
    {
        // Number questions q1..qn for the model, then map results back by index — models
        // cannot reliably echo Guids.
        var answerByText = answers
            .Where(a => !string.IsNullOrWhiteSpace(a.Text))
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => string.Join(" ", g.Select(a => a.Text)));

        var payload = new
        {
            questions = paper.Questions.Select((q, i) => new
            {
                id = i + 1,
                prompt = q.Prompt,
                rubric = q.Rubric,
                maxScore = q.MaxScore,
                studentAnswer = answerByText.GetValueOrDefault(q.Id, "")
            })
        };

        const string system = "You are a fair but rigorous grader for school exams. Output ONLY strict JSON.";
        var user = $$"""
            Grade each student answer against its rubric. Give actionable feedback (1-2 sentences each).

            {{JsonSerializer.Serialize(payload)}}

            Return JSON exactly shaped as:
            {"results":[{"id":1,"verdict":"correct|partial|incorrect","score":0,"feedback":"..."}],"overallFeedback":"2-3 sentences of encouragement plus what to review"}
            """;

        var parsed = AiJson.Parse<GradedExam>(await chat.ChatAsync(system, user, ct));
        var graded = parsed.Results ?? [];

        var results = new List<ExamQuestionResult>();
        for (var i = 0; i < paper.Questions.Count; i++)
        {
            var q = paper.Questions[i];
            var g = graded.FirstOrDefault(r => r.Id == i + 1);
            var score = Math.Clamp(g?.Score ?? 0, 0, q.MaxScore);
            var verdict = NormalizeVerdict(g?.Verdict) ?? VerdictFromScore(score, q.MaxScore);
            var feedback = string.IsNullOrWhiteSpace(g?.Feedback)
                ? "No detailed feedback was returned; compare your answer with the lesson notes."
                : g!.Feedback!.Trim();
            results.Add(new ExamQuestionResult(q.Id, verdict, score, q.MaxScore, feedback));
        }

        var totalMax = results.Sum(r => r.MaxScore);
        var totalScore = results.Sum(r => r.Score);
        var percent = totalMax == 0 ? 0 : Math.Round(totalScore * 100.0 / totalMax, 1);
        var overall = string.IsNullOrWhiteSpace(parsed.OverallFeedback)
            ? $"You scored {percent:0.#}% on this attempt."
            : parsed.OverallFeedback!.Trim();

        return new ExamEvaluation(percent, overall, results);
    }

    private static string VerdictFromScore(int score, int max) => score switch
    {
        _ when max > 0 && score >= max * 0.7 => "correct",
        _ when max > 0 && score >= max * 0.4 => "partial",
        _ => "incorrect"
    };

    private static string? NormalizeVerdict(string? v) =>
        string.IsNullOrWhiteSpace(v) ? null : v.Trim().ToLowerInvariant() switch
        {
            "correct" => "correct",
            "partial" => "partial",
            "incorrect" => "incorrect",
            _ => null
        };

    private sealed record GeneratedExam(List<GeneratedQuestion>? Questions);
    private sealed record GeneratedQuestion(string? Prompt, string? Rubric, int? MaxScore);
    private sealed record GradedExam(List<GradedResult>? Results, string? OverallFeedback);
    private sealed record GradedResult(int Id, string? Verdict, int Score, string? Feedback);
}

/// <summary>AI study-notes engine on top of a real model (summary + key points + elaboration).</summary>
public class AiNotesEngine(IAiChat chat) : IAiNotesService
{
    public async Task<LessonSummary> SummarizeAsync(LessonNotesContext ctx, CancellationToken ct = default)
    {
        const string system = "You are a friendly tutor for Ethiopian students. Output ONLY strict JSON, no commentary.";

        var user = $$"""
            Course: {{ctx.CourseTitle}}
            Lesson: {{ctx.LessonTitle}}
            Teacher's notes: {{ctx.NotesContent}}

            Produce a study aid that stays faithful to the teacher's notes:
            - "summary": 3-5 sentences in plain language.
            - "keyPoints": 4-6 short bullet strings.
            - "elaborated": a clearer, expanded explanation (6-10 sentences) with one worked intuition or everyday example per main idea.

            Return JSON exactly shaped as:
            {"summary":"...","keyPoints":["..."],"elaborated":"..."}
            """;

        var parsed = AiJson.Parse<NotesResult>(await chat.ChatAsync(system, user, ct));
        if (string.IsNullOrWhiteSpace(parsed.Summary) || string.IsNullOrWhiteSpace(parsed.Elaborated))
            throw new DomainException("The AI could not summarize these notes. Please try again.");

        var keyPoints = (parsed.KeyPoints ?? [])
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Take(8)
            .ToList();

        return new LessonSummary(parsed.Summary.Trim(), keyPoints, parsed.Elaborated.Trim());
    }

    private sealed record NotesResult(string? Summary, List<string>? KeyPoints, string? Elaborated);
}
