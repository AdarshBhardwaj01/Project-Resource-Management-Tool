using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.EmployeePortal;

namespace PRM.ConsoleUI.UI.Screens.Employee;

public class ViewMyAllocationsScreen
{
    private static readonly (string Title, int Width)[] Columns =
    [
        ("Project", 16),
        ("%", 4),
        ("From", 9),
        ("To", 9),
        ("Status", 0)
    ];

    private readonly EmployeePortalApiClient _employeePortalApiClient;

    public ViewMyAllocationsScreen(EmployeePortalApiClient employeePortalApiClient)
    {
        _employeePortalApiClient = employeePortalApiClient;
    }

    public async Task ShowAsync()
    {
        try
        {
            await ShowAllocationsLoopAsync();
        }
        finally
        {
            ConsoleHelper.EndScreenSession();
        }
    }

    private async Task ShowAllocationsLoopAsync()
    {
        while (true)
        {
            try
            {
                ConsoleHelper.WriteHeader("My Allocations");
                var allocations = await _employeePortalApiClient.GetMyAllocationsAsync();
                if (allocations.Count == 0)
                {
                    Console.WriteLine("You have no active project allocations.");
                }
                else
                {
                    WriteAllocationsTable(allocations);
                    Console.WriteLine();
                    Console.WriteLine(
                        $"Total Utilisation: {allocations.Sum(allocation => allocation.UtilisationPercent)}%");
                }
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

    private static void WriteAllocationsTable(IReadOnlyList<EmployeeAllocationItemDto> allocations)
    {
        ConsoleHelper.WritePipeTableHeader(Columns);
        var tableWidth = ConsoleHelper.GetPipeTableWidth(Columns);
        Console.WriteLine(new string('-', tableWidth));
        foreach (var allocation in allocations)
        {
            ConsoleHelper.WritePipeTableRow(
                (allocation.ProjectName, Columns[0].Width),
                ($"{allocation.UtilisationPercent}%", Columns[1].Width),
                (allocation.FromDate, Columns[2].Width),
                (allocation.ToDate, Columns[3].Width),
                (allocation.Status, Columns[4].Width));
        }
        Console.WriteLine(new string('-', tableWidth));
    }
}
