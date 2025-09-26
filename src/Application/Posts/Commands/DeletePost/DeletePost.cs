using Application.Common;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Posts.Commands.DeletePost;

public class DeletePostCommand : IRequest<Result>
{
    public string PostId { get; set; } = null!;
}

public class DeletePostCommandHandler(IPostService postService) : IRequestHandler<DeletePostCommand, Result>
{
    public async Task<Result> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        return await postService.DeletePostAsync(request.PostId);
    }
}
