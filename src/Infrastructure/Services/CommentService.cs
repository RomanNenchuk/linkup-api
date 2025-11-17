using Application.Common;
using Application.Common.Interfaces;
using Infrastructure.Persistence;
using Application.Posts.Commands.CreatePostComment;
using Application.Posts.Queries.GetPostComments;
using Microsoft.AspNetCore.Identity;
using Infrastructure.Identity;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using AutoMapper.QueryableExtensions;
using AutoMapper;


namespace Infrastructure.Services;

public class CommentService(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser, IMapper mapper) : ICommentService
{
    public async Task<Result<string>> CreatePostCommentAsync(CreatePostCommentDto dto)
    {
        var user = await userManager.FindByIdAsync(dto.AuthorId);
        if (user == null) return Result<string>.Failure("User not found");

        var post = await dbContext.Posts.FirstOrDefaultAsync(p => p.Id == dto.PostId);
        if (post == null) return Result<string>.Failure("Post not found");

        var comment = new PostComment
        {
            Content = dto.Content,
            PostId = dto.PostId,
            AuthorId = dto.AuthorId,
            RepliedTo = dto.RepliedTo,
        };

        dbContext.Add(comment);
        var result = await dbContext.SaveChangesAsync() > 0;
        return result ? Result<string>.Success(post.Id) : Result<string>.Failure("Failed to create comment");
    }

    public async Task<Result> DeletePostCommentAsync(string commentId)
    {
        var comment = await dbContext.PostComments.FirstOrDefaultAsync(pc => pc.Id == commentId);
        if (comment == null) return Result.Failure("Comment not found");
        if (comment.AuthorId != currentUser.Id!) return Result.Failure("Access denied");

        dbContext.Remove(comment);
        var result = await dbContext.SaveChangesAsync() > 0;
        return result ? Result.Success() : Result.Failure("Failed to delete the comment");
    }

    public async Task<Result<List<PostCommentResponseDto>>> GetPostCommentsAsync(string postId)
    {
        var userId = currentUser.Id;
        var comments = await dbContext.PostComments
            .Where(c => c.PostId == postId)
            .ProjectTo<PostCommentResponseDto>(mapper.ConfigurationProvider)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var authorIds = comments.Select(c => c.Author.Id).Distinct().ToList();

        var authors = await userManager.Users
            .Where(u => authorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

        var commentIds = comments.Select(c => c.Id).ToList();

        var likedCommentIds = await dbContext.PostCommentReactions
            .Where(r => r.UserId == userId && commentIds.Contains(r.PostCommentId))
            .Select(r => r.PostCommentId)
            .ToListAsync();

        foreach (var comment in comments)
        {
            if (authors.TryGetValue(comment.Author.Id, out var displayName))
                comment.Author.DisplayName = displayName;
            if (userId != null)
                comment.IsLikedByCurrentUser = likedCommentIds.Contains(comment.Id);
        }

        return Result<List<PostCommentResponseDto>>.Success(comments);
    }
    public async Task<Result> TogglePostCommentReactionAsync(string commentId, string userId, bool isLiked)
    {
        var comment = await dbContext.PostComments.FirstOrDefaultAsync(x => x.Id == commentId);
        if (comment == null) return Result.Failure("Comment does not exist");

        var reaction = await dbContext.PostCommentReactions
            .FirstOrDefaultAsync(x => x.PostCommentId == commentId && x.UserId == userId);
        if (reaction == null && isLiked)
            dbContext.Add(new PostCommentReaction { UserId = userId, PostCommentId = commentId });
        else if (reaction != null && !isLiked)
            dbContext.Remove(reaction);
        else
            return Result.Success(); // already in desired state

        var result = await dbContext.SaveChangesAsync() > 0;

        return result ? Result.Success() : Result.Failure("Failed to toggle reaction");
    }

}
