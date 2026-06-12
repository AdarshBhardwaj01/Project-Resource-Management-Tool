using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PRM.Business.Interfaces.Services;
using PRM.Business.Mappings;
using PRM.Business.Services;
using PRM.Business.Services.Ai;
using PRM.Common.Constants;

namespace PRM.Business.Extensions;

public static class BusinessServiceExtensions
{
    public static IServiceCollection AddPrmBusinessServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddAutoMapper(typeof(AuthMappingProfile).Assembly);
        services.AddHttpClient("GeminiLlm", client =>
        {
            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddHttpClient("GroqLlm", client =>
        {
            client.BaseAddress = new Uri("https://api.groq.com/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddHttpClient("GemmaLlm", client =>
        {
            client.BaseAddress = new Uri("http://164.52.211.238/");
            client.Timeout = TimeSpan.FromSeconds(120);
        });
        services.AddScoped<IAiService, PrmAiService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IAllocationService, AllocationService>();
        services.AddScoped<ISystemConfigService, SystemConfigService>();
        services.AddScoped<IEmployeeStatusSchedulerService, EmployeeStatusSchedulerService>();
        services.AddScoped<IPrmSchedulerService, PrmSchedulerService>();
        services.AddScoped<IManagerService, ManagerService>();
        services.AddScoped<IEmployeePortalService, EmployeePortalService>();
        return services;
    }
}
