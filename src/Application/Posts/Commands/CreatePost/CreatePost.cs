using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Posts.Commands.CreatePost;

public class CreatePostCommand : IRequest<Result<string>>
{
    public string Content { get; set; } = null!;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public List<CloudinaryUploadDto>? ImageRecords { get; set; }
}

public class CreatePostCommandHandler(IPostService postService, ICurrentUserService currentUserService)
    : IRequestHandler<CreatePostCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreatePostCommand request, CancellationToken ct)
    {
        var userId = currentUserService.Id!;
        var createPostDto = new CreatePostDto
        {
            AuthorId = userId,
            Content = request.Content,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Address = request.Address,
            Photos = request.ImageRecords,
        };

        var postResult = await postService.CreatePostAsync(createPostDto);

        return postResult.IsSuccess && postResult.Value != null
            ? Result<string>.Success(postResult.Value)
            : Result<string>.Failure(postResult.Error!);
    }
}
