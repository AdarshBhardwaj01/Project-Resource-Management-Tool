using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;

namespace PRM.ConsoleUI.UI.Screens.Users;

public class DeactivateUserScreen
{
    private readonly UserApiClient _userApiClient;

    public DeactivateUserScreen(UserApiClient userApiClient)
    {
        _userApiClient = userApiClient;
    }

    public async Task ShowAsync()
    {
        ConsoleHelper.WriteHeader("Deactivate User");
        var usernameOrId = ConsoleHelper.ReadInput("Enter Username or User ID");
        try
        {
            var user = await _userApiClient.GetUserAsync(usernameOrId);
            Console.WriteLine();
            Console.WriteLine($"User found: {user.FullName} ({user.Role})");
            Console.WriteLine($"Status     : {user.Status}");
            Console.WriteLine();
            Console.WriteLine("Are you sure you want to deactivate this account?");
            Console.WriteLine("Deactivated users cannot log in. Their data is preserved.");
            Console.WriteLine();
            Console.WriteLine("[Y] Yes, Deactivate     [B] Back");
            Console.Write("Enter choice: ");
            var confirm = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (confirm != "Y")
            {
                return;
            }
            var message = await _userApiClient.DeactivateUserAsync(usernameOrId);
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
