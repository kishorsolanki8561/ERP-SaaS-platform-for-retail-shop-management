using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Messaging;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Modules.Billing.Entities;
using ErpSaas.Modules.Billing.Enums;
using ErpSaas.Modules.Billing.Infrastructure;
using ErpSaas.Modules.Billing.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Billing;

// ── Test-local DbContext that includes Billing entity config ──────────────────

/// <summary>
/// Extends <see cref="TenantDbContext"/> so that the in-memory provider can
/// resolve <c>Invoice</c> and <c>InvoiceLine</c> during unit tests without
/// modifying the production <c>TenantDbContext</c>.
/// </summary>
internal sealed class BillingTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        BillingModelConfiguration.Configure(modelBuilder);
    }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

[Trait("Category", "Unit")]
public class BillingServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly ISequenceService _sequence = Substitute.For<ISequenceService>();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly BillingService _sut;
    private readonly SqliteConnection _sqliteConnection;

    private int _seqCounter;

    public BillingServiceTests()
    {
        var stubCtx = new StubTenantContext(shopId: 1L);

        // Use SQLite in-memory (supports transactions, unlike EF in-memory provider).
        // Keep the connection open for the lifetime of the test so the in-memory DB persists.
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;
        var auditInterceptor = new AuditSaveChangesInterceptor(stubCtx);
        var tenantInterceptor = new TenantSaveChangesInterceptor(stubCtx);

        _db = new BillingTenantDbContext(opts, stubCtx, auditInterceptor, tenantInterceptor);
        _db.Database.EnsureCreated();

        // Deterministic sequence output (CLAUDE.md §6.5)
        _sequence.NextAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult($"INV-{++_seqCounter:00000}"));

        _sut = new BillingService(_db, _errorLogger, _sequence, _notifications,
            Substitute.For<ILogger<BillingService>>());
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqliteConnection.Dispose();
    }

    // ── CreateDraftInvoiceAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CreateDraftInvoiceAsync_ValidDto_ReturnsSuccessWithId()
    {
        var dto = MakeCreateDto();

        var result = await _sut.CreateDraftInvoiceAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateDraftInvoiceAsync_CallsSequenceService()
    {
        await _sut.CreateDraftInvoiceAsync(MakeCreateDto());

        await _sequence.Received(1).NextAsync("INVOICE_RETAIL", 1L, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDraftInvoiceAsync_NewInvoiceHasDraftStatus()
    {
        var dto = new CreateInvoiceDto(DateTime.UtcNow.Date, 42L, 1L, 1L, "test note");

        var result = await _sut.CreateDraftInvoiceAsync(dto);
        result.IsSuccess.Should().BeTrue();

        var saved = await _db.Set<Invoice>().FindAsync(result.Value);
        saved.Should().NotBeNull();
        saved!.Status.Should().Be(InvoiceStatus.Draft);
        saved.CustomerId.Should().Be(42L);
        saved.Notes.Should().Be("test note");
    }

    [Fact]
    public async Task CreateDraftInvoiceAsync_WhenSequenceFails_ReturnsFailure()
    {
        _sequence.NextAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns<Task<string>>(_ => throw new InvalidOperationException("sequence broke"));

        var result = await _sut.CreateDraftInvoiceAsync(MakeCreateDto());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
    }

    // ── AddLineAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddLineAsync_ValidLine_ReturnsSuccess()
    {
        var invoiceId = await CreateDraftAsync();
        var dto = new AddInvoiceLineDto(ProductId: 10L, ProductUnitId: 1L,
            QuantityInBilledUnit: 2m, UnitPrice: 100m, DiscountPercent: 0m);

        var result = await _sut.AddLineAsync(invoiceId, dto);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AddLineAsync_RecalculatesTotals()
    {
        var invoiceId = await CreateDraftAsync();
        var dto = new AddInvoiceLineDto(10L, 1L, 2m, 100m, 0m);

        await _sut.AddLineAsync(invoiceId, dto);

        var invoice = await _db.Set<Invoice>().Include(i => i.Lines)
            .FirstAsync(i => i.Id == invoiceId);
        // SubTotal = 2 * 100 = 200
        invoice.SubTotal.Should().Be(200m);
        // GrandTotal = 200 + 18% tax = 236
        invoice.GrandTotal.Should().BeApproximately(236m, 0.01m);
    }

    [Fact]
    public async Task AddLineAsync_UnitSnapshotFields_ArePopulated()
    {
        var invoiceId = await CreateDraftAsync();
        var dto = new AddInvoiceLineDto(10L, 5L, 3m, 50m, 0m);

        await _sut.AddLineAsync(invoiceId, dto);

        var invoice = await _db.Set<Invoice>().Include(i => i.Lines)
            .FirstAsync(i => i.Id == invoiceId);
        var line = invoice.Lines.Single();
        line.ProductUnitId.Should().Be(5L);
        line.ConversionFactorSnapshot.Should().Be(1m);
        line.QuantityInBilledUnit.Should().Be(3m);
        // QuantityInBaseUnit = QuantityInBilledUnit * ConversionFactorSnapshot
        line.QuantityInBaseUnit.Should().Be(3m * 1m);
    }

    [Fact]
    public async Task AddLineAsync_WhenInvoiceNotFound_ReturnsNotFound()
    {
        var result = await _sut.AddLineAsync(9999L,
            new AddInvoiceLineDto(10L, 1L, 1m, 50m, 0m));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddLineAsync_WhenInvoiceIsFinalized_ReturnsConflict()
    {
        var invoiceId = await CreateDraftAsync();
        await _sut.FinalizeInvoiceAsync(invoiceId);

        var result = await _sut.AddLineAsync(invoiceId,
            new AddInvoiceLineDto(10L, 1L, 1m, 50m, 0m));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    // ── FinalizeInvoiceAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task FinalizeInvoiceAsync_DraftInvoice_ReturnsSuccessAndChangesStatus()
    {
        var invoiceId = await CreateDraftAsync();

        var result = await _sut.FinalizeInvoiceAsync(invoiceId);

        result.IsSuccess.Should().BeTrue();
        var invoice = await _db.Set<Invoice>().FindAsync(invoiceId);
        invoice!.Status.Should().Be(InvoiceStatus.Finalized);
    }

    [Fact]
    public async Task FinalizeInvoiceAsync_AlreadyFinalized_ReturnsConflict()
    {
        var invoiceId = await CreateDraftAsync();
        await _sut.FinalizeInvoiceAsync(invoiceId);

        var result = await _sut.FinalizeInvoiceAsync(invoiceId);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task FinalizeInvoiceAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.FinalizeInvoiceAsync(9999L);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // ── CancelInvoiceAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CancelInvoiceAsync_DraftInvoice_ReturnsSuccessAndChangesStatus()
    {
        var invoiceId = await CreateDraftAsync();

        var result = await _sut.CancelInvoiceAsync(invoiceId, "Customer request");

        result.IsSuccess.Should().BeTrue();
        var invoice = await _db.Set<Invoice>().FindAsync(invoiceId);
        invoice!.Status.Should().Be(InvoiceStatus.Cancelled);
        invoice.Notes.Should().Contain("Customer request");
    }

    [Fact]
    public async Task CancelInvoiceAsync_AlreadyCancelled_ReturnsConflict()
    {
        var invoiceId = await CreateDraftAsync();
        await _sut.CancelInvoiceAsync(invoiceId, "first cancel");

        var result = await _sut.CancelInvoiceAsync(invoiceId, "second cancel");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CancelInvoiceAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.CancelInvoiceAsync(9999L, "reason");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelInvoiceAsync_FinalizedInvoice_ReturnsSuccessAndChangesStatus()
    {
        var invoiceId = await CreateDraftAsync();
        await _sut.FinalizeInvoiceAsync(invoiceId);

        // Finalized invoices can still be cancelled.
        var result = await _sut.CancelInvoiceAsync(invoiceId, "wrong finalization");

        result.IsSuccess.Should().BeTrue();
        var invoice = await _db.Set<Invoice>().FindAsync(invoiceId);
        invoice!.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    // ── ListInvoicesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListInvoicesAsync_ReturnsOnlyNonDeletedInvoices()
    {
        await CreateDraftAsync();
        await CreateDraftAsync();

        var results = await _sut.ListInvoicesAsync(1, 50, null);

        results.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListInvoicesAsync_SearchFiltersCorrectly()
    {
        var invoiceId = await CreateDraftAsync();
        var invoice = await _db.Set<Invoice>().FindAsync(invoiceId);
        var knownNumber = invoice!.InvoiceNumber;

        var results = await _sut.ListInvoicesAsync(1, 50, knownNumber);

        results.Items.Should().HaveCount(1);
        results.Items[0].InvoiceNumber.Should().Be(knownNumber);
    }

    [Fact]
    public async Task ListInvoicesAsync_PagingIsRespected()
    {
        for (var i = 0; i < 5; i++) await CreateDraftAsync();

        var page1 = await _sut.ListInvoicesAsync(1, 3, null);
        var page2 = await _sut.ListInvoicesAsync(2, 3, null);

        page1.Items.Should().HaveCount(3);
        page2.Items.Should().HaveCount(2);
    }

    // ── GetInvoiceAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetInvoiceAsync_ExistingId_ReturnsDetailDto()
    {
        var invoiceId = await CreateDraftAsync();

        var result = await _sut.GetInvoiceAsync(invoiceId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(invoiceId);
    }

    [Fact]
    public async Task GetInvoiceAsync_NonExistingId_ReturnsNull()
    {
        var result = await _sut.GetInvoiceAsync(9999L);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetInvoiceAsync_IncludesLines()
    {
        var invoiceId = await CreateDraftAsync();
        await _sut.AddLineAsync(invoiceId, new AddInvoiceLineDto(1L, 1L, 2m, 50m, 0m));

        var result = await _sut.GetInvoiceAsync(invoiceId);

        result!.Lines.Should().HaveCount(1);
    }

    // ── SetPaymentTermsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task SetPaymentTermsAsync_FinalizedInvoice_SetsTermsAndDueDate()
    {
        var invoiceId = await CreateDraftAsync();
        await _sut.FinalizeInvoiceAsync(invoiceId);
        var dto = new SetPaymentTermsDto("Net30", 30);

        var result = await _sut.SetPaymentTermsAsync(invoiceId, dto);

        result.IsSuccess.Should().BeTrue();
        var invoice = await _db.Set<Invoice>().FindAsync(invoiceId);
        invoice!.PaymentTerms.Should().Be("Net30");
        invoice.DueDate.Should().NotBeNull();
    }

    [Fact]
    public async Task SetPaymentTermsAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.SetPaymentTermsAsync(9999L, new SetPaymentTermsDto("Net30", 30));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetPaymentTermsAsync_CancelledInvoice_ReturnsConflict()
    {
        var invoiceId = await CreateDraftAsync();
        await _sut.CancelInvoiceAsync(invoiceId, "test");

        var result = await _sut.SetPaymentTermsAsync(invoiceId, new SetPaymentTermsDto("Net30", 30));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    // ── PayInvoiceAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task PayInvoiceAsync_CashPayment_FullAmount_StatusBecomePaid()
    {
        var invoiceId = await CreateDraftAsync();
        await _sut.AddLineAsync(invoiceId, new AddInvoiceLineDto(1L, 1L, 1m, 100m, 0m));
        await _sut.FinalizeInvoiceAsync(invoiceId);

        var invoice = await _db.Set<Invoice>().FindAsync(invoiceId);
        var grandTotal = invoice!.GrandTotal;

        var dto = new PayInvoiceDto(
            [new PaymentAllocationDto(PaymentMode.Cash, grandTotal)]);

        var result = await _sut.PayInvoiceAsync(invoiceId, dto);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Set<Invoice>().FindAsync(invoiceId);
        updated!.Status.Should().Be(InvoiceStatus.Paid);
        updated.OutstandingAmount.Should().BeLessThanOrEqualTo(0m);
    }

    [Fact]
    public async Task PayInvoiceAsync_PartialPayment_StatusRemainsFinalized()
    {
        var invoiceId = await CreateDraftAsync();
        await _sut.AddLineAsync(invoiceId, new AddInvoiceLineDto(1L, 1L, 1m, 200m, 0m));
        await _sut.FinalizeInvoiceAsync(invoiceId);

        var dto = new PayInvoiceDto([new PaymentAllocationDto(PaymentMode.Cash, 50m)]);

        var result = await _sut.PayInvoiceAsync(invoiceId, dto);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Set<Invoice>().FindAsync(invoiceId);
        updated!.Status.Should().Be(InvoiceStatus.Finalized);
        updated.PaidAmount.Should().Be(50m);
    }

    [Fact]
    public async Task PayInvoiceAsync_NotFound_ReturnsNotFound()
    {
        var dto = new PayInvoiceDto([new PaymentAllocationDto(PaymentMode.Cash, 100m)]);
        var result = await _sut.PayInvoiceAsync(9999L, dto);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PayInvoiceAsync_CancelledInvoice_ReturnsConflict()
    {
        var invoiceId = await CreateDraftAsync();
        await _sut.CancelInvoiceAsync(invoiceId, "test");

        var dto = new PayInvoiceDto([new PaymentAllocationDto(PaymentMode.Cash, 100m)]);
        var result = await _sut.PayInvoiceAsync(invoiceId, dto);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PayInvoiceAsync_DraftInvoice_ReturnsConflict()
    {
        var invoiceId = await CreateDraftAsync();

        var dto = new PayInvoiceDto([new PaymentAllocationDto(PaymentMode.Cash, 100m)]);
        var result = await _sut.PayInvoiceAsync(invoiceId, dto);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    // ── FinalizeInvoiceAsync — SMS notification ───────────────────────────────

    [Fact]
    public async Task FinalizeInvoiceAsync_WithCustomerPhone_EnqueuesNotification()
    {
        var dto = new CreateInvoiceDto(
            DateTime.UtcNow.Date,
            CustomerId: 1L,
            WarehouseId: 1L,
            ShopId: 1L,
            Notes: null,
            CustomerName: "Test Customer",
            CustomerPhone: "+91-9876543210");

        var idResult = await _sut.CreateDraftInvoiceAsync(dto);
        await _sut.FinalizeInvoiceAsync(idResult.Value!);

        await _notifications.Received(1).EnqueueAsync(
            Arg.Any<long>(),
            Arg.Any<ErpSaas.Infrastructure.Data.Entities.Messaging.Enums.NotificationChannel>(),
            "+91-9876543210",
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CreateInvoiceDto MakeCreateDto(long customerId = 1L, long shopId = 1L)
        => new(DateTime.UtcNow.Date, customerId, 1L, shopId, null);

    private async Task<long> CreateDraftAsync()
    {
        var result = await _sut.CreateDraftInvoiceAsync(MakeCreateDto());
        result.IsSuccess.Should().BeTrue("pre-condition: draft creation must succeed");
        return result.Value!;
    }

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 99L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
