using Dapper;
using ErpSaas.Infrastructure.Audit;
using ErpSaas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Identity.Services;

public sealed class AuditLogService(
    LogDbContext logDb,
    PlatformDbContext platformDb) : IAuditLogService
{
    public async Task<AuditLogPagedDto> ListAsync(
        string entityType,
        string? entityId,
        DateTime? from,
        DateTime? to,
        long? shopId,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var conn = logDb.Database.GetDbConnection();
        if (conn.State == System.Data.ConnectionState.Closed)
            await conn.OpenAsync(ct);

        var skip = (pageNumber - 1) * pageSize;

        var countSql = """
            SELECT COUNT(*)
            FROM [log].[AuditLog]
            WHERE (
                    ([EntityName] = @EntityType AND (@EntityId IS NULL OR [EntityId] = @EntityId))
                 OR ([ParentEntityName] = @EntityType AND (@EntityId IS NULL OR [ParentEntityId] = @EntityId))
                )
              AND (@ShopId IS NULL OR [ShopId] = @ShopId)
              AND (@From IS NULL OR [OccurredAtUtc] >= @From)
              AND (@To IS NULL OR [OccurredAtUtc] <= @To)
            """;

        var dataSql = """
            SELECT [Id], [EventType], [EntityName], [EntityId],
                   [ParentEntityName], [ParentEntityId],
                   [OldValues], [NewValues],
                   [UserId], [OccurredAtUtc]
            FROM [log].[AuditLog]
            WHERE (
                    ([EntityName] = @EntityType AND (@EntityId IS NULL OR [EntityId] = @EntityId))
                 OR ([ParentEntityName] = @EntityType AND (@EntityId IS NULL OR [ParentEntityId] = @EntityId))
                )
              AND (@ShopId IS NULL OR [ShopId] = @ShopId)
              AND (@From IS NULL OR [OccurredAtUtc] >= @From)
              AND (@To IS NULL OR [OccurredAtUtc] <= @To)
            ORDER BY [OccurredAtUtc] DESC
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            """;

        var param = new
        {
            EntityType = entityType,
            EntityId = entityId,
            ShopId = shopId,
            From = from,
            To = to,
            Skip = skip,
            Take = pageSize
        };

        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, param);

        var rows = await conn.QueryAsync<AuditLogRow>(dataSql, param);
        var rowList = rows.ToList();

        // Batch-resolve user display names (one query instead of N+1)
        var userIds = rowList.Select(r => r.UserId).Where(id => id.HasValue)
                             .Select(id => id!.Value).Distinct().ToList();
        var userNames = new Dictionary<long, string>();
        if (userIds.Count > 0)
        {
            var users = await platformDb.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.DisplayName })
                .ToListAsync(ct);
            userNames = users.ToDictionary(u => u.Id, u => u.DisplayName);
        }

        var items = rowList.Select(r =>
        {
            var diff = AuditFieldRegistry.ComputeDiff(r.EntityName, r.OldValues, r.NewValues);
            var userName = r.UserId.HasValue && userNames.TryGetValue(r.UserId.Value, out var n)
                ? n : "System";
            return new AuditLogEntryDto(
                r.Id,
                r.EventType,
                r.EntityName,
                r.EntityId,
                r.ParentEntityName,
                r.ParentEntityId,
                r.UserId,
                userName,
                r.OccurredAtUtc,
                diff);
        }).ToList();

        return new AuditLogPagedDto(items, totalCount);
    }

    private sealed record AuditLogRow(
        long Id,
        string EventType,
        string EntityName,
        string? EntityId,
        string? ParentEntityName,
        string? ParentEntityId,
        string? OldValues,
        string? NewValues,
        long? UserId,
        DateTime OccurredAtUtc);
}
