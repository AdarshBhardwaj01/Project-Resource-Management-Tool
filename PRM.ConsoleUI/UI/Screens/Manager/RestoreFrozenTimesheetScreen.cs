using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Manager;

namespace PRM.ConsoleUI.UI.Screens.Manager;

public class RestoreFrozenTimesheetScreen
{
    private static readonly (string Title, int Width)[] FrozenColumns =
    [
        ("#", 3),
        ("Employee", 18),
        ("Week Start", 12),
        ("Status", 8),
        ("Reminders", 0)
    ];

    private readonly ManagerApiClient _managerApiClient;

    public RestoreFrozenTimesheetScreen(ManagerApiClient managerApiClient)
    {
        _managerApiClient = managerApiClient;
    }

    public async Task ShowAsync()
    {
        while (true)
        {
            try
            {
                ConsoleHelper.WriteHeader("Restore Frozen Timesheets");
                var frozenTimesheets = await _managerApiClient.GetFrozenTimesheetsAsync();
                Console.WriteLine();
                if (frozenTimesheets.Count == 0)
                {
                    Console.WriteLine("No frozen timesheets found for your team.");
                }
                else
                {
                    var rows = frozenTimesheets.Select(item => new (string Value, int Width)[]
                    {
                        (item.RowNumber.ToString(), FrozenColumns[0].Width),
                        (item.EmployeeName, FrozenColumns[1].Width),
                        (item.WeekStartDate, FrozenColumns[2].Width),
                        (item.Status, FrozenColumns[3].Width),
                        (item.ReminderCount.ToString(), FrozenColumns[4].Width)
                    });
                    ConsoleHelper.WritePipeTable(FrozenColumns, rows);
                }
                Console.WriteLine();
                ConsoleHelper.WriteActions(
                    ("R", "Restore timesheet access"),
                    ("B", "Back"));
                var choice = ConsoleHelper.ReadActionChoice();
                if (choice == "B" || string.IsNullOrWhiteSpace(choice))
                {
                    return;
                }
                if (choice != "R")
                {
                    ConsoleHelper.WriteError("Invalid option.");
                    ConsoleHelper.Pause();
                    continue;
                }
                if (frozenTimesheets.Count == 0)
                {
                    ConsoleHelper.WriteError("No frozen timesheets available to restore.");
                    ConsoleHelper.Pause();
                    continue;
                }
                await RestoreSelectedTimesheetAsync(frozenTimesheets);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError(ex.Message);
                ConsoleHelper.Pause();
                return;
            }
        }
    }

    private async Task RestoreSelectedTimesheetAsync(IReadOnlyList<FrozenTimesheetItemDto> frozenTimesheets)
    {
        Console.WriteLine();
        Console.Write("Enter row number to restore: ");
        var rowInput = Console.ReadLine()?.Trim();
        if (!int.TryParse(rowInput, out var rowNumber) || rowNumber <= 0)
        {
            ConsoleHelper.WriteError("Invalid row number.");
            ConsoleHelper.Pause();
            return;
        }
        var selected = frozenTimesheets.FirstOrDefault(item => item.RowNumber == rowNumber);
        if (selected is null)
        {
            ConsoleHelper.WriteError("Frozen timesheet not found.");
            ConsoleHelper.Pause();
            return;
        }
        try
        {
            var message = await _managerApiClient.RestoreFrozenTimesheetAsync(new RestoreFrozenTimesheetRequest
            {
                EmployeeId = selected.EmployeeId,
                WeekStartDate = selected.WeekStartDate
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
