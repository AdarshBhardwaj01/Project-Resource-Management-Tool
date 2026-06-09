using PRM.Common.Helpers;
using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Manager;

namespace PRM.ConsoleUI.UI.Screens.Manager;

public class TeamTimesheetsScreen
{
    private static readonly (string Title, int Width)[] SummaryColumns =
    [
        ("Employee", 16),
        ("Project", 16),
        ("Hrs", 4),
        ("Status", 0)
    ];

    private static readonly (string Title, int Width)[] DetailColumns =
    [
        ("Project", 16),
        ("Hrs", 4),
        ("Activity Tags", 0)
    ];

    private readonly ManagerApiClient _managerApiClient;

    public TeamTimesheetsScreen(ManagerApiClient managerApiClient)
    {
        _managerApiClient = managerApiClient;
    }

    public async Task ShowAsync()
    {
        while (true)
        {
            try
            {
                ConsoleHelper.WriteHeader("Timesheets - My Team");
                Console.WriteLine("Filter by week (DD-MM-YYYY) or press Enter for current week:");
                Console.Write("Week: ");
                var weekInput = Console.ReadLine()?.Trim();

                DateTime? weekStartDate = null;

                if (!string.IsNullOrWhiteSpace(weekInput))
                {
                    weekStartDate = DateValidator.ParseRequired(weekInput, "Week");
                }

                var timesheets = await _managerApiClient.GetTeamTimesheetsAsync(weekStartDate);
                Console.WriteLine();
                Console.WriteLine($"Week: {timesheets.WeekStartDate}");
                Console.WriteLine();

                if (timesheets.Rows.Count == 0)
                {
                    Console.WriteLine("No team timesheets found for this week.");
                }
                else
                {
                    WriteSummaryTable(timesheets.Rows);
                }

                Console.WriteLine();
                ConsoleHelper.WriteActions(("V", "View employee timesheet detail"), ("B", "Back"));
                var choice = ConsoleHelper.ReadActionChoice();

                if (choice == "B" || string.IsNullOrWhiteSpace(choice))
                {
                    return;
                }

                if (choice == "V")
                {
                    if (timesheets.Rows.Count == 0)
                    {
                        ConsoleHelper.WriteError("No timesheets available to view.");
                        ConsoleHelper.Pause();
                        continue;
                    }

                    await ShowEmployeeDetailAsync(timesheets.Rows, weekStartDate);
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

    private async Task ShowEmployeeDetailAsync(
        IReadOnlyList<ManagerTeamTimesheetRowDto> rows,
        DateTime? weekStartDate)
    {
        Console.WriteLine();
        Console.Write("Enter employee name: ");
        var employeeName = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(employeeName))
        {
            return;
        }

        var employee = rows.FirstOrDefault(row =>
            row.EmployeeName.Equals(employeeName, StringComparison.OrdinalIgnoreCase));

        if (employee is null)
        {
            ConsoleHelper.WriteError("Employee not found.");
            ConsoleHelper.Pause();
            return;
        }

        try
        {
            var detail = await _managerApiClient.GetEmployeeTimesheetDetailAsync(
                employee.EmployeeId,
                weekStartDate);

            ConsoleHelper.ClearScreen();
            ConsoleHelper.WriteHeader("Employee Timesheet Detail");
            Console.WriteLine($"Employee           : {detail.EmployeeName}");
            Console.WriteLine($"Week               : {detail.WeekStartDate}");
            Console.Write("Status             : ");
            WriteTimesheetStatus(detail.Status);
            Console.WriteLine();
            Console.WriteLine($"Total Hours        : {detail.TotalHours}");
            Console.WriteLine();
            Console.WriteLine("Project Breakdown:");

            var detailRows = detail.Entries.Select(entry => new (string Value, int Width)[]
            {
                (entry.ProjectName, DetailColumns[0].Width),
                (entry.Hours.ToString(), DetailColumns[1].Width),
                (entry.ActivityTags, DetailColumns[2].Width)
            });

            ConsoleHelper.WritePipeTable(DetailColumns, detailRows);
            Console.WriteLine();
            ConsoleHelper.Pause();
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
            ConsoleHelper.Pause();
        }
    }

    private static void WriteSummaryTable(IReadOnlyList<ManagerTeamTimesheetRowDto> rows)
    {
        ConsoleHelper.WritePipeTableHeader(SummaryColumns);
        var tableWidth = ConsoleHelper.GetPipeTableWidth(SummaryColumns);
        Console.WriteLine(new string('-', tableWidth));

        foreach (var row in rows)
        {
            Console.Write(ConsoleHelper.FormatPipeTableCells(
                (row.EmployeeName, SummaryColumns[0].Width),
                (row.ProjectName, SummaryColumns[1].Width),
                (row.Hours.ToString(), SummaryColumns[2].Width)));
            Console.Write(" | ");
            WriteTimesheetStatus(row.Status, SummaryColumns[3].Width);
            Console.WriteLine();
        }

        Console.WriteLine(new string('-', tableWidth));
    }

    private static void WriteTimesheetStatus(string status, int? padWidth = null)
    {
        if (status == "MISSED")
        {
            const string missedText = "MISSED \u26a0";
            Console.Write(missedText);

            if (padWidth.HasValue && missedText.Length < padWidth.Value)
            {
                Console.Write(new string(' ', padWidth.Value - missedText.Length));
            }

            return;
        }

        var text = status;

        if (padWidth.HasValue && text.Length < padWidth.Value)
        {
            Console.Write(text.PadRight(padWidth.Value));
        }
        else
        {
            Console.Write(text);
        }
    }
}
