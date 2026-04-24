using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Infrastructure.Data;

public class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Entities added here as analytics facts/dims are built in later phases.
    }
}
