using System.Globalization;
using PRM.Common.Exceptions;
using PRM.Common.Helpers;
using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.EmployeePortal;

namespace PRM.ConsoleUI.UI.Screens.Employee;

public class ViewMyTimesheetsScreen
{
    private static readonly (string Title, int Width)[] HistoryColumns =
    [
        ("Week Start", 12),
        ("Total Hrs", 9),
        ("Status", 0)
    ];

    private static readonly (string Title, int Width)[] DetailColumns =
    [
        ("Project", 16),
        ("Hrs", 4),
        ("Activity Tags", 0)
    ];

    private readonly EmployeePortalApiClient _employeePortalApiClient;

    public ViewMyTimesheetsScreen(EmployeePortalApiClient employeePortalApiClient)
    {
        _employeePortalApiClient = employeePortalApiClient;
    }

    public async Task ShowAsync()
    {
        try
        {
            await ShowTimesheetsLoopAsync();
        }
        finally
        {
            ConsoleHelper.EndScreenSession();
        }
    }

    private async Task ShowTimesheetsLoopAsync()
    {
        while (true)
        {
            try
            {
                ConsoleHelper.WriteHeader("My Timesheets");
                var timesheets = await _employeePortalApiClient.GetMyTimesheetsAsync();

                if (timesheets.Count == 0)
                {
                    Console.WriteLine("You have not submitted any timesheets yet.");
                    Console.WriteLine();
                    ConsoleHelper.WriteActions(("B", "Back"));
                    var emptyChoice = ConsoleHelper.ReadActionChoice();

                    if (emptyChoice == "B" || string.IsNullOrWhiteSpace(emptyChoice))
                    {
                        return;
                    }

                    ConsoleHelper.WriteError("Invalid option.");
                    ConsoleHelper.Pause();
                    continue;
                }

                WriteHistoryTable(timesheets);
                Console.WriteLine();
                ConsoleHelper.WriteActions(("V", "View week details"), ("B", "Back"));
                var choice = ConsoleHelper.ReadActionChoice();

                if (choice == "B" || string.IsNullOrWhiteSpace(choice))
                {
                    return;
                }

                if (choice != "V")
                {
                    ConsoleHelper.WriteError("Invalid option.");
                    ConsoleHelper.Pause();
                    continue;
                }

                Console.WriteLine();
                Console.Write("Enter week start date (DD-MM-YYYY): ");
                var weekStart = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(weekStart))
                {
                    continue;
                }

                EmployeeTimesheetHistoryItemDto? selectedTimesheet;

                try
                {
                    selectedTimesheet = FindTimesheetByWeekInput(timesheets, weekStart);
                }
                catch (BusinessValidationException ex)
                {
                    ConsoleHelper.WriteError(ex.Message);
                    ConsoleHelper.Pause();
                    continue;
                }

                if (selectedTimesheet is null)
                {
                    ConsoleHelper.WriteError("Timesheet not found for that week.");
                    ConsoleHelper.Pause();
                    continue;
                }

                await ShowDetailAsync(selectedTimesheet);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError(ex.Message);
                ConsoleHelper.Pause();
                return;
            }
        }
    }

    private async Task ShowDetailAsync(EmployeeTimesheetHistoryItemDto timesheet)
    {
        while (true)
        {
            try
            {
                ConsoleHelper.ClearScreen();
                var detail = await _employeePortalApiClient.GetTimesheetDetailAsync(timesheet.TimesheetId);
                var tableWidth = ConsoleHelper.GetPipeTableWidth(DetailColumns);

                ConsoleHelper.WriteProjectLabel(
                    $"Week: {detail.WeekStartDate} - Status: {detail.Status}",
                    tableWidth);

                ConsoleHelper.WritePipeTableHeader(DetailColumns);
                Console.WriteLine(new string('-', tableWidth));

                foreach (var entry in detail.Entries)
                {
                    ConsoleHelper.WritePipeTableRow(
                        (entry.ProjectName, DetailColumns[0].Width),
                        (entry.Hours.ToString(), DetailColumns[1].Width),
                        (entry.ActivityTags, DetailColumns[2].Width));
                }

                Console.WriteLine(new string('-', tableWidth));
                Console.WriteLine($"Total: {detail.TotalHours} hrs");
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

    private static void WriteHistoryTable(IReadOnlyList<EmployeeTimesheetHistoryItemDto> timesheets)
    {
        ConsoleHelper.WritePipeTableHeader(HistoryColumns);
        var tableWidth = ConsoleHelper.GetPipeTableWidth(HistoryColumns);
        Console.WriteLine(new string('-', tableWidth));

        foreach (var timesheet in timesheets)
        {
            Console.Write(ConsoleHelper.FormatPipeTableCells(
                (timesheet.WeekStartDate, HistoryColumns[0].Width),
                ($"{timesheet.TotalHours} hrs", HistoryColumns[1].Width)));
            Console.Write(" | ");
            WriteTimesheetStatus(timesheet.Status, HistoryColumns[2].Width);
            Console.WriteLine();
        }

        Console.WriteLine(new string('-', tableWidth));
    }

    private static EmployeeTimesheetHistoryItemDto? FindTimesheetByWeekInput(
        IReadOnlyList<EmployeeTimesheetHistoryItemDto> timesheets,
        string weekStartInput)
    {
        var parsedInput = ParseWeekStartDate(weekStartInput);
        var normalizedWeekStart = WeekHelper.GetWeekStartDate(parsedInput);

        return timesheets.FirstOrDefault(timesheet =>
            ParseWeekStartDate(timesheet.WeekStartDate).Date == normalizedWeekStart.Date);
    }

    private static DateTime ParseWeekStartDate(string input)
    {
        if (DateTime.TryParseExact(
                input.Trim(),
                "dd-MM-yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
        {
            return parsedDate.Date;
        }

        throw new BusinessValidationException("Week start date must be a valid date (DD-MM-YYYY).");
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
