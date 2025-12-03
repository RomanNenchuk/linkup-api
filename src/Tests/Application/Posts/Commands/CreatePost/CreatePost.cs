using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Posts.Commands.CreatePost;
using Moq;

namespace Tests.Application.Posts.Commands.CreatePost;

public class CreatePostCommandHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSuccess_WhenPostServiceCreatesPost()
    {
        // arrange
        var mockPostService = new Mock<IPostService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        mockCurrentUser.Setup(x => x.Id).Returns("user-123");

        mockPostService
            .Setup(s => s.CreatePostAsync(It.IsAny<CreatePostDto>()))
            .ReturnsAsync(Result<string>.Success("new-post-id"));

        var handler = new CreatePostCommandHandler(
            mockPostService.Object,
            mockCurrentUser.Object
        );

        var command = new CreatePostCommand
        {
            Content = "Hello world!",
            Latitude = 10,
            Longitude = 20,
            Address = "Somewhere",
            ImageRecords =
            [
                new CloudinaryUploadDto { Url = "url", PublicId = "id" }
            ]
        };

        // act
        var result = await handler.Handle(command, CancellationToken.None);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new-post-id", result.Value);

        mockPostService.Verify(s =>
            s.CreatePostAsync(It.IsAny<CreatePostDto>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenPostServiceFails()
    {
        // arrange
        var mockPostService = new Mock<IPostService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        mockCurrentUser.Setup(x => x.Id).Returns("user-123");

        mockPostService
            .Setup(s => s.CreatePostAsync(It.IsAny<CreatePostDto>()))
            .ReturnsAsync(Result<string>.Failure("some error"));

        var handler = new CreatePostCommandHandler(
            mockPostService.Object,
            mockCurrentUser.Object
        );

        var command = new CreatePostCommand
        {
            Content = "Test content"
        };

        // act
        var result = await handler.Handle(command, CancellationToken.None);

        // assert
        Assert.False(result.IsSuccess);
        Assert.Equal("some error", result.Error);

        mockPostService.Verify(s =>
            s.CreatePostAsync(It.IsAny<CreatePostDto>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_PassesCorrectDto_ToService()
    {
        // arrange
        var mockPostService = new Mock<IPostService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        mockCurrentUser.Setup(x => x.Id).Returns("user-123");

        CreatePostDto? capturedDto = null;

        mockPostService
            .Setup(s => s.CreatePostAsync(It.IsAny<CreatePostDto>()))
            .Callback<CreatePostDto>(dto => capturedDto = dto)
            .ReturnsAsync(Result<string>.Success("id"));

        var handler = new CreatePostCommandHandler(
            mockPostService.Object,
            mockCurrentUser.Object
        );

        var command = new CreatePostCommand
        {
            Content = "Hello!",
            Latitude = 1,
            Longitude = 2,
            Address = "UA",
            ImageRecords =
            [
                new CloudinaryUploadDto { Url = "aaa", PublicId = "bbb" }
            ]
        };

        // act
        await handler.Handle(command, CancellationToken.None);

        // assert that CreatePostDto was built correctly:
        Assert.NotNull(capturedDto);
        Assert.Equal("user-123", capturedDto!.AuthorId);
        Assert.Equal("Hello!", capturedDto.Content);
        Assert.Equal(1, capturedDto.Latitude);
        Assert.Equal(2, capturedDto.Longitude);
        Assert.Equal("UA", capturedDto.Address);
        Assert.Single(capturedDto.Photos!);
    }
}
