using ErpSaas.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ErpSaas.Infrastructure.Data.Interceptors;

public sealed class AuditSaveChangesInterceptor(ITenantContext tenantContext) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, ct);

        var now = DateTime.UtcNow;

        foreach (var entry in eventData.Context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.CreatedByUserId = tenantContext.CurrentUserId == 0
                    ? null
                    : tenantContext.CurrentUserId;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedByUserId = tenantContext.CurrentUserId == 0
                    ? null
                    : tenantContext.CurrentUserId;
            }
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }
}
