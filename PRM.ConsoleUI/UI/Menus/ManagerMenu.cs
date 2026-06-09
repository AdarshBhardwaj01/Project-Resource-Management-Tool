using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.ConsoleUI.UI.Screens.Manager;

namespace PRM.ConsoleUI.UI.Menus;

public class ManagerMenu
{
    private readonly AuthSession _session;
    private readonly AuthApiClient _authApiClient;
    private readonly ResourceDashboardScreen _resourceDashboardScreen;
    private readonly AllocateResourceScreen _allocateResourceScreen;
    private readonly MyProjectsScreen _myProjectsScreen;
    private readonly TeamTimesheetsScreen _teamTimesheetsScreen;
    private readonly AiAssistantScreen _aiAssistantScreen;

    public ManagerMenu(
        AuthSession session,
        AuthApiClient authApiClient,
        ResourceDashboardScreen resourceDashboardScreen,
        AllocateResourceScreen allocateResourceScreen,
        MyProjectsScreen myProjectsScreen,
        TeamTimesheetsScreen teamTimesheetsScreen,
        AiAssistantScreen aiAssistantScreen)
    {
        _session = session;
        _authApiClient = authApiClient;
        _resourceDashboardScreen = resourceDashboardScreen;
        _allocateResourceScreen = allocateResourceScreen;
        _myProjectsScreen = myProjectsScreen;
        _teamTimesheetsScreen = teamTimesheetsScreen;
        _aiAssistantScreen = aiAssistantScreen;
    }

    public async Task ShowAsync()
    {
        while (true)
        {
            var now = DateTime.Now.ToString("dd-MMM-yyyy  HH:mm");

            ConsoleHelper.WriteHeader($"Welcome, {_session.FullName}!  |  {now}");

            Console.WriteLine("1. Resource Dashboard");
            Console.WriteLine("2. Allocate Resource");
            Console.WriteLine("3. My Projects");
            Console.WriteLine("4. Timesheets");
            Console.WriteLine("5. AI Assistant");
            Console.WriteLine("6. Logout");
            Console.WriteLine();
            Console.Write("Enter option: ");

            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    await _resourceDashboardScreen.ShowAsync();
                    break;
                case "2":
                    await _allocateResourceScreen.ShowAsync();
                    break;
                case "3":
                    await _myProjectsScreen.ShowAsync();
                    break;
                case "4":
                    await _teamTimesheetsScreen.ShowAsync();
                    break;
                case "5":
                    await _aiAssistantScreen.ShowAsync();
                    break;
                case "6":
                    _authApiClient.Logout();
                    return;
                default:
                    ConsoleHelper.WriteError("Invalid option.");
                    ConsoleHelper.Pause();
                    break;
            }
        }
    }
}
