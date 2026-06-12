using Moq;
using PRM.Business.Interfaces.Repositories;
using PRM.Business.Services;
using PRM.Common.Exceptions;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Tests.Services;

public class AllocationServiceTests
{
    private readonly Mock<IAllocationRepository> _allocationRepoMock = new();
    private readonly AllocationService _sut;

    public AllocationServiceTests()
    {
        _sut = new AllocationService(_allocationRepoMock.Object);
    }

    // ── GetAllAllocationsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAllocationsAsync_NoFilter_ReturnsAllAllocations()
    {
        var allocations = new List<Allocation>
        {
            MakeAllocation(id: 1, toDate: DateTime.UtcNow.AddDays(10)),
            MakeAllocation(id: 2, toDate: DateTime.UtcNow.AddDays(20))
        };
        _allocationRepoMock
            .Setup(r => r.GetAllAsync(null, null, null, default))
            .ReturnsAsync(allocations);

        var result = await _sut.GetAllAllocationsAsync(null, null, null);

        Assert.Equal(2, result.Allocations.Count);
        Assert.Equal(2, result.Total);
    }

    [Fact]
    public async Task GetAllAllocationsAsync_ActiveFilter_PassesUppercaseStatusToRepo()
    {
        _allocationRepoMock
            .Setup(r => r.GetAllAsync(null, null, "ACTIVE", default))
            .ReturnsAsync(new List<Allocation>());

        await _sut.GetAllAllocationsAsync(null, null, "active");

        _allocationRepoMock.Verify(
            r => r.GetAllAsync(null, null, "ACTIVE", default),
            Times.Once);
    }

    [Theory]
    [InlineData("PENDING")]
    [InlineData("DONE")]
    [InlineData("invalid")]
    public async Task GetAllAllocationsAsync_InvalidStatus_ThrowsBusinessValidationException(string status)
    {
        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.GetAllAllocationsAsync(null, null, status));
    }

    [Fact]
    public async Task GetAllAllocationsAsync_NullStatus_DoesNotThrow()
    {
        _allocationRepoMock
            .Setup(r => r.GetAllAsync(null, null, null, default))
            .ReturnsAsync(new List<Allocation>());

        var exception = await Record.ExceptionAsync(() =>
            _sut.GetAllAllocationsAsync(null, null, null));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GetAllAllocationsAsync_CalculatesActiveAndExpiredCounts()
    {
        var today = DateTime.UtcNow.Date;
        var allocations = new List<Allocation>
        {
            MakeAllocation(id: 1, toDate: today.AddDays(5)),  // ACTIVE
            MakeAllocation(id: 2, toDate: today.AddDays(15)), // ACTIVE
            MakeAllocation(id: 3, toDate: today.AddDays(-2))  // EXPIRED
        };
        _allocationRepoMock
            .Setup(r => r.GetAllAsync(null, null, null, default))
            .ReturnsAsync(allocations);

        var result = await _sut.GetAllAllocationsAsync(null, null, null);

        Assert.Equal(3, result.Total);
        Assert.Equal(2, result.ActiveCount);
        Assert.Equal(1, result.ExpiredCount);
    }

    [Fact]
    public async Task GetAllAllocationsAsync_WithEmployeeIdFilter_PassesFilterToRepo()
    {
        _allocationRepoMock
            .Setup(r => r.GetAllAsync(42, null, null, default))
            .ReturnsAsync(new List<Allocation>());

        await _sut.GetAllAllocationsAsync(employeeId: 42, projectId: null, status: null);

        _allocationRepoMock.Verify(
            r => r.GetAllAsync(42, null, null, default),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAllocationsAsync_EmptyResult_ReturnsZeroCounts()
    {
        _allocationRepoMock
            .Setup(r => r.GetAllAsync(null, null, null, default))
            .ReturnsAsync(new List<Allocation>());

        var result = await _sut.GetAllAllocationsAsync(null, null, null);

        Assert.Empty(result.Allocations);
        Assert.Equal(0, result.Total);
        Assert.Equal(0, result.ActiveCount);
        Assert.Equal(0, result.ExpiredCount);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Allocation MakeAllocation(int id, DateTime toDate) =>
        new()
        {
            Id = id,
            UserId = 1,
            ProjectId = 1,
            UtilisationPercent = 50,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = toDate,
            Resource = new Resource
            {
                UserId = 1,
                Status = ResourceStatus.Allocated,
                User = new User
                {
                    Id = 1,
                    FullName = "Test Employee",
                    Designation = "Developer",
                    IsActive = true
                }
            },
            Project = new Project
            {
                Id = 1,
                Name = "Test Project"
            }
        };
}
