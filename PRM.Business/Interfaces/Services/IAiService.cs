using PRM.Models.DTOs.Ai;
using PRM.Models.DTOs.Manager;

namespace PRM.Business.Interfaces.Services;

public interface IAiService
{
    Task<SkillMatchResponse> GetSkillMatchAsync(
        AiSkillMatchContext context,
        CancellationToken cancellationToken = default);
    Task<string> GetRiskSummaryAsync(
        AiRiskSummaryContext context,
        CancellationToken cancellationToken = default);
}
