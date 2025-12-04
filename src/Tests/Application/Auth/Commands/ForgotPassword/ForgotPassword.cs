using Application.Auth.Commands.ForgotPassword;
using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using Moq;

namespace Tests.Application.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IVerificationLinkService> _linkService = new();
    private readonly Mock<ITokenService> _tokenService = new();

    private ForgotPasswordCommandHandler CreateHandler()
        => new(
            _userService.Object,
            _emailService.Object,
            _linkService.Object,
            _tokenService.Object
        );

    private static User CreateUser()
        => new()
        {
            Id = "123",
            DisplayName = "TestName",
            UserName = "test@example.com",
            Email = "test@example.com",
            EmailConfirmed = false
        };

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        _userService
            .Setup(s => s.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(Result<User>.Failure("User not found", 404));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ForgotPasswordCommand { Email = "a@a.com" }, default);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.Code);
    }


    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCooldownActive()
    {
        var user = CreateUser();

        _userService.Setup(s => s.GetUserByEmailAsync(user.Email))
            .ReturnsAsync(Result<User>.Success(user));

        _tokenService.Setup(t =>
                t.GetCooldownRemainingSecondsAsync(user.Id, VerificationTokenType.PasswordReset))
            .ReturnsAsync(30);

        var handler = CreateHandler();

        var result = await handler.Handle(new ForgotPasswordCommand { Email = user.Email }, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(429, result.Code);
    }


    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenTokenGenerationFails()
    {
        var user = CreateUser();

        _userService.Setup(s => s.GetUserByEmailAsync(user.Email))
            .ReturnsAsync(Result<User>.Success(user));

        _tokenService.Setup(t =>
                t.GetCooldownRemainingSecondsAsync(user.Id, VerificationTokenType.PasswordReset))
            .ReturnsAsync(0);

        _tokenService.Setup(t => t.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(Result<string>.Failure("Token error", 500));

        var handler = CreateHandler();

        var result = await handler.Handle(new ForgotPasswordCommand { Email = user.Email }, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.Code);
    }


    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenSavingTokenFails()
    {
        var user = CreateUser();

        _userService.Setup(s => s.GetUserByEmailAsync(user.Email))
            .ReturnsAsync(Result<User>.Success(user));

        _tokenService.Setup(t => t.GetCooldownRemainingSecondsAsync(user.Id,
                VerificationTokenType.PasswordReset))
            .ReturnsAsync(0);

        _tokenService.Setup(t => t.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(Result<string>.Success("reset-token"));

        _tokenService.Setup(t => t.SaveVerificationTokenAsync("reset-token", user.Id,
                VerificationTokenType.PasswordReset))
            .ReturnsAsync(Result.Failure("Save error", 500));

        var handler = CreateHandler();

        var result = await handler.Handle(new ForgotPasswordCommand { Email = user.Email }, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.Code);
    }


    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenEmailSendingFails()
    {
        var user = CreateUser();

        _userService.Setup(s => s.GetUserByEmailAsync(user.Email))
            .ReturnsAsync(Result<User>.Success(user));

        _tokenService.Setup(t => t.GetCooldownRemainingSecondsAsync(user.Id,
                VerificationTokenType.PasswordReset))
            .ReturnsAsync(0);

        _tokenService.Setup(t => t.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(Result<string>.Success("reset-token"));

        _tokenService.Setup(t => t.SaveVerificationTokenAsync("reset-token", user.Id,
                VerificationTokenType.PasswordReset))
            .ReturnsAsync(Result.Success());

        _linkService.Setup(l => l.BuildPasswordResetLink("reset-token"))
            .Returns("http://reset-link");

        _emailService.Setup(e => e.SendEmailAsync(user.Email, "Email reset", "http://reset-link"))
            .ReturnsAsync(Result<string>.Failure("Email failed", 400));

        var handler = CreateHandler();

        var result = await handler.Handle(new ForgotPasswordCommand { Email = user.Email }, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.Code);
    }


    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenEverythingIsValid()
    {
        var user = CreateUser();

        _userService.Setup(s => s.GetUserByEmailAsync(user.Email))
            .ReturnsAsync(Result<User>.Success(user));

        _tokenService.Setup(t => t.GetCooldownRemainingSecondsAsync(user.Id,
                VerificationTokenType.PasswordReset))
            .ReturnsAsync(0);

        _tokenService.Setup(t => t.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(Result<string>.Success("reset-token"));

        _tokenService.Setup(t => t.SaveVerificationTokenAsync("reset-token", user.Id,
                VerificationTokenType.PasswordReset))
            .ReturnsAsync(Result.Success());

        _linkService.Setup(l => l.BuildPasswordResetLink("reset-token"))
            .Returns("http://reset-link");

        _emailService.Setup(e => e.SendEmailAsync(user.Email, "Email reset", "http://reset-link"))
            .ReturnsAsync(Result<string>.Success(string.Empty));

        var handler = CreateHandler();

        var result = await handler.Handle(new ForgotPasswordCommand { Email = user.Email }, default);

        Assert.True(result.IsSuccess);
    }
}
