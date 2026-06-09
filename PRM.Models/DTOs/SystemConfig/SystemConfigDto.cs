namespace PRM.Models.DTOs.SystemConfig;

public class SystemConfigDto
{
    public int MaxWeeklyHours { get; set; }

    public int SchedulerIntervalHours { get; set; }

    public string LlmProvider { get; set; } = string.Empty;

    public string LlmApiKeyDisplay { get; set; } = string.Empty;
}
