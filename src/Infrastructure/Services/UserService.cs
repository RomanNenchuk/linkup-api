using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Users.Queries.GetUsersByDisplayName;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Services;

public class UserService(
    UserManager<ApplicationUser> userManager,
    IUserRepository userRepository,
    IUserFollowRepository userFollowRepository,
    ICurrentUserService currentUser,
    IMapper mapper) : IUserService
{
    public async Task<Result<User>> GetUserByIdAsync(string id)
    {
        var user = await userRepository.FindByIdAsync(id);
        if (user == null) return Result<User>.Failure("User not found", 404);
        return Result<User>.Success(user);
    }

    public async Task<Result<User>> GetUserByEmailAsync(string email)
    {
        var user = await userRepository.FindByEmailAsync(email);
        if (user == null) return Result<User>.Failure("User not found", 404);
        return Result<User>.Success(user);
    }

    public async Task<Result<PagedResult<SearchedUserDto>>> GetUsersByDisplayNameAsync(GetUsersByDisplayNameQuery query)
    {
        string? currentUserId = currentUser.Id;

        int offset = 0;
        if (!string.IsNullOrEmpty(query.Cursor) && int.TryParse(query.Cursor, out var parsed))
            offset = parsed;

        var users = await userRepository.SearchByDisplayNameAsync(query.DisplayName, offset, query.PageSize);

        List<string> followingIds = [];
        if (currentUserId != null)
        {
            var userIds = users.Select(u => u.Id).ToList();

            followingIds = await userFollowRepository
                .GetFolloweeIdsAsync(currentUserId, CancellationToken.None);
        }

        var mapped = users.Select(u =>
        {
            var dto = mapper.Map<SearchedUserDto>(u);
            dto.IsFollowed = currentUserId != null && followingIds.Contains(u.Id);
            return dto;
        }).ToList();

        var newCursor = (offset + query.PageSize).ToString();

        return Result<PagedResult<SearchedUserDto>>.Success(
            new PagedResult<SearchedUserDto>
            {
                Items = mapped,
                NextCursor = newCursor
            });
    }

    public async Task<Result> ToggleFollowAsync(string followerId, string followeeId, bool isFollowed)
    {
        var followee = await userManager.FindByIdAsync(followeeId);
        if (followee == null) return Result.Failure("Followee not found");

        var relation = await userFollowRepository.GetFollowRelationAsync(followerId, followeeId, CancellationToken.None);

        if (relation == null)
        {
            await userFollowRepository.AddFollowAsync(
                new UserFollow { FollowerId = followerId, FolloweeId = followeeId },
                CancellationToken.None);
        }
        else
        {
            await userFollowRepository.RemoveFollowAsync(relation, CancellationToken.None);
        }

        return Result.Success();
    }

    public async Task<Result<UserProfileDto>> GetUserInformationAsync(string userId, string? currentUserId)
    {
        var user = await userRepository.GetUserWithFollowRelationsAsync(userId);
        if (user == null)
            return Result<UserProfileDto>.Failure("User not found");

        var dto = mapper.Map<UserProfileDto>(user);

        dto.FollowersCount = user.FollowersCount;
        dto.FollowingCount = user.FollowingCount;

        dto.IsFollowing = currentUserId != null && user.FollowerIds.Contains(currentUserId);

        return Result<UserProfileDto>.Success(dto);
    }
}
