using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Resources;

namespace PRM.ConsoleUI.UI.Screens.Employees;

public class UpdateEmployeeScreen
{
    private readonly EmployeeApiClient _employeeApiClient;

    public UpdateEmployeeScreen(EmployeeApiClient employeeApiClient)
    {
        _employeeApiClient = employeeApiClient;
    }

    public async Task ShowAsync()
    {
        ConsoleHelper.WriteHeader("Update Employee");
        var userIdInput = ConsoleHelper.ReadInput("User ID");
        if (!int.TryParse(userIdInput, out var userId))
        {
            ConsoleHelper.WriteError("Invalid User ID.");
            ConsoleHelper.Pause();
            return;
        }
        try
        {
            var resource = await _employeeApiClient.GetEmployeeAsync(userId);
            Console.WriteLine();
            Console.WriteLine($"Updating: {resource.FullName} ({resource.Department})");
            Console.WriteLine("Leave a field blank to keep the current value.");
            Console.WriteLine();
            var department = ReadOptional("Department", resource.Department);
            var designation = ReadOptional("Designation", resource.Designation);
            ConsoleHelper.WriteSeparator();
            Console.WriteLine("[S] Save     [B] Back");
            Console.Write("Enter choice: ");
            var action = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (action == "B")
            {
                return;
            }
            if (action != "S")
            {
                ConsoleHelper.WriteError("Invalid choice.");
                ConsoleHelper.Pause();
                return;
            }
            var message = await _employeeApiClient.UpdateEmployeeAsync(userId, new UpdateResourceRequest
            {
                Department = department,
                Designation = designation
            });
            ConsoleHelper.WriteSuccess(message);
            ConsoleHelper.Pause();
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
            ConsoleHelper.Pause();
        }
    }

    private static string ReadOptional(string label, string currentValue)
    {
        Console.Write($"{label} [{currentValue}] : ");
        var input = Console.ReadLine()?.Trim();
        return string.IsNullOrWhiteSpace(input) ? currentValue : input;
    }
}
