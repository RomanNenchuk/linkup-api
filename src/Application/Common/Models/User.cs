namespace Application.Common.Models;

public class User
{
    public string Id { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool EmailConfirmed { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }

    public List<string> FollowerIds { get; set; } = new();
    public List<string> FollowingIds { get; set; } = new();
}
