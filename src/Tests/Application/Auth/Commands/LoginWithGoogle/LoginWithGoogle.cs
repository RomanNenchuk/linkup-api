using System.Security.Claims;
using Application.Auth.Commands.LoginWithGoogle;
using Application.Common;
using Application.Common.Interfaces;
using Moq;
using Application.Common.Models;

namespace Tests.Application.Auth.Commands.LoginWithGoogle;

public class LoginWithGoogleCommandHandlerTests
{
    private readonly Mock<IAccountService> _accountService = new();
    private readonly Mock<ITokenService> _tokenService = new();

    private LoginWithGoogleCommandHandler CreateHandler()
        => new(_accountService.Object, _tokenService.Object);

    private static ClaimsPrincipal CreateClaimsPrincipal()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, "google@example.com")
        ], "Google");

        return new ClaimsPrincipal(identity);
    }

    private static User CreateUserDto() => new() { Id = "u1", Email = "google@example.com" };

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenGoogleLoginFails()
    {
        // Arrange
        var claims = CreateClaimsPrincipal();

        _accountService
            .Setup(s => s.LoginWithGoogleAsync(claims))
            .ReturnsAsync(Result<User>.Failure("Google login failed", 401));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new LoginWithGoogleCommand(claims), default);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.Code);
        Assert.Equal("Google login failed", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenRefreshTokenFails()
    {
        var claims = CreateClaimsPrincipal();
        var user = CreateUserDto();

        _accountService
            .Setup(s => s.LoginWithGoogleAsync(claims))
            .ReturnsAsync(Result<User>.Success(user));

        _tokenService
            .Setup(t => t.IssueRefreshToken(user))
            .ReturnsAsync(Result<string>.Failure("RT error", 500));

        var handler = CreateHandler();

        var result = await handler.Handle(new LoginWithGoogleCommand(claims), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.Code);
        Assert.Equal("RT error", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenLoginAndRefreshTokenSucceed()
    {
        var claims = CreateClaimsPrincipal();
        var user = CreateUserDto();

        _accountService
            .Setup(s => s.LoginWithGoogleAsync(claims))
            .ReturnsAsync(Result<User>.Success(user));

        _tokenService
            .Setup(t => t.IssueRefreshToken(user))
            .ReturnsAsync(Result<string>.Success("refresh-token-123"));

        var handler = CreateHandler();

        var result = await handler.Handle(new LoginWithGoogleCommand(claims), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("refresh-token-123", result.Value);
    }
}
