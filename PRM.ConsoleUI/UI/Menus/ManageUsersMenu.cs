using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.ConsoleUI.UI.Screens.Users;

namespace PRM.ConsoleUI.UI.Menus;

public class ManageUsersMenu
{
    private readonly UserApiClient _userApiClient;
    private readonly CreateUserScreen _createUserScreen;
    private readonly ViewAllUsersScreen _viewAllUsersScreen;
    private readonly ResetUserPasswordScreen _resetUserPasswordScreen;
    private readonly DeactivateUserScreen _deactivateUserScreen;

    public ManageUsersMenu(
        UserApiClient userApiClient,
        CreateUserScreen createUserScreen,
        ViewAllUsersScreen viewAllUsersScreen,
        ResetUserPasswordScreen resetUserPasswordScreen,
        DeactivateUserScreen deactivateUserScreen)
    {
        _userApiClient = userApiClient;
        _createUserScreen = createUserScreen;
        _viewAllUsersScreen = viewAllUsersScreen;
        _resetUserPasswordScreen = resetUserPasswordScreen;
        _deactivateUserScreen = deactivateUserScreen;
    }

    public async Task ShowAsync()
    {
        while (true)
        {
            ConsoleHelper.WriteHeader("Manage Users");
            Console.WriteLine("1. Create User Account");
            Console.WriteLine("2. View All Users");
            Console.WriteLine("3. Reset User Password");
            Console.WriteLine("4. Deactivate User");
            Console.WriteLine("5. Back");
            Console.WriteLine();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    await _createUserScreen.ShowAsync();
                    break;
                case "2":
                    await _viewAllUsersScreen.ShowAsync();
                    break;
                case "3":
                    await _resetUserPasswordScreen.ShowAsync();
                    break;
                case "4":
                    await _deactivateUserScreen.ShowAsync();
                    break;
                case "5":
                    return;
                default:
                    ConsoleHelper.WriteError("Invalid option.");
                    ConsoleHelper.Pause();
                    break;
            }
        }
    }
}
