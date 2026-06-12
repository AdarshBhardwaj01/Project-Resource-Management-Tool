using PRM.Common.Helpers;
using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Manager;

namespace PRM.ConsoleUI.UI.Screens.Manager;

public class AllocateResourceScreen
{
    private static readonly (string Title, int Width)[] EndAllocationColumns =
    [
        ("#", 3),
        ("Employee", 16),
        ("%", 4),
        ("From", 9),
        ("To", 9)
    ];

    private static readonly (string Title, int Width)[] AiMatchColumns =
    [
        ("#", 3),
        ("Name", 16),
        ("Skills Match", 22),
        ("Availability", 12),
        ("Recent Activity", 16)
    ];

    private readonly ManagerApiClient _managerApiClient;

    public AllocateResourceScreen(ManagerApiClient managerApiClient)
    {
        _managerApiClient = managerApiClient;
    }

    public async Task ShowAsync()
    {
        while (true)
        {
            ConsoleHelper.WriteHeader("Allocate Resource");
            Console.WriteLine("1. Find resource using AI (recommended)");
            Console.WriteLine("2. Allocate directly (I already know who I want)");
            Console.WriteLine("3. End an existing allocation");
            Console.WriteLine("4. Back");
            Console.WriteLine();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    await ShowAiResourceSearchAsync();
                    break;
                case "2":
                    await ShowDirectAllocationAsync();
                    break;
                case "3":
                    await ShowEndAllocationAsync();
                    break;
                case "4":
                    return;
                default:
                    ConsoleHelper.WriteError("Invalid option.");
                    ConsoleHelper.Pause();
                    break;
            }
        }
    }

    private async Task ShowAiResourceSearchAsync()
    {
        while (true)
        {
            try
            {
                ConsoleHelper.ClearScreen();
                ConsoleHelper.WriteHeader("Find Resource using AI");
                Console.WriteLine();
                Console.WriteLine("Describe your requirement in plain English:");
                Console.Write("> ");
                var requirement = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(requirement))
                {
                    return;
                }
                Console.WriteLine();
                Console.WriteLine("Searching... (AI matching in progress)");
                var response = await _managerApiClient.GetSkillMatchAsync(new SkillMatchRequest
                {
                    Requirement = requirement,
                    SearchEntireOrganization = true,
                    RequireSingleEmployeeMatch = true,
                    MaxSuggestions = 1
                });
                Console.WriteLine();
                if (response.Suggestions.Count == 0)
                {
                    Console.WriteLine(
                        string.IsNullOrWhiteSpace(response.NoMatchReason)
                            ? "No matching employee was found in the organization."
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
                    "Note: Suggestions are AI-generated. Verify availability and skills before allocating.");
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

    private async Task ShowDirectAllocationAsync()
    {
        try
        {
            var projects = await _managerApiClient.GetMyProjectsAsync();
            if (projects.Count == 0)
            {
                ConsoleHelper.WriteHeader("Direct Allocation");
                Console.WriteLine("You have no assigned projects.");
                ConsoleHelper.Pause();
                return;
            }
            ConsoleHelper.WriteHeader("Direct Allocation");
            DisplayProjectOptions(projects);
            var projectIdInput = ConsoleHelper.ReadInput("Select Project");
            if (!int.TryParse(projectIdInput, out var projectId) || projectId <= 0)
            {
                ConsoleHelper.WriteError("Invalid Project ID.");
                ConsoleHelper.Pause();
                return;
            }
            var selectedProject = projects.FirstOrDefault(project => project.Id == projectId);
            if (selectedProject is null)
            {
                ConsoleHelper.WriteError("Project not found or not assigned to you.");
                ConsoleHelper.Pause();
                return;
            }
            Console.WriteLine($"Selected Project   : {selectedProject.Name} ({selectedProject.Id})");
            Console.WriteLine();
            var employeeIdInput = ConsoleHelper.ReadInput("Enter Resource ID");
            if (!int.TryParse(employeeIdInput, out var employeeId) || employeeId <= 0)
            {
                ConsoleHelper.WriteError("Invalid Resource ID.");
                ConsoleHelper.Pause();
                return;
            }
            await CompleteAllocationAsync(projectId, employeeId, selectedProject.Name);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
            ConsoleHelper.Pause();
        }
    }

    private async Task CompleteAllocationAsync(
        int projectId,
        int employeeId,
        string projectName,
        bool useAiLabels = false)
    {
        try
        {
            if (useAiLabels)
            {
                ConsoleHelper.ClearScreen();
                ConsoleHelper.WriteHeader("Allocate Resource");
            }
            Console.WriteLine($"Selected Project   : {projectName} ({projectId})");
            Console.WriteLine();
            var preview = await _managerApiClient.GetEmployeeUtilisationPreviewAsync(employeeId);
            var labelWidth = ConsoleHelper.GetPipeTableWidth(
                useAiLabels ? AiMatchColumns : EndAllocationColumns);
            Console.WriteLine();
            ConsoleHelper.WriteProjectLabel(preview.FullName, labelWidth);
            Console.WriteLine(
                $"Current Utilisation: {preview.CurrentUtilisationPercent}% ({preview.UtilisationNote})");
            Console.WriteLine();
            Console.WriteLine("Set Allocation:");
            var utilisationInput = useAiLabels
                ? ConsoleHelper.ReadFormField("Utilisation %")
                : ConsoleHelper.ReadInput("Utilisation %");
            if (!int.TryParse(utilisationInput, out var utilisationPercent)
                || utilisationPercent <= 0
                || utilisationPercent > 100)
            {
                ConsoleHelper.WriteError("Utilisation must be between 1 and 100.");
                ConsoleHelper.Pause();
                return;
            }
            var fromDateInput = ConsoleHelper.ReadFormField("From Date", "DD-MM-YYYY");
            var toDateInput = ConsoleHelper.ReadFormField("To Date", "DD-MM-YYYY");
            if (string.IsNullOrWhiteSpace(fromDateInput))
            {
                ConsoleHelper.WriteError("From date is required.");
                ConsoleHelper.Pause();
                return;
            }
            if (string.IsNullOrWhiteSpace(toDateInput))
            {
                ConsoleHelper.WriteError("To date is required.");
                ConsoleHelper.Pause();
                return;
            }
            DateTime fromDate;
            DateTime toDate;
            try
            {
                fromDate = DateValidator.ParseRequired(fromDateInput, "From date");
                toDate = DateValidator.ParseRequired(toDateInput, "To date");
            }
            catch (PRM.Common.Exceptions.BusinessValidationException ex)
            {
                ConsoleHelper.WriteError(ex.Message);
                ConsoleHelper.Pause();
                return;
            }
            var request = new CreateAllocationRequest
            {
                ProjectId = projectId,
                EmployeeId = employeeId,
                UtilisationPercent = utilisationPercent,
                FromDate = fromDate,
                ToDate = toDate
            };
            Console.WriteLine();
            Console.WriteLine("Validating...");
            var validation = await _managerApiClient.ValidateAllocationAsync(request);
            var validationStatus = validation.IsValid
                ? useAiLabels ? "✓ Valid" : "Valid"
                : useAiLabels ? "Invalid" : "Invalid";
            Console.WriteLine(
                $"{validation.EmployeeName} total in this period: " +
                $"{validation.CurrentUtilisation}% + {validation.ProposedUtilisation}% = " +
                $"{validation.TotalUtilisation}%  {validationStatus}");
            if (!validation.IsValid)
            {
                ConsoleHelper.Pause();
                return;
            }
            ConsoleHelper.WriteSeparator();
            ConsoleHelper.WriteActions(
                ("C", useAiLabels ? "Confirm Allocation" : "Confirm"),
                ("B", "Back"));
            var action = ConsoleHelper.ReadActionChoice();
            if (action == "B")
            {
                return;
            }
            if (action != "C")
            {
                ConsoleHelper.WriteError("Invalid choice.");
                ConsoleHelper.Pause();
                return;
            }
            var message = await _managerApiClient.AllocateResourceAsync(request);
            ConsoleHelper.WriteSuccess(message);
            ConsoleHelper.Pause();
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
            ConsoleHelper.Pause();
        }
    }

    private async Task ShowEndAllocationAsync()
    {
        try
        {
            var projects = await _managerApiClient.GetMyProjectsAsync();
            if (projects.Count == 0)
            {
                ConsoleHelper.WriteHeader("End Allocation");
                Console.WriteLine("You have no assigned projects.");
                ConsoleHelper.Pause();
                return;
            }
            ConsoleHelper.WriteHeader("End Allocation");
            DisplayProjectOptions(projects);
            var projectIdInput = ConsoleHelper.ReadInput("Select Project");
            if (!int.TryParse(projectIdInput, out var projectId) || projectId <= 0)
            {
                ConsoleHelper.WriteError("Invalid Project ID.");
                ConsoleHelper.Pause();
                return;
            }
            var selectedProject = projects.FirstOrDefault(project => project.Id == projectId);
            if (selectedProject is null)
            {
                ConsoleHelper.WriteError("Project not found or not assigned to you.");
                ConsoleHelper.Pause();
                return;
            }
            var allocations = await _managerApiClient.GetProjectActiveAllocationsAsync(projectId);
            Console.WriteLine();
            Console.WriteLine($"Selected Project   : {selectedProject.Name} ({selectedProject.Id})");
            Console.WriteLine();
            if (allocations.Count == 0)
            {
                Console.WriteLine("No active allocations on this project.");
                ConsoleHelper.Pause();
                return;
            }
            var rows = allocations.Select(allocation => new (string Value, int Width)[]
            {
                ($"{allocation.RowNumber}.", EndAllocationColumns[0].Width),
                (allocation.EmployeeName, EndAllocationColumns[1].Width),
                ($"{allocation.UtilisationPercent}%", EndAllocationColumns[2].Width),
                (allocation.FromDate, EndAllocationColumns[3].Width),
                (allocation.ToDate, EndAllocationColumns[4].Width)
            });
            ConsoleHelper.WritePipeTable(EndAllocationColumns, rows);
            Console.WriteLine();
            Console.Write("Select allocation to end: ");
            var selectionInput = Console.ReadLine()?.Trim();
            if (!int.TryParse(selectionInput, out var selectionNumber))
            {
                ConsoleHelper.WriteError("Invalid selection.");
                ConsoleHelper.Pause();
                return;
            }
            var selectedAllocation = allocations.FirstOrDefault(
                allocation => allocation.RowNumber == selectionNumber);
            if (selectedAllocation is null)
            {
                ConsoleHelper.WriteError("Allocation not found.");
                ConsoleHelper.Pause();
                return;
            }
            var today = DateTime.Now.ToString("dd-MMM-yyyy");
            Console.WriteLine();
            Console.WriteLine(
                $"End {selectedAllocation.EmployeeName}'s allocation on {selectedProject.Name}?");
            Console.WriteLine($"Set end date to today ({today})?");
            Console.WriteLine();
            ConsoleHelper.WriteActions(("Y", "Yes, End Now"), ("B", "Back"));
            var confirm = ConsoleHelper.ReadActionChoice();
            if (confirm != "Y")
            {
                return;
            }
            var message = await _managerApiClient.EndAllocationAsync(selectedAllocation.Id);
            ConsoleHelper.WriteSuccess(message);
            Console.WriteLine("Employee status updated to BENCH if no other active allocations remain.");
            ConsoleHelper.Pause();
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
            ConsoleHelper.Pause();
        }
    }

    private static void DisplayProjectOptions(IReadOnlyList<ManagerProjectItemDto> projects)
    {
        Console.WriteLine("Your Projects:");
        Console.WriteLine($"{"ID",-4}| {"Name",-16}| {"Status",-8}| {"End Date"}");
        ConsoleHelper.WriteSeparator();
        foreach (var project in projects)
        {
            Console.WriteLine(
                $"{project.Id,-4}| {Truncate(project.Name, 16),-16}| {project.Status,-8}| {project.EndDate}");
        }
        ConsoleHelper.WriteSeparator();
        Console.WriteLine();
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
