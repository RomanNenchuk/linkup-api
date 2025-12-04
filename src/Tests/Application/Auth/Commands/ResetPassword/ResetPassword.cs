using Application.Auth.Commands.ResetPassword;
using Application.Common;
using Application.Common.Interfaces;
using Moq;

namespace Tests.Application.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IAccountService> _accountService = new();

    private ResetPasswordCommandHandler CreateHandler()
        => new(_accountService.Object);

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenResetFails()
    {
        // Arrange
        _accountService
            .Setup(s => s.ResetPasswordAsync("token-123", "NewPass1"))
            .ReturnsAsync(Result.Failure("Invalid token", 400));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(
            new ResetPasswordCommand
            {
                VerificationToken = "token-123",
                NewPassword = "NewPass1"
            },
            default);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.Code);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenPasswordResetOk()
    {
        // Arrange
        _accountService
            .Setup(s => s.ResetPasswordAsync("valid-token", "SuperPass1"))
            .ReturnsAsync(Result.Success());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(
            new ResetPasswordCommand
            {
                VerificationToken = "valid-token",
                NewPassword = "SuperPass1"
            },
            default);

        // Assert
        Assert.True(result.IsSuccess);
    }
}
