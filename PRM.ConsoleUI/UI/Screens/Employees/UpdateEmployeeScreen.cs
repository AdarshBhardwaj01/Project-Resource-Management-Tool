using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Employees;

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

        var employeeIdInput = ConsoleHelper.ReadInput("Employee ID");

        if (!int.TryParse(employeeIdInput, out var employeeId))
        {
            ConsoleHelper.WriteError("Invalid Employee ID.");
            ConsoleHelper.Pause();
            return;
        }

        try
        {
            var employee = await _employeeApiClient.GetEmployeeAsync(employeeId);

            Console.WriteLine();
            Console.WriteLine($"Updating: {employee.FullName} ({employee.Department})");
            Console.WriteLine("Leave a field blank to keep the current value.");
            Console.WriteLine();

            var fullName = ReadOptional("Full Name", employee.FullName);
            var department = ReadOptional("Department", employee.Department);
            var designation = ReadOptional("Designation", employee.Designation);

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

            var message = await _employeeApiClient.UpdateEmployeeAsync(employeeId, new UpdateEmployeeRequest
            {
                FullName = fullName,
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
