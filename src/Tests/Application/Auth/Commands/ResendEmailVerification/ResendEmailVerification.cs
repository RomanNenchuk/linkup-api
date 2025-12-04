using Application.Auth.Commands.ResendEmailVerification;
using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using Moq;

namespace Tests.Application.Auth.Commands.ResendEmailVerification;

public class ResendEmailVerificationCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUser;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<IUserService> _userService;
    private readonly Mock<IVerificationLinkService> _linkService;
    private readonly Mock<ITokenService> _tokenService;

    private readonly ResendEmailVerificationCommandHandler _handler;

    public ResendEmailVerificationCommandHandlerTests()
    {
        _currentUser = new Mock<ICurrentUserService>();
        _emailService = new Mock<IEmailService>();
        _userService = new Mock<IUserService>();
        _linkService = new Mock<IVerificationLinkService>();
        _tokenService = new Mock<ITokenService>();

        _handler = new ResendEmailVerificationCommandHandler(
            _currentUser.Object,
            _emailService.Object,
            _userService.Object,
            _linkService.Object,
            _tokenService.Object
        );
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenUserNotAuthorized()
    {
        _currentUser.Setup(x => x.Id).Returns((string?)null);

        var result = await _handler.Handle(new ResendEmailVerificationCommand(), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.Code);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenUserNotFound()
    {
        _currentUser.Setup(x => x.Id).Returns("user-1");

        _userService
            .Setup(u => u.GetUserByIdAsync("user-1"))
            .ReturnsAsync(Result<User>.Failure("Not found", 404));

        var result = await _handler.Handle(new ResendEmailVerificationCommand(), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.Code);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenCooldownActive()
    {
        _currentUser.Setup(x => x.Id).Returns("user-1");

        _userService.Setup(u => u.GetUserByIdAsync("user-1"))
            .ReturnsAsync(Result<User>.Success(new User { Id = "user-1", Email = "a@a.com" }));

        _tokenService
            .Setup(t => t.GetCooldownRemainingSecondsAsync("user-1", VerificationTokenType.EmailVerification))
            .ReturnsAsync(30);

        var result = await _handler.Handle(new ResendEmailVerificationCommand(), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(429, result.Code);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenTokenGenerationFails()
    {
        _currentUser.Setup(x => x.Id).Returns("user-1");

        _userService.Setup(u => u.GetUserByIdAsync("user-1"))
            .ReturnsAsync(Result<User>.Success(new User { Id = "user-1", Email = "a@a.com" }));

        _tokenService
            .Setup(t => t.GetCooldownRemainingSecondsAsync("user-1", VerificationTokenType.EmailVerification))
            .ReturnsAsync(0);

        _tokenService
            .Setup(t => t.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync(Result<string>.Failure("fail", 500));

        var result = await _handler.Handle(new ResendEmailVerificationCommand(), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.Code);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenSaveVerificationTokenFails()
    {
        _currentUser.Setup(x => x.Id).Returns("user-1");

        var user = new User { Id = "user-1", Email = "test@test.com" };

        _userService.Setup(u => u.GetUserByIdAsync("user-1"))
            .ReturnsAsync(Result<User>.Success(user));

        _tokenService.Setup(t => t.GetCooldownRemainingSecondsAsync("user-1", VerificationTokenType.EmailVerification))
            .ReturnsAsync(0);

        _tokenService.Setup(t => t.GenerateEmailConfirmationTokenAsync(user))
            .ReturnsAsync(Result<string>.Success("token123"));

        _tokenService.Setup(t => t.SaveVerificationTokenAsync("token123", "user-1", VerificationTokenType.EmailVerification))
            .ReturnsAsync(Result.Failure("save failed", 500));

        var result = await _handler.Handle(new ResendEmailVerificationCommand(), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.Code);
    }

    [Fact]
    public async Task Handle_SuccessfullySendsEmail()
    {
        _currentUser.Setup(x => x.Id).Returns("user-1");

        var user = new User { Id = "user-1", Email = "test@test.com" };

        _userService.Setup(u => u.GetUserByIdAsync("user-1"))
            .ReturnsAsync(Result<User>.Success(user));

        _tokenService.Setup(t => t.GetCooldownRemainingSecondsAsync("user-1", VerificationTokenType.EmailVerification))
            .ReturnsAsync(0);

        _tokenService.Setup(t => t.GenerateEmailConfirmationTokenAsync(user))
            .ReturnsAsync(Result<string>.Success("tokenXYZ"));

        _tokenService.Setup(t => t.SaveVerificationTokenAsync("tokenXYZ", "user-1", VerificationTokenType.EmailVerification))
            .ReturnsAsync(Result.Success());

        _linkService.Setup(l => l.BuildEmailConfirmationLink("tokenXYZ"))
            .Returns("http://example.com/confirm?token=tokenXYZ");

        var result = await _handler.Handle(new ResendEmailVerificationCommand(), default);

        Assert.True(result.IsSuccess);
        _emailService.Verify(e => e.SendEmailAsync("test@test.com", "Email confirmation",
            It.IsAny<string>()), Times.Once);
    }
}
