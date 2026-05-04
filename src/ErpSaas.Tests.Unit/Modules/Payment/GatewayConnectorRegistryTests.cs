using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Modules.Payment.Connectors;
using ErpSaas.Modules.Payment.Connectors.Simulated;
using ErpSaas.Modules.Payment.Entities;
using ErpSaas.Modules.Payment.Infrastructure;
using ErpSaas.Shared.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Payment;

internal sealed class RegistryTenantDbContext(
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
public sealed class GatewayConnectorRegistryTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly SqliteConnection _sqlite;
    private readonly IPaymentGatewayConnector _razorpayConnector;
    private readonly GatewayConnectorRegistry _sut;
    private const long ShopId = 1L;

    public GatewayConnectorRegistryTests()
    {
        var ctx = new RegistryStubTenantContext(ShopId);
        _sqlite = new SqliteConnection("DataSource=:memory:");
        _sqlite.Open();
        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqlite)
            .Options;
        _db = new RegistryTenantDbContext(opts, ctx, new AuditSaveChangesInterceptor(ctx), new TenantSaveChangesInterceptor(ctx));
        _db.Database.EnsureCreated();

        _razorpayConnector = Substitute.For<IPaymentGatewayConnector>();

        var services = new ServiceCollection();
        services.AddSingleton(_db);
        services.AddKeyedSingleton<IPaymentGatewayConnector>("Simulated", new SimulatedGatewayConnector(_db));
        services.AddKeyedSingleton<IPaymentGatewayConnector>("Razorpay", _razorpayConnector);

        var provider = services.BuildServiceProvider();
        _sut = new GatewayConnectorRegistry(provider, _db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqlite.Dispose();
    }

    [Fact]
    public async Task Resolve_WhenNoAccountConfigured_ReturnsSimulated()
    {
        var connector = await _sut.ResolveAsync("Razorpay");

        Assert.IsType<SimulatedGatewayConnector>(connector);
    }

    [Fact]
    public async Task Resolve_WhenRazorpayAccountActive_ReturnsRazorpay()
    {
        _db.Set<PaymentGatewayAccount>().Add(new PaymentGatewayAccount
        {
            ShopId = ShopId,
            GatewayCode = "Razorpay",
            CredentialsJsonEncrypted = "{\"KeyId\":\"k\",\"KeySecret\":\"s\"}",
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        var connector = await _sut.ResolveAsync("Razorpay");

        Assert.Same(_razorpayConnector, connector);
    }

    [Fact]
    public async Task Resolve_WhenAccountInactive_ReturnsSimulated()
    {
        _db.Set<PaymentGatewayAccount>().Add(new PaymentGatewayAccount
        {
            ShopId = ShopId,
            GatewayCode = "Razorpay",
            CredentialsJsonEncrypted = "{\"KeyId\":\"k\",\"KeySecret\":\"s\"}",
            IsActive = false,
        });
        await _db.SaveChangesAsync();

        var connector = await _sut.ResolveAsync("Razorpay");

        Assert.IsType<SimulatedGatewayConnector>(connector);
    }

    [Fact]
    public async Task Resolve_UnknownGatewayCode_ReturnsSimulated()
    {
        var connector = await _sut.ResolveAsync("UnknownGateway999");

        Assert.IsType<SimulatedGatewayConnector>(connector);
    }

    private sealed class RegistryStubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
