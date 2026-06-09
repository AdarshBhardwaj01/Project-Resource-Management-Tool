using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Users;

namespace PRM.ConsoleUI.UI.Screens.Users;

public class ResetUserPasswordScreen
{
    private readonly UserApiClient _userApiClient;

    public ResetUserPasswordScreen(UserApiClient userApiClient)
    {
        _userApiClient = userApiClient;
    }

    public async Task ShowAsync()
    {
        ConsoleHelper.WriteHeader("Reset User Password");

        var usernameOrId = ConsoleHelper.ReadInput("Enter Username or User ID");

        try
        {
            var user = await _userApiClient.GetUserAsync(usernameOrId);
            Console.WriteLine();
            Console.WriteLine($"User found: {user.FullName} ({user.Role})");
            Console.WriteLine();

            var newPassword = ConsoleHelper.ReadPassword("New Temporary Password");
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

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ConsoleHelper.WriteError("New temporary password is required.");
                ConsoleHelper.Pause();
                return;
            }

            var message = await _userApiClient.ResetPasswordAsync(usernameOrId, new ResetUserPasswordRequest
            {
                NewTemporaryPassword = newPassword
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
