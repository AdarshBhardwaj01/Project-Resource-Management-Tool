using PRM.ConsoleUI.UI.Helpers;
using PRM.ConsoleUI.UI.Screens.Projects;

namespace PRM.ConsoleUI.UI.Menus;

public class ManageProjectsMenu
{
    private readonly CreateProjectScreen _createProjectScreen;
    private readonly ViewAllProjectsScreen _viewAllProjectsScreen;
    private readonly UpdateProjectScreen _updateProjectScreen;
    private readonly ManageProjectMilestonesScreen _manageProjectMilestonesScreen;

    public ManageProjectsMenu(
        CreateProjectScreen createProjectScreen,
        ViewAllProjectsScreen viewAllProjectsScreen,
        UpdateProjectScreen updateProjectScreen,
        ManageProjectMilestonesScreen manageProjectMilestonesScreen)
    {
        _createProjectScreen = createProjectScreen;
        _viewAllProjectsScreen = viewAllProjectsScreen;
        _updateProjectScreen = updateProjectScreen;
        _manageProjectMilestonesScreen = manageProjectMilestonesScreen;
    }

    public async Task ShowAsync()
    {
        while (true)
        {
            ConsoleHelper.WriteHeader("Manage Projects");
            Console.WriteLine("1. Create Project");
            Console.WriteLine("2. View All Projects");
            Console.WriteLine("3. Update Project");
            Console.WriteLine("4. Manage Milestones");
            Console.WriteLine("5. Back");
            Console.WriteLine();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    await _createProjectScreen.ShowAsync();
                    break;
                case "2":
                    await _viewAllProjectsScreen.ShowAsync();
                    break;
                case "3":
                    await _updateProjectScreen.ShowAsync();
                    break;
                case "4":
                    await _manageProjectMilestonesScreen.ShowAsync();
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
