using System.Net.Http.Json;
using PRM.Models.DTOs.Auth;
using PRM.Models.DTOs.EmployeePortal;

namespace PRM.ConsoleUI.Services;

public class EmployeePortalApiClient : ApiClientBase
{
    public EmployeePortalApiClient(HttpClient httpClient, AuthSession session)
        : base(httpClient, session)
    {
    }

    public async Task<IReadOnlyList<EmployeeAllocationItemDto>> GetMyAllocationsAsync()
    {
        ApplyAuthorizationHeader();

        var response = await HttpClient.GetAsync("api/employee/allocations");
        await EnsureSuccessAsync(response, "Failed to load allocations.");

        return await ReadJsonAsync<List<EmployeeAllocationItemDto>>(response)
            ?? [];
    }

    public async Task<TimesheetSubmitPreviewResponse> GetTimesheetSubmitPreviewAsync(string? weekStartDate = null)
    {
        ApplyAuthorizationHeader();

        var url = string.IsNullOrWhiteSpace(weekStartDate)
            ? "api/employee/timesheets/submit-preview"
            : $"api/employee/timesheets/submit-preview?weekStart={Uri.EscapeDataString(weekStartDate)}";

        var response = await HttpClient.GetAsync(url);
        await EnsureSuccessAsync(response, "Failed to load timesheet preview.");

        return await ReadJsonAsync<TimesheetSubmitPreviewResponse>(response)
            ?? new TimesheetSubmitPreviewResponse();
    }

    public async Task<string> SubmitTimesheetAsync(SubmitTimesheetRequest request)
    {
        ApplyAuthorizationHeader();

        var response = await HttpClient.PostAsJsonAsync("api/employee/timesheets", request);
        await EnsureSuccessAsync(response, "Timesheet submission failed.");

        var result = await ReadJsonAsync<ApiMessageResponse>(response);
        return result?.Message ?? "Timesheet submitted.";
    }

    public async Task<IReadOnlyList<EmployeeTimesheetHistoryItemDto>> GetMyTimesheetsAsync()
    {
        ApplyAuthorizationHeader();

        var response = await HttpClient.GetAsync("api/employee/timesheets");
        await EnsureSuccessAsync(response, "Failed to load timesheets.");

        return await ReadJsonAsync<List<EmployeeTimesheetHistoryItemDto>>(response)
            ?? [];
    }

    public async Task<EmployeeTimesheetDetailDto> GetTimesheetDetailAsync(int timesheetId)
    {
        ApplyAuthorizationHeader();

        var response = await HttpClient.GetAsync($"api/employee/timesheets/{timesheetId}");
        await EnsureSuccessAsync(response, "Timesheet not found.");

        return await ReadJsonAsync<EmployeeTimesheetDetailDto>(response)
            ?? throw new InvalidOperationException("Timesheet not found.");
    }
}
