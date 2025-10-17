namespace Web.DTOs;

public class CreatePostCommentRequest
{
    public string Content { get; set; } = null!;
    public string? RepliedTo { get; set; }
}