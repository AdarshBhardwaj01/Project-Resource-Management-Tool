using System.Net.Http.Json;
using PRM.Models.DTOs.Allocations;

namespace PRM.ConsoleUI.Services;

public class AllocationApiClient : ApiClientBase
{
    public AllocationApiClient(HttpClient httpClient, AuthSession session)
        : base(httpClient, session)
    {
    }

    public async Task<AllocationListResponse> GetAllAllocationsAsync(
        int? employeeId = null,
        int? projectId = null,
        string? status = null)
    {
        ApplyAuthorizationHeader();

        var query = new List<string>();

        if (employeeId.HasValue)
        {
            query.Add($"employeeId={employeeId.Value}");
        }

        if (projectId.HasValue)
        {
            query.Add($"projectId={projectId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Add($"status={Uri.EscapeDataString(status)}");
        }

        var url = query.Count == 0
            ? "api/allocations"
            : $"api/allocations?{string.Join("&", query)}";

        var response = await HttpClient.GetAsync(url);
        await EnsureSuccessAsync(response, "Failed to load allocations.");

        return await ReadJsonAsync<AllocationListResponse>(response)
            ?? new AllocationListResponse();
    }
}
