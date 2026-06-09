using PRM.ConsoleUI.UI.Helpers;

using PRM.ConsoleUI.UI.Screens.Employees;



namespace PRM.ConsoleUI.UI.Menus;



public class ManageEmployeesMenu

{

    private readonly AddEmployeeScreen _addEmployeeScreen;

    private readonly ViewAllEmployeesScreen _viewAllEmployeesScreen;

    private readonly UpdateEmployeeScreen _updateEmployeeScreen;

    private readonly DeactivateEmployeeScreen _deactivateEmployeeScreen;

    private readonly ManageEmployeeSkillsScreen _manageEmployeeSkillsScreen;

    private readonly AssignManagerScreen _assignManagerScreen;



    public ManageEmployeesMenu(

        AddEmployeeScreen addEmployeeScreen,

        ViewAllEmployeesScreen viewAllEmployeesScreen,

        UpdateEmployeeScreen updateEmployeeScreen,

        DeactivateEmployeeScreen deactivateEmployeeScreen,

        ManageEmployeeSkillsScreen manageEmployeeSkillsScreen,

        AssignManagerScreen assignManagerScreen)

    {

        _addEmployeeScreen = addEmployeeScreen;

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

            ConsoleHelper.WriteHeader("Manage Employees");



            Console.WriteLine("1. Add Employee");

            Console.WriteLine("2. View All Employees");

            Console.WriteLine("3. Update Employee");

            Console.WriteLine("4. Deactivate Employee");

            Console.WriteLine("5. Manage Employee Skills");

            Console.WriteLine("6. Assign Manager");

            Console.WriteLine("7. Back");

            Console.WriteLine();

            Console.Write("Enter option: ");



            var choice = Console.ReadLine()?.Trim();



            switch (choice)

            {

                case "1":

                    await _addEmployeeScreen.ShowAsync();

                    break;

                case "2":

                    await _viewAllEmployeesScreen.ShowAsync();

                    break;

                case "3":

                    await _updateEmployeeScreen.ShowAsync();

                    break;

                case "4":

                    await _deactivateEmployeeScreen.ShowAsync();

                    break;

                case "5":

                    await _manageEmployeeSkillsScreen.ShowAsync();

                    break;

                case "6":

                    await _assignManagerScreen.ShowAsync();

                    break;

                case "7":

                    return;

                default:

                    ConsoleHelper.WriteError("Invalid option.");

                    ConsoleHelper.Pause();

                    break;

            }

        }

    }

}


