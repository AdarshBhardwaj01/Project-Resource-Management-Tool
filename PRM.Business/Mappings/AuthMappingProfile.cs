using AutoMapper;

using PRM.Business.Mappings.Resolvers;

using PRM.Models.DTOs.Auth;

using PRM.Models.Entities;



namespace PRM.Business.Mappings;



public class AuthMappingProfile : Profile

{

    public AuthMappingProfile()

    {

        CreateMap<User, LoginResponse>()

            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))

            .ForMember(dest => dest.Token, opt => opt.MapFrom<JwtTokenResolver>());

    }

}


