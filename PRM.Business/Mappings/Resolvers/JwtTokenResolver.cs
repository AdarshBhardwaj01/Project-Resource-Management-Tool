using AutoMapper;
using PRM.Business.Interfaces.Services;
using PRM.Models.DTOs.Auth;
using PRM.Models.Entities;

namespace PRM.Business.Mappings.Resolvers;

public class JwtTokenResolver : IValueResolver<User, LoginResponse, string>
{
    private readonly IJwtTokenService _jwtTokenService;

    public JwtTokenResolver(IJwtTokenService jwtTokenService)
    {
        _jwtTokenService = jwtTokenService;
    }

    public string Resolve(User source, LoginResponse destination, string destMember, ResolutionContext context)
    {
        return _jwtTokenService.GenerateToken(source);
    }
}
