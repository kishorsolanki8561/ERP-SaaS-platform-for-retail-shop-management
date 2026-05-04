using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Modules.Payment.Connectors;
using ErpSaas.Modules.Payment.Connectors.Simulated;
using ErpSaas.Modules.Payment.Entities;
using ErpSaas.Modules.Payment.Enums;
using ErpSaas.Modules.Payment.Infrastructure;
using ErpSaas.Shared.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Tests.Unit.Modules.Payment;

internal sealed class SimulatorTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        PaymentModelConfiguration.Configure(modelBuilder);
    }
}

[Trait("Category", "Unit")]
public sealed class SimulatedGatewayConnectorTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly SqliteConnection _sqlite;
    private readonly SimulatedGatewayConnector _sut;
    private const long ShopId = 1L;

    public SimulatedGatewayConnectorTests()
    {
        var ctx = new SimStubTenantContext(ShopId);
        _sqlite = new SqliteConnection("DataSource=:memory:");
        _sqlite.Open();
        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqlite)
            .Options;
        _db = new SimulatorTenantDbContext(opts, ctx, new AuditSaveChangesInterceptor(ctx), new TenantSaveChangesInterceptor(ctx));
        _db.Database.EnsureCreated();
        _sut = new SimulatedGatewayConnector(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqlite.Dispose();
    }

    [Fact]
    public async Task InitiateAsync_AlwaysReturnsSuccess()
    {
        var req = new GatewayInitiateRequest("TXN-001", 500m, "INR", null, null, null, null);

        var result = await _sut.InitiateAsync(req, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.GatewayTxnId));
        Assert.StartsWith("sim_pay_", result.GatewayTxnId);
        Assert.Contains("simulated-gateway.local", result.PaymentUrl);
    }

    [Fact]
    public async Task InitiateAsync_EachCallProducesUniqueId()
    {
        var req = new GatewayInitiateRequest("TXN-A", 100m, "INR", null, null, null, null);

        var r1 = await _sut.InitiateAsync(req, CancellationToken.None);
        var r2 = await _sut.InitiateAsync(req, CancellationToken.None);

        Assert.NotEqual(r1.GatewayTxnId, r2.GatewayTxnId);
    }

    [Fact]
    public async Task RefundAsync_AlwaysReturnsSuccess()
    {
        var req = new GatewayRefundRequest("sim_pay_abc", "TXN-001", 250m, "Customer request");

        var result = await _sut.RefundAsync(req, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.GatewayRefundId));
        Assert.StartsWith("sim_rfnd_", result.GatewayRefundId);
        Assert.Equal(250m, result.RefundedAmount);
    }

    [Fact]
    public async Task FetchSettlementReport_NoSuccessTransactions_ReturnsEmptyLines()
    {
        var report = await _sut.FetchSettlementReportAsync(DateTime.UtcNow.Date, CancellationToken.None);

        Assert.Equal("Simulated", report.GatewayCode);
        Assert.Empty(report.Lines);
    }

    [Fact]
    public async Task FetchSettlementReport_ReturnsLinesForSuccessTransactions()
    {
        var txn = new PaymentGatewayTransaction
        {
            ShopId = ShopId,
            GatewayCode = "Simulated",
            OurReferenceNumber = "SIM-001",
            GatewayTxnId = "sim_pay_existing",
            Amount = 1000m,
            Currency = "INR",
            Status = PaymentGatewayStatus.Success,
            Purpose = PaymentPurpose.InvoicePayment,
            InitiatedAtUtc = DateTime.UtcNow,
        };
        _db.Set<PaymentGatewayTransaction>().Add(txn);
        await _db.SaveChangesAsync();

        var report = await _sut.FetchSettlementReportAsync(DateTime.UtcNow.Date, CancellationToken.None);

        Assert.Single(report.Lines);
        var line = report.Lines[0];
        Assert.Equal("sim_pay_existing", line.GatewayTxnId);
        Assert.Equal(1000m, line.SettledAmount);
        Assert.Equal(20m, line.Fee);        // 2% of 1000
        Assert.True(line.NetSettled < 1000m);
    }

    [Fact]
    public void VerifyWebhookSignature_AlwaysTrue()
    {
        Assert.True(_sut.VerifyWebhookSignature("{}", "any-sig", "any-secret"));
        Assert.True(_sut.VerifyWebhookSignature("", "", ""));
    }

    [Fact]
    public void ParseWebhookEvent_ReturnsNull()
    {
        var result = _sut.ParseWebhookEvent("{}");
        Assert.Null(result);
    }

    private sealed class SimStubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
