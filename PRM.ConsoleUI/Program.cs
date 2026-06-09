using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PRM.ConsoleUI.Services;
using PRM.ConsoleUI.UI.Helpers;
using PRM.ConsoleUI.UI.Menus;
using PRM.ConsoleUI.UI.Screens;
using PRM.ConsoleUI.UI.Screens.Allocations;
using PRM.ConsoleUI.UI.Screens.Employees;
using PRM.ConsoleUI.UI.Screens.Employee;
using PRM.ConsoleUI.UI.Screens.Manager;
using PRM.ConsoleUI.UI.Screens.Projects;
using PRM.ConsoleUI.UI.Screens.SystemConfig;
using PRM.ConsoleUI.UI.Screens.Users;

ConsoleEncoding.Configure();

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var apiBaseUrl = configuration["ApiSettings:BaseUrl"]
    ?? throw new InvalidOperationException("ApiSettings:BaseUrl is missing.");

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddSingleton<AuthSession>();

services.AddHttpClient<AuthApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

services.AddHttpClient<UserApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

services.AddHttpClient<EmployeeApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

services.AddHttpClient<ProjectApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

services.AddHttpClient<AllocationApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

services.AddHttpClient<SystemConfigApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

services.AddHttpClient<ManagerApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

services.AddHttpClient<EmployeePortalApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

services.AddScoped<LoginScreen>();
services.AddScoped<ChangePasswordScreen>();
services.AddScoped<CreateUserScreen>();
services.AddScoped<ViewAllUsersScreen>();
services.AddScoped<ResetUserPasswordScreen>();
services.AddScoped<DeactivateUserScreen>();
services.AddScoped<AddEmployeeScreen>();
services.AddScoped<ViewAllEmployeesScreen>();
services.AddScoped<AssignManagerScreen>();
services.AddScoped<UpdateEmployeeScreen>();
services.AddScoped<DeactivateEmployeeScreen>();
services.AddScoped<ManageEmployeeSkillsScreen>();
services.AddScoped<CreateProjectScreen>();
services.AddScoped<ViewAllProjectsScreen>();
services.AddScoped<UpdateProjectScreen>();
services.AddScoped<ManageProjectMilestonesScreen>();
services.AddScoped<ViewAllAllocationsScreen>();
services.AddScoped<SystemConfigurationScreen>();
services.AddScoped<ResourceDashboardScreen>();
services.AddScoped<AllocateResourceScreen>();
services.AddScoped<MyProjectsScreen>();
services.AddScoped<TeamTimesheetsScreen>();
services.AddScoped<AiAssistantScreen>();
services.AddScoped<SubmitTimesheetScreen>();
services.AddScoped<ViewMyTimesheetsScreen>();
services.AddScoped<ViewMyAllocationsScreen>();
services.AddScoped<ManageUsersMenu>();
services.AddScoped<ManageEmployeesMenu>();
services.AddScoped<ManageProjectsMenu>();
services.AddScoped<AdminMenu>();
services.AddScoped<ManagerMenu>();
services.AddScoped<EmployeeMenu>();
services.AddScoped<ApplicationHost>();

using var serviceProvider = services.BuildServiceProvider();
using var scope = serviceProvider.CreateScope();
var applicationHost = scope.ServiceProvider.GetRequiredService<ApplicationHost>();

await applicationHost.RunAsync();
