using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Projects;

namespace PRM.ConsoleUI.UI.Screens.Projects;

public class ViewAllProjectsScreen
{
    private static readonly (string Title, int Width)[] Columns =
    [
        ("ID", 3),
        ("Name", 15),
        ("Manager", 12),
        ("End Date", 9),
        ("Status", 0)
    ];

    private readonly ProjectApiClient _projectApiClient;

    public ViewAllProjectsScreen(ProjectApiClient projectApiClient)
    {
        _projectApiClient = projectApiClient;
    }

    public async Task ShowAsync()
    {
        try
        {
            var response = await _projectApiClient.GetAllProjectsAsync();
            DisplayProjectList(response);

            ConsoleHelper.WriteActions(("B", "Back"));
            var choice = ConsoleHelper.ReadActionChoice();

            if (choice != "B" && !string.IsNullOrWhiteSpace(choice))
            {
                ConsoleHelper.WriteError("Invalid option.");
                ConsoleHelper.Pause();
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
            ConsoleHelper.Pause();
        }
    }

    private static void DisplayProjectList(ProjectListResponse response)
    {
        ConsoleHelper.WriteHeader("All Projects");

        var rows = response.Projects.Select(project => new (string Value, int Width)[]
        {
            (project.Id.ToString(), Columns[0].Width),
            (project.Name, Columns[1].Width),
            (project.ManagerName, Columns[2].Width),
            (project.EndDate, Columns[3].Width),
            (project.Status, Columns[4].Width)
        });

        ConsoleHelper.WritePipeTable(Columns, rows);
    }
}
