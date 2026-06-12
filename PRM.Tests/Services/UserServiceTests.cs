using AutoMapper;
using Moq;
using PRM.Business.Interfaces.Repositories;
using PRM.Business.Services;
using PRM.Common.Exceptions;
using PRM.Models.DTOs.Users;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IRoleRepository> _roleRepoMock = new();
    private readonly Mock<IResourceRepository> _resourceRepoMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(
            _userRepoMock.Object,
            _roleRepoMock.Object,
            _resourceRepoMock.Object,
            _hasherMock.Object,
            _mapperMock.Object);
    }

    // ── CreateUserAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_EmployeeRole_CreatesUserAndResourceProfile()
    {
        // Arrange
        var request = MakeCreateRequest(role: (int)ApplicationRole.Employee);
        SetupCreateUserSuccess(request, userId: 5);

        // Act
        var result = await _sut.CreateUserAsync(request);

        // Assert
        Assert.Contains("Account created", result);
        _resourceRepoMock.Verify(
            r => r.AddAsync(It.Is<Resource>(res => res.Status == ResourceStatus.Bench), default),
            Times.Once);
        _resourceRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_ManagerRole_CreatesUserAndResourceProfile()
    {
        var request = MakeCreateRequest(role: (int)ApplicationRole.Manager);
        SetupCreateUserSuccess(request, userId: 6);

        var result = await _sut.CreateUserAsync(request);

        Assert.Contains("Account created", result);
        _resourceRepoMock.Verify(
            r => r.AddAsync(It.Is<Resource>(res => res.Status == ResourceStatus.Bench), default),
            Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_AdminRole_DoesNotCreateResourceProfile()
    {
        var request = MakeCreateRequest(role: (int)ApplicationRole.Admin);
        SetupCreateUserSuccess(request, userId: 7);

        await _sut.CreateUserAsync(request);

        _resourceRepoMock.Verify(
            r => r.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateUsername_ThrowsBusinessValidationException()
    {
        var request = MakeCreateRequest();
        _userRepoMock
            .Setup(r => r.ExistsByUsernameAsync(request.Username, default))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.CreateUserAsync(request));
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateEmail_ThrowsBusinessValidationException()
    {
        var request = MakeCreateRequest();
        _userRepoMock
            .Setup(r => r.ExistsByUsernameAsync(request.Username, default))
            .ReturnsAsync(false);
        _userRepoMock
            .Setup(r => r.ExistsByEmailAsync(request.Email, default))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.CreateUserAsync(request));
    }

    [Theory]
    [InlineData("", "user@x.com", "username", "Pass@1")]
    [InlineData("Full Name", "", "username", "Pass@1")]
    [InlineData("Full Name", "user@x.com", "", "Pass@1")]
    [InlineData("Full Name", "user@x.com", "username", "")]
    public async Task CreateUserAsync_MissingFields_ThrowsBusinessValidationException(
        string fullName, string email, string username, string password)
    {
        var request = new CreateUserRequest
        {
            FullName = fullName,
            Email = email,
            Username = username,
            TemporaryPassword = password,
            Role = (int)ApplicationRole.Employee
        };

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.CreateUserAsync(request));
    }

    // ── DeactivateUserAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateUserAsync_ValidUser_DeactivatesUserAndResource()
    {
        var user = MakeUser(id: 10, isActive: true);
        _userRepoMock
            .Setup(r => r.FindByUsernameOrIdAsync("10", default))
            .ReturnsAsync(user);

        var result = await _sut.DeactivateUserAsync("10", currentUserId: 99);

        Assert.Contains("deactivated", result, StringComparison.OrdinalIgnoreCase);
        Assert.False(user.IsActive);
        _resourceRepoMock.Verify(r => r.DeactivateByUserIdAsync(10, default), Times.Once);
    }

    [Fact]
    public async Task DeactivateUserAsync_SelfDeactivation_ThrowsBusinessValidationException()
    {
        var user = MakeUser(id: 20, isActive: true);
        _userRepoMock
            .Setup(r => r.FindByUsernameOrIdAsync("20", default))
            .ReturnsAsync(user);

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.DeactivateUserAsync("20", currentUserId: 20));
    }

    [Fact]
    public async Task DeactivateUserAsync_AlreadyInactiveUser_ThrowsBusinessValidationException()
    {
        var user = MakeUser(id: 30, isActive: false);
        _userRepoMock
            .Setup(r => r.FindByUsernameOrIdAsync("30", default))
            .ReturnsAsync(user);

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.DeactivateUserAsync("30", currentUserId: 99));
    }

    [Fact]
    public async Task DeactivateUserAsync_UserNotFound_ThrowsBusinessValidationException()
    {
        _userRepoMock
            .Setup(r => r.FindByUsernameOrIdAsync("999", default))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.DeactivateUserAsync("999", currentUserId: 1));
    }

    // ── ReactivateUserAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ReactivateUserAsync_InactiveUser_ReactivatesUser()
    {
        var user = MakeUser(id: 40, isActive: false);
        _userRepoMock
            .Setup(r => r.GetByIdAsync(40, default))
            .ReturnsAsync(user);
        _resourceRepoMock
            .Setup(r => r.ReactivateByUserIdAsync(40, default))
            .ReturnsAsync(true);

        var result = await _sut.ReactivateUserAsync(40);

        Assert.Contains("reactivated", result, StringComparison.OrdinalIgnoreCase);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task ReactivateUserAsync_AlreadyActiveUser_ThrowsBusinessValidationException()
    {
        var user = MakeUser(id: 50, isActive: true);
        _userRepoMock
            .Setup(r => r.GetByIdAsync(50, default))
            .ReturnsAsync(user);

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.ReactivateUserAsync(50));
    }

    [Fact]
    public async Task ReactivateUserAsync_UserNotFound_ThrowsBusinessValidationException()
    {
        _userRepoMock
            .Setup(r => r.GetByIdAsync(999, default))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.ReactivateUserAsync(999));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static User MakeUser(int id, bool isActive) =>
        new() { Id = id, FullName = "Test User", Username = $"user{id}", IsActive = isActive };

    private static CreateUserRequest MakeCreateRequest(int role = (int)ApplicationRole.Employee) =>
        new()
        {
            FullName = "Test User",
            Email = "test@company.com",
            Username = "testuser",
            TemporaryPassword = "TestPass@1",
            Role = role
        };

    private void SetupCreateUserSuccess(CreateUserRequest request, int userId)
    {
        var newUser = new User { Id = userId, Username = request.Username };
        _userRepoMock.Setup(r => r.ExistsByUsernameAsync(request.Username, default)).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(request.Email, default)).ReturnsAsync(false);
        _roleRepoMock
            .Setup(r => r.GetByNameAsync(It.IsAny<string>(), default))
            .ReturnsAsync(new Role { Id = 1, RoleName = "Employee" });
        _mapperMock.Setup(m => m.Map<User>(request)).Returns(newUser);
        _hasherMock.Setup(h => h.Hash(request.TemporaryPassword)).Returns("hashed");
        _userRepoMock.Setup(r => r.AddAsync(newUser, default)).Returns(Task.CompletedTask);
        _userRepoMock.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);
        _userRepoMock.Setup(r => r.AssignRoleAsync(userId, 1, default)).Returns(Task.CompletedTask);
        _resourceRepoMock.Setup(r => r.AddAsync(It.IsAny<Resource>(), default)).Returns(Task.CompletedTask);
        _resourceRepoMock.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);
    }
}
