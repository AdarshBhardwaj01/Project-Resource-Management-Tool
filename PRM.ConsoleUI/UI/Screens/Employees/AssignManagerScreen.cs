using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Employees;

namespace PRM.ConsoleUI.UI.Screens.Employees;

public class AssignManagerScreen
{
    private readonly EmployeeApiClient _employeeApiClient;

    public AssignManagerScreen(EmployeeApiClient employeeApiClient)
    {
        _employeeApiClient = employeeApiClient;
    }

    public async Task ShowAsync()
    {
        ConsoleHelper.WriteHeader("Assign Manager");

        var employeeUserIdInput = ConsoleHelper.ReadInput("Employee User ID");
        var managerUserIdInput = ConsoleHelper.ReadInput("Manager User ID");

        if (!int.TryParse(employeeUserIdInput, out var employeeUserId) || employeeUserId <= 0)
        {
            ConsoleHelper.WriteError("Invalid Employee User ID.");
            ConsoleHelper.Pause();
            return;
        }

        if (!int.TryParse(managerUserIdInput, out var managerUserId) || managerUserId <= 0)
        {
            ConsoleHelper.WriteError("Invalid Manager User ID.");
            ConsoleHelper.Pause();
            return;
        }

        Console.WriteLine();
        Console.WriteLine("[S] Save     [B] Back");
        Console.Write("Choice: ");

        var choice = Console.ReadLine()?.Trim().ToUpperInvariant();

        if (choice == "B")
        {
            return;
        }

        if (choice != "S")
        {
            ConsoleHelper.WriteError("Invalid choice.");
            ConsoleHelper.Pause();
            return;
        }

        try
        {
            var message = await _employeeApiClient.AssignManagerAsync(new AssignManagerRequest
            {
                EmployeeUserId = employeeUserId,
                ManagerUserId = managerUserId
            });

            ConsoleHelper.WriteSuccess(message);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
        }

        ConsoleHelper.Pause();
    }
}
