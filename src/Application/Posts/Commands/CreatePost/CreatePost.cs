using Application.Common;
using Application.Common.Interfaces;
using MediatR;
using PostEntity = Domain.Entities.Post;

namespace Application.Posts.Commands.CreatePost;

public class CreatePostCommand : IRequest<Result<PostEntity>>
{
    public string Title { get; set; } = null!;
    public string? Content { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public List<string>? PhotoUrls { get; set; }
}

public class CreatePostCommandHandler(IPostService postService, IUserService userService)
    : IRequestHandler<CreatePostCommand, Result<PostEntity>>
{
    public async Task<Result<PostEntity>> Handle(CreatePostCommand request, CancellationToken ct)
    {
        var userId = userService.Id!;
        var createPostDto = new CreatePostDto
        {
            AuthorId = userId,
            Title = request.Title,
            Content = request.Content,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Address = request.Address,
            PhotoUrls = request.PhotoUrls,
        };

        var postResult = await postService.CreatePostAsync(createPostDto);

        return postResult.IsSuccess && postResult.Value != null
            ? Result<PostEntity>.Success(postResult.Value)
            : Result<PostEntity>.Failure(postResult.Error!);
    }
}
