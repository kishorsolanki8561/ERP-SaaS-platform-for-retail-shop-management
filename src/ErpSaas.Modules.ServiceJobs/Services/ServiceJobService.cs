using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Modules.ServiceJobs.Entities;
using ErpSaas.Modules.ServiceJobs.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.ServiceJobs.Services;

public sealed class ServiceJobService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ISequenceService sequence,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), IServiceJobService
{
    public async Task<Result<long>> ReceiveAsync(ReceiveServiceJobDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("ServiceJob.Receive", async () =>
        {
            var jobNumber = await sequence.NextAsync(Constants.SequenceCodes.ServiceJob, tenant.ShopId, ct);
            var job = new ServiceJob
            {
                ShopId = tenant.ShopId,
                JobNumber = jobNumber,
                ReceivedAtDate = DateTime.UtcNow.Date,
                BranchId = dto.BranchId,
                CustomerId = dto.CustomerId,
                ProductId = dto.ProductId,
                ItemDescription = dto.ItemDescription,
                SerialNumber = dto.SerialNumber,
                ReportedIssue = dto.ReportedIssue,
                IsUnderWarranty = dto.IsUnderWarranty,
                WarrantyRegistrationId = dto.WarrantyRegistrationId,
                Status = ServiceJobStatus.Received,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<ServiceJob>().Add(job);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(job.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> DiagnoseAsync(long jobId, DiagnoseServiceJobDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("ServiceJob.Diagnose", async () =>
        {
            var job = await GetJobOrFail(jobId, ct);
            if (job is null) return Result<bool>.NotFound(Errors.ServiceJobs.NotFound);
            if (job.Status != ServiceJobStatus.Received)
                return Result<bool>.Conflict(Errors.ServiceJobs.InvalidStatusTransition);

            job.DiagnosisNotes = dto.DiagnosisNotes;
            job.EstimatedCost = dto.EstimatedCost;
            job.AssignedTechnicianUserId = dto.AssignedTechnicianUserId;
            job.Status = ServiceJobStatus.Diagnosed;
            job.DiagnosedAtUtc = DateTime.UtcNow;
            job.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> CustomerApproveAsync(long jobId, CancellationToken ct = default)
    {
        return await ExecuteAsync("ServiceJob.CustomerApprove", async () =>
        {
            var job = await GetJobOrFail(jobId, ct);
            if (job is null) return Result<bool>.NotFound(Errors.ServiceJobs.NotFound);
            if (job.Status != ServiceJobStatus.Diagnosed)
                return Result<bool>.Conflict(Errors.ServiceJobs.InvalidStatusTransition);

            job.Status = ServiceJobStatus.Approved;
            job.ApprovedByCustomerAtUtc = DateTime.UtcNow;
            job.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> StartProgressAsync(long jobId, CancellationToken ct = default)
    {
        return await ExecuteAsync("ServiceJob.StartProgress", async () =>
        {
            var job = await GetJobOrFail(jobId, ct);
            if (job is null) return Result<bool>.NotFound(Errors.ServiceJobs.NotFound);
            if (job.Status != ServiceJobStatus.Approved)
                return Result<bool>.Conflict(Errors.ServiceJobs.InvalidStatusTransition);

            job.Status = ServiceJobStatus.InProgress;
            job.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> MarkReadyAsync(long jobId, CancellationToken ct = default)
    {
        return await ExecuteAsync("ServiceJob.MarkReady", async () =>
        {
            var job = await GetJobOrFail(jobId, ct);
            if (job is null) return Result<bool>.NotFound(Errors.ServiceJobs.NotFound);
            if (job.Status != ServiceJobStatus.InProgress)
                return Result<bool>.Conflict(Errors.ServiceJobs.InvalidStatusTransition);

            var parts = await _db.Set<ServiceJobPart>()
                .Where(p => p.ServiceJobId == jobId).ToListAsync(ct);
            var labor = await _db.Set<ServiceJobLabor>()
                .Where(l => l.ServiceJobId == jobId).ToListAsync(ct);

            job.ActualPartsCost = parts.Sum(p => p.LineCost);
            job.ActualLaborCost = labor.Sum(l => l.LaborCost);
            job.TotalCost = job.ActualPartsCost + job.ActualLaborCost;
            job.Status = ServiceJobStatus.Ready;
            job.CompletedAtUtc = DateTime.UtcNow;
            job.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> DeliverAsync(long jobId, CancellationToken ct = default)
    {
        return await ExecuteAsync("ServiceJob.Deliver", async () =>
        {
            var job = await GetJobOrFail(jobId, ct);
            if (job is null) return Result<bool>.NotFound(Errors.ServiceJobs.NotFound);
            if (job.Status != ServiceJobStatus.Ready)
                return Result<bool>.Conflict(Errors.ServiceJobs.InvalidStatusTransition);

            job.Status = ServiceJobStatus.Delivered;
            job.DeliveredAtUtc = DateTime.UtcNow;
            job.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> RejectAsync(long jobId, string reason, CancellationToken ct = default)
    {
        return await ExecuteAsync("ServiceJob.Reject", async () =>
        {
            var job = await GetJobOrFail(jobId, ct);
            if (job is null) return Result<bool>.NotFound(Errors.ServiceJobs.NotFound);
            if (job.Status is ServiceJobStatus.Delivered or ServiceJobStatus.Rejected)
                return Result<bool>.Conflict(Errors.ServiceJobs.AlreadyDelivered);

            job.DiagnosisNotes = (job.DiagnosisNotes ?? string.Empty) + $"\nRejected: {reason}";
            job.Status = ServiceJobStatus.Rejected;
            job.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> AddPartAsync(long jobId, AddPartDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("ServiceJob.AddPart", async () =>
        {
            var job = await GetJobOrFail(jobId, ct);
            if (job is null) return Result<bool>.NotFound(Errors.ServiceJobs.NotFound);

            var part = new ServiceJobPart
            {
                ShopId = tenant.ShopId,
                ServiceJobId = jobId,
                ProductId = dto.ProductId,
                ProductNameSnapshot = "—",
                Quantity = dto.Quantity,
                UnitCost = 0,
                LineCost = 0,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<ServiceJobPart>().Add(part);
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> AddLaborAsync(long jobId, AddLaborDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("ServiceJob.AddLabor", async () =>
        {
            var job = await GetJobOrFail(jobId, ct);
            if (job is null) return Result<bool>.NotFound(Errors.ServiceJobs.NotFound);

            var labor = new ServiceJobLabor
            {
                ShopId = tenant.ShopId,
                ServiceJobId = jobId,
                TechnicianUserId = dto.TechnicianUserId,
                TechnicianNameSnapshot = "—",
                Hours = dto.Hours,
                HourlyRate = dto.HourlyRate,
                LaborCost = dto.Hours * dto.HourlyRate,
                Notes = dto.Notes,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<ServiceJobLabor>().Add(labor);
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<ServiceJobDetailDto?> GetAsync(long jobId, CancellationToken ct = default)
    {
        var job = await _db.Set<ServiceJob>()
            .Include(j => j.Parts)
            .Include(j => j.LaborEntries)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);
        return job is null ? null : MapDetail(job);
    }

    public async Task<ServiceJobDetailDto?> GetByJobNumberAsync(string jobNumber, CancellationToken ct = default)
    {
        var job = await _db.Set<ServiceJob>()
            .Include(j => j.Parts)
            .Include(j => j.LaborEntries)
            .FirstOrDefaultAsync(j => j.JobNumber == jobNumber, ct);
        return job is null ? null : MapDetail(job);
    }

    public async Task<IReadOnlyList<ServiceJobSummaryDto>> ListAsync(ServiceJobStatus? status, CancellationToken ct = default)
    {
        var query = _db.Set<ServiceJob>().AsQueryable();
        if (status.HasValue) query = query.Where(j => j.Status == status.Value);
        return await query
            .OrderByDescending(j => j.CreatedAtUtc)
            .Select(j => new ServiceJobSummaryDto(
                j.Id, j.JobNumber, j.ReceivedAtDate, j.CustomerId,
                j.CustomerNameSnapshot, j.ItemDescription, j.SerialNumber,
                j.Status, j.TotalCost, j.DeliveredAtUtc))
            .ToListAsync(ct);
    }

    private async Task<ServiceJob?> GetJobOrFail(long jobId, CancellationToken ct)
        => await _db.Set<ServiceJob>().FirstOrDefaultAsync(j => j.Id == jobId, ct);

    private static ServiceJobDetailDto MapDetail(ServiceJob j) => new(
        j.Id, j.JobNumber, j.ReceivedAtDate, j.CustomerId,
        j.CustomerNameSnapshot, j.CustomerPhoneSnapshot,
        j.ProductId, j.ItemDescription, j.SerialNumber,
        j.IsUnderWarranty, j.WarrantyRegistrationId,
        j.ReportedIssue, j.DiagnosisNotes, j.Status,
        j.AssignedTechnicianUserId, j.EstimatedCost,
        j.ActualPartsCost, j.ActualLaborCost, j.TotalCost,
        j.DiagnosedAtUtc, j.ApprovedByCustomerAtUtc, j.CompletedAtUtc, j.DeliveredAtUtc,
        j.ResultingInvoiceId,
        j.Parts.Select(p => new PartLineDto(p.Id, p.ProductId, p.ProductNameSnapshot, p.Quantity, p.UnitCost, p.LineCost)).ToList(),
        j.LaborEntries.Select(l => new LaborLineDto(l.Id, l.TechnicianUserId, l.TechnicianNameSnapshot, l.Hours, l.HourlyRate, l.LaborCost, l.Notes)).ToList());
}
