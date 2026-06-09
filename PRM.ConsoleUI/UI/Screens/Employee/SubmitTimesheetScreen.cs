using PRM.Common.Helpers;
using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.EmployeePortal;

namespace PRM.ConsoleUI.UI.Screens.Employee;

public class SubmitTimesheetScreen
{
    private static readonly string[] ActivityTagOptions =
    [
        "Backend API Development",
        "Microservices / Architecture",
        "Database Design & Queries",
        "WebSocket / Real-time Features",
        "Frontend Development",
        "Code Review / Mentoring",
        "Bug Fixing",
        "DevOps / Deployment",
        "Testing & QA",
        "Documentation",
        "Other (type manually)"
    ];

    private readonly EmployeePortalApiClient _employeePortalApiClient;
    private readonly AuthSession _session;

    public SubmitTimesheetScreen(
        EmployeePortalApiClient employeePortalApiClient,
        AuthSession session)
    {
        _employeePortalApiClient = employeePortalApiClient;
        _session = session;
    }

    public async Task ShowAsync()
    {
        try
        {
            ConsoleHelper.WriteHeader("Submit Timesheet");
            Console.WriteLine($"Employee   : {_session.FullName}");
            Console.WriteLine("Week Start : Enter date (DD-MM-YYYY) or press Enter for last Monday.");
            Console.Write("> ");
            var weekInput = Console.ReadLine()?.Trim();

            string? weekStartParam = null;

            if (!string.IsNullOrWhiteSpace(weekInput))
            {
                var parsedDate = DateValidator.ParseRequired(weekInput, "Week start date");
                weekStartParam = parsedDate.ToString("dd-MM-yyyy");
            }

            Console.WriteLine();
            Console.WriteLine("Checking your active allocations for this week...");

            var preview = await _employeePortalApiClient.GetTimesheetSubmitPreviewAsync(weekStartParam);

            if (preview.AlreadySubmitted)
            {
                Console.WriteLine();
                ConsoleHelper.WriteError("Timesheet for this week has already been submitted.");
                ConsoleHelper.Pause();
                return;
            }

            if (preview.Projects.Count == 0)
            {
                Console.WriteLine();
                Console.WriteLine("You have no project allocations for this week.");
                ConsoleHelper.Pause();
                return;
            }

            Console.WriteLine();
            var draftEntries = new List<DraftTimesheetEntry>();

            for (var index = 0; index < preview.Projects.Count; index++)
            {
                var project = preview.Projects[index];
                WriteProjectSection(index + 1, preview.Projects.Count, project);

                var hoursInput = ConsoleHelper.ReadKeyedPrompt("B", "Hours worked this week: ");

                if (hoursInput.Equals("B", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(hoursInput))
                {
                    ConsoleHelper.WriteError("Hours are required for each allocated project.");
                    ConsoleHelper.Pause();
                    return;
                }

                if (!decimal.TryParse(hoursInput, out var hours) || hours < 0)
                {
                    ConsoleHelper.WriteError("Invalid hours.");
                    ConsoleHelper.Pause();
                    return;
                }

                if (hours > project.ExpectedMaxHours)
                {
                    ConsoleHelper.WriteError(
                        $"Hours cannot exceed {project.ExpectedMaxHours} for this allocation.");
                    ConsoleHelper.Pause();
                    return;
                }

                WriteActivityTagMenu();
                Console.Write("Select tags (comma-separated): ");
                var tagInput = Console.ReadLine()?.Trim() ?? string.Empty;
                var activityTags = ResolveActivityTags(tagInput);

                if (string.IsNullOrWhiteSpace(activityTags))
                {
                    ConsoleHelper.WriteError("Select at least one activity tag.");
                    ConsoleHelper.Pause();
                    return;
                }

                draftEntries.Add(new DraftTimesheetEntry
                {
                    ProjectId = project.ProjectId,
                    ProjectName = project.ProjectName,
                    Hours = hours,
                    ActivityTags = activityTags,
                    DisplayTags = FormatDisplayTags(activityTags)
                });

                Console.WriteLine();
            }

            WriteSummary(preview, draftEntries);
            ConsoleHelper.WriteActions(("S", "Submit Timesheet"), ("B", "Back"));
            var choice = ConsoleHelper.ReadActionChoice();

            if (choice == "B" || string.IsNullOrWhiteSpace(choice))
            {
                return;
            }

            if (choice != "S")
            {
                ConsoleHelper.WriteError("Invalid option.");
                ConsoleHelper.Pause();
                return;
            }

            var message = await _employeePortalApiClient.SubmitTimesheetAsync(new SubmitTimesheetRequest
            {
                WeekStartDate = preview.WeekStartDate,
                Entries = draftEntries
                    .Select(entry => new SubmitTimesheetEntryRequest
                    {
                        ProjectId = entry.ProjectId,
                        Hours = entry.Hours,
                        ActivityTags = entry.ActivityTags
                    })
                    .ToList()
            });

            Console.WriteLine();
            Console.WriteLine($"{message} \u2713");
            ConsoleHelper.Pause();
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
            ConsoleHelper.Pause();
        }
        finally
        {
            ConsoleHelper.EndScreenSession();
        }
    }

    private static void WriteProjectSection(
        int projectNumber,
        int totalProjects,
        TimesheetSubmitProjectItemDto project)
    {
        ConsoleHelper.WriteSeparator();
        Console.WriteLine($"PROJECT {projectNumber} OF {totalProjects} — {project.ProjectName}");
        ConsoleHelper.WriteSeparator();
        Console.WriteLine(
            $"Allocation: {project.UtilisationPercent}% | Expected: {project.ExpectedMaxHours} hrs max");
        ConsoleHelper.WriteSeparator();
        Console.WriteLine();
    }

    private static void WriteActivityTagMenu()
    {
        Console.WriteLine("Activity Tags (select one or more):");

        for (var index = 0; index < ActivityTagOptions.Length; index++)
        {
            Console.WriteLine($"{index + 1,2}. {ActivityTagOptions[index]}");
        }

        Console.WriteLine();
    }

    private static string ResolveActivityTags(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var selectedTags = new List<string>();

        foreach (var part in input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, out var selection) || selection <= 0 || selection > ActivityTagOptions.Length)
            {
                continue;
            }

            if (selection == ActivityTagOptions.Length)
            {
                Console.Write("Enter custom tag: ");
                var customTag = Console.ReadLine()?.Trim();

                if (!string.IsNullOrWhiteSpace(customTag))
                {
                    selectedTags.Add(customTag);
                }

                continue;
            }

            selectedTags.Add(ActivityTagOptions[selection - 1]);
        }

