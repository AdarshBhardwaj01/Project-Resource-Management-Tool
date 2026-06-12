using PRM.Models.DTOs.SystemConfig;

namespace PRM.Business.Interfaces.Services;

public interface ISystemConfigService
{
    Task<SystemConfigDto> GetSystemConfigAsync(CancellationToken cancellationToken = default);
    Task<string> UpdateSystemConfigAsync(
        UpdateSystemConfigRequest request,
        CancellationToken cancellationToken = default);
}
