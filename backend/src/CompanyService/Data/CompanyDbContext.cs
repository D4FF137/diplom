using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace CompanyService.Data;

public class CompanyDbContext : DbContext
{
    public CompanyDbContext(DbContextOptions<CompanyDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Company>(entity =>
        {
            // Use lowercase table name without quotes to match PostgreSQL default behavior
            entity.ToTable("companies");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("name"); // Explicitly set column name to lowercase
            
            entity.Property(e => e.Id)
                .HasColumnName("id");
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("createdat");
        });
    }
}


