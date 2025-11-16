using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Posts.Commands.EditPost;

public class EditPostCommand : IRequest<Result>
{
    public string PostId { get; set; } = null!;
    public string? Content { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public List<CloudinaryUploadDto>? PhotosToAdd { get; set; }
    public List<string>? PhotosToDelete { get; set; }
}

public class EditPostCommandHandler(IPostService postService)
    : IRequestHandler<EditPostCommand, Result>
{
    public async Task<Result> Handle(EditPostCommand request, CancellationToken ct)
    {
        var editPostDto = new EditPostDto
        {
            PostId = request.PostId,
            Content = request.Content,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Address = request.Address,
            PhotosToAdd = request.PhotosToAdd,
            PhotosToDelete = request.PhotosToDelete,
        };

        var postResult = await postService.EditPostAsync(editPostDto);

        return postResult.IsSuccess ? Result.Success() : Result.Failure(postResult.Error!);
    }
}
