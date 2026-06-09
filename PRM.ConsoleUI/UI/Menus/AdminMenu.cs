using PRM.ConsoleUI.Services;

using PRM.ConsoleUI.UI.Helpers;

using PRM.ConsoleUI.UI.Screens.Allocations;

using PRM.ConsoleUI.UI.Screens.SystemConfig;



namespace PRM.ConsoleUI.UI.Menus;



public class AdminMenu

{

    private readonly AuthSession _session;

    private readonly AuthApiClient _authApiClient;

    private readonly ManageUsersMenu _manageUsersMenu;

    private readonly ManageEmployeesMenu _manageEmployeesMenu;

    private readonly ManageProjectsMenu _manageProjectsMenu;

    private readonly ViewAllAllocationsScreen _viewAllAllocationsScreen;

    private readonly SystemConfigurationScreen _systemConfigurationScreen;



    public AdminMenu(

        AuthSession session,

        AuthApiClient authApiClient,

        ManageUsersMenu manageUsersMenu,

        ManageEmployeesMenu manageEmployeesMenu,

        ManageProjectsMenu manageProjectsMenu,

        ViewAllAllocationsScreen viewAllAllocationsScreen,

        SystemConfigurationScreen systemConfigurationScreen)

    {

        _session = session;

        _authApiClient = authApiClient;

        _manageUsersMenu = manageUsersMenu;

        _manageEmployeesMenu = manageEmployeesMenu;

        _manageProjectsMenu = manageProjectsMenu;

        _viewAllAllocationsScreen = viewAllAllocationsScreen;

        _systemConfigurationScreen = systemConfigurationScreen;

    }



    public async Task ShowAsync()

    {

        while (true)

        {

            var now = DateTime.Now.ToString("dd-MM-yyyy  HH:mm");



            ConsoleHelper.WriteHeader(

                "Admin Panel",

                $"Welcome, {_session.FullName}  |  {now}");



            Console.WriteLine("1. Manage Employees");

            Console.WriteLine("2. Manage Projects");

            Console.WriteLine("3. View All Allocations");

            Console.WriteLine("4. Manage Users");

            Console.WriteLine("5. System Configuration");

            Console.WriteLine("6. Logout");

            Console.WriteLine();

            Console.Write("Enter option: ");



            var choice = Console.ReadLine()?.Trim();



            switch (choice)

            {

                case "1":

                    await _manageEmployeesMenu.ShowAsync();

                    break;

                case "2":

                    await _manageProjectsMenu.ShowAsync();

                    break;

                case "3":

                    await _viewAllAllocationsScreen.ShowAsync();

                    break;

                case "4":

                    await _manageUsersMenu.ShowAsync();

                    break;

                case "5":

                    await _systemConfigurationScreen.ShowAsync();

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


