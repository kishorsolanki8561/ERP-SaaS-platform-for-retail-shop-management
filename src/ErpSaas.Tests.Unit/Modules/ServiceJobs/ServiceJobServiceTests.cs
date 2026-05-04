using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Modules.ServiceJobs.Enums;
using ErpSaas.Modules.ServiceJobs.Infrastructure;
using ErpSaas.Modules.ServiceJobs.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.ServiceJobs;

internal sealed class ServiceJobTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ServiceJobModelConfiguration.Configure(modelBuilder);
    }
}

[Trait("Category", "Unit")]
public sealed class ServiceJobServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly ISequenceService _sequence = Substitute.For<ISequenceService>();
    private readonly SqliteConnection _sqlite;
    private int _seqCounter;
    private readonly ServiceJobService _sut;
    private readonly StubTenantContext _ctx = new(1L);

    public ServiceJobServiceTests()
    {
        _sqlite = new SqliteConnection("DataSource=:memory:");
        _sqlite.Open();
        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqlite)
            .Options;
        _db = new ServiceJobTenantDbContext(opts, _ctx, new AuditSaveChangesInterceptor(_ctx), new TenantSaveChangesInterceptor(_ctx));
        _db.Database.EnsureCreated();

        _sequence.NextAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult($"SJ-{++_seqCounter:000000}"));

        _sut = new ServiceJobService(_db, _errorLogger, _sequence, _ctx);
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqlite.Dispose();
    }

    private static ReceiveServiceJobDto MakeReceiveDto(string desc = "Drill Machine") =>
        new(CustomerId: 1, BranchId: 1, ProductId: 1, ItemDescription: desc,
            SerialNumber: "SN-001", ReportedIssue: "Not working", IsUnderWarranty: false,
            WarrantyRegistrationId: null);

    [Fact]
    public async Task ReceiveAsync_HappyPath_PersistsJob()
    {
        var result = await _sut.ReceiveAsync(MakeReceiveDto());
        Assert.True(result.IsSuccess);
        Assert.Equal(ServiceJobStatus.Received, (await _db.Set<global::ErpSaas.Modules.ServiceJobs.Entities.ServiceJob>().FirstAsync()).Status);
    }

    [Fact]
    public async Task ReceiveAsync_AssignsJobNumberFromSequence()
    {
        var result = await _sut.ReceiveAsync(MakeReceiveDto());
        var job = await _db.Set<global::ErpSaas.Modules.ServiceJobs.Entities.ServiceJob>().FirstAsync();
        Assert.Equal("SJ-000001", job.JobNumber);
    }

    [Fact]
    public async Task DiagnoseAsync_FromReceived_TransitionsToDiagnosed()
    {
        var id = (await _sut.ReceiveAsync(MakeReceiveDto())).Value;
        var result = await _sut.DiagnoseAsync(id, new DiagnoseServiceJobDto("Battery dead", 500, null));
        Assert.True(result.IsSuccess);
        var job = await _db.Set<global::ErpSaas.Modules.ServiceJobs.Entities.ServiceJob>().FirstAsync();
        Assert.Equal(ServiceJobStatus.Diagnosed, job.Status);
        Assert.Equal("Battery dead", job.DiagnosisNotes);
    }

    [Fact]
    public async Task DiagnoseAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.DiagnoseAsync(9999, new DiagnoseServiceJobDto("Notes", null, null));
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.ServiceJobs.NotFound, result.Errors[0]);
    }

    [Fact]
    public async Task DiagnoseAsync_WrongStatus_ReturnsConflict()
    {
        var id = (await _sut.ReceiveAsync(MakeReceiveDto())).Value;
        await _sut.DiagnoseAsync(id, new DiagnoseServiceJobDto("Notes", null, null));
        var result = await _sut.DiagnoseAsync(id, new DiagnoseServiceJobDto("Again", null, null));
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.ServiceJobs.InvalidStatusTransition, result.Errors[0]);
    }

    [Fact]
    public async Task CustomerApproveAsync_FromDiagnosed_TransitionsToApproved()
    {
        var id = (await _sut.ReceiveAsync(MakeReceiveDto())).Value;
        await _sut.DiagnoseAsync(id, new DiagnoseServiceJobDto("Notes", null, null));
        var result = await _sut.CustomerApproveAsync(id);
        Assert.True(result.IsSuccess);
        var job = await _db.Set<global::ErpSaas.Modules.ServiceJobs.Entities.ServiceJob>().FirstAsync();
        Assert.Equal(ServiceJobStatus.Approved, job.Status);
    }

    [Fact]
    public async Task CustomerApproveAsync_WrongStatus_ReturnsConflict()
    {
        var id = (await _sut.ReceiveAsync(MakeReceiveDto())).Value;
        var result = await _sut.CustomerApproveAsync(id);
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.ServiceJobs.InvalidStatusTransition, result.Errors[0]);
    }

    [Fact]
    public async Task StartProgressAsync_FromApproved_TransitionsToInProgress()
    {
        var id = (await _sut.ReceiveAsync(MakeReceiveDto())).Value;
        await _sut.DiagnoseAsync(id, new DiagnoseServiceJobDto("Notes", null, null));
        await _sut.CustomerApproveAsync(id);
        var result = await _sut.StartProgressAsync(id);
        Assert.True(result.IsSuccess);
        var job = await _db.Set<global::ErpSaas.Modules.ServiceJobs.Entities.ServiceJob>().FirstAsync();
        Assert.Equal(ServiceJobStatus.InProgress, job.Status);
    }

    [Fact]
    public async Task MarkReadyAsync_AggregatesTotalCost()
    {
        var id = (await _sut.ReceiveAsync(MakeReceiveDto())).Value;
        await _sut.DiagnoseAsync(id, new DiagnoseServiceJobDto("Notes", null, null));
        await _sut.CustomerApproveAsync(id);
        await _sut.StartProgressAsync(id);
        await _sut.AddPartAsync(id, new AddPartDto(1, 2));
        await _sut.AddLaborAsync(id, new AddLaborDto(1, 3, 100, null));
        var result = await _sut.MarkReadyAsync(id);
        Assert.True(result.IsSuccess);
        var job = await _db.Set<global::ErpSaas.Modules.ServiceJobs.Entities.ServiceJob>().FirstAsync();
        Assert.Equal(ServiceJobStatus.Ready, job.Status);
        Assert.Equal(300m, job.TotalCost);
    }

    [Fact]
    public async Task DeliverAsync_FromReady_TransitionsToDelivered()
    {
        var id = (await _sut.ReceiveAsync(MakeReceiveDto())).Value;
        await _sut.DiagnoseAsync(id, new DiagnoseServiceJobDto("Notes", null, null));
        await _sut.CustomerApproveAsync(id);
        await _sut.StartProgressAsync(id);
        await _sut.MarkReadyAsync(id);
        var result = await _sut.DeliverAsync(id);
        Assert.True(result.IsSuccess);
        var job = await _db.Set<global::ErpSaas.Modules.ServiceJobs.Entities.ServiceJob>().FirstAsync();
        Assert.Equal(ServiceJobStatus.Delivered, job.Status);
        Assert.NotNull(job.DeliveredAtUtc);
    }

    [Fact]
    public async Task RejectAsync_FromReceived_TransitionsToRejected()
    {
        var id = (await _sut.ReceiveAsync(MakeReceiveDto())).Value;
        var result = await _sut.RejectAsync(id, "Cannot repair");
        Assert.True(result.IsSuccess);
        var job = await _db.Set<global::ErpSaas.Modules.ServiceJobs.Entities.ServiceJob>().FirstAsync();
        Assert.Equal(ServiceJobStatus.Rejected, job.Status);
    }

    [Fact]
    public async Task RejectAsync_AlreadyDelivered_ReturnsConflict()
    {
        var id = (await _sut.ReceiveAsync(MakeReceiveDto())).Value;
        await _sut.DiagnoseAsync(id, new DiagnoseServiceJobDto("Notes", null, null));
        await _sut.CustomerApproveAsync(id);
        await _sut.StartProgressAsync(id);
        await _sut.MarkReadyAsync(id);
        await _sut.DeliverAsync(id);
        var result = await _sut.RejectAsync(id, "Too late");
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.ServiceJobs.AlreadyDelivered, result.Errors[0]);
    }

    [Fact]
    public async Task GetByJobNumberAsync_Found_ReturnsDetail()
    {
        await _sut.ReceiveAsync(MakeReceiveDto("Angle Grinder"));
        var result = await _sut.GetByJobNumberAsync("SJ-000001");
        Assert.NotNull(result);
        Assert.Equal("SJ-000001", result!.JobNumber);
        Assert.Equal("Angle Grinder", result.ItemDescription);
    }

    [Fact]
    public async Task GetByJobNumberAsync_NotFound_ReturnsNull()
    {
        var result = await _sut.GetByJobNumberAsync("SJ-NOTEXIST");
        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsync_WithStatusFilter_ReturnsMatchingOnly()
    {
        await _sut.ReceiveAsync(MakeReceiveDto("Job 1"));
        var id2 = (await _sut.ReceiveAsync(MakeReceiveDto("Job 2"))).Value;
        await _sut.DiagnoseAsync(id2, new DiagnoseServiceJobDto("Fixed", null, null));

        var received = await _sut.ListAsync(ServiceJobStatus.Received);
        var diagnosed = await _sut.ListAsync(ServiceJobStatus.Diagnosed);

        Assert.Single(received);
        Assert.Single(diagnosed);
    }

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
