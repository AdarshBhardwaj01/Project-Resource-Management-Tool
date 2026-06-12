using System.Text;
using PRM.Models.DTOs.Ai;

namespace PRM.Business.Services.Ai;

internal static class AiPromptBuilder
{
    public static string BuildSkillMatchPrompt(AiSkillMatchContext context)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You are a resource management assistant for an IT services company.");
        if (context.RequireSingleEmployeeMatch)
        {
            builder.AppendLine(
                "Recommend exactly 1 employee who individually matches the full combined requirement. " +
                "Do not split requested skills across multiple employees.");
        }
        else
        {
            builder.AppendLine($"Recommend up to {context.MaxSuggestions} employees whose skills and availability best match the requirement.");
        }
        builder.AppendLine("Only choose employee IDs from the candidate list below.");
        builder.AppendLine("Do not recommend anyone whose skills do not match the required technologies.");
        builder.AppendLine("Return ONLY valid JSON in this exact shape with no markdown:");
        builder.AppendLine("{\"suggestions\":[{\"employeeId\":1,\"reason\":\"short reason\"}]}");
        builder.AppendLine();
        if (!string.IsNullOrWhiteSpace(context.ProjectName))
        {
            builder.AppendLine($"Project: {context.ProjectName}");
        }
        builder.AppendLine($"Requirement: {context.Requirement}");
        if (context.RequireFullAvailability || context.MinAvailablePercent is > 0)
        {
            var requiredPercent = context.RequireFullAvailability
                ? 100
                : context.MinAvailablePercent ?? 0;
            if (context.AvailableFromDate.HasValue)
            {
                builder.AppendLine(
                    $"Availability constraint: at least {requiredPercent}% free from " +
                    $"{context.AvailableFromDate.Value:dd-MMM-yyyy}.");
            }
            else
            {
                builder.AppendLine($"Availability constraint: at least {requiredPercent}% free.");
            }
        }
        builder.AppendLine();
        builder.AppendLine("Candidates:");
        foreach (var candidate in context.Candidates)
        {
            var status = candidate.IsOnBench ? "BENCH (fully available)" : candidate.Availability;
            builder.AppendLine(
                $"- ID {candidate.EmployeeId}: {candidate.FullName}, Department: {candidate.Department}, " +
                $"Skills: {candidate.Skills}, Status: {status}");
        }
        return builder.ToString();
    }

    public static string BuildRiskSummaryPrompt(AiRiskSummaryContext context)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You are a project health analyst for an IT services company.");
        builder.AppendLine("Write a concise 2-4 sentence executive risk summary for the manager.");
        builder.AppendLine("Be factual, use the data provided, and mention concrete risks or confirm the project is on track.");
        builder.AppendLine("Return plain text only with no bullet points or markdown.");
        builder.AppendLine();
        builder.AppendLine($"Project: {context.ProjectName}");
        builder.AppendLine($"Health Status: {context.HealthStatus}");
        builder.AppendLine();
        builder.AppendLine("Milestones:");
        AppendLines(builder, context.Milestones, "No milestones recorded.");
        builder.AppendLine("Current Allocations:");
        AppendLines(builder, context.Allocations, "No active allocations.");
        builder.AppendLine("Risk Indicators:");
        AppendLines(builder, context.RiskFlags, "No negative risk indicators.");
        return builder.ToString();
    }

    private static void AppendLines(StringBuilder builder, IReadOnlyList<string> lines, string emptyText)
    {
        if (lines.Count == 0)
        {
            builder.AppendLine($"- {emptyText}");
            return;
        }
        foreach (var line in lines)
        {
            builder.AppendLine($"- {line}");
        }
    }
}
