using ErpSaas.Infrastructure.Data.Entities.Log;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Infrastructure.Data;

public class LogDbContext(DbContextOptions<LogDbContext> options) : DbContext(options)
{
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ThirdPartyApiLog> ThirdPartyApiLogs => Set<ThirdPartyApiLog>();
    public DbSet<SequenceAllocation> SequenceAllocations => Set<SequenceAllocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ErrorLog>(b =>
        {
            b.ToTable("ErrorLog", schema: "log");
            b.HasKey(e => e.Id);
            b.Property(e => e.OperationName).HasMaxLength(200).IsRequired();
            b.Property(e => e.ExceptionType).HasMaxLength(500).IsRequired();
            b.Property(e => e.Message).HasMaxLength(4000).IsRequired();
            b.Property(e => e.CorrelationId).HasMaxLength(50);
        });

        modelBuilder.Entity<AuditLog>(b =>
        {
            b.ToTable("AuditLog", schema: "log");
            b.HasKey(e => e.Id);
            b.Property(e => e.EventType).HasMaxLength(200).IsRequired();
            b.Property(e => e.EntityName).HasMaxLength(200).IsRequired();
            b.Property(e => e.EntityId).HasMaxLength(100);
            b.Property(e => e.CorrelationId).HasMaxLength(50);
        });

        modelBuilder.Entity<ThirdPartyApiLog>(b =>
        {
            b.ToTable("ThirdPartyApiLog", schema: "log");
            b.HasKey(e => e.Id);
            b.Property(e => e.Provider).HasMaxLength(100).IsRequired();
            b.Property(e => e.HttpMethod).HasMaxLength(10).IsRequired();
            b.Property(e => e.Url).HasMaxLength(2000).IsRequired();
            b.Property(e => e.ErrorMessage).HasMaxLength(1000);
        });

        modelBuilder.Entity<SequenceAllocation>(b =>
        {
            b.ToTable("SequenceAllocation", schema: "log");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(50).IsRequired();
        });
    }
}
