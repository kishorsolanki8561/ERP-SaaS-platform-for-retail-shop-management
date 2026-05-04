using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Modules.Payment.Connectors;
using ErpSaas.Modules.Payment.Connectors.Results;
using ErpSaas.Modules.Payment.Connectors.Simulated;
using ErpSaas.Modules.Payment.Entities;
using ErpSaas.Modules.Payment.Enums;
using ErpSaas.Modules.Payment.Infrastructure;
using ErpSaas.Modules.Payment.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Payment;

internal sealed class PaymentTenantDbContext(
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
public sealed class PaymentGatewayServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly SqliteConnection _sqlite;
    private readonly PaymentGatewayService _sut;
    private readonly PaymentReconciliationService _reconciliation;
    private readonly IGatewayConnectorRegistry _connectorRegistry;
    private const long ShopId = 1L;

    public PaymentGatewayServiceTests()
    {
        var ctx = new StubTenantContext(ShopId);
        _sqlite = new SqliteConnection("DataSource=:memory:");
        _sqlite.Open();
        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqlite)
            .Options;
        _db = new PaymentTenantDbContext(opts, ctx, new AuditSaveChangesInterceptor(ctx), new TenantSaveChangesInterceptor(ctx));
        _db.Database.EnsureCreated();

        // Use the simulated connector — all gateway calls succeed without real credentials
        var simulated = new SimulatedGatewayConnector(_db);
        _connectorRegistry = Substitute.For<IGatewayConnectorRegistry>();
        _connectorRegistry.ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IPaymentGatewayConnector>(simulated));

        _sut = new PaymentGatewayService(_db, _errorLogger, ctx, _connectorRegistry);
        _reconciliation = new PaymentReconciliationService(_db, _errorLogger, ctx, _connectorRegistry);
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqlite.Dispose();
    }

    // ── UpsertAccount ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpsertAccountAsync_NewAccount_Persists()
    {
        var dto = new UpsertGatewayAccountDto("Razorpay", "{\"KeyId\":\"rk_test\",\"KeySecret\":\"s\"}", "wh_secret", true, true);
        var result = await _sut.UpsertAccountAsync(dto);
        Assert.True(result.IsSuccess);
        Assert.Equal(1, await _db.Set<PaymentGatewayAccount>().CountAsync());
    }

    [Fact]
    public async Task UpsertAccountAsync_ExistingAccount_UpdatesInPlace()
    {
        var dto = new UpsertGatewayAccountDto("Razorpay", "{\"KeyId\":\"v1\",\"KeySecret\":\"s\"}", null, true, true);
        await _sut.UpsertAccountAsync(dto);

        var updated = new UpsertGatewayAccountDto("Razorpay", "{\"KeyId\":\"v2\",\"KeySecret\":\"s\"}", null, true, true);
        var result = await _sut.UpsertAccountAsync(updated);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, await _db.Set<PaymentGatewayAccount>().CountAsync());
        var account = await _db.Set<PaymentGatewayAccount>().FirstAsync();
        Assert.Contains("v2", account.CredentialsJsonEncrypted);
    }

    // ── InitiateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task InitiateAsync_GatewayNotFound_ReturnsNotFound()
    {
        var dto = new InitiatePaymentDto(PaymentPurpose.InvoicePayment, "Razorpay", 500m, "INR", 1, null, null);
        var result = await _sut.InitiateAsync(dto);
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Payment.GatewayAccountNotFound, result.Errors[0]);
    }

    [Fact]
    public async Task InitiateAsync_WhenSimulated_StoresGatewayTxnId()
    {
        await _sut.UpsertAccountAsync(new UpsertGatewayAccountDto("Razorpay", "{\"KeyId\":\"k\",\"KeySecret\":\"s\"}", null, true, true));
        var dto = new InitiatePaymentDto(PaymentPurpose.InvoicePayment, "Razorpay", 1000m, "INR", 42, null, null);

        var result = await _sut.InitiateAsync(dto);

        Assert.True(result.IsSuccess);
        var txn = await _db.Set<PaymentGatewayTransaction>().FirstAsync();
        Assert.Equal(PaymentGatewayStatus.Pending, txn.Status);
        Assert.False(string.IsNullOrEmpty(txn.GatewayTxnId));   // populated by simulated connector
        Assert.Equal(1000m, txn.Amount);
    }

    [Fact]
    public async Task InitiateAsync_WhenConnectorFails_MarksTransactionFailed()
    {
        await _sut.UpsertAccountAsync(new UpsertGatewayAccountDto("Razorpay", "{\"KeyId\":\"k\",\"KeySecret\":\"s\"}", null, true, true));

        // Override registry to return a failing connector
        var failingConnector = Substitute.For<IPaymentGatewayConnector>();
        failingConnector.InitiateAsync(Arg.Any<GatewayInitiateRequest>(), Arg.Any<CancellationToken>())
            .Returns(GatewayInitiateResult.Failure("ERR_001", "Gateway down"));
        _connectorRegistry.ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(failingConnector));

        var result = await _sut.InitiateAsync(new InitiatePaymentDto(PaymentPurpose.InvoicePayment, "Razorpay", 500m, "INR", 1, null, null));

        Assert.True(result.IsSuccess); // row is created
        var txn = await _db.Set<PaymentGatewayTransaction>().FirstAsync();
        Assert.Equal(PaymentGatewayStatus.Failed, txn.Status);
        Assert.Equal("ERR_001", txn.FailureCode);
    }

    // ── ConfirmAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmAsync_TransactionNotFound_ReturnsNotFound()
    {
        var dto = new ConfirmPaymentDto("gw-001", "UPI", null, null, 2m, 0.36m, 997.64m);
        var result = await _sut.ConfirmAsync(9999, dto);
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Payment.TransactionNotFound, result.Errors[0]);
    }

    [Fact]
    public async Task ConfirmAsync_HappyPath_SetsStatusSuccess()
    {
        await _sut.UpsertAccountAsync(new UpsertGatewayAccountDto("Razorpay", "{\"KeyId\":\"k\",\"KeySecret\":\"s\"}", null, true, true));
        var txnId = (await _sut.InitiateAsync(new InitiatePaymentDto(PaymentPurpose.InvoicePayment, "Razorpay", 500m, "INR", 1, null, null))).Value;

        var result = await _sut.ConfirmAsync(txnId, new ConfirmPaymentDto("rzp-001", "UPI", "user@upi", null, 1m, 0.18m, 498.82m));

        Assert.True(result.IsSuccess);
        var txn = await _db.Set<PaymentGatewayTransaction>().FirstAsync();
        Assert.Equal(PaymentGatewayStatus.Success, txn.Status);
        Assert.Equal("rzp-001", txn.GatewayTxnId);
    }

    [Fact]
    public async Task ConfirmAsync_AlreadyFinal_ReturnsConflict()
    {
        await _sut.UpsertAccountAsync(new UpsertGatewayAccountDto("Razorpay", "{\"KeyId\":\"k\",\"KeySecret\":\"s\"}", null, true, true));
        var txnId = (await _sut.InitiateAsync(new InitiatePaymentDto(PaymentPurpose.InvoicePayment, "Razorpay", 500m, "INR", 1, null, null))).Value;
        await _sut.CancelAsync(txnId);

        var result = await _sut.ConfirmAsync(txnId, new ConfirmPaymentDto("rzp-x", "UPI", null, null, 0, 0, 0));
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Payment.TransactionAlreadyFinal, result.Errors[0]);
    }

    // ── RefundAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RefundAsync_NotSuccess_ReturnsConflict()
    {
        await _sut.UpsertAccountAsync(new UpsertGatewayAccountDto("Razorpay", "{\"KeyId\":\"k\",\"KeySecret\":\"s\"}", null, true, true));
        var txnId = (await _sut.InitiateAsync(new InitiatePaymentDto(PaymentPurpose.InvoicePayment, "Razorpay", 500m, "INR", 1, null, null))).Value;

        var result = await _sut.RefundAsync(txnId, new RefundPaymentDto(500m, "Customer cancelled"));
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Payment.RefundRequiresSuccess, result.Errors[0]);
    }

    [Fact]
    public async Task RefundAsync_ExceedsAmount_ReturnsConflict()
    {
        await _sut.UpsertAccountAsync(new UpsertGatewayAccountDto("Razorpay", "{\"KeyId\":\"k\",\"KeySecret\":\"s\"}", null, true, true));
        var txnId = (await _sut.InitiateAsync(new InitiatePaymentDto(PaymentPurpose.InvoicePayment, "Razorpay", 500m, "INR", 1, null, null))).Value;
        await _sut.ConfirmAsync(txnId, new ConfirmPaymentDto("rzp-001", "UPI", null, null, 1m, 0.18m, 498.82m));

        var result = await _sut.RefundAsync(txnId, new RefundPaymentDto(600m, "Refund"));
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Payment.RefundExceedsAmount, result.Errors[0]);
    }

    [Fact]
    public async Task RefundAsync_WhenSuccess_StoresRefundGatewayTxnId()
    {
        await _sut.UpsertAccountAsync(new UpsertGatewayAccountDto("Razorpay", "{\"KeyId\":\"k\",\"KeySecret\":\"s\"}", null, true, true));
        var txnId = (await _sut.InitiateAsync(new InitiatePaymentDto(PaymentPurpose.InvoicePayment, "Razorpay", 500m, "INR", 1, null, null))).Value;
        await _sut.ConfirmAsync(txnId, new ConfirmPaymentDto("rzp-001", "UPI", null, null, 1m, 0.18m, 498.82m));

        var result = await _sut.RefundAsync(txnId, new RefundPaymentDto(500m, "Refund"));

        Assert.True(result.IsSuccess);
        var txn = await _db.Set<PaymentGatewayTransaction>().FirstAsync();
        Assert.Equal(PaymentGatewayStatus.Refunded, txn.Status);
        Assert.False(string.IsNullOrEmpty(txn.RefundGatewayTxnId)); // simulated connector sets this
    }

    [Fact]
    public async Task RefundAsync_WhenConnectorFails_DoesNotFlipStatus()
    {
        await _sut.UpsertAccountAsync(new UpsertGatewayAccountDto("Razorpay", "{\"KeyId\":\"k\",\"KeySecret\":\"s\"}", null, true, true));
        var txnId = (await _sut.InitiateAsync(new InitiatePaymentDto(PaymentPurpose.InvoicePayment, "Razorpay", 500m, "INR", 1, null, null))).Value;
        await _sut.ConfirmAsync(txnId, new ConfirmPaymentDto("rzp-001", "UPI", null, null, 1m, 0.18m, 498.82m));

        // Override to failing connector
        var failingConnector = Substitute.For<IPaymentGatewayConnector>();
        failingConnector.RefundAsync(Arg.Any<GatewayRefundRequest>(), Arg.Any<CancellationToken>())
            .Returns(GatewayRefundResult.Failure("REFUND_ERR", "Gateway refused"));
        _connectorRegistry.ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(failingConnector));

        var result = await _sut.RefundAsync(txnId, new RefundPaymentDto(500m, "Refund"));

        Assert.False(result.IsSuccess);
        var txn = await _db.Set<PaymentGatewayTransaction>().FirstAsync();
        Assert.Equal(PaymentGatewayStatus.Success, txn.Status); // unchanged — gateway call failed
    }

    [Fact]
    public async Task RefundAsync_PartialRefund_SetsPartiallyRefundedStatus()
    {
        await _sut.UpsertAccountAsync(new UpsertGatewayAccountDto("Razorpay", "{\"KeyId\":\"k\",\"KeySecret\":\"s\"}", null, true, true));
        var txnId = (await _sut.InitiateAsync(new InitiatePaymentDto(PaymentPurpose.InvoicePayment, "Razorpay", 500m, "INR", 1, null, null))).Value;
        await _sut.ConfirmAsync(txnId, new ConfirmPaymentDto("rzp-001", "UPI", null, null, 1m, 0.18m, 498.82m));

        var result = await _sut.RefundAsync(txnId, new RefundPaymentDto(200m, "Partial"));

        Assert.True(result.IsSuccess);
        var txn = await _db.Set<PaymentGatewayTransaction>().FirstAsync();
        Assert.Equal(PaymentGatewayStatus.PartiallyRefunded, txn.Status);
    }

    // ── Webhook ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleWebhookAsync_CapturedEvent_MarksSuccess()
    {
        await _sut.UpsertAccountAsync(new UpsertGatewayAccountDto("Razorpay", "{\"KeyId\":\"k\",\"KeySecret\":\"s\"}", "sec", true, true));
        var txnId = (await _sut.InitiateAsync(new InitiatePaymentDto(PaymentPurpose.InvoicePayment, "Razorpay", 300m, "INR", 1, null, null))).Value;
        var txn = await _db.Set<PaymentGatewayTransaction>().FirstAsync(t => t.Id == txnId);

        // Wire connector to return a captured event
        var connector = Substitute.For<IPaymentGatewayConnector>();
        connector.VerifyWebhookSignature(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        connector.ParseWebhookEvent(Arg.Any<string>())
            .Returns(new WebhookEvent("payment.captured", txn.GatewayTxnId, 300m, null, null, null));
        _connectorRegistry.ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(connector));

        await _sut.HandleWebhookAsync("Razorpay", "{}", "sig", CancellationToken.None);

        await _db.Entry(txn).ReloadAsync();
        Assert.Equal(PaymentGatewayStatus.Success, txn.Status);
    }

    [Fact]
    public async Task HandleWebhookAsync_FailedEvent_MarksFailure()
    {
        await _sut.UpsertAccountAsync(new UpsertGatewayAccountDto("Razorpay", "{\"KeyId\":\"k\",\"KeySecret\":\"s\"}", "sec", true, true));
        var txnId = (await _sut.InitiateAsync(new InitiatePaymentDto(PaymentPurpose.InvoicePayment, "Razorpay", 300m, "INR", 1, null, null))).Value;
        var txn = await _db.Set<PaymentGatewayTransaction>().FirstAsync(t => t.Id == txnId);

        var connector = Substitute.For<IPaymentGatewayConnector>();
        connector.VerifyWebhookSignature(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        connector.ParseWebhookEvent(Arg.Any<string>())
            .Returns(new WebhookEvent("payment.failed", txn.GatewayTxnId, null, "PAYMENT_DECLINED", "Insufficient funds", null));
        _connectorRegistry.ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(connector));

        await _sut.HandleWebhookAsync("Razorpay", "{}", "sig", CancellationToken.None);

        await _db.Entry(txn).ReloadAsync();
        Assert.Equal(PaymentGatewayStatus.Failed, txn.Status);
        Assert.Equal("PAYMENT_DECLINED", txn.FailureCode);
    }

    // ── Reconciliation ────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveExceptionAsync_NotFound_ReturnsNotFound()
    {
        var result = await _reconciliation.ResolveExceptionAsync(9999, new ResolveExceptionDto("Fixed"));
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Payment.ExceptionNotFound, result.Errors[0]);
    }

    [Fact]
    public async Task RunReconciliationAsync_MissingInGateway_CreatesException()
    {
        await _sut.UpsertAccountAsync(new UpsertGatewayAccountDto("Razorpay", "{\"KeyId\":\"k\",\"KeySecret\":\"s\"}", null, true, true));
        var txnId = (await _sut.InitiateAsync(new InitiatePaymentDto(PaymentPurpose.InvoicePayment, "Razorpay", 300m, "INR", 1, null, null))).Value;
        await _sut.ConfirmAsync(txnId, new ConfirmPaymentDto("rzp-settle-001", "CARD", null, "1234", 3m, 0.54m, 296.46m));

        // Settlement report is empty — our Success transaction is not in gateway report
        var emptyReportConnector = Substitute.For<IPaymentGatewayConnector>();
        emptyReportConnector.FetchSettlementReportAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new SettlementReport("Razorpay", DateTime.UtcNow.Date, []));
        _connectorRegistry.ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(emptyReportConnector));

        var result = await _reconciliation.RunReconciliationAsync("Razorpay", DateTime.UtcNow.Date);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
        var ex = await _db.Set<ReconciliationException>().FirstAsync();
        Assert.Equal(ReconciliationExceptionType.MissingInGateway, ex.ExceptionType);
    }

    [Fact]
    public async Task RunReconciliationAsync_AmountMismatch_CreatesException()
    {
        await _sut.UpsertAccountAsync(new UpsertGatewayAccountDto("Razorpay", "{\"KeyId\":\"k\",\"KeySecret\":\"s\"}", null, true, true));
        var txnId = (await _sut.InitiateAsync(new InitiatePaymentDto(PaymentPurpose.InvoicePayment, "Razorpay", 500m, "INR", 1, null, null))).Value;
        await _sut.ConfirmAsync(txnId, new ConfirmPaymentDto("rzp-mismatch", "CARD", null, null, 5m, 0.9m, 494.1m));

        // Gateway reports different amount
        var mismatchConnector = Substitute.For<IPaymentGatewayConnector>();
        mismatchConnector.FetchSettlementReportAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new SettlementReport("Razorpay", DateTime.UtcNow.Date,
            [new SettlementLine("rzp-mismatch", "TXN-REF", 450m, 4m, 0.72m, 445.28m, DateTime.UtcNow)]));
        _connectorRegistry.ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mismatchConnector));

        var result = await _reconciliation.RunReconciliationAsync("Razorpay", DateTime.UtcNow.Date);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
        var ex = await _db.Set<ReconciliationException>().FirstAsync();
        Assert.Equal(ReconciliationExceptionType.AmountMismatch, ex.ExceptionType);
    }

    [Fact]
    public async Task RunReconciliationAsync_Matched_StampsSettlementFields()
    {
        await _sut.UpsertAccountAsync(new UpsertGatewayAccountDto("Razorpay", "{\"KeyId\":\"k\",\"KeySecret\":\"s\"}", null, true, true));
        var txnId = (await _sut.InitiateAsync(new InitiatePaymentDto(PaymentPurpose.InvoicePayment, "Razorpay", 1000m, "INR", 1, null, null))).Value;
        await _sut.ConfirmAsync(txnId, new ConfirmPaymentDto("rzp-matched", "UPI", null, null, 10m, 1.8m, 988.2m));

        var settledAt = DateTime.UtcNow.AddDays(-1);
        var matchedConnector = Substitute.For<IPaymentGatewayConnector>();
        matchedConnector.FetchSettlementReportAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new SettlementReport("Razorpay", DateTime.UtcNow.Date,
            [new SettlementLine("rzp-matched", "TXN-REF", 1000m, 10m, 1.8m, 988.2m, settledAt)]));
        _connectorRegistry.ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(matchedConnector));

        var result = await _reconciliation.RunReconciliationAsync("Razorpay", DateTime.UtcNow.Date);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value); // no exceptions
        var txn = await _db.Set<PaymentGatewayTransaction>().FirstAsync();
        Assert.NotNull(txn.SettledAtUtc);
        Assert.Equal(10m, txn.GatewayFee);
        Assert.Equal(988.2m, txn.NetSettled);
    }

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
