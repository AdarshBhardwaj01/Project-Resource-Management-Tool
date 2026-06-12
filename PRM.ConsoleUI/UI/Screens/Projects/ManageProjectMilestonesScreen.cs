using PRM.Common.Helpers;
using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Projects;

namespace PRM.ConsoleUI.UI.Screens.Projects;

public class ManageProjectMilestonesScreen
{
    private static readonly (string Title, int Width)[] MilestoneColumns =
    [
        ("#", 4),
        ("Title", 20),
        ("Due Date", 9),
        ("Status", 0)
    ];

    private readonly ProjectApiClient _projectApiClient;

    public ManageProjectMilestonesScreen(ProjectApiClient projectApiClient)
    {
        _projectApiClient = projectApiClient;
    }

    public async Task ShowAsync()
    {
        ConsoleHelper.WriteHeader("Milestones");
        Console.Write("Enter Project ID: ");
        var projectIdInput = Console.ReadLine()?.Trim();
        if (!int.TryParse(projectIdInput, out var projectId))
        {
            ConsoleHelper.WriteError("Invalid Project ID.");
            ConsoleHelper.Pause();
            return;
        }
        try
        {
            _ = await _projectApiClient.GetProjectAsync(projectId);
            while (true)
            {
                var project = await _projectApiClient.GetProjectAsync(projectId);
                DisplayMilestonesScreen(project, projectId);
                Console.WriteLine("1. Add Milestone");
                Console.WriteLine("2. Update Milestone Status");
                Console.WriteLine("3. Back");
                Console.WriteLine();
                Console.Write("Enter option: ");
                var choice = Console.ReadLine()?.Trim();
                switch (choice)
                {
                    case "1":
                        await AddMilestoneAsync(project);
                        break;
                    case "2":
                        await UpdateMilestoneAsync(project);
                        break;
                    case "3":
                        return;
                    default:
                        ConsoleHelper.WriteError("Invalid option.");
                        ConsoleHelper.Pause();
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
            ConsoleHelper.Pause();
        }
    }

    private static void DisplayMilestonesScreen(ProjectDetailDto project, int projectId)
    {
        ConsoleHelper.WriteHeader("Milestones");
        Console.WriteLine($"Enter Project ID: {projectId}");
        Console.WriteLine();
        var tableWidth = ConsoleHelper.GetPipeTableWidth(MilestoneColumns);
        ConsoleHelper.WriteProjectLabel(project.Name, tableWidth);
        var rows = project.Milestones.Select(milestone => new (string Value, int Width)[]
        {
            ($"{milestone.SortOrder}.", MilestoneColumns[0].Width),
            (milestone.Title, MilestoneColumns[1].Width),
            (milestone.DueDate, MilestoneColumns[2].Width),
            (milestone.Status, MilestoneColumns[3].Width)
        });
        ConsoleHelper.WritePipeTable(MilestoneColumns, rows);
        Console.WriteLine();
    }

    private async Task AddMilestoneAsync(ProjectDetailDto project)
    {
        ConsoleHelper.WriteHeader("Add Milestone");
        var title = ConsoleHelper.ReadInput("Milestone Title");
        var dueDateInput = ConsoleHelper.ReadInput("Due Date (DD-MM-YYYY)");
        try
        {
            var message = await _projectApiClient.AddMilestoneAsync(project.Id, new CreateMilestoneRequest
            {
                Title = title,
                DueDate = DateValidator.ParseRequired(dueDateInput, "Due date"),
                SortOrder = 0
            });
            ConsoleHelper.WriteSuccess(message);
            ConsoleHelper.Pause();
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
            ConsoleHelper.Pause();
        }
    }

    private async Task UpdateMilestoneAsync(ProjectDetailDto project)
    {
        if (project.Milestones.Count == 0)
        {
            ConsoleHelper.WriteError("No milestones to update.");
            ConsoleHelper.Pause();
            return;
        }
        ConsoleHelper.WriteHeader("Update Milestone Status");
        Console.Write("Enter milestone number (from list): ");
        var input = Console.ReadLine()?.Trim();
        if (!int.TryParse(input, out var milestoneNumber))
        {
            ConsoleHelper.WriteError("Invalid milestone number.");
            ConsoleHelper.Pause();
            return;
        }
        var selectedMilestone = project.Milestones.FirstOrDefault(
            milestone => milestone.SortOrder == milestoneNumber || milestone.Id == milestoneNumber);
        if (selectedMilestone is null)
        {
            selectedMilestone = project.Milestones.ElementAtOrDefault(milestoneNumber - 1);
        }
        if (selectedMilestone is null)
        {
            ConsoleHelper.WriteError("Milestone not found.");
            ConsoleHelper.Pause();
            return;
        }
        Console.WriteLine($"Updating: {selectedMilestone.Title}");
        var title = ReadOptional("Title", selectedMilestone.Title);
        var dueDateInput = ReadOptional("Due Date", selectedMilestone.DueDate);
        Console.WriteLine("Status: (1) Not Started  (2) In Progress  (3) Done");
        Console.Write("Enter choice: ");
        var statusChoice = Console.ReadLine()?.Trim();
        if (statusChoice is not ("1" or "2" or "3"))
        {
            ConsoleHelper.WriteError("Invalid status.");
            ConsoleHelper.Pause();
            return;
        }
        try
        {
            var message = await _projectApiClient.UpdateMilestoneAsync(
                project.Id,
                selectedMilestone.Id,
                new UpdateMilestoneRequest
                {
                    Title = title,
                    DueDate = DateValidator.ParseRequired(dueDateInput, "Due date"),
                    Status = int.Parse(statusChoice),
                    SortOrder = selectedMilestone.SortOrder
                });
            ConsoleHelper.WriteSuccess(message);
            ConsoleHelper.Pause();
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
            ConsoleHelper.Pause();
        }
    }

    private static string ReadOptional(string label, string currentValue)
    {
        Console.Write($"{label} [{currentValue}] : ");
        var input = Console.ReadLine()?.Trim();
        return string.IsNullOrWhiteSpace(input) ? currentValue : input;
    }
}
