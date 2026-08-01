using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CivilQc.Ai;

/// <summary>
/// Generic OpenAI-compatible chat completions client.
/// Works with OpenAI, OpenRouter, Ollama, or any compatible endpoint.
/// </summary>
public class OpenAiClient
{
    private readonly AiConfig _config;
    private readonly HttpClient _http;

    public OpenAiClient(AiConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
    }

    /// <summary>
    /// Send a chat completion request and return the assistant's text response.
    /// </summary>
    public async Task<string> ChatAsync(string systemPrompt, string userMessage)
    {
        var url = $"{_config.ApiBase.TrimEnd('/')}/chat/completions";

        var messages = new[]
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userMessage }
        };

        var payload = new
        {
            model = _config.Model,
            messages,
            temperature = 0.2
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);

        var response = await _http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new AiApiException(
                $"AI API returned {(int)response.StatusCode}: {TruncateForDisplay(body)}");
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            throw new AiApiException(
                $"Unexpected API response format. Could not parse response as JSON. " +
                $"Response starts with: {TruncateForDisplay(body, 200)}", ex);
        }
        var root = doc.RootElement;

        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            throw new AiApiException("AI API returned no choices.");

        var content = choices[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? string.Empty;
    }

    private static string TruncateForDisplay(string text, int maxLen = 500)
    {
        if (text.Length <= maxLen)
            return text;
        return text[..maxLen] + "...";
    }
}

/// <summary>
/// Thrown when the AI API returns an error.
/// </summary>
public class AiApiException : Exception
{
    public AiApiException(string message) : base(message) { }
    public AiApiException(string message, Exception inner) : base(message, inner) { }
}
