using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Modules.Purchasing.Entities;
using ErpSaas.Modules.Purchasing.Enums;
using ErpSaas.Modules.Purchasing.Infrastructure;
using ErpSaas.Modules.Purchasing.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Purchasing;

internal sealed class PurchasingTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        PurchasingModelConfiguration.Configure(modelBuilder);
    }
}

[Trait("Category", "Unit")]
public class PurchasingServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly ISequenceService _sequence  = Substitute.For<ISequenceService>();
    private readonly IAutoVoucherService _autoVoucher = Substitute.For<IAutoVoucherService>();
    private readonly PurchasingService _sut;
    private readonly SqliteConnection _sqliteConnection;
    private int _seqCounter;

    public PurchasingServiceTests()
    {
        var stubCtx = new StubTenantContext(shopId: 1L);

        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        var auditInterceptor  = new AuditSaveChangesInterceptor(stubCtx);
        var tenantInterceptor = new TenantSaveChangesInterceptor(stubCtx);
        _db = new PurchasingTenantDbContext(opts, stubCtx, auditInterceptor, tenantInterceptor);
        _db.Database.EnsureCreated();

        _sequence.NextAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult($"PO-{++_seqCounter:000000}"));

        _sut = new PurchasingService(_db, _errorLogger, _sequence, stubCtx, _autoVoucher);
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqliteConnection.Dispose();
    }

    // ── CreateSupplierAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateSupplierAsync_ValidDto_ReturnsSuccessWithId()
    {
        var dto = new CreateSupplierDto("Acme Ltd", "ACM", "29AAACR5055K1ZK", null,
            "9999999999", null, null, null, null, null, 0, null);

        var result = await _sut.CreateSupplierAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateSupplierAsync_DuplicateCode_ReturnsConflict()
    {
        _db.Set<Supplier>().Add(new Supplier
        {
            ShopId = 1L, Name = "Acme", Code = "ACM",
            IsActive = true, CreatedAtUtc = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();

        var dto = new CreateSupplierDto("Acme 2", "ACM", null, null, null, null, null, null, null, null, 0, null);
        var result = await _sut.CreateSupplierAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Errors.Purchasing.SupplierCodeExists);
    }

    // ── UpdateSupplierAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSupplierAsync_NotFound_ReturnsNotFound()
    {
        var dto = new UpdateSupplierDto("X", null, null, null, null, null, null, null, null, true, null);
        var result = await _sut.UpdateSupplierAsync(99999L, dto);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Errors.Purchasing.SupplierNotFound);
    }

    // ── CreatePurchaseOrderAsync ──────────────────────────────────────────────

    [Fact]
    public async Task CreatePurchaseOrderAsync_ValidSupplier_ReturnsSuccessWithId()
    {
        var supplier = SeedSupplier();
        var dto = new CreatePurchaseOrderDto(supplier.Id, DateTime.Today, null, null, null,
            [new(1L, 1L, 10m, 500m, 0m, 18m)]);

        var result = await _sut.CreatePurchaseOrderAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_SupplierNotFound_ReturnsNotFound()
    {
        var dto = new CreatePurchaseOrderDto(99999L, DateTime.Today, null, null, null,
            [new(1L, 1L, 10m, 500m, 0m, 18m)]);

        var result = await _sut.CreatePurchaseOrderAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Errors.Purchasing.SupplierNotFound);
    }

    // ── SendPurchaseOrderAsync ────────────────────────────────────────────────

    [Fact]
    public async Task SendPurchaseOrderAsync_DraftPo_TransitionsToSent()
    {
        var po = SeedDraftPo();

        var result = await _sut.SendPurchaseOrderAsync(po.Id);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Set<PurchaseOrder>().FindAsync(po.Id);
        updated!.Status.Should().Be(PurchaseOrderStatus.Sent);
    }

    [Fact]
    public async Task SendPurchaseOrderAsync_AlreadySent_ReturnsConflict()
    {
        var po = SeedDraftPo();
        po.Status = PurchaseOrderStatus.Sent;
        await _db.SaveChangesAsync();

        var result = await _sut.SendPurchaseOrderAsync(po.Id);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Errors.Purchasing.PoNotDraft);
    }

    // ── CancelPurchaseOrderAsync ──────────────────────────────────────────────

    [Fact]
    public async Task CancelPurchaseOrderAsync_Draft_TransitionsToCancelled()
    {
        var po = SeedDraftPo();

        var result = await _sut.CancelPurchaseOrderAsync(po.Id);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Set<PurchaseOrder>().FindAsync(po.Id);
        updated!.Status.Should().Be(PurchaseOrderStatus.Cancelled);
    }

    // ── CreateBillAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBillAsync_ValidDto_ReturnsSuccessWithId()
    {
        var supplier = SeedSupplier();
        var dto = new CreateBillDto(supplier.Id, "SUP-INV-001", null,
            DateTime.Today, null, null, 5000m, 900m, 5900m);

        var result = await _sut.CreateBillAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    // ── PayBillAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task PayBillAsync_FullPayment_TransitionsToPaid()
    {
        var bill = SeedApprovedBill(5000m);

        var dto = new PayBillDto(bill.Id, DateTime.Today, 5000m, "CASH", null, null);
        var result = await _sut.PayBillAsync(dto);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Set<Bill>().FindAsync(bill.Id);
        updated!.Status.Should().Be(BillStatus.Paid);
        updated.OutstandingAmount.Should().Be(0);
    }

    [Fact]
    public async Task PayBillAsync_Overpayment_ReturnsConflict()
    {
        var bill = SeedApprovedBill(5000m);

        var dto = new PayBillDto(bill.Id, DateTime.Today, 6000m, "CASH", null, null);
        var result = await _sut.PayBillAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Errors.Purchasing.BillOverpayment);
    }

    [Fact]
    public async Task PayBillAsync_DraftBill_ReturnsConflict()
    {
        var supplier = SeedSupplier();
        var bill = new Bill
        {
            ShopId = 1L, BillNumber = "BILL-000001",
            SupplierId = supplier.Id, SupplierNameSnapshot = supplier.Name,
            BillDate = DateTime.Today, Status = BillStatus.Draft,
            GrandTotal = 1000m, OutstandingAmount = 1000m,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Set<Bill>().Add(bill);
        await _db.SaveChangesAsync();

        var dto = new PayBillDto(bill.Id, DateTime.Today, 500m, "CASH", null, null);
        var result = await _sut.PayBillAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Errors.Purchasing.BillNotApproved);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }

    private Supplier SeedSupplier()
    {
        var s = new Supplier
        {
            ShopId = 1L, Name = "Test Supplier", Code = "TEST",
            IsActive = true, CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Set<Supplier>().Add(s);
        _db.SaveChanges();
        return s;
    }

    private PurchaseOrder SeedDraftPo()
    {
        var supplier = SeedSupplier();
        var po = new PurchaseOrder
        {
            ShopId = 1L, PoNumber = "PO-000001",
            SupplierId = supplier.Id, SupplierNameSnapshot = supplier.Name,
            OrderDate = DateTime.Today, Status = PurchaseOrderStatus.Draft,
            GrandTotal = 5900m, CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Set<PurchaseOrder>().Add(po);
        _db.SaveChanges();
        return po;
    }

    private Bill SeedApprovedBill(decimal amount)
    {
        var supplier = SeedSupplier();
        var bill = new Bill
        {
            ShopId = 1L, BillNumber = "BILL-000001",
            SupplierId = supplier.Id, SupplierNameSnapshot = supplier.Name,
            BillDate = DateTime.Today, Status = BillStatus.Approved,
            GrandTotal = amount, PaidAmount = 0, OutstandingAmount = amount,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Set<Bill>().Add(bill);
        _db.SaveChanges();
        return bill;
    }
}
