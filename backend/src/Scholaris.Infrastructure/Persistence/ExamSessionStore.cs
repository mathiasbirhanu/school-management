using Microsoft.Extensions.Caching.Memory;
using Scholaris.Application.Interfaces;

namespace Scholaris.Infrastructure.Persistence;

/// <summary>
/// Ephemeral in-memory holder for AI exam papers. Deliberately NOT a database table:
/// per product decision exams vary per student and per attempt and are never persisted.
/// Papers self-expire (90 min) and are destroyed right after evaluation (one-shot Take).
/// </summary>
public class ExamSessionStore(IMemoryCache cache) : IExamSessionStore
{
    public void Save(ExamPaper paper, TimeSpan ttl) =>
        cache.Set(Key(paper.ExamId), paper, ttl);

    public ExamPaper? Take(Guid examId)
    {
        if (!cache.TryGetValue(Key(examId), out ExamPaper? paper)) return null;
        cache.Remove(Key(examId));
        return paper;
    }

    private static string Key(Guid id) => $"ai-exam:{id}";
}
