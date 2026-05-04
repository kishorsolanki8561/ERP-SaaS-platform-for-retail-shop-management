using System.Text.Json;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Hr.Entities;
using ErpSaas.Modules.Hr.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Hr.Services;

public sealed class PayrollService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), IPayrollService
{
    public async Task<Result<long>> GenerateAsync(GeneratePayrollDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("HR.Payroll.Generate", async () =>
        {
            var employee = await _db.Set<Employee>()
                .Include(e => e.SalaryComponents)
                .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId, ct);
            if (employee is null) return Result<long>.NotFound(Errors.Hr.EmployeeNotFound);

            var alreadyExists = await _db.Set<Payroll>()
                .AnyAsync(p => p.EmployeeId == dto.EmployeeId && p.Year == dto.Year && p.Month == dto.Month, ct);
            if (alreadyExists) return Result<long>.Conflict(Errors.Hr.PayrollAlreadyExists);

            var from = new DateTime(dto.Year, dto.Month, 1);
            var to = from.AddMonths(1);
            var attendance = await _db.Set<Attendance>()
                .Where(a => a.EmployeeId == dto.EmployeeId && a.Date >= from && a.Date < to)
                .ToListAsync(ct);

            var presentDays = attendance.Count(a => a.StatusCode == "Present" || a.StatusCode == "HalfDay");
            var leaveDays = attendance.Count(a => a.StatusCode == "Leave");
            var workingDays = DateTime.DaysInMonth(dto.Year, dto.Month);
            var absentDays = workingDays - presentDays - leaveDays;

            var components = employee.SalaryComponents.ToList();
            var grossEarnings = components.Where(c => c.Type == ComponentType.Earning).Sum(c => c.Amount);
            var totalDeductions = components.Where(c => c.Type == ComponentType.Deduction).Sum(c => c.Amount);

            if (workingDays > 0 && absentDays > 0)
                grossEarnings = grossEarnings * (workingDays - absentDays) / workingDays;

            var netPay = grossEarnings - totalDeductions;
            var details = components.Select(c => new { c.ComponentCode, c.Type, c.Amount }).ToList();

            var payroll = new Payroll
            {
                ShopId = tenant.ShopId,
                EmployeeId = dto.EmployeeId,
                Year = dto.Year,
                Month = dto.Month,
                GrossEarnings = Math.Round(grossEarnings, 2),
                TotalDeductions = Math.Round(totalDeductions, 2),
                NetPay = Math.Round(netPay, 2),
                PresentDays = presentDays,
                AbsentDays = absentDays < 0 ? 0 : absentDays,
                LeaveDays = leaveDays,
                Status = PayrollStatus.Draft,
                DetailsJson = JsonSerializer.Serialize(details),
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<Payroll>().Add(payroll);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(payroll.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> ApproveAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("HR.Payroll.Approve", async () =>
        {
            var entity = await _db.Set<Payroll>().FirstOrDefaultAsync(p => p.Id == id, ct);
            if (entity is null) return Result<bool>.NotFound(Errors.Hr.PayrollNotFound);
            if (entity.Status != PayrollStatus.Draft) return Result<bool>.Conflict(Errors.Hr.PayrollNotDraft);

            entity.Status = PayrollStatus.Approved;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: false);
    }

    public async Task<Result<bool>> PayAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("HR.Payroll.Pay", async () =>
        {
            var entity = await _db.Set<Payroll>().FirstOrDefaultAsync(p => p.Id == id, ct);
            if (entity is null) return Result<bool>.NotFound(Errors.Hr.PayrollNotFound);
            if (entity.Status != PayrollStatus.Approved) return Result<bool>.Conflict(Errors.Hr.PayrollNotApproved);

            entity.Status = PayrollStatus.Paid;
            entity.PaidAtUtc = DateTime.UtcNow;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: false);
    }

    public async Task<PayslipDto?> GetPayslipAsync(long id, CancellationToken ct = default)
    {
        var p = await _db.Set<Payroll>()
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;
        return new PayslipDto(
            p.Id, p.Employee.FirstName + " " + p.Employee.LastName,
            p.Employee.EmployeeCode, p.Year, p.Month,
            p.GrossEarnings, p.TotalDeductions, p.NetPay,
            p.PresentDays, p.AbsentDays, p.LeaveDays, p.DetailsJson);
    }

    public async Task<IReadOnlyList<PayrollDto>> ListAsync(int year, int month, CancellationToken ct = default)
        => await _db.Set<Payroll>()
            .Include(p => p.Employee)
            .Where(p => p.Year == year && p.Month == month)
            .OrderBy(p => p.Employee.FirstName)
            .Select(p => new PayrollDto(
                p.Id, p.EmployeeId,
                p.Employee.FirstName + " " + p.Employee.LastName,
                p.Year, p.Month, p.GrossEarnings, p.TotalDeductions, p.NetPay,
                p.PresentDays, p.AbsentDays, p.LeaveDays, p.Status, p.PaidAtUtc))
            .ToListAsync(ct);
}
