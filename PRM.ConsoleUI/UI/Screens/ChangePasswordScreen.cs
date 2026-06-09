using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Auth;

namespace PRM.ConsoleUI.UI.Screens;

public class ChangePasswordScreen
{
    private readonly AuthApiClient _authApiClient;

    public ChangePasswordScreen(AuthApiClient authApiClient)
    {
        _authApiClient = authApiClient;
    }

    public async Task ShowAsync()
    {
        ConsoleHelper.WriteHeader(
            "Change Password",
            "You must set a new password to continue.");

        var newPassword = ConsoleHelper.ReadPassword("New Password");
        var confirmPassword = ConsoleHelper.ReadPassword("Confirm Password");

        ConsoleHelper.WriteSeparator();
        Console.WriteLine("[S] Save and Continue");
        Console.Write("Enter choice: ");

        var action = Console.ReadLine()?.Trim().ToUpperInvariant();

        if (action != "S")
        {
            await ShowAsync();
            return;
        }

        try
        {
            var message = await _authApiClient.ChangePasswordAsync(new ChangePasswordRequest
            {
                NewPassword = newPassword,
                ConfirmPassword = confirmPassword
            });

            ConsoleHelper.WriteSuccess(message);
            ConsoleHelper.Pause();
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
            ConsoleHelper.Pause();
            await ShowAsync();
        }
    }
}
