using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Hr.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record StaffActivityDto(
    long Id, long EmployeeId, string EmployeeName,
    string ActivityType, string? Description,
    long? RelatedEntityId, string? RelatedEntityType,
    DateTime OccurredAtUtc);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IStaffActivityService
{
    Task<IReadOnlyList<StaffActivityDto>> ListAsync(long? employeeId = null, CancellationToken ct = default);
    Task<Result<bool>> TrackAsync(long employeeId, string activityType, string? description = null, long? relatedEntityId = null, string? relatedEntityType = null, CancellationToken ct = default);
}
