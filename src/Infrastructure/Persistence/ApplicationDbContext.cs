using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IApplicationDbContext
{
    public DbSet<Post> Posts { get; set; } = null!;
    public DbSet<PostPhoto> PostPhotos { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<PostReaction> PostReactions { get; set; } = null!;
    public DbSet<VerificationToken> VerificationTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Post>(entity =>
        {
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(p => p.AuthorId);

            entity.HasIndex(p => new { p.CreatedAt, p.Id });

            // spatial column (geometry) + index
            // 4326 → SRID (Spatial Reference System Identifier) (global standart for latitude longtitude)
            entity.Property(p => p.Location).HasColumnType("geography (Point,4326)");
            // Generalized Search Tree
            entity.HasIndex(p => p.Location).HasMethod("GIST");
        });

        builder.Entity<PostReaction>(entity =>
        {
            entity.HasKey(pr => new { pr.PostId, pr.UserId });

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(pr => pr.UserId);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(rt => rt.UserId);

            entity
                .HasIndex(rt => rt.Token)
                .IsUnique();
        });

        builder.Entity<VerificationToken>(entity =>
        {
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(vt => vt.UserId);

            entity
                .HasIndex(vt => vt.Token)
                .IsUnique();
        });
    }
}
