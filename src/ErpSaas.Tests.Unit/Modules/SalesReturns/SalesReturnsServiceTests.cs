using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Modules.SalesReturns.Entities;
using ErpSaas.Modules.SalesReturns.Enums;
using ErpSaas.Modules.SalesReturns.Infrastructure;
using ErpSaas.Modules.SalesReturns.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.SalesReturns;

internal sealed class SalesReturnsTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        SalesReturnsModelConfiguration.Configure(modelBuilder);
    }
}

[Trait("Category", "Unit")]
public class SalesReturnsServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly ISequenceService _sequence  = Substitute.For<ISequenceService>();
    private readonly IAutoVoucherService _autoVoucher = Substitute.For<IAutoVoucherService>();
    private readonly SalesReturnsService _sut;
    private readonly SqliteConnection _sqliteConnection;
    private int _seqCounter;

    public SalesReturnsServiceTests()
    {
        var stubCtx = new StubTenantContext(shopId: 1L);

        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        var auditInterceptor  = new AuditSaveChangesInterceptor(stubCtx);
        var tenantInterceptor = new TenantSaveChangesInterceptor(stubCtx);
        _db = new SalesReturnsTenantDbContext(opts, stubCtx, auditInterceptor, tenantInterceptor);
        _db.Database.EnsureCreated();

        _sequence.NextAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult($"SR-{++_seqCounter:000000}"));

        _sut = new SalesReturnsService(_db, _errorLogger, _sequence, stubCtx,
            Substitute.For<ILogger<SalesReturnsService>>(), _autoVoucher);
    }

    public void Dispose() { _db.Dispose(); _sqliteConnection.Dispose(); }

    [Fact]
    public async Task CreateSalesReturnAsync_ValidDto_ReturnsSuccessWithId()
    {
        var dto = new CreateSalesReturnDto(1L, DateTime.Today, RefundMethod.CreditNote, "Defective",
            [new(1L, 1L, 1L, 2m)]);

        var result = await _sut.CreateSalesReturnAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ApproveSalesReturnAsync_DraftReturn_TransitionsToApproved()
    {
        var sr = SeedDraftReturn();

        var result = await _sut.ApproveSalesReturnAsync(sr.Id);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Set<SalesReturn>().FindAsync(sr.Id);
        updated!.Status.Should().Be(SalesReturnStatus.Approved);
    }

    [Fact]
    public async Task ApproveSalesReturnAsync_NotDraft_ReturnsConflict()
    {
        var sr = SeedDraftReturn();
        sr.Status = SalesReturnStatus.Approved;
        await _db.SaveChangesAsync();

        var result = await _sut.ApproveSalesReturnAsync(sr.Id);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Errors.SalesReturns.SalesReturnNotDraft);
    }

    [Fact]
    public async Task ApproveSalesReturnAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.ApproveSalesReturnAsync(99999L);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Errors.SalesReturns.SalesReturnNotFound);
    }

    [Fact]
    public async Task IssueCreditNoteAsync_ValidDto_ReturnsSuccessWithId()
    {
        var dto = new IssueCreditNoteDto(1L, 1000m, null, null);

        var result = await _sut.IssueCreditNoteAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ApplyCreditNoteAsync_ValidAmount_ReducesRemainingBalance()
    {
        var cn = SeedIssuedCreditNote(1000m);

        var dto = new ApplyCreditNoteDto(cn.Id, 1L, 600m);
        var result = await _sut.ApplyCreditNoteAsync(dto);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Set<CreditNote>().FindAsync(cn.Id);
        updated!.RemainingAmount.Should().Be(400m);
    }

    [Fact]
    public async Task ApplyCreditNoteAsync_OverAmount_ReturnsConflict()
    {
        var cn = SeedIssuedCreditNote(1000m);

        var dto = new ApplyCreditNoteDto(cn.Id, 1L, 1500m);
        var result = await _sut.ApplyCreditNoteAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Errors.SalesReturns.CreditNoteInsufficient);
    }

    [Fact]
    public async Task ApplyCreditNoteAsync_FullAmount_TransitionsToApplied()
    {
        var cn = SeedIssuedCreditNote(1000m);

        var dto = new ApplyCreditNoteDto(cn.Id, 1L, 1000m);
        await _sut.ApplyCreditNoteAsync(dto);

        var updated = await _db.Set<CreditNote>().FindAsync(cn.Id);
        updated!.Status.Should().Be(CreditNoteStatus.Applied);
    }

    [Fact]
    public async Task CancelCreditNoteAsync_Issued_TransitionsToCancelled()
    {
        var cn = SeedIssuedCreditNote(500m);

        var result = await _sut.CancelCreditNoteAsync(cn.Id);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Set<CreditNote>().FindAsync(cn.Id);
        updated!.Status.Should().Be(CreditNoteStatus.Cancelled);
    }

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }

    private SalesReturn SeedDraftReturn()
    {
        var sr = new SalesReturn
        {
            ShopId = 1L, ReturnNumber = "SR-000001",
            InvoiceId = 1L, InvoiceNumberSnapshot = "INV-000001",
            CustomerId = 1L, CustomerNameSnapshot = "Customer",
            ReturnDate = DateTime.Today, Status = SalesReturnStatus.Draft,
            RefundMethod = RefundMethod.CreditNote,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Set<SalesReturn>().Add(sr);
        _db.SaveChanges();
        return sr;
    }

    private CreditNote SeedIssuedCreditNote(decimal amount)
    {
        var cn = new CreditNote
        {
            ShopId = 1L, CreditNoteNumber = "CN-000001",
            CustomerId = 1L, CustomerNameSnapshot = "Customer",
            IssueDate = DateTime.Today, Status = CreditNoteStatus.Issued,
            Amount = amount, UsedAmount = 0, RemainingAmount = amount,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Set<CreditNote>().Add(cn);
        _db.SaveChanges();
        return cn;
    }
}
