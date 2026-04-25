using ErpSaas.Infrastructure.Data.Entities.Messaging;
using ErpSaas.Infrastructure.Data.Entities.Messaging.Enums;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Infrastructure.Data;

public class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationQueue> NotificationQueues => Set<NotificationQueue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationTemplate>(e =>
        {
            e.ToTable("NotificationTemplate", schema: "notifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.Code, x.Channel }).IsUnique();
            e.Property(x => x.SubjectTemplate).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<NotificationQueue>(e =>
        {
            e.ToTable("NotificationQueue", schema: "notifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Recipient).HasMaxLength(256).IsRequired();
            e.Property(x => x.Subject).HasMaxLength(500).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.ErrorMessage).HasMaxLength(1000);
            e.Property(x => x.TemplateCode).HasMaxLength(100);
            e.Property(x => x.CorrelationId).HasMaxLength(100);
            e.HasIndex(x => x.Status);
        });
    }
}
