using System.Text.Json;
using ErpSaas.Infrastructure.Data.Entities.Log;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ErpSaas.Infrastructure.Data.Interceptors;

public sealed class AuditSaveChangesInterceptor(
    ITenantContext tenantContext,
    LogDbContext? logDb = null) : SaveChangesInterceptor
{
    private record PendingEntry(
        AuditableAttribute Attr,
        string EntityName,
        EntityState Operation,
        string? OldValues,
        string? NewValues,
        string? ParentEntityId);

    private readonly List<PendingEntry> _pending = [];

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        _pending.Clear();

        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, ct);

        var now = DateTime.UtcNow;
        var currentUserId = tenantContext.CurrentUserId == 0 ? (long?)null : tenantContext.CurrentUserId;

        foreach (var entry in eventData.Context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.CreatedByUserId = currentUserId;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedByUserId = currentUserId;
            }

            // Detect soft-delete: IsDeleted flipping false→true on a Modified entry
            bool isSoftDelete = false;
            if (entry.State == EntityState.Modified)
            {
                var originalIsDeleted = (bool)entry.OriginalValues[nameof(BaseEntity.IsDeleted)]!;
                var currentIsDeleted  = entry.Entity.IsDeleted;
                if (!originalIsDeleted && currentIsDeleted)
                {
                    isSoftDelete = true;
                    entry.Entity.DeletedAtUtc     = now;
                    entry.Entity.DeletedByUserId  = currentUserId;
                }
            }

            var attr = entry.Entity.GetType()
                .GetCustomAttributes(typeof(AuditableAttribute), inherit: true)
                .OfType<AuditableAttribute>()
                .FirstOrDefault();

            if (attr is null) continue;
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

            string? oldValues = null;
            string? newValues = null;
            EntityState effectiveState = isSoftDelete ? EntityState.Deleted : entry.State;

            if (isSoftDelete || entry.State == EntityState.Deleted)
            {
                var original = entry.OriginalValues.Properties
                    .ToDictionary(p => p.Name, p => entry.OriginalValues[p]?.ToString());
                oldValues = JsonSerializer.Serialize(original);
            }
            else if (entry.State == EntityState.Modified)
            {
                var original = entry.OriginalValues.Properties
                    .ToDictionary(p => p.Name, p => entry.OriginalValues[p]?.ToString());
                var current  = entry.CurrentValues.Properties
                    .ToDictionary(p => p.Name, p => entry.CurrentValues[p]?.ToString());
                oldValues = JsonSerializer.Serialize(original);
                newValues = JsonSerializer.Serialize(current);
            }
            else if (entry.State == EntityState.Added)
            {
                var current = entry.CurrentValues.Properties
                    .ToDictionary(p => p.Name, p => entry.CurrentValues[p]?.ToString());
                newValues = JsonSerializer.Serialize(current);
            }

            // Resolve parent entity ID for child entities
            string? parentEntityId = null;
            if (!string.IsNullOrEmpty(attr.ParentIdProperty))
            {
                var prop = entry.Entity.GetType().GetProperty(attr.ParentIdProperty);
                parentEntityId = prop?.GetValue(entry.Entity)?.ToString();
            }

            _pending.Add(new PendingEntry(attr, entry.Entity.GetType().Name, effectiveState, oldValues, newValues, parentEntityId));
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken ct = default)
    {
        if (_pending.Count == 0 || logDb is null)
            return await base.SavedChangesAsync(eventData, result, ct);

        var now = DateTime.UtcNow;
        var currentUserId = tenantContext.CurrentUserId == 0 ? (long?)null : tenantContext.CurrentUserId;
        var shopId        = tenantContext.ShopId == 0 ? (long?)null : tenantContext.ShopId;

        // Re-read entity IDs after save so Added entities have their DB-assigned ID
        var entityIdMap = new Dictionary<string, string?>();
        if (eventData.Context is not null)
        {
            foreach (var entry in eventData.Context.ChangeTracker.Entries<BaseEntity>())
                entityIdMap[entry.Entity.GetType().Name] = entry.Entity.Id.ToString();
        }

        foreach (var p in _pending)
        {
            entityIdMap.TryGetValue(p.EntityName, out var entityId);

            var eventType = p.Operation switch
            {
                EntityState.Added    => "Insert",
                EntityState.Modified => "Update",
                EntityState.Deleted  => "Delete",
                _                    => p.Attr.EventType,
            };

            logDb.AuditLogs.Add(new AuditLog
            {
                EventType        = eventType,
                EntityName       = p.EntityName,
                EntityId         = entityId,
                ParentEntityName = p.Attr.ParentEntityType,
                ParentEntityId   = p.ParentEntityId,
                OldValues        = p.OldValues,
                NewValues        = p.NewValues,
                UserId           = currentUserId,
                ShopId           = shopId,
                OccurredAtUtc    = now,
            });
        }

        await logDb.SaveChangesAsync(ct);
        _pending.Clear();

        return await base.SavedChangesAsync(eventData, result, ct);
    }
}
