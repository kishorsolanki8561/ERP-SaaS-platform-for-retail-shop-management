using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Modules.Quotations.Entities;
using ErpSaas.Modules.Quotations.Enums;
using ErpSaas.Modules.Quotations.Infrastructure;
using ErpSaas.Modules.Quotations.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Quotations;

internal sealed class QuotationsTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        QuotationsModelConfiguration.Configure(modelBuilder);
    }
}

[Trait("Category", "Unit")]
public sealed class QuotationsServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly ISequenceService _sequence = Substitute.For<ISequenceService>();
    private readonly SqliteConnection _sqlite;
    private int _seqCounter;
    private readonly QuotationsService _sut;

    public QuotationsServiceTests()
    {
        var ctx = new StubTenantContext(1L);
        _sqlite = new SqliteConnection("DataSource=:memory:");
        _sqlite.Open();
        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqlite)
            .Options;
        _db = new QuotationsTenantDbContext(opts, ctx, new AuditSaveChangesInterceptor(ctx), new TenantSaveChangesInterceptor(ctx));
        _db.Database.EnsureCreated();

        _sequence.NextAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var code = (string)call[0];
                var prefix = code switch
                {
                    "QUOTATION" => "QT",
                    "SALES_ORDER" => "SO",
                    "DELIVERY_CHALLAN" => "DC",
                    _ => "DOC",
                };
                return Task.FromResult($"{prefix}-{++_seqCounter:000000}");
            });

        _sut = new QuotationsService(_db, _errorLogger, _sequence, ctx, Substitute.For<ILogger<QuotationsService>>());
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqlite.Dispose();
    }

    private static QuotationLineInput SampleLine() => new(
        ProductId: 1, ProductNameSnapshot: "Widget A",
        ProductUnitId: 1, UnitCodeSnapshot: "PCS", ConversionFactor: 1m,
        QuantityInBilledUnit: 2, UnitPrice: 100m, DiscountAmount: 0m, GstRate: 18m);

    private static SalesOrderLineInput SampleSoLine() => new(
        ProductId: 1, ProductNameSnapshot: "Widget A",
        ProductUnitId: 1, UnitCodeSnapshot: "PCS", ConversionFactor: 1m,
        QuantityInBilledUnit: 2, UnitPrice: 100m, DiscountAmount: 0m, GstRate: 18m);

    [Fact]
    public async Task CreateQuotationAsync_HappyPath_PersistsQuotationWithLines()
    {
        var dto = new CreateQuotationDto(1, "ACME Corp", DateTime.Today.AddDays(30), null, [SampleLine()]);

        var result = await _sut.CreateQuotationAsync(dto);

        Assert.True(result.IsSuccess);
        var q = await _db.Set<Quotation>().Include(x => x.Lines).FirstAsync();
        Assert.Equal(QuotationStatus.Draft, q.Status);
        Assert.Single(q.Lines);
        Assert.StartsWith("QT-", q.QuotationNumber);
    }

    [Fact]
    public async Task CreateQuotationAsync_ComputesTotalsCorrectly()
    {
        var dto = new CreateQuotationDto(1, "Customer", DateTime.Today.AddDays(15), null,
            [new QuotationLineInput(1, "P1", 1, "PCS", 1m, 2m, 100m, 10m, 18m)]);

        var result = await _sut.CreateQuotationAsync(dto);

        var q = await _db.Set<Quotation>().FirstAsync();
        // Gross=200, Discount=10, Taxable=190, Tax=18%*190=34.20, LineTotal=224.20
        Assert.Equal(200m, q.SubTotal);
        Assert.Equal(10m, q.TotalDiscount);
    }

    [Fact]
    public async Task SendQuotationAsync_Draft_StatusBecomesSent()
    {
        var create = await _sut.CreateQuotationAsync(new CreateQuotationDto(1, "C", DateTime.Today.AddDays(10), null, [SampleLine()]));

        var result = await _sut.SendQuotationAsync(create.Value);

        Assert.True(result.IsSuccess);
        var q = await _db.Set<Quotation>().FindAsync(create.Value);
        Assert.Equal(QuotationStatus.Sent, q!.Status);
    }

    [Fact]
    public async Task SendQuotationAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.SendQuotationAsync(9999);
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Quotations.QuotationNotFound, result.Errors[0]);
    }

    [Fact]
    public async Task SendQuotationAsync_AlreadySent_ReturnsConflict()
    {
        var create = await _sut.CreateQuotationAsync(new CreateQuotationDto(1, "C", DateTime.Today.AddDays(10), null, [SampleLine()]));
        await _sut.SendQuotationAsync(create.Value);

        var result = await _sut.SendQuotationAsync(create.Value);
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Quotations.QuotationNotInDraft, result.Errors[0]);
    }

    [Fact]
    public async Task ConvertQuotationToSalesOrderAsync_CreatesSOWithSameLines()
    {
        var create = await _sut.CreateQuotationAsync(new CreateQuotationDto(1, "ACME", DateTime.Today.AddDays(10), null, [SampleLine()]));

        var soId = await _sut.ConvertQuotationToSalesOrderAsync(create.Value);

        Assert.True(soId.IsSuccess);
        var so = await _db.Set<SalesOrder>().Include(s => s.Lines).FirstAsync();
        Assert.Equal(SalesOrderStatus.Confirmed, so.Status);
        Assert.Single(so.Lines);
    }

    [Fact]
    public async Task ConvertQuotationToSalesOrderAsync_QuotationNotFound_ReturnsNotFound()
    {
        var result = await _sut.ConvertQuotationToSalesOrderAsync(9999);
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Quotations.QuotationNotFound, result.Errors[0]);
    }

    [Fact]
    public async Task CancelSalesOrderAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.CancelSalesOrderAsync(9999);
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Quotations.SalesOrderNotFound, result.Errors[0]);
    }

    [Fact]
    public async Task CancelSalesOrderAsync_AlreadyCancelled_ReturnsConflict()
    {
        var soId = await _sut.CreateSalesOrderAsync(new CreateSalesOrderDto(1, "C", null, null, null, null, [SampleSoLine()]));
        await _sut.CancelSalesOrderAsync(soId.Value);

        var result = await _sut.CancelSalesOrderAsync(soId.Value);
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Quotations.SalesOrderAlreadyCancelled, result.Errors[0]);
    }

    [Fact]
    public async Task CreateDeliveryChallanAsync_HappyPath_PersistsChallanWithLines()
    {
        var soId = await _sut.CreateSalesOrderAsync(new CreateSalesOrderDto(1, "C", null, null, null, null, [SampleSoLine()]));

        var dto = new CreateDeliveryChallanDto(soId.Value, DateTime.Today, "123 Street", null, null, null);
        var result = await _sut.CreateDeliveryChallanAsync(dto);

        Assert.True(result.IsSuccess);
        var dc = await _db.Set<DeliveryChallan>().Include(d => d.Lines).FirstAsync();
        Assert.Equal(DeliveryChallanStatus.Draft, dc.Status);
        Assert.Single(dc.Lines);
    }

    [Fact]
    public async Task DispatchDeliveryChallanAsync_Draft_StatusBecomesDispatched()
    {
        var soId = await _sut.CreateSalesOrderAsync(new CreateSalesOrderDto(1, "C", null, null, null, null, [SampleSoLine()]));
        var dcId = await _sut.CreateDeliveryChallanAsync(new CreateDeliveryChallanDto(soId.Value, DateTime.Today, null, null, null, null));

        var result = await _sut.DispatchDeliveryChallanAsync(dcId.Value);

        Assert.True(result.IsSuccess);
        var dc = await _db.Set<DeliveryChallan>().FindAsync(dcId.Value);
        Assert.Equal(DeliveryChallanStatus.Dispatched, dc!.Status);
        Assert.NotNull(dc.DispatchedDate);
    }

    [Fact]
    public async Task DispatchDeliveryChallanAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.DispatchDeliveryChallanAsync(9999);
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Quotations.DeliveryChallanNotFound, result.Errors[0]);
    }

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
