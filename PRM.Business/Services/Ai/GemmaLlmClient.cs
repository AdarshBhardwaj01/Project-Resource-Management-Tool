using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PRM.Business.Services.Ai;

internal sealed class GemmaLlmClient : ILlmClient
{
    private const string ModelName = "gemma3:12b-it-q8_0";
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GemmaLlmClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<string?> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/generate");
        request.Headers.Add("apikey", _apiKey);
        request.Content = JsonContent.Create(new GemmaGenerateRequest
        {
            Model = ModelName,
            Prompt = prompt,
            Stream = false
        });
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        var payload = await response.Content.ReadFromJsonAsync<GemmaGenerateResponse>(
            cancellationToken: cancellationToken);
        return payload?.Response?.Trim();
    }

    private sealed class GemmaGenerateRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private sealed class GemmaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }
}
