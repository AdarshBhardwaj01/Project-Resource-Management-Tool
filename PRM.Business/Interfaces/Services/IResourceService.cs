using PRM.Models.DTOs.Resources;

namespace PRM.Business.Interfaces.Services;

public interface IResourceService
{
    Task<string> CreateResourceAsync(CreateResourceRequest request, CancellationToken cancellationToken = default);
    Task<ResourceListResponse> GetAllResourcesAsync(string? status, string? department, CancellationToken cancellationToken = default);
    Task<ResourceDetailDto> GetResourceAsync(int userId, CancellationToken cancellationToken = default);
    Task<string> UpdateResourceAsync(int userId, UpdateResourceRequest request, CancellationToken cancellationToken = default);
    Task<string> DeactivateResourceAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResourceSkillDto>> GetResourceSkillsAsync(int userId, CancellationToken cancellationToken = default);
    Task<string> AddResourceSkillAsync(int userId, AddResourceSkillRequest request, CancellationToken cancellationToken = default);
    Task<string> UpdateResourceSkillAsync(int userId, int skillId, UpdateResourceSkillRequest request, CancellationToken cancellationToken = default);
    Task<string> RemoveResourceSkillAsync(int userId, int skillId, CancellationToken cancellationToken = default);
    Task<string> AssignManagerAsync(AssignManagerRequest request, CancellationToken cancellationToken = default);
}
