using PRM.Common.Helpers;
using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Projects;

namespace PRM.ConsoleUI.UI.Screens.Projects;

public class CreateProjectScreen
{
    private readonly ProjectApiClient _projectApiClient;

    public CreateProjectScreen(ProjectApiClient projectApiClient)
    {
        _projectApiClient = projectApiClient;
    }

    public async Task ShowAsync()
    {
        ConsoleHelper.WriteHeader("Create Project");
        var name = ConsoleHelper.ReadFormField("Project Name");
        var description = ConsoleHelper.ReadFormField("Description");
        var startDateInput = ConsoleHelper.ReadFormField("Start Date", "DD-MM-YYYY");
        var endDateInput = ConsoleHelper.ReadFormField("End Date", "DD-MM-YYYY");
        var statusChoice = ConsoleHelper.ReadFormChoice(
            "Status",
            "(1) PLANNED  (2) ACTIVE  (3) ON_HOLD");
        if (string.IsNullOrWhiteSpace(statusChoice))
        {
            statusChoice = "1";
        }
        if (statusChoice is not ("1" or "2" or "3"))
        {
            ConsoleHelper.WriteError("Invalid status selected.");
            ConsoleHelper.Pause();
            return;
        }
        var managerIdInput = ConsoleHelper.ReadFormField("Assign Manager", "Enter Manager ID");
        if (!int.TryParse(managerIdInput, out var managerId) || managerId <= 0)
        {
            ConsoleHelper.WriteError("Invalid Manager ID.");
            ConsoleHelper.Pause();
            return;
        }
        ConsoleHelper.WriteSeparator();
        ConsoleHelper.WriteActions(("S", "Save"), ("B", "Back"));
        var action = ConsoleHelper.ReadActionChoice();
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
        try
        {
            var message = await _projectApiClient.CreateProjectAsync(new CreateProjectRequest
            {
                Name = name,
                Description = description,
                StartDate = DateValidator.ParseRequired(startDateInput, "Start date"),
                EndDate = DateValidator.ParseRequired(endDateInput, "End date"),
                ManagerId = managerId,
                Status = int.Parse(statusChoice)
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
}
