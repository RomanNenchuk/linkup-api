using Application.Auth.Commands.Register;
using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using Moq;

namespace Tests.Application.Auth.Commands.Register;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IAccountService> _accountService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IVerificationLinkService> _linkService = new();
    private readonly Mock<ITokenService> _tokenService = new();

    private RegisterCommandHandler CreateHandler() =>
        new(_accountService.Object, _emailService.Object, _linkService.Object, _tokenService.Object);

    private static User CreateUser() =>
        new User { Id = "u1", Email = "test@example.com" };

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserCreationFails()
    {
        _accountService
            .Setup(s => s.CreateUserAsync("test@example.com", "User", "Password123"))
            .ReturnsAsync(Result<User>.Failure("Creation failed", 400));

        var handler = CreateHandler();

        var result = await handler.Handle(new RegisterCommand
        {
            Email = "test@example.com",
            DisplayName = "User",
            Password = "Password123"
        }, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("Creation failed", result.Error);
        Assert.Equal(400, result.Code);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenEmailConfirmationTokenGenerationFails()
    {
        var user = CreateUser();

        _accountService
            .Setup(s => s.CreateUserAsync(user.Email, "User", "Password123"))
            .ReturnsAsync(Result<User>.Success(user));

        _tokenService
            .Setup(s => s.GenerateEmailConfirmationTokenAsync(user))
            .ReturnsAsync(Result<string>.Failure("Token error", 500));

        var handler = CreateHandler();

        var result = await handler.Handle(new RegisterCommand
        {
            Email = user.Email,
            DisplayName = "User",
            Password = "Password123"
        }, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("Token error", result.Error);
        Assert.Equal(500, result.Code);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenSavingTokenFails()
    {
        var user = CreateUser();

        _accountService
            .Setup(s => s.CreateUserAsync(user.Email, "User", "Password123"))
            .ReturnsAsync(Result<User>.Success(user));

        _tokenService
            .Setup(s => s.GenerateEmailConfirmationTokenAsync(user))
            .ReturnsAsync(Result<string>.Success("confirmation-token"));

        _tokenService
            .Setup(s => s.SaveVerificationTokenAsync("confirmation-token", user.Id, VerificationTokenType.EmailVerification))
            .ReturnsAsync(Result.Failure("Saving failed", 500));

        var handler = CreateHandler();

        var result = await handler.Handle(new RegisterCommand
        {
            Email = user.Email,
            DisplayName = "User",
            Password = "Password123"
        }, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("Saving failed", result.Error);
        Assert.Equal(500, result.Code);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenRefreshTokenFails()
    {
        var user = CreateUser();

        _accountService
            .Setup(s => s.CreateUserAsync(user.Email, "User", "Password123"))
            .ReturnsAsync(Result<User>.Success(user));

        _tokenService
            .Setup(s => s.GenerateEmailConfirmationTokenAsync(user))
            .ReturnsAsync(Result<string>.Success("confirmation-token"));

        _tokenService
            .Setup(s => s.SaveVerificationTokenAsync("confirmation-token", user.Id, VerificationTokenType.EmailVerification))
            .ReturnsAsync(Result.Success());

        _tokenService
            .Setup(s => s.IssueRefreshToken(user))
            .ReturnsAsync(Result<string>.Failure("RT error", 400));

        var handler = CreateHandler();

        var result = await handler.Handle(new RegisterCommand
        {
            Email = user.Email,
            DisplayName = "User",
            Password = "Password123"
        }, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("Failed to issue refresh token. Please, try to login", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenEverythingIsValid()
    {
        var user = CreateUser();

        _accountService
            .Setup(s => s.CreateUserAsync(user.Email, "User", "Password123"))
            .ReturnsAsync(Result<User>.Success(user));

        _tokenService
            .Setup(s => s.GenerateEmailConfirmationTokenAsync(user))
            .ReturnsAsync(Result<string>.Success("confirm-token"));

        _tokenService
            .Setup(s => s.SaveVerificationTokenAsync("confirm-token", user.Id, VerificationTokenType.EmailVerification))
            .ReturnsAsync(Result.Success());

        _linkService
            .Setup(s => s.BuildEmailConfirmationLink("confirm-token"))
            .Returns("https://example.com/confirm?token=confirm-token");

        _tokenService
            .Setup(s => s.IssueRefreshToken(user))
            .ReturnsAsync(Result<string>.Success("refresh-token"));

        _tokenService
            .Setup(s => s.GenerateAccessToken(user))
            .Returns("access-token");

        var handler = CreateHandler();

        var result = await handler.Handle(new RegisterCommand
        {
            Email = user.Email,
            DisplayName = "User",
            Password = "Password123"
        }, default);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value!.AccessToken);
        Assert.Equal("refresh-token", result.Value.RefreshToken);
    }
}
