using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Employees;

namespace PRM.ConsoleUI.UI.Screens.Employees;

public class AddEmployeeScreen
{
    private readonly EmployeeApiClient _employeeApiClient;

    public AddEmployeeScreen(EmployeeApiClient employeeApiClient)
    {
        _employeeApiClient = employeeApiClient;
    }

    public async Task ShowAsync()
    {
        ConsoleHelper.WriteHeader("Add Employee");

        var userIdInput = ConsoleHelper.ReadInput("User ID");

        if (!int.TryParse(userIdInput, out var userId) || userId <= 0)
        {
            ConsoleHelper.WriteError("Invalid User ID.");
            ConsoleHelper.Pause();
            return;
        }

        var fullName = ConsoleHelper.ReadInput("Full Name");
        var email = ConsoleHelper.ReadInput("Email");
        var department = ConsoleHelper.ReadInput("Department");
        var designation = ConsoleHelper.ReadInput("Designation");

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

        try
        {
            var message = await _employeeApiClient.CreateEmployeeAsync(new CreateEmployeeRequest
            {
                UserId = userId,
                FullName = fullName,
                Email = email,
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
}
