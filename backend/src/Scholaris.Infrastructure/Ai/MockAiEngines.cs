using System.Text.RegularExpressions;
using Scholaris.Application.Interfaces;

namespace Scholaris.Infrastructure.Ai;

/// <summary>
/// Deterministic offline stand-in used when Ai:Provider=Mock (the default). It produces
/// concept-anchored questions, plausible grading and useful summaries with NO network and
/// NO API key, so the whole product is demoable before any provider key is configured.
/// </summary>
public partial class MockAiExamEngine : IAiExamService
{
    public Task<ExamPaper> GenerateAsync(ExamGenerationContext ctx, CancellationToken ct = default)
    {
        var concepts = SplitConcepts(ctx.Concepts);
        if (concepts.Count == 0) concepts.Add(ctx.TopicTitle);

        var templates = new (string Template, string Rubric)[]
        {
            ("In your own words, explain \u201c{0}\u201d and why it matters in {1}. Give one concrete example.",
                "Names the key ideas of the concept, uses correct terminology, includes a concrete example."),
            ("A classmate states a common misconception about \u201c{0}\u201d. State that misconception and correct it clearly.",
                "Identifies a plausible misconception and corrects it with accurate reasoning."),
            ("Describe, step by step, how you would apply \u201c{0}\u201d to solve a typical problem from {1}.",
                "Shows ordered, logical steps that connect the concept to the procedure."),
            ("Compare \u201c{0}\u201d with another key idea from {1}: give one similarity and one important difference.",
                "Gives a valid similarity and a meaningful difference using course vocabulary."),
            ("Pose one everyday-life question that uses \u201c{0}\u201d in the context of {1}, then answer it. (Variation: {2})",
                "Creates a sensible real-life scenario and answers it correctly using the concept.")
        };

        var questions = new List<ExamQuestion>();
        var seedTail = ctx.VariationSeed.Length >= 6 ? ctx.VariationSeed[^6..] : "000000";
        for (var i = 0; i < 5; i++)
        {
            var concept = concepts[i % concepts.Count];
            var (template, _) = templates[i % templates.Length];
            questions.Add(new ExamQuestion(
                Guid.NewGuid(),
                string.Format(template, concept, ctx.TopicTitle, seedTail),
                10,
                $"A strong answer references \u201c{concept}\u201d, uses correct {ctx.Subject} terminology and reasons clearly."));
        }

        return Task.FromResult(new ExamPaper(Guid.NewGuid(), Guid.Empty, ctx.TopicTitle, questions));
    }

    public Task<ExamEvaluation> EvaluateAsync(ExamPaper paper, IReadOnlyList<ExamAnswer> answers, CancellationToken ct = default)
    {
        var answerByText = answers
            .Where(a => !string.IsNullOrWhiteSpace(a.Text))
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => string.Join(" ", g.Select(a => a.Text)));

        var results = new List<ExamQuestionResult>();
        foreach (var q in paper.Questions)
        {
            var answer = answerByText.GetValueOrDefault(q.Id, "");
            var keywords = Keywords(q.Prompt + " " + q.Rubric).Take(8).ToList();

            int score;
            string feedback;
            if (answer.Trim().Length < 3)
            {
                score = 0;
                feedback = "No meaningful answer was given. Re-read the lesson notes for this concept, then retry — a fresh AI paper will be generated.";
            }
            else
            {
                var baseScore = answer.Length switch
                {
                    < 40 => 2,
                    < 120 => 4,
                    < 260 => 6,
                    _ => 7
                };
                var hits = keywords.Count(k => answer.Contains(k, StringComparison.OrdinalIgnoreCase));
                score = Math.Min(q.MaxScore, baseScore + Math.Min(hits, 3));
                var keyword = keywords.FirstOrDefault();
                feedback = hits > 0
                    ? $"You connected your answer to {hits} key idea(s){(keyword != null ? $" (e.g. \u201c{keyword}\u201d)" : "")}. {HintFor(score, q.MaxScore)}"
                    : $"Your answer does not yet reference the key concepts. {HintFor(score, q.MaxScore)}";
            }

            var verdict = score >= q.MaxScore * 0.7 ? "correct" : score >= q.MaxScore * 0.4 ? "partial" : "incorrect";
            results.Add(new ExamQuestionResult(q.Id, verdict, score, q.MaxScore, feedback));
        }

        var totalMax = results.Sum(r => r.MaxScore);
        var totalScore = results.Sum(r => r.Score);
        var percent = totalMax == 0 ? 0 : Math.Round(totalScore * 100.0 / totalMax, 1);
        var overall = $"You scored {percent:0.#}% on this AI-graded attempt. " +
                      (percent >= 70
                          ? "Strong work — you may move on to the next topic."
                          : "Review the highlighted concepts and generate a new attempt; every attempt is fresh.");

        return Task.FromResult(new ExamEvaluation(percent, overall, results));
    }

    private static string HintFor(int score, int max) => score >= max * 0.7
        ? "Well done — clear and on point."
        : score >= max * 0.4
            ? "On the right track, but key parts are missing or imprecise."
            : "Not there yet — study the concept and try a new attempt.";

    private static List<string> SplitConcepts(string raw) =>
        raw.Split([',', ';', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
           .Where(c => c.Length > 2)
           .Distinct(StringComparer.OrdinalIgnoreCase)
           .ToList();

    private static IEnumerable<string> Keywords(string text) =>
        WordRegex().Matches(text)
            .Select(m => m.Value)
            .Where(w => w.Length > 6)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex(@"[A-Za-z]{7,}")]
    private static partial Regex WordRegex();
}

public partial class MockAiNotesEngine : IAiNotesService
{
    public Task<LessonSummary> SummarizeAsync(LessonNotesContext ctx, CancellationToken ct = default)
    {
        var sentences = SentenceRegex().Split(ctx.NotesContent)
            .Select(s => s.Trim())
            .Where(s => s.Length > 15)
            .Take(6)
            .ToList();

        if (sentences.Count == 0)
            sentences.Add($"This lesson introduces the core ideas of {ctx.LessonTitle}.");

        var summary = $"This lesson on {ctx.LessonTitle} ({ctx.CourseTitle}) walks through {sentences.Count} main points. " +
                      string.Join(" ", sentences.Take(2));
        if (summary.Length > 320) summary = summary[..317] + "...";

        var keyPoints = sentences
            .Select((s, i) => $"{i + 1}. {s}")
            .Select(s => s.Length > 110 ? s[..107] + "..." : s)
            .ToList();

        var elaborated = "Let's unpack the teacher's notes step by step.\n\n" +
                         string.Join("\n\n", sentences.Select(s => "\u2022 " + s)) +
                         "\n\nIn plain words: read each point, then say it back in your own words — if you can explain it simply, you understand it. " +
                         "When ready, take the AI practice test for this topic; every attempt generates fresh questions.";

        return Task.FromResult(new LessonSummary(summary, keyPoints, elaborated));
    }

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceRegex();
}
