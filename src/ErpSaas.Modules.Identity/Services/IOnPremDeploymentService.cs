using ErpSaas.Infrastructure.Data.Entities.Replication;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Identity.Services;

public sealed record OnPremDeploymentDto(
    long Id,
    string DeploymentId,
    string ShopLocalEndpoint,
    string PublicKey,
    string SoftwareVersion,
    ReplicationMode Mode,
    OnPremDeploymentStatus Status,
    DateTime InstalledAtUtc,
    DateTime LastReplicationAtUtc,
    DateTime? LastFullReplicationAtUtc);

public sealed record ReplicationLogDto(
    long Id,
    string DeploymentId,
    ReplicationDirection Direction,
    ReplicationStatus Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    int RowsTransferred,
    int RowsConflicted,
    int RowsFailed,
    long PayloadBytes,
    string? ErrorSummary);

public sealed record ConflictArchiveDto(
    long Id,
    string DeploymentId,
    string EntityName,
    long EntityId,
    ConflictResolutionStrategy Strategy,
    ConflictResolutionOutcome Outcome,
    string? ResolutionNote,
    long? ResolvedByUserId,
    DateTime? ResolvedAtUtc);

public sealed record RegisterDeploymentDto(
    string DeploymentId,
    string ShopLocalEndpoint,
    string PublicKey,
    string SoftwareVersion,
    ReplicationMode Mode);

public sealed record ResolveConflictDto(
    ConflictResolutionOutcome Outcome,
    string? ResolutionNote);

public interface IOnPremDeploymentService
{
    Task<Result<OnPremDeploymentDto>> RegisterAsync(RegisterDeploymentDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<OnPremDeploymentDto>> ListAsync(CancellationToken ct = default);
    Task<Result<OnPremDeploymentDto>> GetAsync(long id, CancellationToken ct = default);
    Task<Result<bool>> UpdateStatusAsync(long id, OnPremDeploymentStatus status, CancellationToken ct = default);
    Task<Result<bool>> UpdateModeAsync(long id, ReplicationMode mode, CancellationToken ct = default);
    Task<IReadOnlyList<ReplicationLogDto>> ListLogsAsync(long deploymentId, int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<ConflictArchiveDto> Items, int TotalCount)> ListConflictsAsync(
        long? deploymentId, ConflictResolutionOutcome? outcome, int page, int pageSize, CancellationToken ct = default);
    Task<Result<bool>> ResolveConflictAsync(long conflictId, ResolveConflictDto dto, CancellationToken ct = default);
}
