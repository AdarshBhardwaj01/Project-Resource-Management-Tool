using System.Net.Http.Headers;
using System.Text.Json;
using PRM.Models.DTOs.Auth;

namespace PRM.ConsoleUI.Services;

public abstract class ApiClientBase
{
    protected readonly HttpClient HttpClient;
    protected readonly AuthSession Session;

    protected ApiClientBase(HttpClient httpClient, AuthSession session)
    {
        HttpClient = httpClient;
        Session = session;
    }

    protected void ApplyAuthorizationHeader()
    {
        if (string.IsNullOrWhiteSpace(Session.Token))
        {
            throw new InvalidOperationException("You are not logged in. Please log in again.");
        }
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Session.Token);
    }

    protected static async Task EnsureSuccessAsync(HttpResponseMessage response, string fallbackMessage)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }
        var message = await TryReadErrorMessageAsync(response);
        throw new InvalidOperationException(message ?? $"{fallbackMessage} (HTTP {(int)response.StatusCode})");
    }

    protected static async Task<string?> TryReadErrorMessageAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }
        try
        {
            var error = JsonSerializer.Deserialize<ApiErrorResponse>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return error?.Message;
        }
        catch (JsonException)
        {
            return content;
        }
    }

    protected static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return default;
        }
        return JsonSerializer.Deserialize<T>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
