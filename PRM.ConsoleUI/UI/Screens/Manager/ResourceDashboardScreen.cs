using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Manager;

namespace PRM.ConsoleUI.UI.Screens.Manager;

public class ResourceDashboardScreen
{
    private static readonly (string Title, int Width)[] BenchColumns =
    [
        ("ID", 4),
        ("Name", 16),
        ("Department", 12),
        ("Skills", 0)
    ];

    private static readonly (string Title, int Width)[] ActiveColumns =
    [
        ("ID", 4),
        ("Name", 16),
        ("Alloc %", 7),
        ("Availability", 0)
    ];

    private static readonly (string Title, int Width)[] AllocationColumns =
    [
        ("Project", 16),
        ("%", 4),
        ("From", 9),
        ("To", 9)
    ];

    private readonly ManagerApiClient _managerApiClient;

    public ResourceDashboardScreen(ManagerApiClient managerApiClient)
    {
        _managerApiClient = managerApiClient;
    }

    public async Task ShowAsync()
    {
        while (true)
        {
            try
            {
                var response = await _managerApiClient.GetResourceDashboardAsync();
                DisplayDashboard(response);

                ConsoleHelper.WriteActions(("D", "Drill into employee details"), ("B", "Back"));
                var choice = ConsoleHelper.ReadActionChoice();

                if (choice == "B")
                {
                    return;
                }

                if (choice == "D")
                {
                    await PromptEmployeeDetailsAsync();
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

    private static void DisplayDashboard(ResourceDashboardResponse response)
    {
        var period = DateTime.Now.ToString("MMM yyyy");
        ConsoleHelper.WriteHeader($"Resource Dashboard - {period}");

        Console.WriteLine($"ON BENCH ({response.BenchCount} employees available)");

        var benchRows = response.BenchEmployees.Select(employee => new (string Value, int Width)[]
        {
            (employee.Id.ToString(), BenchColumns[0].Width),
            (employee.FullName, BenchColumns[1].Width),
            (employee.Department, BenchColumns[2].Width),
            (employee.Skills, BenchColumns[3].Width)
        });

        ConsoleHelper.WritePipeTable(BenchColumns, benchRows);
        Console.WriteLine();

        Console.WriteLine("ACTIVE EMPLOYEES");

        var activeRows = response.ActiveEmployees.Select(employee => new (string Value, int Width)[]
        {
            (employee.Id.ToString(), ActiveColumns[0].Width),
            (employee.FullName, ActiveColumns[1].Width),
            ($"{employee.AllocatedPercent}%", ActiveColumns[2].Width),
            (employee.Availability, ActiveColumns[3].Width)
        });

        ConsoleHelper.WritePipeTable(ActiveColumns, activeRows);

        Console.WriteLine();
        Console.WriteLine(
            $"Bench: {response.BenchCount}  |  " +
            $"Partial: {response.PartialCount}");
    }

    private async Task PromptEmployeeDetailsAsync()
    {
        Console.WriteLine();
        Console.Write("Enter Employee ID: ");
        var input = Console.ReadLine()?.Trim();

        if (!int.TryParse(input, out var employeeId))
        {
            ConsoleHelper.WriteError("Invalid Employee ID.");
            ConsoleHelper.Pause();
            return;
        }

        try
        {
            var employee = await _managerApiClient.GetEmployeeDrillDownAsync(employeeId);
            await ShowEmployeeDetailsAsync(employee);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
            ConsoleHelper.Pause();
        }
    }

    private static Task ShowEmployeeDetailsAsync(EmployeeDrillDownDto employee)
    {
        while (true)
        {
            ConsoleHelper.ClearScreen();

            var labelWidth = ConsoleHelper.GetPipeTableWidth(AllocationColumns);
            ConsoleHelper.WriteProjectLabel(employee.FullName, labelWidth);

            Console.WriteLine($"Department         : {employee.Department}");
            Console.WriteLine($"Current Status     : {employee.CurrentStatus}");
            Console.WriteLine($"Profile Skills     : {employee.ProfileSkills}");
            Console.WriteLine();
            Console.WriteLine("Active Allocations:");

            var allocationRows = employee.ActiveAllocations.Select(allocation => new (string Value, int Width)[]
            {
                (allocation.ProjectName, AllocationColumns[0].Width),
                ($"{allocation.UtilisationPercent}%", AllocationColumns[1].Width),
                (allocation.FromDate, AllocationColumns[2].Width),
                (allocation.ToDate, AllocationColumns[3].Width)
            });

            ConsoleHelper.WritePipeTable(AllocationColumns, allocationRows);
            Console.WriteLine();
            Console.WriteLine("Recent Activity Tags (last 4 weeks):");
            Console.WriteLine(employee.RecentActivityTags);
            Console.WriteLine();

            ConsoleHelper.WriteActions(("B", "Back"));
            var choice = ConsoleHelper.ReadActionChoice();

            if (choice == "B" || string.IsNullOrWhiteSpace(choice))
            {
                return Task.CompletedTask;
            }

            ConsoleHelper.WriteError("Invalid option.");
            ConsoleHelper.Pause();
        }
    }
}
