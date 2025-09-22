namespace Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? Id { get; }
    string? Email { get; }
}
