using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;
using PRM.Common.Exceptions;
using PRM.Models.DTOs.SystemConfig;
using PRM.Models.Enums;

namespace PRM.Business.Services;

public class SystemConfigService : ISystemConfigService
{
    private readonly ISystemConfigRepository _systemConfigRepository;

    public SystemConfigService(ISystemConfigRepository systemConfigRepository)
    {
        _systemConfigRepository = systemConfigRepository;
    }

    public async Task<SystemConfigDto> GetSystemConfigAsync(CancellationToken cancellationToken = default)
    {
        var config = await GetConfigOrThrowAsync(cancellationToken);

        return MapToDto(config);
    }

    public async Task<string> UpdateSystemConfigAsync(
        UpdateSystemConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateUpdateRequest(request);

        var config = await _systemConfigRepository.GetSingletonForUpdateAsync(cancellationToken)
            ?? throw new BusinessValidationException("System configuration not found.");

        config.MaxWeeklyHours = request.MaxWeeklyHours;
        config.SchedulerIntervalHours = request.SchedulerIntervalHours;
        config.LlmProvider = (LlmProvider)request.LlmProvider;

        if (!string.IsNullOrWhiteSpace(request.LlmApiKey))
        {
            config.LlmApiKey = request.LlmApiKey.Trim();
        }

        await _systemConfigRepository.SaveChangesAsync(cancellationToken);

        return "System configuration updated successfully.";
    }

    private async Task<Models.Entities.SystemConfig> GetConfigOrThrowAsync(CancellationToken cancellationToken)
    {
        return await _systemConfigRepository.GetSingletonAsync(cancellationToken)
            ?? throw new BusinessValidationException("System configuration not found.");
    }

    private static SystemConfigDto MapToDto(Models.Entities.SystemConfig config)
    {
        return new SystemConfigDto
        {
            MaxWeeklyHours = config.MaxWeeklyHours,
            SchedulerIntervalHours = config.SchedulerIntervalHours,
            LlmProvider = FormatProviderDisplay(config.LlmProvider),
            LlmApiKeyDisplay = FormatApiKeyDisplay(config.LlmApiKey)
        };
    }

    private static string FormatProviderDisplay(LlmProvider provider)
    {
        return provider switch
        {
            LlmProvider.Gemini => "Google Gemini",
            LlmProvider.Groq => "Groq",
            _ => provider.ToString()
        };
    }

    private static string FormatApiKeyDisplay(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "(not set)";
        }

        return new string('*', 27);
    }

    private static void ValidateUpdateRequest(UpdateSystemConfigRequest request)
    {
        if (request.MaxWeeklyHours <= 0)
        {
            throw new BusinessValidationException("Max weekly hours must be greater than zero.");
        }

        if (request.SchedulerIntervalHours <= 0)
        {
            throw new BusinessValidationException("Scheduler interval must be greater than zero.");
        }

        if (!Enum.IsDefined(typeof(LlmProvider), request.LlmProvider))
        {
            throw new BusinessValidationException("Invalid LLM provider.");
        }

        if (request.LlmApiKey?.Length > 500)
        {
            throw new BusinessValidationException("LLM API key cannot exceed 500 characters.");
        }
    }
}
