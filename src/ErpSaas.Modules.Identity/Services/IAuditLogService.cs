using ErpSaas.Infrastructure.Audit;

namespace ErpSaas.Modules.Identity.Services;

public sealed record AuditLogEntryDto(
    long Id,
    string EventType,
    string EntityName,
    string? EntityId,
    string? ParentEntityName,
    string? ParentEntityId,
    long? ChangedByUserId,
    string ChangedByName,
    DateTime OccurredAtUtc,
    IReadOnlyList<AuditChangedField> ChangedFields);

public sealed record AuditLogPagedDto(
    IReadOnlyList<AuditLogEntryDto> Items,
    int TotalCount);

public interface IAuditLogService
{
    Task<AuditLogPagedDto> ListAsync(
        string entityType,
        string? entityId,
        DateTime? from,
        DateTime? to,
        long? shopId,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);
}
