using PRM.Models.Enums;

namespace PRM.Models.Entities;

public class SystemConfig
{
    public int Id { get; set; }

    public LlmProvider LlmProvider { get; set; } = LlmProvider.Gemini;

    public string LlmApiKey { get; set; } = string.Empty;

    public int SchedulerIntervalHours { get; set; } = 4;

    public int MaxWeeklyHours { get; set; } = 40;
}
