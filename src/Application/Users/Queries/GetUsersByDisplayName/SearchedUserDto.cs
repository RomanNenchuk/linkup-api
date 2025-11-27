using AutoMapper;

namespace Application.Users.Queries.GetUsersByDisplayName;

public class SearchedUserDto
{
    public string Id { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public bool IsFollowed { get; set; }
}
