using AutoMapper;
using PRM.Business.Helpers;
using PRM.Models.DTOs.Users;
using PRM.Models.Entities;

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
            .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department.Trim()))
            .ForMember(dest => dest.Designation, opt => opt.MapFrom(src => src.Designation.Trim()))
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.ForcePasswordChange, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Resource, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
            .ForMember(dest => dest.ManagedProjects, opt => opt.Ignore());

        CreateMap<User, UserListItemDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => UserRoleHelper.GetPrimaryRoleName(src)!.ToUpperInvariant()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsActive ? "Active" : "Inactive"));

        CreateMap<User, UserDetailDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => UserRoleHelper.GetPrimaryRoleName(src)!.ToUpperInvariant()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsActive ? "Active" : "Inactive"));
    }
}
