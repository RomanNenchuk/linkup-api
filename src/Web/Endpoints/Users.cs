using Application.Users.Commands.ToggleFollow;
using Application.Users.Queries.GetUserInfo;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Web.Infrastructure;

namespace Web.Endpoints;

public class Users : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(GetUserInfo, "{userId}");

        app.MapGroup(this)
            .RequireAuthorization()
            .MapPost(ToggleFollow, "{followeeId}/toggle-follow");
    }


    private async Task<IResult> GetUserInfo(ISender sender, string userId)
    {
        var result = await sender.Send(new GetUserInfoQuery
        {
            UserId = userId
        });

        return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
    }

    private async Task<IResult> ToggleFollow(ISender sender, [FromRoute] string followeeId, [FromBody] ToggleFollowRequest request)
    {
        var result = await sender.Send(new ToggleFollowCommand
        {
            FolloweeId = followeeId,
            IsFollowed = request.IsFollowed
        });

        return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result.Error);
    }
}
