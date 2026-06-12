using PRM.Models.DTOs.EmployeePortal;

namespace PRM.Business.Interfaces.Services;

public interface IEmployeePortalService
{
    Task<IReadOnlyList<EmployeeAllocationItemDto>> GetMyAllocationsAsync(
        int userId,
        CancellationToken cancellationToken = default);
    Task<TimesheetSubmitPreviewResponse> GetTimesheetSubmitPreviewAsync(
        int userId,
        DateTime? weekStartDate,
        CancellationToken cancellationToken = default);
    Task<string> SubmitTimesheetAsync(
        int userId,
        SubmitTimesheetRequest request,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeTimesheetHistoryItemDto>> GetMyTimesheetsAsync(
        int userId,
        CancellationToken cancellationToken = default);
    Task<EmployeeTimesheetDetailDto> GetTimesheetDetailAsync(
        int userId,
        int timesheetId,
        CancellationToken cancellationToken = default);
}
