using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Hr.Entities;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Hr.Services;

public sealed class AttendanceService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), IAttendanceService
{
    public async Task<Result<long>> CheckInAsync(CheckInDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("HR.Attendance.CheckIn", async () =>
        {
            var today = DateTime.UtcNow.Date;
            var exists = await _db.Set<Attendance>()
                .AnyAsync(a => a.EmployeeId == dto.EmployeeId && a.Date == today, ct);
            if (exists) return Result<long>.Conflict(Errors.Hr.AttendanceAlreadyExists);

            var entity = new Attendance
            {
                ShopId = tenant.ShopId,
                EmployeeId = dto.EmployeeId,
                Date = today,
                CheckInUtc = DateTime.UtcNow,
                StatusCode = "Present",
                Notes = dto.Notes,
                CheckInLatitude = dto.Latitude,
                CheckInLongitude = dto.Longitude,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<Attendance>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: false);
    }

    public async Task<Result<bool>> CheckOutAsync(long employeeId, CancellationToken ct = default)
    {
        return await ExecuteAsync("HR.Attendance.CheckOut", async () =>
        {
            var today = DateTime.UtcNow.Date;
            var entity = await _db.Set<Attendance>()
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == today, ct);
            if (entity is null) return Result<bool>.NotFound(Errors.Hr.AttendanceNotFound);

            entity.CheckOutUtc = DateTime.UtcNow;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: false);
    }

    public async Task<Result<bool>> BulkMarkAsync(BulkAttendanceDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("HR.Attendance.BulkMark", async () =>
        {
            foreach (var entry in dto.Entries)
            {
                var exists = await _db.Set<Attendance>()
                    .AnyAsync(a => a.EmployeeId == entry.EmployeeId && a.Date == dto.Date.Date, ct);
                if (!exists)
                {
                    _db.Set<Attendance>().Add(new Attendance
                    {
                        ShopId = tenant.ShopId,
                        EmployeeId = entry.EmployeeId,
                        Date = dto.Date.Date,
                        StatusCode = entry.StatusCode,
                        Notes = entry.Notes,
                        CreatedAtUtc = DateTime.UtcNow,
                    });
                }
            }
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<IReadOnlyList<AttendanceDto>> ListAsync(int year, int month, CancellationToken ct = default)
    {
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1);
        return await _db.Set<Attendance>()
            .Include(a => a.Employee)
            .Where(a => a.Date >= from && a.Date < to)
            .OrderBy(a => a.Date).ThenBy(a => a.EmployeeId)
            .Select(a => new AttendanceDto(
                a.Id, a.EmployeeId,
                a.Employee.FirstName + " " + a.Employee.LastName,
                a.Date, a.CheckInUtc, a.CheckOutUtc, a.StatusCode, a.Notes))
            .ToListAsync(ct);
    }
}
