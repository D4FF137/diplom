using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace TaskService.Data;

public class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options) { }

    public DbSet<UserTask> Tasks { get; set; }
    public DbSet<ChecklistItem> ChecklistItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<UserTask>(entity =>
        {
            entity.ToTable("tasks");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("companyid");
            entity.Property(e => e.CreatorId).HasColumnName("creatorid");
            entity.Property(e => e.TargetGroupId).HasColumnName("targetgroupid");
            entity.Property(e => e.TargetUserId).HasColumnName("targetuserid");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.DueDate).HasColumnName("duedate");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat");

            // Explicitly define the relationship so EF doesn't create a shadow UserTaskId column
            entity.HasMany(e => e.ChecklistItems)
                  .WithOne()
                  .HasForeignKey(ci => ci.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChecklistItem>(entity =>
        {
            entity.ToTable("checklistitems");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TaskId).HasColumnName("taskid");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.IsCompleted).HasColumnName("iscompleted");
            entity.Property(e => e.CompletedByUserId).HasColumnName("completedbyuserid");
            entity.Property(e => e.CompletedAt).HasColumnName("completedat");
        });
    }
}
