using AutoMapper;
using PRM.Models.DTOs.Users;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<CreateUserRequest, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName.Trim()))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Trim()))
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username.Trim()))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => (UserRole)src.Role))
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.ForcePasswordChange, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Employee, opt => opt.Ignore())
            .ForMember(dest => dest.ManagedProjects, opt => opt.Ignore());

        CreateMap<User, UserListItemDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString().ToUpperInvariant()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsActive ? "Active" : "Inactive"));

        CreateMap<User, UserDetailDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString().ToUpperInvariant()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsActive ? "Active" : "Inactive"));
    }
}
