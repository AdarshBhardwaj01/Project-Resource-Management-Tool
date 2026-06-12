using AutoMapper;
using PRM.Models.DTOs.Resources;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Mappings;

public class ResourceMappingProfile : Profile
{
    public ResourceMappingProfile()
    {
        CreateMap<CreateResourceRequest, Resource>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => ResourceStatus.Bench))
            .ForMember(dest => dest.UtilisationPercent, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.ManagerUserId, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Manager, opt => opt.Ignore())
            .ForMember(dest => dest.Skills, opt => opt.Ignore())
            .ForMember(dest => dest.Allocations, opt => opt.Ignore())
            .ForMember(dest => dest.Timesheets, opt => opt.Ignore());

        CreateMap<Resource, ResourceListItemDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
            .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.User.Department))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString().ToUpperInvariant()));

        CreateMap<ResourceSkill, ResourceSkillDto>()
            .ForMember(dest => dest.SkillId, opt => opt.MapFrom(src => src.SkillId))
            .ForMember(dest => dest.SkillName, opt => opt.MapFrom(src => src.Skill.Name))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.ToString()))
            .ForMember(dest => dest.ProficiencyLevel, opt => opt.MapFrom(src => src.ProficiencyLevel.ToString()));
    }
}
