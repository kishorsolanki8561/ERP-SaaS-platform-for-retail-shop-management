using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Crm.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record CustomerDto(
    long Id,
    string CustomerCode,
    string DisplayName,
    string CustomerType,
    string? Email,
    string? Phone,
    string? GstNumber,
    decimal CreditLimit,
    decimal Outstanding,
    bool IsActive,
    long? GroupId,
    string? GroupName);

public record CreateCustomerDto(
    string DisplayName,
    string CustomerType,
    string? Email,
    string? Phone,
    string? GstNumber,
    decimal CreditLimit,
    long? GroupId);

public record UpdateCustomerDto(
    string DisplayName,
    string? Email,
    string? Phone,
    string? GstNumber,
    decimal CreditLimit,
    long? GroupId);

public record CustomerGroupDto(
    long Id,
    string Code,
    string Name,
    decimal DiscountPercent,
    bool IsActive);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface ICrmService
{
    Task<IReadOnlyList<CustomerDto>> ListCustomersAsync(
        int page, int pageSize, string? search, CancellationToken ct = default);

    Task<CustomerDto?> GetCustomerAsync(long id, CancellationToken ct = default);

    Task<Result<long>> CreateCustomerAsync(CreateCustomerDto dto, CancellationToken ct = default);

    Task<Result<bool>> UpdateCustomerAsync(long id, UpdateCustomerDto dto, CancellationToken ct = default);

    Task<Result<bool>> DeactivateCustomerAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<CustomerGroupDto>> ListGroupsAsync(CancellationToken ct = default);

    Task<Result<long>> CreateGroupAsync(
        string code, string name, decimal discountPercent, CancellationToken ct = default);
}
