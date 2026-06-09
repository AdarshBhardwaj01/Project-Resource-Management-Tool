using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Employees;

namespace PRM.ConsoleUI.UI.Screens.Employees;

public class DeactivateEmployeeScreen
{
    private readonly EmployeeApiClient _employeeApiClient;

    public DeactivateEmployeeScreen(EmployeeApiClient employeeApiClient)
    {
        _employeeApiClient = employeeApiClient;
    }

    public async Task ShowAsync()
    {
        ConsoleHelper.WriteHeader("Deactivate Employee");

        Console.Write("Enter Employee ID: ");
        var employeeIdInput = Console.ReadLine()?.Trim();

        if (!int.TryParse(employeeIdInput, out var employeeId))
        {
            ConsoleHelper.WriteError("Invalid Employee ID.");
            ConsoleHelper.Pause();
            return;
        }

        try
        {
            var employee = await _employeeApiClient.GetEmployeeAsync(employeeId);
            DisplayEmployeeDetails(employee);

            Console.WriteLine();
            Console.WriteLine($"Are you sure you want to deactivate {employee.FullName}?");
    
            Console.WriteLine();
            Console.WriteLine("[Y] Yes, Deactivate     [B] Cancel");
            Console.Write("Enter choice: ");

            var confirm = Console.ReadLine()?.Trim().ToUpperInvariant();

            if (confirm != "Y")
            {
                return;
            }

            var message = await _employeeApiClient.DeactivateEmployeeAsync(employeeId);
            ConsoleHelper.WriteSuccess(message);
            ConsoleHelper.Pause();
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
            ConsoleHelper.Pause();
        }
    }

    private static void DisplayEmployeeDetails(EmployeeDetailDto employee)
    {
        Console.WriteLine();
        ConsoleHelper.WriteBanner(employee.FullName);
        Console.WriteLine($"Department : {employee.Department}");
        Console.WriteLine($"Status     : {employee.Status} ({employee.UtilisationPercent}%)");

        if (employee.ActiveAllocations.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"Warning: This employee has {employee.ActiveAllocations.Count} active allocation(s). " +
                "Ending their employment will remove them from:");

            foreach (var allocation in employee.ActiveAllocations)
            {
                Console.WriteLine(
                    $"  - {allocation.ProjectName} ({allocation.UtilisationPercent}%, ends {allocation.ToDate})");
            }
        }
    }
}
