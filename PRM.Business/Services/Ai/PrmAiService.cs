using Microsoft.Extensions.Logging;
using PRM.Business.Helpers;
using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;
using PRM.Models.DTOs.Ai;
using PRM.Models.DTOs.Manager;
using PRM.Models.Enums;

namespace PRM.Business.Services.Ai;

public class PrmAiService : IAiService
{
    private readonly ISystemConfigRepository _systemConfigRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PrmAiService> _logger;

    public PrmAiService(
        ISystemConfigRepository systemConfigRepository,
        IHttpClientFactory httpClientFactory,
        ILogger<PrmAiService> logger)
    {
        _systemConfigRepository = systemConfigRepository;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SkillMatchResponse> GetSkillMatchAsync(
        AiSkillMatchContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.Candidates.Count == 0)
        {
            return new SkillMatchResponse
            {
                NoMatchReason = SkillMatchHelper.BuildNoMatchReason(
                    SkillMatchHelper.ParseRequirement(context.Requirement))
            };
        }
        var prompt = AiPromptBuilder.BuildSkillMatchPrompt(context);
        var llmText = await TryCompleteAsync(prompt, cancellationToken);
        var parsed = llmText is null ? null : AiResponseParser.TryParseSkillMatch(llmText, context);
        parsed = SkillMatchResultBuilder.ValidateLlmResponse(parsed, context);
        if (parsed is not null)
        {
            _logger.LogInformation("Skill match generated using configured LLM provider.");
            return parsed;
        }
        if (llmText is not null)
        {
            _logger.LogWarning("LLM skill match response could not be parsed. Falling back to rule-based logic.");
        }
        return RuleBasedAiFallback.BuildSkillMatch(context);
    }

    public async Task<string> GetRiskSummaryAsync(
        AiRiskSummaryContext context,
        CancellationToken cancellationToken = default)
    {
        var prompt = AiPromptBuilder.BuildRiskSummaryPrompt(context);
        var llmText = await TryCompleteAsync(prompt, cancellationToken);
        var parsed = llmText is null ? null : AiResponseParser.TryParsePlainText(llmText);
        if (parsed is not null)
        {
            _logger.LogInformation("Risk summary generated using configured LLM provider.");
            return parsed;
        }
        if (llmText is not null)
        {
            _logger.LogWarning("LLM risk summary response was empty or invalid. Falling back to rule-based logic.");
        }
        return RuleBasedAiFallback.BuildRiskSummary(context);
    }

    private async Task<string?> TryCompleteAsync(string prompt, CancellationToken cancellationToken)
    {
        var config = await _systemConfigRepository.GetSingletonAsync(cancellationToken);
        if (config is null || string.IsNullOrWhiteSpace(config.LlmApiKey))
        {
            _logger.LogInformation("LLM API key is not configured. Using rule-based fallback.");
            return null;
        }
        var apiKey = config.LlmApiKey.Trim();
        var provider = config.LlmProvider;
        var client = CreateLlmClient(provider, apiKey);
        try
        {
            _logger.LogInformation(
                "Sending LLM request using provider {Provider} with configured API key {ApiKeyPreview}. Prompt length: {PromptLength}.",
                provider,
                MaskApiKey(apiKey),
                prompt.Length);

            var response = await client.CompleteAsync(prompt, cancellationToken);

            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogWarning(
                    "LLM provider {Provider} returned an empty or unsuccessful response. Falling back to rule-based logic.",
                    provider);
                return null;
            }

            _logger.LogInformation(
                "LLM provider {Provider} returned a successful response. Response length: {ResponseLength}.",
                provider,
                response.Length);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "LLM request failed for provider {Provider}. Using rule-based fallback.",
                provider);
            return null;
        }
    }

    private ILlmClient CreateLlmClient(LlmProvider provider, string apiKey)
    {
        return provider switch
        {
            LlmProvider.Groq => new GroqLlmClient(
                _httpClientFactory.CreateClient("GroqLlm"),
                apiKey),
            LlmProvider.Gemma => new GemmaLlmClient(
                _httpClientFactory.CreateClient("GemmaLlm"),
                apiKey),
            _ => new GeminiLlmClient(
                _httpClientFactory.CreateClient("GeminiLlm"),
                apiKey)
        };
    }

    private static string MaskApiKey(string apiKey)
    {
        if (apiKey.Length <= 4)
        {
            return "***";
        }

        return $"***{apiKey[^4..]}";
    }
}
