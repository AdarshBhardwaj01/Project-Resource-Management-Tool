using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Resources;

namespace PRM.ConsoleUI.UI.Screens.Employees;

public class ViewAllEmployeesScreen
{
    private readonly EmployeeApiClient _employeeApiClient;

    public ViewAllEmployeesScreen(EmployeeApiClient employeeApiClient)
    {
        _employeeApiClient = employeeApiClient;
    }

    public async Task ShowAsync()
    {
        string? statusFilter = null;
        string? departmentFilter = null;
        while (true)
        {
            try
            {
                var response = await _employeeApiClient.GetAllEmployeesAsync(statusFilter, departmentFilter);
                DisplayEmployeeList(response, statusFilter, departmentFilter);
                ConsoleHelper.WriteSeparator();
                Console.WriteLine("[F] Filter by Status / Department     [C] Clear Filters     [B] Back");
                Console.Write("Enter choice: ");
                var choice = Console.ReadLine()?.Trim().ToUpperInvariant();
                if (choice == "B")
                {
                    return;
                }
                if (choice == "C")
                {
                    statusFilter = null;
                    departmentFilter = null;
                    continue;
                }
                if (choice == "F")
                {
                    (statusFilter, departmentFilter) = PromptFilters();
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

    private static void DisplayEmployeeList(ResourceListResponse response, string? statusFilter, string? departmentFilter)
    {
        ConsoleHelper.WriteHeader("All Employees");
        if (!string.IsNullOrWhiteSpace(statusFilter) || !string.IsNullOrWhiteSpace(departmentFilter))
        {
            var statusText = string.IsNullOrWhiteSpace(statusFilter) ? "All" : statusFilter;
            var departmentText = string.IsNullOrWhiteSpace(departmentFilter) ? "All" : departmentFilter;
            Console.WriteLine($"Filters: Status = {statusText}  |  Department = {departmentText}");
            Console.WriteLine();
        }
        Console.WriteLine($"{"ID",-5}{"Name",-20}{"Department",-12}{"Status"}");
        ConsoleHelper.WriteSeparator();
        foreach (var resource in response.Resources)
        {
            Console.WriteLine(
                $"{resource.UserId,-5}{resource.FullName,-20}{resource.Department,-12}{resource.Status}");
        }
        ConsoleHelper.WriteSeparator();
        Console.WriteLine(
            $"Total: {response.Total}  |  Allocated: {response.AllocatedCount}  |  Bench: {response.BenchCount}");
    }

    private static (string? Status, string? Department) PromptFilters()
    {
        ConsoleHelper.WriteHeader("Filter Employees");
        Console.WriteLine("Filter by status (optional): (1) BENCH  (2) ALLOCATED  [Enter] All");
        Console.Write("Enter choice: ");
        var statusChoice = Console.ReadLine()?.Trim();
        string? statusFilter = statusChoice switch
        {
            "1" => "BENCH",
            "2" => "ALLOCATED",
            _ => null
        };
        var departmentFilter = ConsoleHelper.ReadInput("Department filter (optional, press Enter to skip)");
        return (
            statusFilter,
            string.IsNullOrWhiteSpace(departmentFilter) ? null : departmentFilter.Trim());
    }
}
