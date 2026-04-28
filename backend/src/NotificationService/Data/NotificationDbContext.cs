using Microsoft.EntityFrameworkCore;
using NotificationService.Models;

namespace NotificationService.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<UnreadMessage> UnreadMessages { get; set; }
    public DbSet<UnreadFeed> UnreadFeeds { get; set; }
    public DbSet<ProcessedNotificationEvent> ProcessedNotificationEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UnreadMessage>(entity =>
        {
            entity.ToTable("unreadmessages");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.ChatId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.ChatId, e.UserId, e.CompanyId }).IsUnique();
            
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("companyid");
            entity.Property(e => e.ChatId).HasColumnName("chatid");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.Property(e => e.Count).HasColumnName("count");
            entity.Property(e => e.LastUpdatedAt).HasColumnName("lastupdatedat");
        });

        modelBuilder.Entity<UnreadFeed>(entity =>
        {
            entity.ToTable("unreadfeeds");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.CompanyId }).IsUnique();
            
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("companyid");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.Property(e => e.Count).HasColumnName("count");
            entity.Property(e => e.LastReadAt).HasColumnName("lastreadat");
            entity.Property(e => e.LastUpdatedAt).HasColumnName("lastupdatedat");
        });

        modelBuilder.Entity<ProcessedNotificationEvent>(entity =>
        {
            entity.ToTable("processednotificationevents");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RoutingKey, e.EventKey }).IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoutingKey).HasColumnName("routingkey").HasMaxLength(128);
            entity.Property(e => e.EventKey).HasColumnName("eventkey").HasMaxLength(256);
            entity.Property(e => e.ProcessedAt).HasColumnName("processedat");
        });
    }
}




