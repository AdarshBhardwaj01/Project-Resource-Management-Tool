using AutoMapper;
using PRM.Models.DTOs.Projects;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Mappings;

public class ProjectMappingProfile : Profile
{
    public ProjectMappingProfile()
    {
        CreateMap<CreateProjectRequest, Project>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description.Trim()))
            .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate.Date))
            .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate.Date))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (ProjectStatus)src.Status))
            .ForMember(dest => dest.HealthStatus, opt => opt.MapFrom(_ => ProjectHealthStatus.OnTrack))
            .ForMember(dest => dest.Manager, opt => opt.Ignore())
            .ForMember(dest => dest.Milestones, opt => opt.Ignore())
            .ForMember(dest => dest.Allocations, opt => opt.Ignore())
            .ForMember(dest => dest.TimesheetEntries, opt => opt.Ignore());

        CreateMap<Project, ProjectListItemDto>()
            .ForMember(dest => dest.ManagerName, opt => opt.MapFrom(src => src.Manager.FullName))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => FormatProjectStatus(src.Status)))
            .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate.ToString("dd-MMM-yy")))
            .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate.ToString("dd-MMM-yy")))
            .ForMember(dest => dest.MilestoneCount, opt => opt.MapFrom(src => src.Milestones.Count));

        CreateMap<Milestone, MilestoneItemDto>()
            .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => src.DueDate.ToString("dd-MMM-yy")))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => FormatMilestoneStatus(src.Status)));
    }

    private static string FormatMilestoneStatus(MilestoneStatus status)
    {
        return status switch
        {
            MilestoneStatus.NotStarted => "NOT_STARTED",
            MilestoneStatus.InProgress => "IN_PROGRESS",
            MilestoneStatus.Done => "DONE",
            _ => status.ToString().ToUpperInvariant()
        };
    }

    private static string FormatProjectStatus(ProjectStatus status)
    {
        return status == ProjectStatus.OnHold
            ? "ON_HOLD"
            : status.ToString().ToUpperInvariant();
    }
}
