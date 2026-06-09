using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Users;

namespace PRM.ConsoleUI.UI.Screens.Users;

public class CreateUserScreen
{
    private readonly UserApiClient _userApiClient;

    public CreateUserScreen(UserApiClient userApiClient)
    {
        _userApiClient = userApiClient;
    }

    public async Task ShowAsync()
    {
        ConsoleHelper.WriteHeader("Create User Account");

        var fullName = ConsoleHelper.ReadInput("Full Name");
        var email = ConsoleHelper.ReadInput("Email");
        var username = ConsoleHelper.ReadInput("Username");
        var temporaryPassword = ConsoleHelper.ReadPassword("Temporary Password");

        Console.WriteLine("Role              : (1) Admin  (2) Manager  (3) Employee");
        Console.Write("Enter choice      : ");
        var roleChoice = Console.ReadLine()?.Trim();

        if (roleChoice is not ("1" or "2" or "3"))
        {
            ConsoleHelper.WriteError("Invalid role selected.");
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
            var message = await _userApiClient.CreateUserAsync(new CreateUserRequest
            {
                FullName = fullName,
                Email = email,
                Username = username,
                TemporaryPassword = temporaryPassword,
                Role = int.Parse(roleChoice)
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
