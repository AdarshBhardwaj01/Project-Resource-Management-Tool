using PRM.ConsoleUI.UI.Helpers;
using PRM.ConsoleUI.UI.Screens.Employees;

namespace PRM.ConsoleUI.UI.Menus;

public class ManageEmployeesMenu
{
    private readonly ViewAllEmployeesScreen _viewAllEmployeesScreen;
    private readonly UpdateEmployeeScreen _updateEmployeeScreen;
    private readonly DeactivateEmployeeScreen _deactivateEmployeeScreen;
    private readonly ManageEmployeeSkillsScreen _manageEmployeeSkillsScreen;
    private readonly AssignManagerScreen _assignManagerScreen;

    public ManageEmployeesMenu(
        ViewAllEmployeesScreen viewAllEmployeesScreen,
        UpdateEmployeeScreen updateEmployeeScreen,
        DeactivateEmployeeScreen deactivateEmployeeScreen,
        ManageEmployeeSkillsScreen manageEmployeeSkillsScreen,
        AssignManagerScreen assignManagerScreen)
    {
        _viewAllEmployeesScreen = viewAllEmployeesScreen;
        _updateEmployeeScreen = updateEmployeeScreen;
        _deactivateEmployeeScreen = deactivateEmployeeScreen;
        _manageEmployeeSkillsScreen = manageEmployeeSkillsScreen;
        _assignManagerScreen = assignManagerScreen;
    }

    public async Task ShowAsync()
    {
        while (true)
        {
            ConsoleHelper.WriteHeader("Manage Resources");
            Console.WriteLine("1. View All Resources");
            Console.WriteLine("2. Update Resources");
            Console.WriteLine("3. Deactivate Resources");
            Console.WriteLine("4. Manage Resources Skills");
            Console.WriteLine("5. Assign Manager");
            Console.WriteLine("6. Back");
            Console.WriteLine();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    await _viewAllEmployeesScreen.ShowAsync();
                    break;
                case "2":
                    await _updateEmployeeScreen.ShowAsync();
                    break;
                case "3":
                    await _deactivateEmployeeScreen.ShowAsync();
                    break;
                case "4":
                    await _manageEmployeeSkillsScreen.ShowAsync();
                    break;
                case "5":
                    await _assignManagerScreen.ShowAsync();
                    break;
                case "6":
                    return;
                default:
                    ConsoleHelper.WriteError("Invalid option.");
                    ConsoleHelper.Pause();
                    break;
            }
        }
    }
}
