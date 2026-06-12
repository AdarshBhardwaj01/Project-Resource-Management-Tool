using Microsoft.Extensions.Logging;
using Moq;
using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;
using PRM.Business.Services;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Tests.Services;

public class PrmSchedulerServiceTests
{
    private readonly Mock<IResourceRepository> _resourceRepoMock = new();
    private readonly Mock<IProjectRepository> _projectRepoMock = new();
    private readonly Mock<ISystemConfigRepository> _systemConfigRepoMock = new();
    private readonly Mock<ITimesheetSchedulerService> _timesheetSchedulerMock = new();
    private readonly Mock<IEmailNotificationService> _emailNotificationMock = new();
    private readonly Mock<ILogger<PrmSchedulerService>> _loggerMock = new();
    private readonly PrmSchedulerService _sut;

    public PrmSchedulerServiceTests()
    {
        _sut = new PrmSchedulerService(
            _resourceRepoMock.Object,
            _projectRepoMock.Object,
            _systemConfigRepoMock.Object,
            _timesheetSchedulerMock.Object,
            _emailNotificationMock.Object,
            _loggerMock.Object);
    }

    // ── RecomputeResourceAsync ────────────────────────────────────────────────

    [Fact]
    public async Task RecomputeResourceAsync_ResourceNotFound_DoesNotSaveChanges()
    {
        _resourceRepoMock
            .Setup(r => r.GetByUserIdForSchedulerUpdateAsync(1, default))
            .ReturnsAsync((Resource?)null);

        await _sut.RecomputeResourceAsync(userId: 1);

        _resourceRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task RecomputeResourceAsync_InactiveUser_DoesNotSaveChanges()
    {
        var resource = MakeResource(userId: 2, isUserActive: false);
        _resourceRepoMock
            .Setup(r => r.GetByUserIdForSchedulerUpdateAsync(2, default))
            .ReturnsAsync(resource);

        await _sut.RecomputeResourceAsync(userId: 2);

        _resourceRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task RecomputeResourceAsync_ActiveResource_AppliesStateAndSaves()
    {
        // Arrange: resource on bench, no allocations
        var resource = MakeResource(userId: 3, isUserActive: true);
        _resourceRepoMock
            .Setup(r => r.GetByUserIdForSchedulerUpdateAsync(3, default))
            .ReturnsAsync(resource);
        _projectRepoMock
            .Setup(r => r.GetManagerUserIdsAsync(default))
            .ReturnsAsync(new List<int>());
        _resourceRepoMock
            .Setup(r => r.SaveChangesAsync(default))
            .Returns(Task.CompletedTask);

        await _sut.RecomputeResourceAsync(userId: 3);

        // Bench resource with no allocations stays Bench
        Assert.Equal(ResourceStatus.Bench, resource.Status);
        Assert.Equal(0, resource.UtilisationPercent);
        _resourceRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task RecomputeResourceAsync_ResourceIsManagerOfProject_SetsAllocatedStatus()
    {
        // Arrange: user ID 10 manages a project
        var resource = MakeResource(userId: 10, isUserActive: true);
        _resourceRepoMock
            .Setup(r => r.GetByUserIdForSchedulerUpdateAsync(10, default))
            .ReturnsAsync(resource);
        _projectRepoMock
            .Setup(r => r.GetManagerUserIdsAsync(default))
            .ReturnsAsync(new List<int> { 10 }); // userId 10 is a manager

        await _sut.RecomputeResourceAsync(userId: 10);

        Assert.Equal(ResourceStatus.Allocated, resource.Status);
    }

    // ── RecomputeAllResourcesAsync ────────────────────────────────────────────

    [Fact]
    public async Task RecomputeAllResourcesAsync_NoResources_DoesNotSaveChanges()
    {
        _resourceRepoMock
            .Setup(r => r.GetAllActiveWithAllocationsAsync(default))
            .ReturnsAsync(new List<Resource>());
        _projectRepoMock
            .Setup(r => r.GetManagerUserIdsAsync(default))
            .ReturnsAsync(new List<int>());

        await _sut.RecomputeAllResourcesAsync();

        _resourceRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task RecomputeAllResourcesAsync_ResourceStatusChanges_SavesOnce()
    {
        // Arrange: a resource currently Allocated but has no active allocations → should flip to Bench
        var resource = MakeResource(userId: 5, isUserActive: true, status: ResourceStatus.Allocated);
        _resourceRepoMock
            .Setup(r => r.GetAllActiveWithAllocationsAsync(default))
            .ReturnsAsync(new List<Resource> { resource });
        _projectRepoMock
            .Setup(r => r.GetManagerUserIdsAsync(default))
            .ReturnsAsync(new List<int>());
        _resourceRepoMock
            .Setup(r => r.SaveChangesAsync(default))
            .Returns(Task.CompletedTask);

        await _sut.RecomputeAllResourcesAsync();

        Assert.Equal(ResourceStatus.Bench, resource.Status);
        _resourceRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    // ── RunScheduledTasksAsync ────────────────────────────────────────────────

    [Fact]
    public async Task RunScheduledTasksAsync_InvokesBothRecomputeSteps()
    {
        _resourceRepoMock
            .Setup(r => r.GetAllActiveWithAllocationsAsync(default))
            .ReturnsAsync(new List<Resource>());
        _projectRepoMock
            .Setup(r => r.GetManagerUserIdsAsync(default))
            .ReturnsAsync(new List<int>());
        _systemConfigRepoMock
            .Setup(r => r.GetSingletonAsync(default))
            .ReturnsAsync((SystemConfig?)null);
        _projectRepoMock
            .Setup(r => r.GetAllForHealthSchedulerAsync(default))
            .ReturnsAsync(new List<Project>());
        _timesheetSchedulerMock
            .Setup(s => s.ProcessTimesheetWorkflowAsync(default))
            .Returns(Task.CompletedTask);

        await _sut.RunScheduledTasksAsync();

        _resourceRepoMock.Verify(r => r.GetAllActiveWithAllocationsAsync(default), Times.Once);
        _projectRepoMock.Verify(r => r.GetAllForHealthSchedulerAsync(default), Times.Once);
        _timesheetSchedulerMock.Verify(s => s.ProcessTimesheetWorkflowAsync(default), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Resource MakeResource(
        int userId,
        bool isUserActive,
        ResourceStatus status = ResourceStatus.Bench) =>
        new()
        {
            UserId = userId,
            Status = status,
            UtilisationPercent = 0,
            User = new User
            {
                Id = userId,
                FullName = $"User {userId}",
                IsActive = isUserActive
            },
            Allocations = new List<Allocation>()
        };
}
