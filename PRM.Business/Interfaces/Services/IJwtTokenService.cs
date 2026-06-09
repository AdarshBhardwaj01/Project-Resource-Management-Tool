using PRM.Models.Entities;

namespace PRM.Business.Interfaces.Services;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
