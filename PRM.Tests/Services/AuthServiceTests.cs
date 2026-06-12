using AutoMapper;
using Moq;
using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;
using PRM.Business.Services;
using PRM.Common.Exceptions;
using PRM.Models.DTOs.Auth;
using PRM.Models.Entities;

namespace PRM.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_userRepoMock.Object, _hasherMock.Object, _mapperMock.Object);
    }


    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsLoginResponse()
    {
        var user = MakeActiveUser(id: 1, username: "alice", passwordHash: "hashed");
        _userRepoMock
            .Setup(r => r.GetByUsernameAsync("alice", default))
            .ReturnsAsync(user);
        _hasherMock
            .Setup(h => h.Verify("secret", "hashed"))
            .Returns(true);
        var expectedResponse = new LoginResponse { Username = "alice" };
        _mapperMock
            .Setup(m => m.Map<LoginResponse>(user))
            .Returns(expectedResponse);

        var result = await _sut.LoginAsync(new LoginRequest { Username = "alice", Password = "secret" });

        Assert.Equal("alice", result.Username);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsBusinessValidationException()
    {
        _userRepoMock
            .Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), default))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.LoginAsync(new LoginRequest { Username = "ghost", Password = "pw" }));
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsBusinessValidationException()
    {
        var user = MakeActiveUser(id: 2, username: "bob");
        user.IsActive = false;
        _userRepoMock
            .Setup(r => r.GetByUsernameAsync("bob", default))
            .ReturnsAsync(user);

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.LoginAsync(new LoginRequest { Username = "bob", Password = "pw" }));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsBusinessValidationException()
    {
        var user = MakeActiveUser(id: 3, username: "carol", passwordHash: "hashed");
        _userRepoMock
            .Setup(r => r.GetByUsernameAsync("carol", default))
            .ReturnsAsync(user);
        _hasherMock
            .Setup(h => h.Verify("wrong", "hashed"))
            .Returns(false);

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.LoginAsync(new LoginRequest { Username = "carol", Password = "wrong" }));
    }

    [Theory]
    [InlineData("", "pw")]
    [InlineData("user", "")]
    [InlineData("  ", "pw")]
    public async Task LoginAsync_EmptyCredentials_ThrowsBusinessValidationException(
        string username, string password)
    {
        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.LoginAsync(new LoginRequest { Username = username, Password = password }));
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidRequest_ReturnsSuccessMessage()
    {
        var user = MakeActiveUser(id: 10, username: "dave");
        _userRepoMock
            .Setup(r => r.GetByIdAsync(10, default))
            .ReturnsAsync(user);
        _hasherMock
            .Setup(h => h.Hash("NewPass@1"))
            .Returns("newHash");

        var result = await _sut.ChangePasswordAsync(10, new ChangePasswordRequest
        {
            NewPassword = "NewPass@1",
            ConfirmPassword = "NewPass@1"
        });

        Assert.Contains("Password updated", result);
        Assert.Equal("newHash", user.PasswordHash);
        Assert.False(user.ForcePasswordChange);
    }

    [Fact]
    public async Task ChangePasswordAsync_PasswordMismatch_ThrowsBusinessValidationException()
    {
        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.ChangePasswordAsync(1, new ChangePasswordRequest
            {
                NewPassword = "NewPass@1",
                ConfirmPassword = "Different@1"
            }));
    }

    [Fact]
    public async Task ChangePasswordAsync_UserNotFound_ThrowsBusinessValidationException()
    {
        _userRepoMock
            .Setup(r => r.GetByIdAsync(99, default))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.ChangePasswordAsync(99, new ChangePasswordRequest
            {
                NewPassword = "NewPass@1",
                ConfirmPassword = "NewPass@1"
            }));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("NewPass@1", "")]
    public async Task ChangePasswordAsync_EmptyPassword_ThrowsBusinessValidationException(
        string newPw, string confirmPw)
    {
        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.ChangePasswordAsync(1, new ChangePasswordRequest
            {
                NewPassword = newPw,
                ConfirmPassword = confirmPw
            }));
    }


    private static User MakeActiveUser(int id, string username, string passwordHash = "hash") =>
        new()
        {
            Id = id,
            Username = username,
            FullName = "Test User",
            Email = $"{username}@test.com",
            PasswordHash = passwordHash,
            IsActive = true
        };
}
