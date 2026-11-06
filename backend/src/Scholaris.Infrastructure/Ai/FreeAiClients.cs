using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Scholaris.Infrastructure.Ai;

/// <summary>Google Gemini free tier (generativelanguage.googleapis.com).</summary>
public class GeminiChat(HttpClient http, IConfiguration cfg) : IAiChat
{
    private readonly string _key = cfg["Ai:Gemini:ApiKey"] ?? "";
    private readonly string _model = cfg["Ai:Gemini:Model"] ?? "gemini-1.5-flash";

    public async Task<string> ChatAsync(string system, string user, CancellationToken ct = default)
    {
        var body = new
        {
            system_instruction = new { parts = new[] { new { text = system } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = user } } } },
            generationConfig = new { temperature = 0.9, maxOutputTokens = 2048 }
        };
        using var resp = await http.PostAsJsonAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_key}", body, ct);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content").GetProperty("parts")[0]
            .GetProperty("text").GetString() ?? "";
    }
}

/// <summary>Groq free tier (OpenAI-compatible endpoint, e.g. llama-3.1-8b-instant).</summary>
public class GroqChat(HttpClient http, IConfiguration cfg) : IAiChat
{
    private readonly string _key = cfg["Ai:Groq:ApiKey"] ?? "";
    private readonly string _model = cfg["Ai:Groq:Model"] ?? "llama-3.1-8b-instant";

    public async Task<string> ChatAsync(string system, string user, CancellationToken ct = default)
    {
        var body = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            },
            temperature = 0.9,
            max_tokens = 2048
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _key);
        using var resp = await http.SendAsync(request, ct);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}

/// <summary>Locally hosted Ollama — completely free, no key, for on-premise deployments.</summary>
public class OllamaChat(HttpClient http, IConfiguration cfg) : IAiChat
{
    private readonly string _baseUrl = (cfg["Ai:Ollama:BaseUrl"] ?? "http://localhost:11434").TrimEnd('/');
    private readonly string _model = cfg["Ai:Ollama:Model"] ?? "llama3.1";

    public async Task<string> ChatAsync(string system, string user, CancellationToken ct = default)
    {
        var body = new
        {
            model = _model,
            stream = false,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            }
        };
        using var resp = await http.PostAsJsonAsync($"{_baseUrl}/api/chat", body, ct);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}
