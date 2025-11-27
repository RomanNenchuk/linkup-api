using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Users.Queries.GetUsersByDisplayName;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class UserService(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext,
    ICurrentUserService currentUser, IMapper mapper) : IUserService
{
    public async Task<Result<User>> GetUserByIdAsync(string id)
    {
        var applicationUser = await userManager.FindByIdAsync(id);
        if (applicationUser == null) return Result<User>.Failure("User not found", 404);
        return Result<User>.Success(mapper.Map<User>(applicationUser));
    }

    public async Task<Result<User>> GetUserByEmailAsync(string email)
    {
        var applicationUser = await userManager.FindByEmailAsync(email);
        if (applicationUser == null) return Result<User>.Failure("User not found", 404);
        return Result<User>.Success(mapper.Map<User>(applicationUser));
    }

    public async Task<Result<PagedResult<SearchedUserDto>>> GetUsersByDisplayNameAsync(GetUsersByDisplayNameQuery query)
    {
        string? currentUserId = currentUser.Id;
        int offset = 0;
        if (!string.IsNullOrEmpty(query.Cursor) && int.TryParse(query.Cursor, out var parsed))
            offset = parsed;

        var applicationUsers = await dbContext.Users
            .Where(u => EF.Functions.ILike(u.DisplayName, $"%{query.DisplayName}%"))
            .OrderByDescending(u => u.Followers.Count)
            .Skip(offset)
            .Take(query.PageSize)
            .ToListAsync();

        var followingIds = new List<string>();
        if (currentUserId != null)
        {
            var applicationUserIds = applicationUsers.Select(u => u.Id);
            followingIds = await dbContext.UserFollows.Where(u => u.FollowerId == currentUserId &&
                applicationUserIds.Contains(u.FolloweeId)).Select(u => u.FolloweeId).ToListAsync();
        }

        var mappedUsers = new List<SearchedUserDto>();
        foreach (var user in applicationUsers)
        {
            var mappedUser = mapper.Map<SearchedUserDto>(user);
            mappedUser.IsFollowed = currentUserId != null && followingIds.Contains(user.Id);
            mappedUsers.Add(mappedUser);
        }
        var newCursor = (offset + query.PageSize).ToString();
        var result = new PagedResult<SearchedUserDto>()
        {
            Items = mappedUsers,
            NextCursor = newCursor
        };

        return Result<PagedResult<SearchedUserDto>>.Success(result);
    }


    public async Task<Result> ToggleFollowAsync(string followerId, string followeeId, bool IsFollowed)
    {
        var followee = await userManager.FindByIdAsync(followeeId);
        if (followee == null) return Result.Failure("Followee not found");

        var existingUserFollow = await dbContext.UserFollows.FirstOrDefaultAsync(x =>
            x.FollowerId == followerId && x.FolloweeId == followeeId);

        if (existingUserFollow == null)
            dbContext.UserFollows.Add(new UserFollow { FollowerId = followerId, FolloweeId = followeeId });
        else dbContext.Remove(existingUserFollow);

        var result = await dbContext.SaveChangesAsync() > 0;

        return result ? Result.Success() : Result.Failure("Failed to toggle follow state");

    }

    public async Task<Result<UserProfileDto>> GetUserInformationAsync(string userId, string? currentUserId)
    {
        var user = await dbContext.Users
            .Include(u => u.Followers)
            .Include(u => u.Followings)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return Result<UserProfileDto>.Failure("User not found");

        var userInfo = mapper.Map<UserProfileDto>(user);
        userInfo.FollowersCount = user.Followers.Count;
        userInfo.FollowingCount = user.Followings.Count;

        if (currentUserId != null) userInfo.IsFollowing = user.Followers.Any(f => f.FollowerId == currentUserId);
        else userInfo.IsFollowing = false;


        return Result<UserProfileDto>.Success(userInfo);
    }

}