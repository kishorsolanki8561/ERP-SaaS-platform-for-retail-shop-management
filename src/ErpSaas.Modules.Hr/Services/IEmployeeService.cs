using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Hr.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record EmployeeDto(
    long Id, string EmployeeCode, string FirstName, string LastName,
    string? Phone, string? Email, string Designation, string Department,
    decimal BasicSalary, DateTime DateOfJoining, DateTime? DateOfLeaving,
    bool IsActive, long? LinkedUserId);

public record CreateEmployeeDto(
    string FirstName, string LastName, string? Phone, string? Email,
    DateTime DateOfBirth, DateTime DateOfJoining,
    string Designation, string Department, decimal BasicSalary,
    string? BankAccountNumber, string? BankIfsc, string? PanNumber,
    long? LinkedUserId);

public record UpdateEmployeeDto(
    string? Phone, string? Email, string? Designation, string? Department,
    decimal? BasicSalary, string? BankAccountNumber, string? BankIfsc,
    DateTime? DateOfLeaving, bool? IsActive, long? LinkedUserId);

public record AddDocumentDto(string DocumentType, long UploadedFileId, DateTime? ExpiryDate);

public record EmployeeDocumentDto(long Id, string DocumentType, long UploadedFileId, DateTime? ExpiryDate);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IEmployeeService
{
    Task<Result<long>> CreateAsync(CreateEmployeeDto dto, CancellationToken ct = default);
    Task<Result<bool>> UpdateAsync(long id, UpdateEmployeeDto dto, CancellationToken ct = default);
    Task<EmployeeDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeDto>> ListAsync(CancellationToken ct = default);
    Task<Result<long>> AddDocumentAsync(long employeeId, AddDocumentDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeDocumentDto>> ListDocumentsAsync(long employeeId, CancellationToken ct = default);
}
