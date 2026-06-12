using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.ConsoleUI.UI.Screens.Employee;

namespace PRM.ConsoleUI.UI.Menus;

public class EmployeeMenu
{

    private readonly AuthSession _session;

    private readonly AuthApiClient _authApiClient;

    private readonly SubmitTimesheetScreen _submitTimesheetScreen;

    private readonly ViewMyTimesheetsScreen _viewMyTimesheetsScreen;

    private readonly ViewMyAllocationsScreen _viewMyAllocationsScreen;

    public EmployeeMenu(
        AuthSession session,
        AuthApiClient authApiClient,
        SubmitTimesheetScreen submitTimesheetScreen,
        ViewMyTimesheetsScreen viewMyTimesheetsScreen,
        ViewMyAllocationsScreen viewMyAllocationsScreen)
    {
        _session = session;
        _authApiClient = authApiClient;
        _submitTimesheetScreen = submitTimesheetScreen;
        _viewMyTimesheetsScreen = viewMyTimesheetsScreen;
        _viewMyAllocationsScreen = viewMyAllocationsScreen;
    }

    public async Task ShowAsync()
    {
        while (true)
        {
            var now = DateTime.Now.ToString("dd-MMM-yyyy");
            ConsoleHelper.WriteHeader($"Welcome, {_session.FullName}!", now);
            Console.WriteLine("1. Submit Timesheet");
            Console.WriteLine("2. View My Timesheets");
            Console.WriteLine("3. View My Allocations");
            Console.WriteLine("4. Logout");
            Console.WriteLine();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    await _submitTimesheetScreen.ShowAsync();
                    break;
                case "2":
                    await _viewMyTimesheetsScreen.ShowAsync();
                    break;
                case "3":
                    await _viewMyAllocationsScreen.ShowAsync();
                    break;
                case "4":
                    _authApiClient.Logout();
                    ConsoleHelper.EndScreenSession();
                    return;
                default:
                    ConsoleHelper.WriteError("Invalid option.");
                    ConsoleHelper.Pause();
                    break;
            }

        }

    }

}
