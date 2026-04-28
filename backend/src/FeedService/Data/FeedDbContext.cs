using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace FeedService.Data;

public class FeedDbContext : DbContext
{
    public FeedDbContext(DbContextOptions<FeedDbContext> options) : base(options)
    {
    }

    public DbSet<Post> Posts { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<Comment> Comments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Post>(entity =>
        {
            // Use lowercase table name without quotes to match PostgreSQL default behavior
            entity.ToTable("posts");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.UserId);
            
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Content)
                .IsRequired()
                .HasColumnName("content");
            entity.Property(e => e.UserId)
                .HasColumnName("userid");
            entity.Property(e => e.CompanyId)
                .HasColumnName("companyid");
            entity.Property(e => e.CreatedAt)
                .HasColumnName("createdat");
            entity.Property(e => e.ImageUrl)
                .HasColumnName("imageurl");
        });

        modelBuilder.Entity<Like>(entity =>
        {
            entity.ToTable("likes");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.PostId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.PostId, e.UserId }).IsUnique(); // Один лайк от пользователя на пост
            
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PostId)
                .HasColumnName("postid");
            entity.Property(e => e.UserId)
                .HasColumnName("userid");
            entity.Property(e => e.CompanyId)
                .HasColumnName("companyid");
            entity.Property(e => e.CreatedAt)
                .HasColumnName("createdat");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToTable("comments");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.PostId);
            entity.HasIndex(e => e.UserId);
            
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PostId)
                .HasColumnName("postid");
            entity.Property(e => e.UserId)
                .HasColumnName("userid");
            entity.Property(e => e.Content)
                .IsRequired()
                .HasColumnName("content");
            entity.Property(e => e.CompanyId)
                .HasColumnName("companyid");
            entity.Property(e => e.CreatedAt)
                .HasColumnName("createdat");
        });
    }
}


