using Application.Common.DTOs;
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
    public DbSet<UserFollow> UserFollows { get; set; } = null!;
    public DbSet<VerificationToken> VerificationTokens { get; set; } = null!;
    public DbSet<HeatmapPoint> HeatmapPoints { get; set; } = null!;
    public DbSet<Cluster> Clusters { get; set; } = null!;
    public DbSet<PostComment> PostCommnets { get; set; } = null!;



    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Post>(entity =>
        {
            entity.HasOne<ApplicationUser>()
                .WithMany(u => u.Posts)
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

        builder.Entity<PostComment>(entity =>
        {
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(pc => pc.AuthorId);
        });

        builder.Entity<UserFollow>(entity =>
        {
            entity.HasKey(f => new { f.FollowerId, f.FolloweeId });

            entity.HasOne<ApplicationUser>()
                .WithMany(u => u.Followings)
                .HasForeignKey(f => f.FollowerId);

            entity.HasOne<ApplicationUser>()
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FolloweeId);
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

        builder.Entity<HeatmapPoint>().HasNoKey();
        builder.Entity<Cluster>().HasNoKey();

    }
}
