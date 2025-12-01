using Application.Users.Commands.ToggleFollow;
using Application.Users.Queries.GetRecommendedUsers;
using Application.Users.Queries.GetUserInfo;
using Application.Users.Queries.GetUsersByDisplayName;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Web.Infrastructure;

namespace Web.Endpoints;

public class Users : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(GetUserInfo, "{userId}")
            .MapGet(GetRecommendedUsers, "recommended")
            .MapGet(GetUsersByDisplayName, "");

        app.MapGroup(this)
            .RequireAuthorization()
            .MapPost(ToggleFollow, "{followeeId}/toggle-follow");
    }


    public async Task<IResult> GetUserInfo(ISender sender, string userId)
    {
        var result = await sender.Send(new GetUserInfoQuery
        {
            UserId = userId
        });

        return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
    }

    public async Task<IResult> ToggleFollow(ISender sender, [FromRoute] string followeeId, [FromBody] ToggleFollowRequest request)
    {
        var result = await sender.Send(new ToggleFollowCommand
        {
            FolloweeId = followeeId,
            IsFollowed = request.IsFollowed
        });

        return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result.Error);
    }

    public async Task<IResult> GetRecommendedUsers(ISender sender)
    {
        var result = await sender.Send(new GetRecommendedUsersQuery());
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
    }

    public async Task<IResult> GetUsersByDisplayName(
        ISender sender,
        [FromQuery] string? cursor,
        [FromQuery] string displayName,
        [FromQuery] int pageSize = 10
    )
    {
        var result = await sender.Send(new GetUsersByDisplayNameQuery()
        {
            DisplayName = displayName,
            Cursor = cursor,
            PageSize = pageSize
        });
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
    }
}
