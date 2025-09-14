using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Domain.Entities.Post> Posts { get; set; }
    DbSet<PostPhoto> PostPhotos { get; set; }
    DbSet<RefreshToken> RefreshTokens { get; set; }
}