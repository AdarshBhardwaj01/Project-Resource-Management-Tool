using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Resources;

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
        ConsoleHelper.WriteHeader("Add Resource");
        Console.WriteLine("Note: Department and Designation are taken from the user's account.");
        Console.WriteLine();
        var userIdInput = ConsoleHelper.ReadInput("User ID");
        if (!int.TryParse(userIdInput, out var userId) || userId <= 0)
        {
            ConsoleHelper.WriteError("Invalid User ID.");
            ConsoleHelper.Pause();
            return;
        }
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
            var message = await _employeeApiClient.CreateEmployeeAsync(new CreateResourceRequest
            {
                UserId = userId
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
