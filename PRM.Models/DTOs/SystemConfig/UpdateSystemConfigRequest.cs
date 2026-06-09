namespace PRM.Models.DTOs.SystemConfig;

public class UpdateSystemConfigRequest
{
    public int MaxWeeklyHours { get; set; }

    public int SchedulerIntervalHours { get; set; }

    public int LlmProvider { get; set; }

    public string? LlmApiKey { get; set; }
}
