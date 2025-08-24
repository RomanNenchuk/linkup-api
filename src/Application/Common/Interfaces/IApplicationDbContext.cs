using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Event> Events { get; set; }
    DbSet<RefreshToken> RefreshTokens { get; set; }
}