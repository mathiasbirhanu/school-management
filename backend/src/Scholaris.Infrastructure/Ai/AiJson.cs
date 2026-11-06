using System.Text.Json;
using System.Text.Json.Serialization;
using Scholaris.Domain.Exceptions;

namespace Scholaris.Infrastructure.Ai;

/// <summary>Extracts strict JSON from LLM output (models love wrapping JSON in prose or ```fences```).</summary>
public static class AiJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public static T Parse<T>(string raw)
    {
        var text = raw.Trim();

        if (text.StartsWith("```"))
        {
            var firstNl = text.IndexOf('\n');
            if (firstNl >= 0) text = text[(firstNl + 1)..];
            var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence >= 0) text = text[..lastFence];
            text = text.Trim();
        }

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new DomainException("The AI returned an unreadable response. Please try again.");

        try
        {
            var parsed = JsonSerializer.Deserialize<T>(text[start..(end + 1)], Options);
            return parsed ?? throw new DomainException("The AI returned an empty response. Please try again.");
        }
        catch (JsonException)
        {
            throw new DomainException("The AI returned an unexpected format. Please try again.");
        }
    }
}
