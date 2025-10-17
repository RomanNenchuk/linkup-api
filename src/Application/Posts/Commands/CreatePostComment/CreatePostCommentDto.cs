namespace Application.Posts.Commands.CreatePostComment;

public class CreatePostCommentDto
{
    public string PostId { get; set; } = null!;
    public string? RepliedTo { get; set; }
    public string Content { get; set; } = null!;
    public string AuthorId { get; set; } = null!;
}
