using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Posts.Commands.CreatePost;

public class CreatePostCommand : IRequest<Result<PostResponseDto>>
{
    public string Title { get; set; } = null!;
    public string? Content { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public List<CloudinaryUploadDto>? ImageRecords { get; set; }
}

public class CreatePostCommandHandler(IPostService postService, IUserService userService)
    : IRequestHandler<CreatePostCommand, Result<PostResponseDto>>
{
    public async Task<Result<PostResponseDto>> Handle(CreatePostCommand request, CancellationToken ct)
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
            ImageRecords = request.ImageRecords,
        };

        var postResult = await postService.CreatePostAsync(createPostDto);

        return postResult.IsSuccess && postResult.Value != null
            ? Result<PostResponseDto>.Success(postResult.Value)
            : Result<PostResponseDto>.Failure(postResult.Error!);
    }
}
