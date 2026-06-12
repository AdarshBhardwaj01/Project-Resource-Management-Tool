using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.Models.DTOs.SystemConfig;

namespace PRM.ConsoleUI.UI.Screens.SystemConfig;

public class SystemConfigurationScreen
{
    private readonly SystemConfigApiClient _systemConfigApiClient;

    public SystemConfigurationScreen(SystemConfigApiClient systemConfigApiClient)
    {
        _systemConfigApiClient = systemConfigApiClient;
    }

    public async Task ShowAsync()
    {
        while (true)
        {
            try
            {
                var config = await _systemConfigApiClient.GetSystemConfigAsync();
                DisplayCurrentConfig(config);
                ConsoleHelper.WriteSeparator();
                Console.WriteLine("1. Update LLM API Key");
                Console.WriteLine("2. Change LLM Provider (Gemini / Groq / Gemma)");
                Console.WriteLine("3. Update Scheduler Interval");
                Console.WriteLine("4. Update Max Weekly Hours");
                Console.WriteLine("5. Back");
                Console.WriteLine();
                Console.Write("Enter option: ");
                var choice = Console.ReadLine()?.Trim();
                switch (choice)
                {
                    case "1":
                        await UpdateApiKeyAsync(config);
                        break;
                    case "2":
                        await UpdateProviderAsync(config);
                        break;
                    case "3":
                        await UpdateSchedulerIntervalAsync(config);
                        break;
                    case "4":
                        await UpdateMaxWeeklyHoursAsync(config);
                        break;
                    case "5":
                        return;
                    default:
                        ConsoleHelper.WriteError("Invalid option.");
                        ConsoleHelper.Pause();
                        break;
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError(ex.Message);
                ConsoleHelper.Pause();
            }
        }
    }

    private static void DisplayCurrentConfig(SystemConfigDto config)
    {
        ConsoleHelper.WriteHeader("System Configuration");
        Console.WriteLine("Current Settings:");
        Console.WriteLine($"LLM Provider       : {config.LlmProvider}");
        Console.WriteLine($"LLM API Key        : {config.LlmApiKeyDisplay}");
        Console.WriteLine($"Scheduler Interval : {config.SchedulerIntervalHours} hours");
        Console.WriteLine($"Max Weekly Hours   : {config.MaxWeeklyHours}");
    }

    private async Task UpdateApiKeyAsync(SystemConfigDto config)
    {
        ConsoleHelper.WriteHeader("Update LLM API Key");
        var apiKey = ConsoleHelper.ReadInput("LLM API Key");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            ConsoleHelper.WriteError("LLM API key is required.");
            ConsoleHelper.Pause();
            return;
        }
        var message = await _systemConfigApiClient.UpdateSystemConfigAsync(BuildRequest(config, request =>
        {
            request.LlmApiKey = apiKey.Trim();
        }));
        ConsoleHelper.WriteSuccess(message);
        ConsoleHelper.Pause();
    }

    private async Task UpdateProviderAsync(SystemConfigDto config)
    {
        ConsoleHelper.WriteHeader("Change LLM Provider");
        Console.WriteLine($"Current Provider   : {config.LlmProvider}");
        Console.WriteLine("Select provider    : (1) Google Gemini  (2) Groq  (3) Gemma (self-hosted)");
        Console.Write("Enter choice       : ");
        var choice = Console.ReadLine()?.Trim();
        if (choice is not ("1" or "2" or "3"))
        {
            ConsoleHelper.WriteError("Invalid provider selected.");
            ConsoleHelper.Pause();
            return;
        }
        var message = await _systemConfigApiClient.UpdateSystemConfigAsync(BuildRequest(config, request =>
        {
            request.LlmProvider = int.Parse(choice);
        }));
        ConsoleHelper.WriteSuccess(message);
        ConsoleHelper.Pause();
    }

    private async Task UpdateSchedulerIntervalAsync(SystemConfigDto config)
    {
        ConsoleHelper.WriteHeader("Update Scheduler Interval");
        Console.Write($"Scheduler Interval [{config.SchedulerIntervalHours} hours] : ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            ConsoleHelper.Pause();
            return;
        }
        if (!int.TryParse(input, out var schedulerIntervalHours) || schedulerIntervalHours <= 0)
        {
            ConsoleHelper.WriteError("Invalid scheduler interval.");
            ConsoleHelper.Pause();
            return;
        }
        var message = await _systemConfigApiClient.UpdateSystemConfigAsync(BuildRequest(config, request =>
        {
            request.SchedulerIntervalHours = schedulerIntervalHours;
        }));
        ConsoleHelper.WriteSuccess(message);
        ConsoleHelper.Pause();
    }

    private async Task UpdateMaxWeeklyHoursAsync(SystemConfigDto config)
    {
        ConsoleHelper.WriteHeader("Update Max Weekly Hours");
        Console.Write($"Max Weekly Hours [{config.MaxWeeklyHours}] : ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            ConsoleHelper.Pause();
            return;
        }
        if (!int.TryParse(input, out var maxWeeklyHours) || maxWeeklyHours <= 0)
        {
            ConsoleHelper.WriteError("Invalid max weekly hours.");
            ConsoleHelper.Pause();
            return;
        }
        var message = await _systemConfigApiClient.UpdateSystemConfigAsync(BuildRequest(config, request =>
        {
            request.MaxWeeklyHours = maxWeeklyHours;
        }));
        ConsoleHelper.WriteSuccess(message);
        ConsoleHelper.Pause();
    }

    private static UpdateSystemConfigRequest BuildRequest(
        SystemConfigDto config,
        Action<UpdateSystemConfigRequest>? applyChanges = null)
    {
        var request = new UpdateSystemConfigRequest
        {
            MaxWeeklyHours = config.MaxWeeklyHours,
            SchedulerIntervalHours = config.SchedulerIntervalHours,
            LlmProvider = ResolveProviderId(config.LlmProvider)
        };
        applyChanges?.Invoke(request);
        return request;
    }

    private static int ResolveProviderId(string providerDisplay)
    {
        return providerDisplay switch
        {
            "Google Gemini" or "GEMINI" or "Gemini" => 1,
            "Groq" or "GROQ" => 2,
            "Gemma" or "GEMMA" => 3,
            _ => 1
        };
    }
}
