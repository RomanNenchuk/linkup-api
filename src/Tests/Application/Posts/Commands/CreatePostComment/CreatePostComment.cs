using Application.Common;
using Application.Common.Interfaces;
using Application.Posts.Commands.CreatePostComment;
using Moq;

namespace Tests.Application.Posts.Commands.CreatePostComment
{
    public class CreatePostCommentCommandHandlerTests
    {
        private readonly Mock<ICommentService> _commentService = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenCommentCreated()
        {
            // arrange
            _currentUser.Setup(x => x.Id).Returns("user-1");

            _commentService
                .Setup(x => x.CreatePostCommentAsync(It.IsAny<CreatePostCommentDto>()))
                .ReturnsAsync(Result<string>.Success("comment-id"));

            var handler = new CreatePostCommentCommandHandler(
                _commentService.Object,
                _currentUser.Object
            );

            var command = new CreatePostCommentCommand
            {
                PostId = "post-1",
                Content = "Valid content"
            };

            // act
            var result = await handler.Handle(command, CancellationToken.None);

            // assert
            Assert.True(result.IsSuccess);
            Assert.Equal("comment-id", result.Value);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenServiceFails()
        {
            // arrange
            _currentUser.Setup(x => x.Id).Returns("user-1");

            _commentService
                .Setup(x => x.CreatePostCommentAsync(It.IsAny<CreatePostCommentDto>()))
                .ReturnsAsync(Result<string>.Failure("Error creating comment"));

            var handler = new CreatePostCommentCommandHandler(
                _commentService.Object,
                _currentUser.Object
            );

            var command = new CreatePostCommentCommand
            {
                PostId = "post-1",
                Content = "Valid content"
            };

            // act
            var result = await handler.Handle(command, CancellationToken.None);

            // assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Error creating comment", result.Error);
        }
    }
}