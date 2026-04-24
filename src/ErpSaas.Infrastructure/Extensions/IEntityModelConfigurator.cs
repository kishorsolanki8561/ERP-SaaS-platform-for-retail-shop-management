using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Infrastructure.Extensions;

/// <summary>
/// Implement this in each module and register as a singleton.
/// TenantDbContext picks up all implementations during OnModelCreating,
/// so Infrastructure never needs to reference individual module projects.
/// </summary>
public interface IEntityModelConfigurator
{
    void Configure(ModelBuilder modelBuilder);
}
