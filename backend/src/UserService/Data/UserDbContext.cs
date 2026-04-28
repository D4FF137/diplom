using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace UserService.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            // Use lowercase table name without quotes to match PostgreSQL default behavior
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.Email).IsUnique();
            
            // Configure all properties with explicit lowercase column names
            entity.Property(e => e.Id)
                .HasColumnName("id");
            
            entity.Property(e => e.CompanyId)
                .HasColumnName("companyid");
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("email");
            
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("passwordhash");
            
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("firstname");
            
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("lastname");
            
            entity.Property(e => e.AvatarUrl)
                .IsRequired(false)
                .HasColumnName("avatarurl");
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("createdat");

            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasColumnName("role")
                .HasDefaultValue("Worker");

            entity.Property(e => e.IsBlocked)
                .HasColumnName("isblocked")
                .HasDefaultValue(false);

            entity.Property(e => e.LastSeen)
                .HasColumnName("lastseen")
                .IsRequired(false);
        });
    }
}


