using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.Resources;

namespace PRM.ConsoleUI.UI.Screens.Employees;

public class ManageEmployeeSkillsScreen
{
    private readonly EmployeeApiClient _employeeApiClient;

    public ManageEmployeeSkillsScreen(EmployeeApiClient employeeApiClient)
    {
        _employeeApiClient = employeeApiClient;
    }

    public async Task ShowAsync()
    {
        ConsoleHelper.WriteHeader("Manage Skills");
        Console.Write("Enter Resource ID: ");
        var employeeIdInput = Console.ReadLine()?.Trim();
        if (!int.TryParse(employeeIdInput, out var employeeId))
        {
            ConsoleHelper.WriteError("Invalid Employee ID.");
            ConsoleHelper.Pause();
            return;
        }
        try
        {
            var employee = await _employeeApiClient.GetEmployeeAsync(employeeId);
            while (true)
            {
                var skills = (await _employeeApiClient.GetEmployeeSkillsAsync(employeeId)).ToList();
                DisplaySkillsScreen(employee.FullName, skills);
                Console.WriteLine("1. Add Skill");
                Console.WriteLine("2. Update Proficiency Level");
                Console.WriteLine("3. Remove Skill");
                Console.WriteLine("4. Back");
                Console.WriteLine();
                Console.Write("Enter option: ");
                var choice = Console.ReadLine()?.Trim();
                switch (choice)
                {
                    case "1":
                        await AddSkillAsync(employeeId);
                        break;
                    case "2":
                        await UpdateSkillAsync(employeeId, skills);
                        break;
                    case "3":
                        await RemoveSkillAsync(employeeId, skills);
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
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
            ConsoleHelper.Pause();
        }
    }

    private static void DisplaySkillsScreen(string fullName, IReadOnlyList<ResourceSkillDto> skills)
    {
        ConsoleHelper.WriteHeader("Manage Skills");
        ConsoleHelper.WriteBanner(fullName);
        Console.WriteLine("Current Skills:");
        if (skills.Count == 0)
        {
            Console.WriteLine("  (none)");
        }
        else
        {
            for (var index = 0; index < skills.Count; index++)
            {
                var skill = skills[index];
                Console.WriteLine(
                    $"{index + 1}.  {skill.SkillName,-22}{FormatProficiency(skill.ProficiencyLevel)}");
            }
        }
        ConsoleHelper.WriteSeparator();
    }

    private static string FormatProficiency(string proficiency)
    {
        if (string.IsNullOrWhiteSpace(proficiency))
        {
            return string.Empty;
        }
        return char.ToUpper(proficiency[0]) + proficiency[1..].ToLowerInvariant();
    }

    private async Task AddSkillAsync(int employeeId)
    {
        ConsoleHelper.WriteHeader("Add Skill");
        var skillName = ConsoleHelper.ReadInput("Skill Name");
        Console.WriteLine("Category: (1) Backend  (2) Frontend  (3) DevOps  (4) QA  (5) Other");
        Console.Write("Enter choice: ");
        var categoryChoice = Console.ReadLine()?.Trim();
        Console.WriteLine("Proficiency: (1) Beginner  (2) Intermediate  (3) Advanced");
        Console.Write("Enter choice: ");
        var proficiencyChoice = Console.ReadLine()?.Trim();
        if (categoryChoice is not ("1" or "2" or "3" or "4" or "5")
            || proficiencyChoice is not ("1" or "2" or "3"))
        {
            ConsoleHelper.WriteError("Invalid category or proficiency.");
            ConsoleHelper.Pause();
            return;
        }
        try
        {
            var message = await _employeeApiClient.AddEmployeeSkillAsync(employeeId, new AddResourceSkillRequest
            {
                SkillName = skillName,
                Category = int.Parse(categoryChoice),
                ProficiencyLevel = int.Parse(proficiencyChoice)
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

    private async Task UpdateSkillAsync(int employeeId, IReadOnlyList<ResourceSkillDto> skills)
    {
        if (skills.Count == 0)
        {
            ConsoleHelper.WriteError("No skills to update.");
            ConsoleHelper.Pause();
            return;
        }
        ConsoleHelper.WriteHeader("Update Proficiency Level");
        Console.Write("Enter skill number (from list): ");
        var input = Console.ReadLine()?.Trim();
        if (!int.TryParse(input, out var skillNumber) || skillNumber < 1 || skillNumber > skills.Count)
        {
            ConsoleHelper.WriteError("Invalid skill number.");
            ConsoleHelper.Pause();
            return;
        }
        var selectedSkill = skills[skillNumber - 1];
        Console.WriteLine($"Skill: {selectedSkill.SkillName}");
        Console.WriteLine("Proficiency: (1) Beginner  (2) Intermediate  (3) Advanced");
        Console.Write("Enter choice: ");
        var proficiencyChoice = Console.ReadLine()?.Trim();
        if (proficiencyChoice is not ("1" or "2" or "3"))
        {
            ConsoleHelper.WriteError("Invalid proficiency.");
            ConsoleHelper.Pause();
            return;
        }
        try
        {
            var message = await _employeeApiClient.UpdateEmployeeSkillAsync(
                employeeId,
                selectedSkill.SkillId,
                new UpdateResourceSkillRequest { ProficiencyLevel = int.Parse(proficiencyChoice) });
            ConsoleHelper.WriteSuccess(message);
            ConsoleHelper.Pause();
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError(ex.Message);
            ConsoleHelper.Pause();
        }
    }

    private async Task RemoveSkillAsync(int employeeId, IReadOnlyList<ResourceSkillDto> skills)
    {
        if (skills.Count == 0)
        {
            ConsoleHelper.WriteError("No skills to remove.");
            ConsoleHelper.Pause();
            return;
        }
        ConsoleHelper.WriteHeader("Remove Skill");
        Console.Write("Enter skill number (from list): ");
        var input = Console.ReadLine()?.Trim();
        if (!int.TryParse(input, out var skillNumber) || skillNumber < 1 || skillNumber > skills.Count)
        {
            ConsoleHelper.WriteError("Invalid skill number.");
            ConsoleHelper.Pause();
            return;
        }
        var selectedSkill = skills[skillNumber - 1];
        Console.WriteLine();
        Console.WriteLine($"Remove {selectedSkill.SkillName}?");
        Console.WriteLine("[Y] Yes     [B] Cancel");
        Console.Write("Enter choice: ");
        if (Console.ReadLine()?.Trim().ToUpperInvariant() != "Y")
        {
            return;
        }
        try
        {
            var message = await _employeeApiClient.RemoveEmployeeSkillAsync(employeeId, selectedSkill.SkillId);
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
