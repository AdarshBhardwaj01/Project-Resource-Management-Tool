using AutoMapper;
using PRM.Common.Exceptions;
using PRM.Common.Helpers;
using PRM.Models.DTOs.Employees;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Mappings;

public class EmployeeMappingProfile : Profile
{
    public EmployeeMappingProfile()
    {
        CreateMap<CreateEmployeeRequest, Employee>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName.Trim()))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Trim()))
            .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department.Trim()))
            .ForMember(dest => dest.Designation, opt => opt.MapFrom(src => src.Designation.Trim()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => EmployeeStatus.Bench))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Skills, opt => opt.Ignore())
            .ForMember(dest => dest.Allocations, opt => opt.Ignore())
            .ForMember(dest => dest.Timesheets, opt => opt.Ignore());

        CreateMap<Employee, EmployeeListItemDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString().ToUpperInvariant()));

        CreateMap<EmployeeSkill, EmployeeSkillDto>()
            .ForMember(dest => dest.SkillId, opt => opt.MapFrom(src => src.SkillId))
            .ForMember(dest => dest.SkillName, opt => opt.MapFrom(src => src.Skill.Name))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.ToString()))
            .ForMember(dest => dest.ProficiencyLevel, opt => opt.MapFrom(src => src.ProficiencyLevel.ToString()));
    }
}
