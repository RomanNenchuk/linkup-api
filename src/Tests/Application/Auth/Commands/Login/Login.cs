using Application.Auth.Commands.Login;
using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using Moq;

namespace Tests.Application.Auth.Commands.Login;

public class LoginCommandHandlerTests
{
    private readonly Mock<IAccountService> _accountService = new();
    private readonly Mock<ITokenService> _tokenService = new();

    private LoginCommandHandler CreateHandler()
        => new(_accountService.Object, _tokenService.Object);

    private static User CreateUserDto()
        => new User { Id = "user-1", Email = "test@example.com" };

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenLoginFails()
    {
        // Arrange
        _accountService
            .Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Result<User>.Failure("Invalid credentials", 401));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new LoginCommand { Email = "a@a.com", Password = "Whatever1" }, default);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.Code);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenRefreshTokenIssuanceFails()
    {
        var user = CreateUserDto();

        _accountService.Setup(s => s.LoginAsync(user.Email, "Password1"))
            .ReturnsAsync(Result<User>.Success(user));

        _tokenService.Setup(t => t.IssueRefreshToken(user))
            .ReturnsAsync(Result<string>.Failure("Cannot issue refresh token", 500));

        var handler = CreateHandler();

        var result = await handler.Handle(new LoginCommand { Email = user.Email, Password = "Password1" }, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.Code);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenCredentialsValidAndTokenIssued()
    {
        var user = CreateUserDto();

        _accountService.Setup(s => s.LoginAsync(user.Email, "GoodPass123"))
            .ReturnsAsync(Result<User>.Success(user));

        _tokenService.Setup(t => t.IssueRefreshToken(user))
            .ReturnsAsync(Result<string>.Success("refresh-token-xyz"));

        _tokenService.Setup(t => t.GenerateAccessToken(user))
            .Returns("access-token-abc");

        var handler = CreateHandler();

        var result = await handler.Handle(new LoginCommand { Email = user.Email, Password = "GoodPass123" }, default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("access-token-abc", result.Value.AccessToken);
        Assert.Equal("refresh-token-xyz", result.Value.RefreshToken);
    }
}
