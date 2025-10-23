using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Posts.Commands.CreatePost;
using Application.Posts.Commands.CreatePostComment;
using Application.Posts.Commands.DeletePost;
using Application.Posts.Commands.EditPost;
using Application.Posts.Commands.TogglePostCommentReaction;
using Application.Posts.Commands.TogglePostReaction;
using Application.Posts.Queries.GetHeatmapPoints;
using Application.Posts.Queries.GetPost;
using Application.Posts.Queries.GetPostClusters;
using Application.Posts.Queries.GetPostComments;
using Application.Posts.Queries.GetPosts;
using Domain.Constants;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Web.DTOs;
using Web.Infrastructure;

namespace Web.Endpoints;

public class Posts : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
           .MapGet(GetPosts, "")
           .MapGet(GetHeatmapPoints, "heatmap-points")
           .MapGet(GetPostClusters, "clusters")
           .MapDelete(DeletePost, "{postId}")
           .MapDelete(DeletePostComment, "{postId}/comments/{commentId}")
           .MapGet(GetPost, "{postId}")
           .MapGet(GetPostComments, "{postId}/comments");

        app.MapGroup(this)
           .RequireAuthorization()
           .MapPost(CreatePost, "")
           .MapPatch(EditPost, "{postId}")
           .MapPost(TogglePostReaction, "{postId}/toggle-reaction")
           .MapPost(TogglePostCommentReaction, "{postId}/comments/{commentId}/toggle-reaction")
           .MapPost(CreatePostComment, "{postId}/comments");
    }

    private async Task<IResult> CreatePost(
        [FromForm] CreatePostRequest request,
        [FromServices] ISender sender,
        [FromServices] ICloudinaryService cloudinaryService
        )
    {
        var uploadedAssets = new List<CloudinaryUploadDto>();

        if (request.PostPhotos != null && request.PostPhotos.Count > PostConstants.MaxPhotosPerPost)
            return Results.BadRequest($"You can't upload more that {PostConstants.MaxPhotosPerPost} photos.");

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

    private async Task<IResult> EditPost(
        [FromForm] EditPostRequest request,
        [FromServices] ISender sender,
        [FromServices] ICloudinaryService cloudinaryService,
        [FromServices] IPostService postService,
        [FromRoute] string postId
        )
    {
        var uploadedAssets = new List<CloudinaryUploadDto>();

        if (request.PhotosToAdd != null && request.PhotosToAdd.Count > 0)
        {
            var validationResult = await postService.ValidatePhotoLimitAsync(postId, request.PhotosToAdd.Count,
                request.PostPhotosToDelete, CancellationToken.None);
            if (!validationResult.IsSuccess)
                return Results.BadRequest($"You can't upload more that {PostConstants.MaxPhotosPerPost} photos.");
        }

        if (request.PhotosToAdd != null && request.PhotosToAdd.Count != 0)
        {
            foreach (var file in request.PhotosToAdd)
            {
                if (file == null || file.Length == 0) continue;

                await using var stream = file.OpenReadStream();
                var uploadResult = await cloudinaryService.UploadImageAsync(stream, file.FileName);

                if (!uploadResult.IsSuccess || uploadResult.Value == null)
                    return Results.BadRequest(uploadResult.Error);

                uploadedAssets.Add(uploadResult.Value);
            }
        }

        var command = new EditPostCommand
        {
            PostId = postId,
            Title = request.Title,
            Content = request.Content,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Address = request.Address,
            PhotosToAdd = uploadedAssets,
            PhotosToDelete = request.PostPhotosToDelete
        };

        var result = await sender.Send(command);

        return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
    }

    private async Task<IResult> GetPosts(
        ISender sender,
        [FromQuery] string filter = "recent",
        [FromQuery] double? latitude = null,
        [FromQuery] double? longitude = null,
        [FromQuery] double radius = 10,
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 10)
    {
        if (!Enum.TryParse<PostFilterType>(filter, true, out var filterType))
            filterType = PostFilterType.Recent;

        var query = new GetPostsQuery
        {
            Filter = new PostFilter
            {
                Type = filterType,
                Latitude = latitude,
                Longitude = longitude,
                RadiusKm = radius
            },
            Cursor = cursor,
            PageSize = pageSize
        };
        var result = await sender.Send(query);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
    }
    private async Task<IResult> GetPost(ISender sender, [FromRoute] string postId)
    {
        var result = await sender.Send(new GetPostQuery { PostId = postId });
        return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
    }

    private async Task<IResult> TogglePostReaction(ISender sender, [FromRoute] string postId,
        [FromBody] TogglePostLikeRequest request)
    {
        var result = await sender.Send(new TogglePostReactionCommand
        {
            PostId = postId,
            IsLiked = request.IsLiked
        });

        return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
    }

    private async Task<IResult> TogglePostCommentReaction(ISender sender, [FromRoute] string postId,
        [FromRoute] string commentId, [FromBody] TogglePostCommentLikeRequest request)
    {
        var result = await sender.Send(new TogglePostCommentReactionCommand
        {
            CommentId = commentId,
            IsLiked = request.IsLiked
        });

        return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
    }

    private async Task<IResult> CreatePostComment(ISender sender, [FromRoute] string postId,
        [FromBody] CreatePostCommentRequest request)
    {
        var command = new CreatePostCommentCommand
        {
            PostId = postId,
            Content = request.Content,
            RepliedTo = request.RepliedTo
        };
        var result = await sender.Send(command);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
    }

    private async Task<IResult> DeletePost(string postId, ISender sender)
    {
        var result = await sender.Send(new DeletePostCommand { PostId = postId });
        return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
    }

    private async Task<IResult> DeletePostComment(string postId, string commentId, ISender sender)
    {
        var result = await sender.Send(new DeletePostCommentCommand { CommentId = commentId });
        return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
    }

    private async Task<IResult> GetHeatmapPoints(
        ISender sender, [FromQuery] double minLat, [FromQuery] double minLon,
        [FromQuery] double maxLat, [FromQuery] double maxLon, [FromQuery] int zoom = 6)
    {
        var result = await sender.Send(new GetHeatmapPointsQuery
        {
            MinLat = minLat,
            MaxLat = maxLat,
            MinLon = minLon,
            MaxLon = maxLon,
            Zoom = zoom
        });

        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
    }

    private async Task<IResult> GetPostClusters(ISender sender)
    {
        var result = await sender.Send(new GetPostClustersQuery());
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
    }

    private async Task<IResult> GetPostComments([FromRoute] string postId, ISender sender)
    {
        var result = await sender.Send(new GetPostCommentsQuery { PostId = postId });
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
    }
}
