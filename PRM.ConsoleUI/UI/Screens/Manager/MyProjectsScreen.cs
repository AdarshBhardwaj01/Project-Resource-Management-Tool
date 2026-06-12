using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Manager;

namespace PRM.ConsoleUI.UI.Screens.Manager;

public class MyProjectsScreen
{
    private static readonly (string Title, int Width)[] ListColumns =
    [
        ("#", 3),
        ("Project", 16),
        ("End Date", 9),
        ("Health", 16)
    ];

    private static readonly (string Title, int Width)[] MilestoneColumns =
    [
        ("#", 3),
        ("Title", 16),
        ("Due Date", 9),
        ("Status", 0)
    ];

    private static readonly (string Title, int Width)[] AllocationColumns =
    [
        ("Name", 16),
        ("%", 4),
        ("From", 9),
        ("To", 9)
    ];

    private readonly ManagerApiClient _managerApiClient;

    public MyProjectsScreen(ManagerApiClient managerApiClient)
    {
        _managerApiClient = managerApiClient;
    }

    public async Task ShowAsync()
    {
        while (true)
        {
            try
            {
                ConsoleHelper.WriteHeader("My Projects");
                var projects = await _managerApiClient.GetMyProjectsAsync();
                if (projects.Count == 0)
                {
                    Console.WriteLine("You have no assigned projects.");
                    ConsoleHelper.Pause();
                    return;
                }
                WriteProjectListTable(projects);
                Console.WriteLine();
                ConsoleHelper.WriteActions(("S", "Select project number to view details"), ("B", "Back"));
                var choice = ConsoleHelper.ReadActionChoice();
                if (choice == "B" || string.IsNullOrWhiteSpace(choice))
                {
                    return;
                }
                if (choice != "S")
                {
                    ConsoleHelper.WriteError("Invalid option.");
                    ConsoleHelper.Pause();
                    continue;
                }
                Console.Write("Enter project number: ");
                var input = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }
                if (!int.TryParse(input, out var selectedRow) || selectedRow <= 0)
                {
                    ConsoleHelper.WriteError("Invalid selection.");
                    ConsoleHelper.Pause();
                    continue;
                }
                var selectedProject = projects.FirstOrDefault(project => project.RowNumber == selectedRow);
                if (selectedProject is null)
                {
                    ConsoleHelper.WriteError("Project not found.");
                    ConsoleHelper.Pause();
                    continue;
                }
                ConsoleHelper.ClearScreen();
                await ShowProjectDetailAsync(selectedProject.Id);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError(ex.Message);
                ConsoleHelper.Pause();
                return;
            }
        }
    }

    private async Task ShowProjectDetailAsync(int projectId)
    {
        while (true)
        {
            try
            {
                ConsoleHelper.ClearScreen();
                var project = await _managerApiClient.GetMyProjectDetailAsync(projectId);
                var labelWidth = ConsoleHelper.GetPipeTableWidth(MilestoneColumns);
                ConsoleHelper.WriteProjectLabel(project.Name, labelWidth);
                Console.Write("Health Status      : ");
                ConsoleHelper.WriteHealthStatus(project.HealthStatus);
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Risk Flags:");
                if (project.RiskFlags.Count == 0)
                {
                    Console.WriteLine("(none)");
                }
                else
                {
                    foreach (var flag in project.RiskFlags)
                    {
                        var marker = flag.IsPositive ? "\u2713" : "X";
                        Console.WriteLine($"  {marker} {flag.Message}");
                    }
                }
                Console.WriteLine();
                Console.WriteLine("Milestones:");
                var milestoneRows = project.Milestones.Select(milestone => new (string Value, int Width)[]
                {
                    ($"{milestone.RowNumber}.", MilestoneColumns[0].Width),
                    (milestone.Title, MilestoneColumns[1].Width),
                    (milestone.DueDate, MilestoneColumns[2].Width),
                    (milestone.Status, MilestoneColumns[3].Width)
                });
                ConsoleHelper.WritePipeTable(MilestoneColumns, milestoneRows);
                Console.WriteLine();
                Console.WriteLine("Allocated Resources:");
                var allocationRows = project.Allocations.Select(allocation => new (string Value, int Width)[]
                {
                    (allocation.EmployeeName, AllocationColumns[0].Width),
                    ($"{allocation.UtilisationPercent}%", AllocationColumns[1].Width),
                    (allocation.FromDate, AllocationColumns[2].Width),
                    (allocation.ToDate, AllocationColumns[3].Width)
                });
                ConsoleHelper.WritePipeTable(AllocationColumns, allocationRows);
                Console.WriteLine();
                ConsoleHelper.WriteActions(("A", "Get AI Risk Summary"), ("B", "Back"));
                var choice = ConsoleHelper.ReadActionChoice();
                if (choice == "B" || string.IsNullOrWhiteSpace(choice))
                {
                    return;
                }
                if (choice == "A")
                {
                    await ShowAiRiskSummaryAsync(projectId, project.Name);
                    continue;
                }
                ConsoleHelper.WriteError("Invalid option.");
                ConsoleHelper.Pause();
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError(ex.Message);
                ConsoleHelper.Pause();
                return;
            }
        }
    }

    private static void WriteProjectListTable(IReadOnlyList<ManagerProjectItemDto> projects)
    {
        ConsoleHelper.WritePipeTableHeader(ListColumns);
        var tableWidth = ConsoleHelper.GetPipeTableWidth(ListColumns);
        Console.WriteLine(new string('-', tableWidth));
        foreach (var project in projects)
        {
            Console.Write(ConsoleHelper.FormatPipeTableCells(
                (project.RowNumber.ToString(), ListColumns[0].Width),
                (project.Name, ListColumns[1].Width),
                (project.EndDate, ListColumns[2].Width)));
            Console.Write(" | ");
            ConsoleHelper.WriteHealthStatus(project.HealthStatus, ListColumns[3].Width);
            Console.WriteLine();
        }
        Console.WriteLine(new string('-', tableWidth));
    }

    private async Task ShowAiRiskSummaryAsync(int projectId, string projectName)
    {
        try
        {
            ConsoleHelper.ClearScreen();
            ConsoleHelper.WriteSectionHeader($"AI Risk Summary - {projectName}");
            Console.WriteLine();
            Console.WriteLine("Generating AI summary...");
            var summary = await _managerApiClient.GetProjectRiskSummaryAsync(projectId);
            ConsoleHelper.ClearScreen();
            ConsoleHelper.WriteAiRiskSummaryContent(summary.ProjectName, summary.Summary);
            Console.WriteLine();
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
}
