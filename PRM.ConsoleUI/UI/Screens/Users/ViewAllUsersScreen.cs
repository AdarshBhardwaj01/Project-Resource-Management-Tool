using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;

namespace PRM.ConsoleUI.UI.Screens.Users;

public class ViewAllUsersScreen
{
    private readonly UserApiClient _userApiClient;

    public ViewAllUsersScreen(UserApiClient userApiClient)
    {
        _userApiClient = userApiClient;
    }

    public async Task ShowAsync()
    {
        try
        {
            var response = await _userApiClient.GetAllUsersAsync();
            ConsoleHelper.WriteHeader("All Users");
            Console.WriteLine($"{"ID",-5}{"Username",-18}{"Role",-12}{"Status"}");
            ConsoleHelper.WriteSeparator();
            foreach (var user in response.Users)
            {
                Console.WriteLine($"{user.Id,-5}{user.Username,-18}{user.Role,-12}{user.Status}");
            }
            ConsoleHelper.WriteSeparator();
            Console.WriteLine($"Total: {response.Total}   |   Active: {response.ActiveCount}   |   Inactive: {response.InactiveCount}");
            ConsoleHelper.WriteSeparator();
            Console.WriteLine("[R] Reactivate a user     [B] Back");
            Console.Write("Enter choice: ");
            var choice = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (choice == "B")
            {
                return;
            }
            if (choice == "R")
            {
                await ReactivateUserAsync();
                return;
            }
            ConsoleHelper.Pause();
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
            ConsoleHelper.Pause();
        }
    }

    private async Task ReactivateUserAsync()
    {
        Console.Write("Enter User ID to reactivate: ");
        var input = Console.ReadLine()?.Trim();
        if (!int.TryParse(input, out var userId))
        {
            ConsoleHelper.WriteError("Invalid User ID.");
            ConsoleHelper.Pause();
            return;
        }
        try
        {
            var user = await _userApiClient.GetUserAsync(userId.ToString());
            Console.WriteLine();
            Console.WriteLine($"User: {user.FullName} ({user.Role}) - currently {user.Status}");
            Console.WriteLine();
            Console.WriteLine("Reactivate this account?");
            Console.WriteLine("If a linked employee profile exists, it will also be restored on BENCH.");
            Console.WriteLine("Previous allocations are NOT restored.");
            Console.WriteLine();
            Console.WriteLine("[Y] Yes     [B] Cancel");
            Console.Write("Enter choice: ");
            var confirm = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (confirm != "Y")
            {
                return;
            }
            var message = await _userApiClient.ReactivateUserAsync(userId);
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
