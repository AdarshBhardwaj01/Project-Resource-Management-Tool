using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Resources;

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
        Console.Write("Enter User ID: ");
        var userIdInput = Console.ReadLine()?.Trim();
        if (!int.TryParse(userIdInput, out var userId))
        {
            ConsoleHelper.WriteError("Invalid User ID.");
            ConsoleHelper.Pause();
            return;
        }
        try
        {
            var resource = await _employeeApiClient.GetEmployeeAsync(userId);
            DisplayResourceDetails(resource);
            Console.WriteLine();
            Console.WriteLine($"Are you sure you want to deactivate {resource.FullName}?");
            Console.WriteLine();
            Console.WriteLine("[Y] Yes, Deactivate     [B] Cancel");
            Console.Write("Enter choice: ");
            var confirm = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (confirm != "Y")
            {
                return;
            }
            var message = await _employeeApiClient.DeactivateEmployeeAsync(userId);
            ConsoleHelper.WriteSuccess(message);
            ConsoleHelper.Pause();
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
            ConsoleHelper.Pause();
        }
    }

    private static void DisplayResourceDetails(ResourceDetailDto resource)
    {
        Console.WriteLine();
        ConsoleHelper.WriteBanner(resource.FullName);
        Console.WriteLine($"Department : {resource.Department}");
        Console.WriteLine($"Status     : {resource.Status} ({resource.UtilisationPercent}%)");
        if (resource.ActiveAllocations.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"Warning: This employee has {resource.ActiveAllocations.Count} active allocation(s). " +
                "Ending their employment will remove them from:");
            foreach (var allocation in resource.ActiveAllocations)
            {
                Console.WriteLine(
                    $"  - {allocation.ProjectName} ({allocation.UtilisationPercent}%, ends {allocation.ToDate})");
            }
        }
    }
}
