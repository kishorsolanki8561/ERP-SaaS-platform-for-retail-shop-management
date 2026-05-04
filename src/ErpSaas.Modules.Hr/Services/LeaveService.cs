using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Hr.Entities;
using ErpSaas.Modules.Hr.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Hr.Services;

public sealed class LeaveService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), ILeaveService
{
    public async Task<Result<long>> CreateLeaveTypeAsync(CreateLeaveTypeDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("HR.LeaveType.Create", async () =>
        {
            var exists = await _db.Set<LeaveType>().AnyAsync(t => t.Code == dto.Code, ct);
            if (exists) return Result<long>.Conflict(Errors.Hr.LeaveTypeCodeExists);

            var entity = new LeaveType
            {
                ShopId = tenant.ShopId,
                Code = dto.Code,
                Name = dto.Name,
                DefaultAnnualQuota = dto.DefaultAnnualQuota,
                IsPaid = dto.IsPaid,
                IsCarryForward = dto.IsCarryForward,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<LeaveType>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: false);
    }

    public async Task<IReadOnlyList<LeaveTypeDto>> ListLeaveTypesAsync(CancellationToken ct = default)
        => await _db.Set<LeaveType>()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Code)
            .Select(t => new LeaveTypeDto(t.Id, t.Code, t.Name, t.DefaultAnnualQuota, t.IsPaid, t.IsCarryForward, t.IsActive))
            .ToListAsync(ct);

    public async Task<Result<long>> RequestLeaveAsync(CreateLeaveRequestDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("HR.LeaveRequest.Create", async () =>
        {
            var employee = await _db.Set<Employee>().FirstOrDefaultAsync(e => e.Id == dto.EmployeeId, ct);
            if (employee is null) return Result<long>.NotFound(Errors.Hr.EmployeeNotFound);

            var leaveType = await _db.Set<LeaveType>().FirstOrDefaultAsync(t => t.Id == dto.LeaveTypeId, ct);
            if (leaveType is null) return Result<long>.NotFound(Errors.Hr.LeaveTypeNotFound);

            var entity = new LeaveRequest
            {
                ShopId = tenant.ShopId,
                EmployeeId = dto.EmployeeId,
                LeaveTypeId = dto.LeaveTypeId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Days = dto.Days,
                Reason = dto.Reason,
                Status = LeaveStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<LeaveRequest>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: false);
    }

    public async Task<Result<bool>> ApproveAsync(long id, long approverUserId, CancellationToken ct = default)
    {
        return await ExecuteAsync("HR.LeaveRequest.Approve", async () =>
        {
            var entity = await _db.Set<LeaveRequest>().FirstOrDefaultAsync(r => r.Id == id, ct);
            if (entity is null) return Result<bool>.NotFound(Errors.Hr.LeaveRequestNotFound);
            if (entity.Status != LeaveStatus.Pending) return Result<bool>.Conflict(Errors.Hr.LeaveRequestNotPending);

            entity.Status = LeaveStatus.Approved;
            entity.ApprovedByUserId = approverUserId;
            entity.ApprovedAtUtc = DateTime.UtcNow;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            var balance = await _db.Set<LeaveBalance>()
                .FirstOrDefaultAsync(b => b.EmployeeId == entity.EmployeeId
                    && b.LeaveTypeId == entity.LeaveTypeId
                    && b.Year == entity.StartDate.Year, ct);

            if (balance is not null)
            {
                balance.UsedDays += entity.Days;
                balance.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> RejectAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("HR.LeaveRequest.Reject", async () =>
        {
            var entity = await _db.Set<LeaveRequest>().FirstOrDefaultAsync(r => r.Id == id, ct);
            if (entity is null) return Result<bool>.NotFound(Errors.Hr.LeaveRequestNotFound);
            if (entity.Status != LeaveStatus.Pending) return Result<bool>.Conflict(Errors.Hr.LeaveRequestNotPending);

            entity.Status = LeaveStatus.Rejected;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: false);
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> ListRequestsAsync(CancellationToken ct = default)
        => await _db.Set<LeaveRequest>()
            .Include(r => r.Employee)
            .Include(r => r.LeaveType)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new LeaveRequestDto(
                r.Id, r.EmployeeId,
                r.Employee.FirstName + " " + r.Employee.LastName,
                r.LeaveType.Code, r.StartDate, r.EndDate,
                r.Days, r.Reason, r.Status, r.ApprovedAtUtc))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LeaveBalanceDto>> GetBalancesAsync(long employeeId, int year, CancellationToken ct = default)
        => await _db.Set<LeaveBalance>()
            .Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == employeeId && b.Year == year)
            .Select(b => new LeaveBalanceDto(
                b.LeaveTypeId, b.LeaveType.Code, b.LeaveType.Name,
                b.EntitledDays, b.UsedDays, b.CarryForwardDays,
                b.EntitledDays + b.CarryForwardDays - b.UsedDays))
            .ToListAsync(ct);
}
