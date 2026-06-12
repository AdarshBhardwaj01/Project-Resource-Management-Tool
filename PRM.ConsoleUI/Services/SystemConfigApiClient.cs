using System.Net.Http.Json;
using PRM.Models.DTOs.Auth;
using PRM.Models.DTOs.SystemConfig;

namespace PRM.ConsoleUI.Services;

public class SystemConfigApiClient : ApiClientBase
{
    public SystemConfigApiClient(HttpClient httpClient, AuthSession session)
        : base(httpClient, session)
    {
    }

    public async Task<SystemConfigDto> GetSystemConfigAsync()
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.GetAsync("api/system-config");
        await EnsureSuccessAsync(response, "Failed to load system configuration.");
        return await ReadJsonAsync<SystemConfigDto>(response)
            ?? throw new InvalidOperationException("System configuration not found.");
    }

    public async Task<string> UpdateSystemConfigAsync(UpdateSystemConfigRequest request)
    {
        ApplyAuthorizationHeader();
        var response = await HttpClient.PutAsJsonAsync("api/system-config", request);
        await EnsureSuccessAsync(response, "Update system configuration failed.");
        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "System configuration updated.";
    }
}
