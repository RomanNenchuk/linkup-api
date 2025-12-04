using Application.Common;
using Application.Common.Interfaces;
using Application.PostComments.Commands.CreatePostComment;
using Application.PostComments.Queries.GetPostComments;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CommentService(
    ICommentRepository commentRepo,
    ICommentReactionRepository reactionRepo,
    IPostRepository postRepo,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser,
    IMapper mapper) : ICommentService
{
    public async Task<Result<string>> CreatePostCommentAsync(CreatePostCommentDto dto)
    {
        var user = await userManager.FindByIdAsync(dto.AuthorId);
        if (user == null) return Result<string>.Failure("User not found");

        var post = await postRepo.GetPostWithPhotosAsync(dto.PostId, CancellationToken.None);
        if (post == null) return Result<string>.Failure("Post not found");

        var comment = new PostComment
        {
            Content = dto.Content,
            PostId = dto.PostId,
            AuthorId = dto.AuthorId,
            RepliedTo = dto.RepliedTo,
            CreatedAt = DateTime.UtcNow
        };

        await commentRepo.AddAsync(comment, CancellationToken.None);

        return Result<string>.Success(post.Id);
    }

    public async Task<Result> DeletePostCommentAsync(string commentId)
    {
        var comment = await commentRepo.GetByIdAsync(commentId, CancellationToken.None);
        if (comment == null) return Result.Failure("Comment not found");

        if (comment.AuthorId != currentUser.Id!)
            return Result.Failure("Access denied");

        await commentRepo.RemoveAsync(comment, CancellationToken.None);
        return Result.Success();
    }

    public async Task<Result<List<PostCommentResponseDto>>> GetPostCommentsAsync(string postId)
    {
        var ct = CancellationToken.None;
        var userId = currentUser.Id;

        var comments = await commentRepo.GetPostCommentsAsync(postId, ct);

        var dtoList = mapper.Map<List<PostCommentResponseDto>>(comments);

        // Load authors
        var authorIds = dtoList.Select(c => c.Author.Id).Distinct().ToList();

        var authors = await userManager.Users
            .Where(u => authorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);

        foreach (var commentDto in dtoList)
        {
            if (authors.TryGetValue(commentDto.Author.Id, out var displayName))
                commentDto.Author.DisplayName = displayName;
        }

        // Likes
        if (userId != null)
        {
            var commentIds = dtoList.Select(c => c.Id).ToList();
            var likedIds = await reactionRepo.GetLikedCommentIdsAsync(userId, commentIds, ct);

            foreach (var dto in dtoList)
                dto.IsLikedByCurrentUser = likedIds.Contains(dto.Id);
        }

        return Result<List<PostCommentResponseDto>>.Success(dtoList);
    }

    public async Task<Result> TogglePostCommentReactionAsync(string commentId, string userId, bool isLiked)
    {
        var ct = CancellationToken.None;

        var comment = await commentRepo.GetByIdAsync(commentId, ct);
        if (comment == null) return Result.Failure("Comment does not exist");

        var reaction = await reactionRepo.GetReactionAsync(commentId, userId, ct);

        if (reaction == null && isLiked)
        {
            await reactionRepo.AddReactionAsync(
                new PostCommentReaction { UserId = userId, PostCommentId = commentId },
                ct
            );
        }
        else if (reaction != null && !isLiked)
        {
            await reactionRepo.RemoveReactionAsync(reaction, ct);
        }

        return Result.Success();
    }
}
