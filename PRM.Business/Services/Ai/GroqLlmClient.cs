using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PRM.Business.Services.Ai;

internal sealed class GroqLlmClient : ILlmClient
{
    private const string ModelName = "llama-3.3-70b-versatile";
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GroqLlmClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<string?> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "openai/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = JsonContent.Create(new GroqChatCompletionRequest(prompt));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        var payload = await response.Content.ReadFromJsonAsync<GroqChatCompletionResponse>(
            cancellationToken: cancellationToken);
        return payload?.Choices?
            .FirstOrDefault()?
            .Message?
            .Content?
            .Trim();
    }

    private sealed class GroqChatCompletionRequest
    {
        public GroqChatCompletionRequest(string prompt)
        {
            Model = ModelName;
            Temperature = 0.2;
            Messages =
            [
                new GroqChatMessage
                {
                    Role = "user",
                    Content = prompt
                }
            ];
        }

        [JsonPropertyName("model")]
        public string Model { get; set; }
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
        [JsonPropertyName("messages")]
        public List<GroqChatMessage> Messages { get; set; }
    }

    private sealed class GroqChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class GroqChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<GroqChatChoice>? Choices { get; set; }
    }

    private sealed class GroqChatChoice
    {
        [JsonPropertyName("message")]
        public GroqChatMessage? Message { get; set; }
    }
}
