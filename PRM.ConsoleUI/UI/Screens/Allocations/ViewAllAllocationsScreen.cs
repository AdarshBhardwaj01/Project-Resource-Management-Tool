using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Allocations;

namespace PRM.ConsoleUI.UI.Screens.Allocations;

public class ViewAllAllocationsScreen
{
    private static readonly (string Title, int Width)[] Columns =
    [
        ("Employee", 14),
        ("Project", 15),
        ("%", 4),
        ("From", 9),
        ("To", 9)
    ];

    private readonly AllocationApiClient _allocationApiClient;

    public ViewAllAllocationsScreen(AllocationApiClient allocationApiClient)
    {
        _allocationApiClient = allocationApiClient;
    }

    public async Task ShowAsync()
    {
        int? employeeIdFilter = null;
        int? projectIdFilter = null;

        while (true)
        {
            try
            {
                var response = await _allocationApiClient.GetAllAllocationsAsync(
                    employeeIdFilter,
                    projectIdFilter,
                    "ACTIVE");

                DisplayAllocationList(response, employeeIdFilter, projectIdFilter);

                ConsoleHelper.WriteActions(("F", "Filter by Employee / Project"), ("B", "Back"));
                var choice = ConsoleHelper.ReadActionChoice();

                if (choice == "B")
                {
                    return;
                }

                if (choice == "F")
                {
                    (employeeIdFilter, projectIdFilter) = PromptFilters();
                    continue;
                }

                ConsoleHelper.WriteError("Invalid option.");
                ConsoleHelper.Pause();
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError(ex.Message);
                ConsoleHelper.Pause();
                return;
            }
        }
    }

    private static void DisplayAllocationList(
        AllocationListResponse response,
        int? employeeIdFilter,
        int? projectIdFilter)
    {
        ConsoleHelper.WriteHeader("All Allocations");

        if (employeeIdFilter.HasValue || projectIdFilter.HasValue)
        {
            var employeeText = employeeIdFilter?.ToString() ?? "All";
            var projectText = projectIdFilter?.ToString() ?? "All";

            Console.WriteLine($"Filters: Employee ID = {employeeText}  |  Project ID = {projectText}");
            Console.WriteLine();
        }

        var rows = response.Allocations.Select(allocation => new (string Value, int Width)[]
        {
            (allocation.EmployeeName, Columns[0].Width),
            (allocation.ProjectName, Columns[1].Width),
            ($"{allocation.UtilisationPercent}%", Columns[2].Width),
            (allocation.FromDate, Columns[3].Width),
            (allocation.ToDate, Columns[4].Width)
        });

        ConsoleHelper.WritePipeTable(Columns, rows);

        Console.WriteLine();
        Console.WriteLine($"Total Active Allocations: {response.Allocations.Count}");
    }

    private static (int? EmployeeId, int? ProjectId) PromptFilters()
    {
        ConsoleHelper.WriteHeader("Filter Allocations");

        var employeeIdInput = ConsoleHelper.ReadInput("Employee ID (optional, press Enter to skip)");
        int? employeeId = int.TryParse(employeeIdInput, out var parsedEmployeeId) && parsedEmployeeId > 0
            ? parsedEmployeeId
            : null;

        var projectIdInput = ConsoleHelper.ReadInput("Project ID (optional, press Enter to skip)");
        int? projectId = int.TryParse(projectIdInput, out var parsedProjectId) && parsedProjectId > 0
            ? parsedProjectId
            : null;

        return (employeeId, projectId);
    }
}
