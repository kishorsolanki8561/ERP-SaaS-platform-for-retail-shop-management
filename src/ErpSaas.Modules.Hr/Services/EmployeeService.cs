using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Hr.Entities;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Hr.Services;

public sealed class EmployeeService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ISequenceService sequence,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), IEmployeeService
{
    public async Task<Result<long>> CreateAsync(CreateEmployeeDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("HR.Employee.Create", async () =>
        {
            var code = await sequence.NextAsync(Constants.SequenceCodes.Employee, tenant.ShopId, ct);

            var entity = new Employee
            {
                ShopId = tenant.ShopId,
                EmployeeCode = code,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Phone = dto.Phone,
                Email = dto.Email,
                DateOfBirth = dto.DateOfBirth,
                DateOfJoining = dto.DateOfJoining,
                Designation = dto.Designation,
                Department = dto.Department,
                BasicSalary = dto.BasicSalary,
                BankAccountNumber = dto.BankAccountNumber,
                BankIfsc = dto.BankIfsc,
                PanNumber = dto.PanNumber,
                LinkedUserId = dto.LinkedUserId,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<Employee>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> UpdateAsync(long id, UpdateEmployeeDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("HR.Employee.Update", async () =>
        {
            var entity = await _db.Set<Employee>().FirstOrDefaultAsync(e => e.Id == id, ct);
            if (entity is null) return Result<bool>.NotFound(Errors.Hr.EmployeeNotFound);

            if (dto.Phone is not null) entity.Phone = dto.Phone;
            if (dto.Email is not null) entity.Email = dto.Email;
            if (dto.Designation is not null) entity.Designation = dto.Designation;
            if (dto.Department is not null) entity.Department = dto.Department;
            if (dto.BasicSalary.HasValue) entity.BasicSalary = dto.BasicSalary.Value;
            if (dto.BankAccountNumber is not null) entity.BankAccountNumber = dto.BankAccountNumber;
            if (dto.BankIfsc is not null) entity.BankIfsc = dto.BankIfsc;
            if (dto.DateOfLeaving.HasValue) entity.DateOfLeaving = dto.DateOfLeaving;
            if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
            if (dto.LinkedUserId.HasValue) entity.LinkedUserId = dto.LinkedUserId;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: false);
    }

    public async Task<EmployeeDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var e = await _db.Set<Employee>().FirstOrDefaultAsync(x => x.Id == id, ct);
        return e is null ? null : Map(e);
    }

    public async Task<IReadOnlyList<EmployeeDto>> ListAsync(CancellationToken ct = default)
        => await _db.Set<Employee>()
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Select(e => Map(e))
            .ToListAsync(ct);

    public async Task<Result<long>> AddDocumentAsync(long employeeId, AddDocumentDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("HR.Employee.AddDocument", async () =>
        {
            var exists = await _db.Set<Employee>().AnyAsync(e => e.Id == employeeId, ct);
            if (!exists) return Result<long>.NotFound(Errors.Hr.EmployeeNotFound);

            var doc = new EmployeeDocument
            {
                ShopId = tenant.ShopId,
                EmployeeId = employeeId,
                DocumentType = dto.DocumentType,
                UploadedFileId = dto.UploadedFileId,
                ExpiryDate = dto.ExpiryDate,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<EmployeeDocument>().Add(doc);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(doc.Id);
        }, ct, useTransaction: false);
    }

    public async Task<IReadOnlyList<EmployeeDocumentDto>> ListDocumentsAsync(long employeeId, CancellationToken ct = default)
        => await _db.Set<EmployeeDocument>()
            .Where(d => d.EmployeeId == employeeId)
            .Select(d => new EmployeeDocumentDto(d.Id, d.DocumentType, d.UploadedFileId, d.ExpiryDate))
            .ToListAsync(ct);

    private static EmployeeDto Map(Employee e) => new(
        e.Id, e.EmployeeCode, e.FirstName, e.LastName,
        e.Phone, e.Email, e.Designation, e.Department,
        e.BasicSalary, e.DateOfJoining, e.DateOfLeaving,
        e.IsActive, e.LinkedUserId);
}
