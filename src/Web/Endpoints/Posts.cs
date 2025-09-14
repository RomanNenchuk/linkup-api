using Application.Posts.Commands.CreatePost;
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
           .RequireAuthorization()
           .MapPost(CreatePost, "");
    }

    private async Task<IResult> CreatePost(
        [FromBody] CreatePostRequest request,
        [FromServices] ISender sender
        // [FromServices] ICloudinaryService cloudinaryService
        )
    {
        var uploadedUrls = new List<string>();

        foreach (var file in request.PostPhotos)
        {
            // call Cloudinary service
            // var url = await _cloudinaryService.UploadAsync(file);
            // uploadedUrls.Add(url);
        }

        var command = new CreatePostCommand
        {
            Title = request.Title,
            Content = request.Content,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Address = request.Address,
            PhotoUrls = uploadedUrls
        };

        var result = await sender.Send(command);

        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
    }
}
