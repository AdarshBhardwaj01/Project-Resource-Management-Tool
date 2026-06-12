using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Screens;
using PRM.ConsoleUI.UI.Helpers;

namespace PRM.ConsoleUI.UI.Menus;

public class ApplicationHost
{
    private readonly LoginScreen _loginScreen;
    private readonly ChangePasswordScreen _changePasswordScreen;
    private readonly AdminMenu _adminMenu;
    private readonly ManagerMenu _managerMenu;
    private readonly EmployeeMenu _employeeMenu;
    private readonly AuthSession _session;

    public ApplicationHost(
        LoginScreen loginScreen,
        ChangePasswordScreen changePasswordScreen,
        AdminMenu adminMenu,
        ManagerMenu managerMenu,
        EmployeeMenu employeeMenu,
        AuthSession session)
    {
        _loginScreen = loginScreen;
        _changePasswordScreen = changePasswordScreen;
        _adminMenu = adminMenu;
        _managerMenu = managerMenu;
        _employeeMenu = employeeMenu;
        _session = session;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            var loginResult = await ShowEntryScreenAsync();

            if (loginResult == EntryScreenResult.Exit)
            {
                return;
            }

            if (!_session.IsAuthenticated)
            {
                continue;
            }

            if (_session.ForcePasswordChange)
            {
                await _changePasswordScreen.ShowAsync();
            }

            await ShowRoleMenuAsync();
            ConsoleHelper.ClearScreen();
        }
    }

    private async Task<EntryScreenResult> ShowEntryScreenAsync()
    {
        while (true)
        {
            try
            {
                var loggedIn = await _loginScreen.ShowAsync();

                if (loggedIn)
                {
                    return EntryScreenResult.Continue;
                }
            }
            catch (ApplicationException ex) when (ex.Message == "Exit")
            {
                return EntryScreenResult.Exit;
            }
        }
    }

    private async Task ShowRoleMenuAsync()
    {
        switch (_session.Role)
        {
            case "Admin":
                await _adminMenu.ShowAsync();
                break;
            case "Manager":
                await _managerMenu.ShowAsync();
                break;
            case "Employee":
                await _employeeMenu.ShowAsync();
                break;
        }
    }

    private enum EntryScreenResult
    {
        Continue,
        Exit
    }
}
