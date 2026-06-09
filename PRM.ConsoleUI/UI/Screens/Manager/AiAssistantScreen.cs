using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Manager;

namespace PRM.ConsoleUI.UI.Screens.Manager;

public class AiAssistantScreen
{
    private readonly ManagerApiClient _managerApiClient;
    private readonly AllocateResourceScreen _allocateResourceScreen;

    public AiAssistantScreen(
        ManagerApiClient managerApiClient,
        AllocateResourceScreen allocateResourceScreen)
    {
        _managerApiClient = managerApiClient;
        _allocateResourceScreen = allocateResourceScreen;
    }

    public async Task ShowAsync()
    {
        while (true)
        {
            ConsoleHelper.WriteHeader("AI Assistant");
            Console.WriteLine("1. Skill Match — Find best employees for a project requirement");
            Console.WriteLine("2. Risk Summary — Get a health analysis for a project");
            Console.WriteLine("3. Back");
            Console.WriteLine();
            Console.Write("Enter option: ");

            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    await ShowSkillMatchAsync();
                    break;
                case "2":
                    await ShowRiskSummaryAsync();
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

    private async Task ShowSkillMatchAsync()
    {
        while (true)
        {
            try
            {
                ConsoleHelper.ClearScreen();
                ConsoleHelper.WriteSectionHeader("Skill Match");
                Console.WriteLine();
                Console.WriteLine("Describe your project requirement in plain English:");
                Console.Write("> ");
                var requirement = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(requirement))
                {
                    return;
                }

                Console.WriteLine();
                Console.WriteLine("Searching... (calling AI)");

                var response = await _managerApiClient.GetSkillMatchAsync(new SkillMatchRequest
                {
                    Requirement = requirement
                });

                Console.WriteLine();

                if (response.Suggestions.Count == 0)
                {
                    Console.WriteLine(
                        string.IsNullOrWhiteSpace(response.NoMatchReason)
                            ? "No matching employees were found on your team."
                            : response.NoMatchReason);
                }
                else
                {
                    foreach (var suggestion in response.Suggestions)
                    {
                        Console.WriteLine($"{suggestion.RowNumber}. {suggestion.EmployeeName}");
                        Console.WriteLine($"   Skills Match   : {suggestion.SkillsMatch}");
                        Console.WriteLine($"   Availability   : {suggestion.Availability}");
                        Console.WriteLine($"   Reason         : {suggestion.Reason}");
                    }
                }

                Console.WriteLine();
                Console.WriteLine(
                    "Note: These are AI-generated suggestions. Always verify availability " +
                    "and skills with the employee before allocating.");
                Console.WriteLine();
                ConsoleHelper.WriteActions(("A", "Go to Allocate Resource"), ("B", "Back"));
                var choice = ConsoleHelper.ReadActionChoice();

                if (choice == "B" || string.IsNullOrWhiteSpace(choice))
                {
                    return;
                }

                if (choice == "A")
                {
                    await _allocateResourceScreen.ShowAsync();
                    return;
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

    private async Task ShowRiskSummaryAsync()
    {
        while (true)
        {
            try
            {
                ConsoleHelper.ClearScreen();
                ConsoleHelper.WriteSectionHeader("Risk Summary");
                Console.WriteLine();
                Console.WriteLine("Select project:");

                var projects = await _managerApiClient.GetMyProjectsAsync();

                if (projects.Count == 0)
                {
                    Console.WriteLine("(none)");
                    ConsoleHelper.Pause();
                    return;
                }

                ConsoleHelper.WriteProjectHealthSelectionTable(
                    projects.Select(project => (project.RowNumber, project.Name, project.HealthStatus)));

                Console.WriteLine();
                Console.Write("Enter project number: ");
                var input = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(input))
                {
                    return;
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

                Console.WriteLine();
                Console.WriteLine("Generating AI summary...");

                var summary = await _managerApiClient.GetProjectRiskSummaryAsync(selectedProject.Id);

                ConsoleHelper.ClearScreen();
                ConsoleHelper.WriteAiRiskSummaryContent(summary.ProjectName, summary.Summary);
                Console.WriteLine();
                ConsoleHelper.WriteActions(("B", "Back"));
                var choice = ConsoleHelper.ReadActionChoice();

                if (choice == "B" || string.IsNullOrWhiteSpace(choice))
                {
                    return;
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
}
