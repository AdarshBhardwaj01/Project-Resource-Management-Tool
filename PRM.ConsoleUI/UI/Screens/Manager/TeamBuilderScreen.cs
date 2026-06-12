using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Manager;

namespace PRM.ConsoleUI.UI.Screens.Manager;

public class TeamBuilderScreen
{
    private const int ColRole = 22;
    private const int ColName = 20;
    private const int ColSkills = 20;
    private const int ColAvail = 14;

    private readonly ManagerApiClient _managerApiClient;

    public TeamBuilderScreen(ManagerApiClient managerApiClient)
    {
        _managerApiClient = managerApiClient;
    }

    public async Task ShowAsync()
    {
        while (true)
        {
            try
            {
                ConsoleHelper.ClearScreen();
                ConsoleHelper.WriteSectionHeader("Team Builder");
                Console.WriteLine();
                Console.WriteLine("Describe the roles you need in plain English.");
                Console.WriteLine("Example: 1 Java developer, 1 QA/SDET, 1 DevOps engineer");
                Console.WriteLine();
                Console.Write("> ");
                var prompt = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(prompt))
                {
                    return;
                }
                Console.WriteLine();
                Console.WriteLine("Searching across the organization...");
                var response = await _managerApiClient.GetTeamBuildAsync(new TeamBuildRequest
                {
                    Prompt = prompt
                });
                Console.WriteLine();
                DisplayTeamResult(response);
                Console.WriteLine();
                ConsoleHelper.WriteActions(("N", "New Search"), ("B", "Back"));
                var choice = ConsoleHelper.ReadActionChoice();
                if (choice == "B" || string.IsNullOrWhiteSpace(choice))
                {
                    return;
                }
                if (choice == "N")
                {
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

    private static void DisplayTeamResult(TeamBuildResponse response)
    {
        if (response.Roles.Count == 0)
        {
            Console.WriteLine("No roles were identified from the prompt.");
            return;
        }
        var header = new (string Title, int Width)[]
        {
            ("Role", ColRole),
            ("Assigned To", ColName),
            ("Matched Skills", ColSkills),
            ("Availability", ColAvail)
        };
        var rows = response.Roles.Select(role =>
        {
            if (role.Filled)
            {
                return new (string Value, int Width)[]
                {
                    (role.RoleLabel, ColRole),
                    (role.EmployeeName, ColName),
                    (role.MatchedSkills, ColSkills),
                    (role.Availability, ColAvail)
                };
            }
            return new (string Value, int Width)[]
            {
                (role.RoleLabel, ColRole),
                ("(Not Found)", ColName),
                (string.Empty, ColSkills),
                (string.Empty, ColAvail)
            };
        });
        ConsoleHelper.WritePipeTable(header, rows);
        Console.WriteLine();
        var gapRoles = response.Roles.Where(r => !r.Filled).ToList();
        if (gapRoles.Count > 0)
        {
            Console.WriteLine("Gap Details:");
            foreach (var gap in gapRoles)
            {
                Console.WriteLine($"  [{gap.RoleLabel}] {gap.GapReason}");
            }
            Console.WriteLine();
        }
        Console.WriteLine($"Summary: {response.FilledCount} role(s) filled, {response.GapCount} gap(s).");
        Console.WriteLine();
        Console.WriteLine(
            "Note: Results are based on current bench availability across the entire organization.");
    }
}
