using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Hr.Entities;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Hr.Services;

public sealed class StaffActivityService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), IStaffActivityService
{
    public async Task<IReadOnlyList<StaffActivityDto>> ListAsync(long? employeeId = null, CancellationToken ct = default)
        => await _db.Set<StaffActivity>()
            .Include(a => a.Employee)
            .Where(a => employeeId == null || a.EmployeeId == employeeId)
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(200)
            .Select(a => new StaffActivityDto(
                a.Id, a.EmployeeId,
                a.Employee.FirstName + " " + a.Employee.LastName,
                a.ActivityType, a.Description,
                a.RelatedEntityId, a.RelatedEntityType,
                a.OccurredAtUtc))
            .ToListAsync(ct);

    public async Task<Result<bool>> TrackAsync(
        long employeeId, string activityType, string? description = null,
        long? relatedEntityId = null, string? relatedEntityType = null,
        CancellationToken ct = default)
    {
        return await ExecuteAsync("HR.StaffActivity.Track", async () =>
        {
            _db.Set<StaffActivity>().Add(new StaffActivity
            {
                ShopId = tenant.ShopId,
                EmployeeId = employeeId,
                ActivityType = activityType,
                Description = description,
                RelatedEntityId = relatedEntityId,
                RelatedEntityType = relatedEntityType,
                OccurredAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: false);
    }
}
