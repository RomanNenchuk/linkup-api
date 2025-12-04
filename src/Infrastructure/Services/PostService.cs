using Application.Common;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Posts.Commands.CreatePost;
using Application.Posts.Commands.EditPost;
using Application.Posts.Queries.GetPosts;
using AutoMapper;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Infrastructure.Services;

public class PostService(IMapper mapper, UserManager<ApplicationUser> userManager, ICurrentUserService currentUser,
    ICloudinaryService cloudinaryService, IPostRepository postRepo, IUserFollowRepository userFollowRepo,
    IPostReactionRepository reactionRepo) : IPostService
{
    public async Task<Result<string>> CreatePostAsync(CreatePostDto dto)
    {
        var creator = await userManager.FindByIdAsync(dto.AuthorId);
        if (creator == null)
            return Result<string>.Failure("Author not found");

        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        var post = new Post
        {
            AuthorId = dto.AuthorId,
            Content = dto.Content,
            Location = dto.Latitude.HasValue && dto.Longitude.HasValue
                ? geometryFactory.CreatePoint(new Coordinate(dto.Longitude.Value, dto.Latitude.Value))
                : null,
            Address = dto.Address,
            PostPhotos = dto.Photos?
                .Select(p => new PostPhoto { Url = p.Url, PublicId = p.PublicId })
                .ToList() ?? [],
            CreatedAt = DateTime.UtcNow
        };

        await postRepo.AddPostAsync(post, default);

        return Result<string>.Success(post.Id);
    }


    public async Task<Result<PagedResult<PostResponseDto>>> GetTopPostsAsync(GetPostsQuery query, CancellationToken ct)
    {
        var oneWeekAgo = DateTime.UtcNow.AddDays(-7);

        int offset = 0;
        if (!string.IsNullOrEmpty(query.Cursor) && int.TryParse(query.Cursor, out var parsed))
            offset = parsed;

        var posts = await postRepo.GetTopPostsAsync(oneWeekAgo, offset, query.PageSize, query.Params.Latitude,
            query.Params.Longitude, query.Params.RadiusKm, ct);

        var result = await BuildPagedPostResultAsync(posts, query, ct);

        result.NextCursor = posts.Count == query.PageSize
            ? (offset + query.PageSize).ToString()
            : null;

        return Result<PagedResult<PostResponseDto>>.Success(result);
    }

    public async Task<Result<PagedResult<PostResponseDto>>> GetFollowingPostsAsync(GetPostsQuery query, CancellationToken ct)
    {
        if (currentUser?.Id is null)
            return Result<PagedResult<PostResponseDto>>.Success(new PagedResult<PostResponseDto> { Items = [] });

        var followeeIds = await userFollowRepo.GetFolloweeIdsAsync(currentUser.Id, ct);

        var posts = await postRepo.GetFollowingPostsAsync(followeeIds, query.Cursor, query.PageSize,
            query.Params.Latitude, query.Params.Longitude, query.Params.RadiusKm, ct);

        var result = await BuildPagedPostResultAsync(posts, query, ct);

        result.NextCursor = posts.Count == query.PageSize && query.Params.SortType != PostSortType.Top
            ? $"{posts.Last().CreatedAt:o}|{posts.Last().Id}"
            : null;

        return Result<PagedResult<PostResponseDto>>.Success(result);
    }

    public async Task<Result<PagedResult<PostResponseDto>>> GetRecentPostsAsync(GetPostsQuery query, CancellationToken ct)
    {
        var posts = await postRepo.GetRecentPostsAsync(query.Params.AuthorId, query.Cursor, query.PageSize,
            query.Params.Latitude, query.Params.Longitude, query.Params.RadiusKm, ct);

        var result = await BuildPagedPostResultAsync(posts, query, ct);
        result.NextCursor = posts.Count == query.PageSize && query.Params.SortType != PostSortType.Top
            ? $"{posts.Last().CreatedAt:o}|{posts.Last().Id}"
            : null;

        return Result<PagedResult<PostResponseDto>>.Success(result);
    }

    private async Task<PagedResult<PostResponseDto>> BuildPagedPostResultAsync(List<Post> posts, GetPostsQuery query, CancellationToken ct)
    {
        var postIds = posts.Select(p => p.Id).ToList();
        var authorIds = posts.Select(p => p.AuthorId).Distinct().ToList();

        var authors = await postRepo.GetAuthorsAsync(authorIds, ct);
        var reactionCounts = await postRepo.GetReactionCountsAsync(postIds, ct);
        var commentCounts = await postRepo.GetCommentCountsAsync(postIds, ct);
        var likedByCurrent = await postRepo.GetLikedPostIdsAsync(postIds, currentUser.Id, ct);

        var converted = posts.Select(p =>
        {
            var dto = mapper.Map<PostResponseDto>(p);

            if (authors.TryGetValue(p.AuthorId, out var author))
            {
                dto.Author = new AuthorDto
                {
                    Id = author.Id,
                    DisplayName = author.DisplayName
                };
            }

            dto.IsLikedByCurrentUser = likedByCurrent.Contains(p.Id);
            dto.ReactionCount = reactionCounts.GetValueOrDefault(p.Id);
            dto.CommentCount = commentCounts.GetValueOrDefault(p.Id);

            return dto;
        }).ToList();

        return new PagedResult<PostResponseDto> { Items = converted };
    }

    public async Task<Result> TogglePostReactionAsync(string postId, string userId, bool isLiked)
    {
        var post = await postRepo.GetPostByIdAsync(postId, default);
        if (post == null)
            return Result.Failure("Post does not exist");

        var reaction = await reactionRepo.GetReactionAsync(postId, userId);

        if (reaction == null && isLiked)
            await reactionRepo.AddReactionAsync(new PostReaction { PostId = postId, UserId = userId });

        else if (reaction != null && !isLiked)
            await reactionRepo.RemoveReactionAsync(reaction);

        return Result.Success();
    }

    public async Task<Result> EditPostAsync(EditPostDto dto)
    {
        var userId = currentUser.Id!;
        var post = await postRepo.GetPostWithPhotosAsync(dto.PostId, default);

        if (post == null)
            return Result.Failure("Post not found");

        if (post.AuthorId != userId)
            return Result.Failure("Access denied");

        if (!string.IsNullOrWhiteSpace(dto.Content))
            post.Content = dto.Content;

        if (!string.IsNullOrWhiteSpace(dto.Address))
            post.Address = dto.Address;

        if (dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            var geom = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            post.Location = geom.CreatePoint(new Coordinate(dto.Longitude.Value, dto.Latitude.Value));
        }

        // delete photos
        if (dto.PhotosToDelete is { Count: > 0 })
        {
            foreach (var pubId in dto.PhotosToDelete)
                await cloudinaryService.DeleteImageAsync(pubId);

            post.PostPhotos.RemoveAll(p => dto.PhotosToDelete.Contains(p.PublicId));
        }

        // add photos
        if (dto.PhotosToAdd is { Count: > 0 })
        {
            post.PostPhotos.AddRange(dto.PhotosToAdd.Select(p => new PostPhoto
            {
                PostId = post.Id,
                Url = p.Url,
                PublicId = p.PublicId
            }));
        }

        post.UpdatedAt = DateTime.UtcNow;

        var saved = await postRepo.SaveChangesAsync(default);
        return saved ? Result.Success() : Result.Failure("Failed to update post");
    }

    public async Task<Result<PostResponseDto>> GetPostDetailsByIdAsync(string postId, CancellationToken ct)
    {
        var post = await postRepo.GetPostWithPhotosAsync(postId, ct);
        if (post == null) return Result<PostResponseDto>.Failure("Post not found");

        var author = await userManager.FindByIdAsync(post.AuthorId);
        if (author == null) return Result<PostResponseDto>.Failure("Author not found");

        var reactionCount = await postRepo.GetReactionCountAsync(postId, ct);
        var commentCount = await postRepo.GetCommentCountAsync(postId, ct);

        var isLiked = currentUser?.Id is not null &&
            await postRepo.IsPostLikedByUserAsync(postId, currentUser.Id, ct);

        var dto = mapper.Map<PostResponseDto>(post);

        dto.Author = new AuthorDto
        {
            Id = author.Id,
            DisplayName = author.DisplayName
        };

        dto.ReactionCount = reactionCount;
        dto.CommentCount = commentCount;
        dto.IsLikedByCurrentUser = isLiked;

        return Result<PostResponseDto>.Success(dto);
    }

    public async Task<Result> ValidatePhotoLimitAsync(string postId, int photosToAddCount, List<string>? photosToDeleteList, CancellationToken ct)
    {
        var post = await postRepo.GetPostWithPhotosAsync(postId, ct);
        if (post == null) return Result.Failure("Post not found");
        var uniquePhotosToDelete = photosToDeleteList?.Distinct().ToList() ?? [];

        if (uniquePhotosToDelete.Count > 0)
        {
            var missingPhotos = uniquePhotosToDelete.Where(publicId => !post.PostPhotos.Any(p => p.PublicId == publicId));
            if (missingPhotos.Any()) return Result.Failure("Invalid set of photos. Some photos not found in post.");
        }

        var resultingPhotoNumber = post.PostPhotos.Count + photosToAddCount - uniquePhotosToDelete?.Count ?? 0;

        if (resultingPhotoNumber > PostConstants.MaxPhotosPerPost)
            return Result.Failure($"You can't upload more that {PostConstants.MaxPhotosPerPost} photos.");

        return Result.Success();
    }
}
