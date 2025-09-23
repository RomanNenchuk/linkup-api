namespace Domain.Entities;

public class UserFollow
{
    public string FollowerId { get; set; } = null!;
    public string FolloweeId { get; set; } = null!;
}