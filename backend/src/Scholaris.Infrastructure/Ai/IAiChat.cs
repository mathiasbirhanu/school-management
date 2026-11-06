namespace Scholaris.Infrastructure.Ai;

/// <summary>Minimal chat abstraction shared by every free-tier AI backend.</summary>
public interface IAiChat
{
    Task<string> ChatAsync(string system, string user, CancellationToken ct = default);
}
