using PRM.Common.Helpers;
using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Projects;

namespace PRM.ConsoleUI.UI.Screens.Projects;

public class UpdateProjectScreen
{
    private readonly ProjectApiClient _projectApiClient;

    public UpdateProjectScreen(ProjectApiClient projectApiClient)
    {
        _projectApiClient = projectApiClient;
    }

    public async Task ShowAsync()
    {
        ConsoleHelper.WriteHeader("Update Project");
        var projectIdInput = ConsoleHelper.ReadInput("Project ID");
        if (!int.TryParse(projectIdInput, out var projectId))
        {
            ConsoleHelper.WriteError("Invalid Project ID.");
            ConsoleHelper.Pause();
            return;
        }
        try
        {
            var project = await _projectApiClient.GetProjectAsync(projectId);
            Console.WriteLine();
            Console.WriteLine($"Updating: {project.Name} ({project.Status})");
            Console.WriteLine("Leave a field blank to keep the current value.");
            Console.WriteLine();
            var name = ReadOptional("Project Name", project.Name);
            var description = ReadOptional("Description", project.Description);
            var startDate = ReadOptionalDate("Start Date", project.StartDate);
            var endDate = ReadOptionalDate("End Date", project.EndDate);
            var status = ReadOptionalStatus(project.Status);
            ConsoleHelper.WriteSeparator();
            Console.WriteLine("[S] Save     [B] Back");
            Console.Write("Enter choice: ");
            var action = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (action == "B")
            {
                return;
            }
            if (action != "S")
            {
                ConsoleHelper.WriteError("Invalid choice.");
                ConsoleHelper.Pause();
                return;
            }
            var message = await _projectApiClient.UpdateProjectAsync(projectId, new UpdateProjectRequest
            {
                Name = name,
                Description = description,
                StartDate = startDate,
                EndDate = endDate,
                Status = status
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

    private static DateTime ReadOptionalDate(string label, string currentValue)
    {
        Console.Write($"{label} [{currentValue}] : ");
        var input = Console.ReadLine()?.Trim();
        return string.IsNullOrWhiteSpace(input)
            ? DateValidator.ParseRequired(currentValue, label)
            : DateValidator.ParseRequired(input, label);
    }

    private static int ReadOptionalStatus(string currentStatus)
    {
        Console.WriteLine($"Current Status: {currentStatus}");
        Console.WriteLine("Status: (1) Planned  (2) Active  (3) On Hold  [Enter] Keep current");
        Console.Write("Enter choice: ");
        var statusChoice = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(statusChoice))
        {
            return currentStatus switch
            {
                "PLANNED" => 1,
                "ACTIVE" => 2,
                "ON_HOLD" => 3,
                _ => 1
            };
        }
        if (statusChoice is not ("1" or "2" or "3"))
        {
            throw new InvalidOperationException("Invalid status selected.");
        }
        return int.Parse(statusChoice);
    }
}