        return string.Join(", ", selectedTags.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string FormatDisplayTags(string activityTags)
    {
        return string.Join(", ", activityTags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(tag => tag switch
            {
                "Microservices / Architecture" => "Microservices",
                "Database Design & Queries" => "Database Design",
                "WebSocket / Real-time Features" => "WebSocket",
                "Code Review / Mentoring" => "Code Review",
                "Backend API Development" => "Backend API",
                _ => tag
            }));
    }

    private static void WriteSummary(
        TimesheetSubmitPreviewResponse preview,
        IReadOnlyList<DraftTimesheetEntry> entries)
    {
        ConsoleHelper.WriteSeparator();
        Console.WriteLine("SUMMARY");
        ConsoleHelper.WriteSeparator();

        foreach (var entry in entries)
        {
            Console.WriteLine(
                $"{entry.ProjectName,-16}{entry.Hours,4:0} hrs    [{entry.DisplayTags}]");
        }

        ConsoleHelper.WriteSeparator();

        var totalHours = entries.Sum(entry => entry.Hours);
        var withinLimit = totalHours <= preview.MaxWeeklyHours;
        var marker = withinLimit ? " \u2713" : string.Empty;

        Console.WriteLine(
            $"{"Total",-16}{totalHours,4:0} hrs / {preview.MaxWeeklyHours} hrs max{marker}");
        Console.WriteLine();
    }

    private sealed class DraftTimesheetEntry
    {
        public int ProjectId { get; init; }

        public string ProjectName { get; init; } = string.Empty;

        public decimal Hours { get; init; }

        public string ActivityTags { get; init; } = string.Empty;

        public string DisplayTags { get; init; } = string.Empty;
    }
}
