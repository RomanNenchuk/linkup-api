using Application.Common;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Posts.Commands.CreatePostComment;

public class CreatePostCommentCommand : IRequest<Result<string>>
{
    public string PostId { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? RepliedTo { get; set; }
}

public class CreatePostCommentCommandHandler(IPostService postService, ICurrentUserService currentUserService)
    : IRequestHandler<CreatePostCommentCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreatePostCommentCommand request, CancellationToken ct)
    {
        var userId = currentUserService.Id!;
        var createPostCommentDto = new CreatePostCommentDto
        {
            PostId = request.PostId,
            AuthorId = userId,
            Content = request.Content,
        };

        var postResult = await postService.CreatePostCommentAsync(createPostCommentDto);

        return postResult.IsSuccess && postResult.Value != null
            ? Result<string>.Success(postResult.Value)
            : Result<string>.Failure(postResult.Error!);
    }
}
