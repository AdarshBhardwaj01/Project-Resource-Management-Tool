using AutoMapper;
using PRM.Business.Helpers;
using PRM.Business.Mappings.Resolvers;
using PRM.Models.DTOs.Auth;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Mappings;

public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<User, LoginResponse>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => UserRoleHelper.GetPrimaryApplicationRole(src) ?? ApplicationRole.Employee))
            .ForMember(dest => dest.Token, opt => opt.MapFrom<JwtTokenResolver>());
    }
}
