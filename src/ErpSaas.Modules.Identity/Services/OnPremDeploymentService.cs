using Dapper;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Replication;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Identity.Services;

public sealed class OnPremDeploymentService(
    PlatformDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant,
    ILogger<OnPremDeploymentService> logger)
    : BaseService<PlatformDbContext>(db, errorLogger), IOnPremDeploymentService
{
    public async Task<Result<OnPremDeploymentDto>> RegisterAsync(
        RegisterDeploymentDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("OnPrem.Register", async () =>
        {
            var existing = await db.OnPremDeployments
                .FirstOrDefaultAsync(d => d.ShopId == tenant.ShopId && d.DeploymentId == dto.DeploymentId, ct);

            if (existing is not null)
            {
                existing.ShopLocalEndpoint = dto.ShopLocalEndpoint;
                existing.SoftwareVersion = dto.SoftwareVersion;
                existing.Mode = dto.Mode;
                existing.Status = OnPremDeploymentStatus.Active;
                await db.SaveChangesAsync(ct);
                return Result<OnPremDeploymentDto>.Success(Map(existing));
            }

            var deployment = new OnPremDeployment
            {
                ShopId = tenant.ShopId,
                DeploymentId = dto.DeploymentId,
                ShopLocalEndpoint = dto.ShopLocalEndpoint,
                PublicKey = dto.PublicKey,
                SoftwareVersion = dto.SoftwareVersion,
                Mode = dto.Mode,
                Status = OnPremDeploymentStatus.Active,
                InstalledAtUtc = DateTime.UtcNow,
                LastReplicationAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
            };

            db.OnPremDeployments.Add(deployment);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Registered on-prem deployment {DeploymentId} for shop {ShopId}",
                dto.DeploymentId, tenant.ShopId);

            return Result<OnPremDeploymentDto>.Success(Map(deployment));
        }, ct, useTransaction: true);
    }

    public async Task<IReadOnlyList<OnPremDeploymentDto>> ListAsync(CancellationToken ct = default)
    {
        var items = await db.OnPremDeployments
            .AsNoTracking()
            .Where(d => d.ShopId == tenant.ShopId)
            .OrderByDescending(d => d.LastReplicationAtUtc)
            .ToListAsync(ct);

        return items.Select(Map).ToList();
    }

    public async Task<Result<OnPremDeploymentDto>> GetAsync(long id, CancellationToken ct = default)
    {
        var deployment = await db.OnPremDeployments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && d.ShopId == tenant.ShopId, ct);

        return deployment is null
            ? Result<OnPremDeploymentDto>.NotFound(Errors.OnPrem.NotFound)
            : Result<OnPremDeploymentDto>.Success(Map(deployment));
    }

    public async Task<Result<bool>> UpdateStatusAsync(
        long id, OnPremDeploymentStatus status, CancellationToken ct = default)
    {
        return await ExecuteAsync("OnPrem.UpdateStatus", async () =>
        {
            var deployment = await db.OnPremDeployments
                .FirstOrDefaultAsync(d => d.Id == id && d.ShopId == tenant.ShopId, ct);

            if (deployment is null)
                return Result<bool>.NotFound(Errors.OnPrem.NotFound);

            deployment.Status = status;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct);
    }

    public async Task<Result<bool>> UpdateModeAsync(
        long id, ReplicationMode mode, CancellationToken ct = default)
    {
        return await ExecuteAsync("OnPrem.UpdateMode", async () =>
        {
            var deployment = await db.OnPremDeployments
                .FirstOrDefaultAsync(d => d.Id == id && d.ShopId == tenant.ShopId, ct);

            if (deployment is null)
                return Result<bool>.NotFound(Errors.OnPrem.NotFound);

            deployment.Mode = mode;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct);
    }

    public async Task<IReadOnlyList<ReplicationLogDto>> ListLogsAsync(
        long deploymentId, int page, int pageSize, CancellationToken ct = default)
    {
        var deployment = await db.OnPremDeployments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deploymentId && d.ShopId == tenant.ShopId, ct);

        if (deployment is null) return [];

        var conn = db.Database.GetDbConnection();
        if (conn.State == System.Data.ConnectionState.Closed)
            await conn.OpenAsync(ct);

        var rows = await conn.QueryAsync<ReplicationLogDto>(
            """
            SELECT Id, DeploymentId, Direction, Status, StartedAtUtc, CompletedAtUtc,
                   RowsTransferred, RowsConflicted, RowsFailed, PayloadBytes, ErrorSummary
            FROM replication.ReplicationLog
            WHERE ShopId = @ShopId AND DeploymentId = @DeploymentId
            ORDER BY StartedAtUtc DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """,
            new { ShopId = tenant.ShopId, DeploymentId = deployment.DeploymentId, Offset = (page - 1) * pageSize, PageSize = pageSize });

        return rows.ToList();
    }

    public async Task<(IReadOnlyList<ConflictArchiveDto> Items, int TotalCount)> ListConflictsAsync(
        long? deploymentId, ConflictResolutionOutcome? outcome, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.ConflictArchives.AsNoTracking().Where(c => c.ShopId == tenant.ShopId);

        if (outcome.HasValue) query = query.Where(c => c.Outcome == outcome.Value);

        if (deploymentId.HasValue)
        {
            var deployment = await db.OnPremDeployments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == deploymentId.Value && d.ShopId == tenant.ShopId, ct);
            if (deployment is not null)
                query = query.Where(c => c.DeploymentId == deployment.DeploymentId);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items.Select(MapConflict).ToList(), total);
    }

    public async Task<Result<bool>> ResolveConflictAsync(
        long conflictId, ResolveConflictDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("OnPrem.ResolveConflict", async () =>
        {
            var conflict = await db.ConflictArchives
                .FirstOrDefaultAsync(c => c.Id == conflictId && c.ShopId == tenant.ShopId, ct);

            if (conflict is null)
                return Result<bool>.NotFound(Errors.OnPrem.ConflictNotFound);

            if (conflict.Outcome != ConflictResolutionOutcome.Pending)
                return Result<bool>.Conflict(Errors.OnPrem.AlreadyResolved);

            conflict.Outcome = dto.Outcome;
            conflict.ResolutionNote = dto.ResolutionNote;
            conflict.ResolvedByUserId = tenant.CurrentUserId;
            conflict.ResolvedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    private static OnPremDeploymentDto Map(OnPremDeployment d) => new(
        d.Id, d.DeploymentId, d.ShopLocalEndpoint, d.PublicKey, d.SoftwareVersion,
        d.Mode, d.Status, d.InstalledAtUtc, d.LastReplicationAtUtc, d.LastFullReplicationAtUtc);

    private static ConflictArchiveDto MapConflict(ConflictArchive c) => new(
        c.Id, c.DeploymentId, c.EntityName, c.EntityId,
        c.Strategy, c.Outcome, c.ResolutionNote, c.ResolvedByUserId, c.ResolvedAtUtc);
}
