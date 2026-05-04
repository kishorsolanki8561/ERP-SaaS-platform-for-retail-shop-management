using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Modules.Verticals.Medical.Enums;
using ErpSaas.Modules.Verticals.Medical.Infrastructure;
using ErpSaas.Modules.Verticals.Medical.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Medical;

internal sealed class MedicalTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        MedicalModelConfiguration.Configure(modelBuilder);
    }
}

[Trait("Category", "Unit")]
public sealed class MedicalInventoryServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly SqliteConnection _sqlite;
    private readonly MedicalInventoryService _sut;
    private readonly StubTenantContext _ctx = new(1L);

    public MedicalInventoryServiceTests()
    {
        _sqlite = new SqliteConnection("DataSource=:memory:");
        _sqlite.Open();
        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqlite)
            .Options;
        _db = new MedicalTenantDbContext(opts, _ctx, new AuditSaveChangesInterceptor(_ctx), new TenantSaveChangesInterceptor(_ctx));
        _db.Database.EnsureCreated();
        _sut = new MedicalInventoryService(_db, _errorLogger, _ctx);
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqlite.Dispose();
    }

    private static CreateDrugBatchDto MakeBatchDto(string batchNumber = "BATCH-001", long productId = 1) =>
        new(productId, batchNumber, "Amoxicillin", "PharmaCo",
            DrugSchedule.H, DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.AddYears(2),
            InitialQuantity: 100, PurchasePrice: 10, SellingPrice: 15, PurchaseBillId: null);

    [Fact]
    public async Task CreateBatchAsync_HappyPath_PersistsBatch()
    {
        var result = await _sut.CreateBatchAsync(MakeBatchDto());
        Assert.True(result.IsSuccess);
        Assert.True(result.Value > 0);
        var batch = await _db.Set<global::ErpSaas.Modules.Verticals.Medical.Entities.DrugBatch>().FirstAsync();
        Assert.Equal(100m, batch.CurrentQuantity);
        Assert.True(batch.IsActive);
    }

    [Fact]
    public async Task CreateBatchAsync_DuplicateBatchNumberForSameProduct_ReturnsConflict()
    {
        await _sut.CreateBatchAsync(MakeBatchDto("BATCH-DUPE", 1));
        var result = await _sut.CreateBatchAsync(MakeBatchDto("BATCH-DUPE", 1));
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Medical.BatchNumberExists, result.Errors[0]);
    }

    [Fact]
    public async Task CreateBatchAsync_SameBatchNumberDifferentProduct_Succeeds()
    {
        await _sut.CreateBatchAsync(MakeBatchDto("BATCH-001", productId: 1));
        var result = await _sut.CreateBatchAsync(MakeBatchDto("BATCH-001", productId: 2));
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetBatchAsync_Found_ReturnsDto()
    {
        var id = (await _sut.CreateBatchAsync(MakeBatchDto())).Value;
        var dto = await _sut.GetBatchAsync(id);
        Assert.NotNull(dto);
        Assert.Equal("BATCH-001", dto!.BatchNumber);
    }

    [Fact]
    public async Task GetBatchAsync_NotFound_ReturnsNull()
    {
        var dto = await _sut.GetBatchAsync(9999);
        Assert.Null(dto);
    }

    [Fact]
    public async Task ListBatchesAsync_FilterByProduct_ReturnsOnlyThatProduct()
    {
        await _sut.CreateBatchAsync(MakeBatchDto("B1", productId: 1));
        await _sut.CreateBatchAsync(MakeBatchDto("B2", productId: 2));
        var list = await _sut.ListBatchesAsync(productId: 1, expiringWithin30Days: null);
        Assert.Single(list);
    }

    [Fact]
    public async Task ListExpiringAsync_OnlyReturnsBatchesExpiringWithinWindow()
    {
        var expiringSoon = MakeBatchDto("EXP-SOON") with { ExpiryDate = DateTime.UtcNow.AddDays(10) };
        var expireLater = MakeBatchDto("EXP-LATER") with { ExpiryDate = DateTime.UtcNow.AddYears(2) };

        await _sut.CreateBatchAsync(expiringSoon);
        await _sut.CreateBatchAsync(expireLater);

        var expiring = await _sut.ListExpiringAsync(30);
        Assert.Single(expiring);
        Assert.Equal("EXP-SOON", expiring[0].BatchNumber);
    }

    [Fact]
    public async Task RecordPrescriptionAsync_HappyPath_DecrementsCurrentQuantity()
    {
        var batchId = (await _sut.CreateBatchAsync(MakeBatchDto())).Value;
        var dto = new RecordPrescriptionDto(batchId, InvoiceId: 1, CustomerId: 1,
            DoctorName: "Dr. Sharma", DoctorRegistrationNumber: "REG-001",
            PrescriptionDate: DateTime.Today, QuantityDispensed: 10, Notes: null);

        var result = await _sut.RecordPrescriptionAsync(dto);
        Assert.True(result.IsSuccess);

        var batch = await _db.Set<global::ErpSaas.Modules.Verticals.Medical.Entities.DrugBatch>().FirstAsync();
        Assert.Equal(90m, batch.CurrentQuantity);
    }

    [Fact]
    public async Task RecordPrescriptionAsync_BatchNotFound_ReturnsNotFound()
    {
        var dto = new RecordPrescriptionDto(9999, 1, 1, "Dr. X", null, DateTime.Today, 5, null);
        var result = await _sut.RecordPrescriptionAsync(dto);
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Medical.BatchNotFound, result.Errors[0]);
    }

    [Fact]
    public async Task RecordPrescriptionAsync_ExpiredBatch_ReturnsConflict()
    {
        var expiredDto = MakeBatchDto("EXPIRED") with { ExpiryDate = DateTime.UtcNow.AddDays(-1) };
        var batchId = (await _sut.CreateBatchAsync(expiredDto)).Value;

        var dto = new RecordPrescriptionDto(batchId, 1, 1, "Dr. X", null, DateTime.Today, 5, null);
        var result = await _sut.RecordPrescriptionAsync(dto);
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Medical.BatchExpired, result.Errors[0]);
    }

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
