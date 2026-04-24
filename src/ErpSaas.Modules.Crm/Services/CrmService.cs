#pragma warning disable CS9107
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Crm.Entities;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Crm.Services;

public sealed class CrmService(
    TenantDbContext db,
    IErrorLogger errorLogger)
    : BaseService<TenantDbContext>(db, errorLogger), ICrmService
{
    public Task<IReadOnlyList<CustomerDto>> ListCustomersAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
        => db.Set<Customer>()
            .Where(c => !c.IsDeleted &&
                (search == null
                    || c.DisplayName.Contains(search)
                    || (c.Phone != null && c.Phone.Contains(search))))
            .OrderBy(c => c.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerDto(
                c.Id,
                c.CustomerCode,
                c.DisplayName,
                c.CustomerType,
                c.Email,
                c.Phone,
                c.GstNumber,
                c.CreditLimitAmount,
                c.OutstandingAmount,
                c.IsActive,
                c.CustomerGroupId,
                c.CustomerGroup != null ? c.CustomerGroup.Name : null))
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<CustomerDto>)t.Result, ct);

    public Task<CustomerDto?> GetCustomerAsync(long id, CancellationToken ct = default)
        => db.Set<Customer>()
            .Where(c => c.Id == id && !c.IsDeleted)
            .Select(c => (CustomerDto?)new CustomerDto(
                c.Id,
                c.CustomerCode,
                c.DisplayName,
                c.CustomerType,
                c.Email,
                c.Phone,
                c.GstNumber,
                c.CreditLimitAmount,
                c.OutstandingAmount,
                c.IsActive,
                c.CustomerGroupId,
                c.CustomerGroup != null ? c.CustomerGroup.Name : null))
            .FirstOrDefaultAsync(ct);

    public async Task<Result<long>> CreateCustomerAsync(
        CreateCustomerDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Crm.CreateCustomer", async () =>
        {
            if (dto.Phone is not null &&
                await db.Set<Customer>().AnyAsync(c => c.Phone == dto.Phone && !c.IsDeleted, ct))
            {
                return Result<long>.Conflict(Errors.Crm.PhoneConflict(dto.Phone!));
            }

            var code = await GenerateCustomerCodeAsync(ct);
            var entity = new Customer
            {
                CustomerCode = code,
                DisplayName = dto.DisplayName,
                CustomerType = dto.CustomerType,
                Email = dto.Email,
                Phone = dto.Phone,
                GstNumber = dto.GstNumber,
                CreditLimitAmount = dto.CreditLimit,
                CustomerGroupId = dto.GroupId,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Set<Customer>().Add(entity);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> UpdateCustomerAsync(
        long id, UpdateCustomerDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Crm.UpdateCustomer", async () =>
        {
            var entity = await db.Set<Customer>()
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);
            if (entity is null)
                return Result<bool>.NotFound(Errors.Crm.CustomerNotFound);

            entity.DisplayName = dto.DisplayName;
            entity.Email = dto.Email;
            entity.Phone = dto.Phone;
            entity.GstNumber = dto.GstNumber;
            entity.CreditLimitAmount = dto.CreditLimit;
            entity.CustomerGroupId = dto.GroupId;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> DeactivateCustomerAsync(
        long id, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Crm.DeactivateCustomer", async () =>
        {
            var entity = await db.Set<Customer>()
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);
            if (entity is null)
                return Result<bool>.NotFound(Errors.Crm.CustomerNotFound);

            entity.IsActive = false;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public Task<IReadOnlyList<CustomerGroupDto>> ListGroupsAsync(CancellationToken ct = default)
        => db.Set<CustomerGroup>()
            .Where(g => g.IsActive && !g.IsDeleted)
            .OrderBy(g => g.Name)
            .Select(g => new CustomerGroupDto(g.Id, g.Code, g.Name, g.DiscountPercent, g.IsActive))
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<CustomerGroupDto>)t.Result, ct);

    public async Task<Result<long>> CreateGroupAsync(
        string code, string name, decimal discountPercent, CancellationToken ct = default)
        => await ExecuteAsync<long>("Crm.CreateGroup", async () =>
        {
            if (await db.Set<CustomerGroup>().AnyAsync(g => g.Code == code && !g.IsDeleted, ct))
                return Result<long>.Conflict(Errors.Crm.GroupConflict(code));

            var entity = new CustomerGroup
            {
                Code = code,
                Name = name,
                DiscountPercent = discountPercent,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Set<CustomerGroup>().Add(entity);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task<string> GenerateCustomerCodeAsync(CancellationToken ct)
    {
        var count = await db.Set<Customer>().CountAsync(ct);
        return $"CUST{(count + 1):D5}";
    }
}
