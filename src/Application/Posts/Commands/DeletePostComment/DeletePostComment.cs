using Application.Common;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Posts.Commands.DeletePost;

public class DeletePostCommentCommand : IRequest<Result>
{
    public string CommentId { get; set; } = null!;
}

public class DeletePostCommentCommandHandler(ICommentService commentService) : IRequestHandler<DeletePostCommentCommand, Result>
{
    public async Task<Result> Handle(DeletePostCommentCommand request, CancellationToken cancellationToken)
    {
        return await commentService.DeletePostCommentAsync(request.CommentId);
    }
}
