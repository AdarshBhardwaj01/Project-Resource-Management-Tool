using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PRM.Business.Services.Ai;

internal sealed class GeminiLlmClient : ILlmClient
{
    private const string ModelName = "gemini-2.0-flash";
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GeminiLlmClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<string?> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var requestUri =
            $"v1beta/models/{ModelName}:generateContent?key={Uri.EscapeDataString(_apiKey)}";

        using var response = await _httpClient.PostAsJsonAsync(
            requestUri,
            new GeminiGenerateContentRequest(prompt),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(
            cancellationToken: cancellationToken);

        return payload?.Candidates?
            .FirstOrDefault()?
            .Content?
            .Parts?
            .FirstOrDefault()?
            .Text?
            .Trim();
    }

    private sealed class GeminiGenerateContentRequest
    {
        public GeminiGenerateContentRequest(string prompt)
        {
            Contents =
            [
                new GeminiContent
                {
                    Parts = [new GeminiPart { Text = prompt }]
                }
            ];
            GenerationConfig = new GeminiGenerationConfig();
        }

        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; }

        [JsonPropertyName("generationConfig")]
        public GeminiGenerationConfig GenerationConfig { get; set; }
    }

    private sealed class GeminiGenerationConfig
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.2;

        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; } = 1024;
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private sealed class GeminiGenerateContentResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }
}
