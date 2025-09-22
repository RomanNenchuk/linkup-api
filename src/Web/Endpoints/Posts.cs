using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Posts.Commands.CreatePost;
using Application.Posts.Commands.ToggleReaction;
using Application.Posts.Queries.GetPosts;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Ocsp;
using Web.DTOs;
using Web.Infrastructure;

namespace Web.Endpoints;

public class Posts : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(GetPosts, "");

        app.MapGroup(this)
           .RequireAuthorization()
           .MapPost(CreatePost, "")
           .MapPost(ToggleReaction, "{postId}/toggle-reaction");
    }

    private async Task<IResult> CreatePost(
        [FromForm] CreatePostRequest request,
        [FromServices] ISender sender,
        [FromServices] ICloudinaryService cloudinaryService
        )
    {
        var uploadedAssets = new List<CloudinaryUploadDto>();

        if (request.PostPhotos != null && request.PostPhotos.Count != 0)
        {
            foreach (var file in request.PostPhotos)
            {
                if (file == null || file.Length == 0) continue;

                await using var stream = file.OpenReadStream();
                var uploadResult = await cloudinaryService.UploadImageAsync(stream, file.FileName);

                if (!uploadResult.IsSuccess || uploadResult.Value == null)
                    return Results.BadRequest(uploadResult.Error);

                uploadedAssets.Add(uploadResult.Value);
            }
        }

        var command = new CreatePostCommand
        {
            Title = request.Title,
            Content = request.Content,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Address = request.Address,
            ImageRecords = uploadedAssets
        };

        var result = await sender.Send(command);

        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
    }

    private async Task<IResult> GetPosts(
        ISender sender,
        [FromQuery] bool ascending = false,
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 10)
    {
        var result = await sender.Send(new GetPostsQuery
        {
            Ascending = ascending,
            Cursor = cursor,
            PageSize = pageSize
        });

        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
    }

    private async Task<IResult> ToggleReaction(ISender sender, [FromRoute] string postId, [FromBody] ToggleLikeRequest request)
    {
        var result = await sender.Send(new ToggleReactionCommand
        {
            PostId = postId,
            IsLiked = request.IsLiked
        });

        return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
    }
}
