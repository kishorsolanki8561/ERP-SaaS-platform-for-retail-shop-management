using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Modules.Pricing.Entities;
using ErpSaas.Modules.Pricing.Enums;
using ErpSaas.Modules.Pricing.Infrastructure;
using ErpSaas.Modules.Pricing.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Pricing;

internal sealed class PricingTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        PricingModelConfiguration.Configure(modelBuilder);
    }
}

[Trait("Category", "Unit")]
public sealed class PricingEngineTests
{
    private readonly IPricingEngine _engine = new PricingEngine();

    private static DiscountRuleDto ActiveRule(string name, DiscountScope scope, decimal? pct = null, decimal? fix = null, int priority = 1)
        => new(1, name, "SEASONAL", scope, pct, fix, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(30), priority, true);

    [Fact]
    public void Calculate_NoDiscountsNoCharges_GrandTotalEqualsSubTotal()
    {
        var cart = new CartInput(null, null, DateTime.Today,
            [new CartLineInput(1, null, 2, 100m)]);

        var result = _engine.Calculate(cart, [], []);

        Assert.Equal(200m, result.SubTotal);
        Assert.Equal(0m, result.TotalDiscount);
        Assert.Equal(200m, result.GrandTotal);
    }

    [Fact]
    public void Calculate_ProductLinePercentDiscount_ReducesLineTotal()
    {
        var cart = new CartInput(null, null, DateTime.Today,
            [new CartLineInput(1, null, 4, 100m)]);
        var rules = new[] { ActiveRule("10% Off", DiscountScope.ProductLine, pct: 10m) };

        var result = _engine.Calculate(cart, rules, []);

        Assert.Equal(40m, result.TotalDiscount);   // 10% of 400
        Assert.Equal(360m, result.GrandTotal);
    }

    [Fact]
    public void Calculate_FixedExtraCharge_AddedToGrandTotal()
    {
        var cart = new CartInput(null, null, DateTime.Today,
            [new CartLineInput(1, null, 1, 500m)]);
        var charges = new[] { new CreateExtraChargeDto("Delivery", ChargeType.FixedAmount, 50m, false, null) };

        var result = _engine.Calculate(cart, [], charges);

        Assert.Equal(50m, result.TotalExtraCharges);
        Assert.Equal(550m, result.GrandTotal);
    }

    [Fact]
    public void Calculate_PercentOfInvoiceCharge_ComputedOnTaxableAmount()
    {
        var cart = new CartInput(null, null, DateTime.Today,
            [new CartLineInput(1, null, 2, 1000m)]);
        var charges = new[] { new CreateExtraChargeDto("Service Fee", ChargeType.PercentOfInvoice, 2m, false, null) };

        var result = _engine.Calculate(cart, [], charges);

        // 2% of 2000 = 40
        Assert.Equal(40m, result.TotalExtraCharges);
    }

    [Fact]
    public void Calculate_PerItemCharge_MultipliesByTotalQuantity()
    {
        var cart = new CartInput(null, null, DateTime.Today,
            [new CartLineInput(1, null, 3, 100m), new CartLineInput(2, null, 2, 200m)]);
        var charges = new[] { new CreateExtraChargeDto("Packing", ChargeType.PerItem, 10m, false, null) };

        var result = _engine.Calculate(cart, [], charges);

        // 5 items * 10 = 50
        Assert.Equal(50m, result.TotalExtraCharges);
    }

    [Fact]
    public void Calculate_ExpiredRule_NotApplied()
    {
        var expired = new DiscountRuleDto(1, "Expired", "SEASONAL", DiscountScope.ProductLine,
            20m, null, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-1), 1, true);
        var cart = new CartInput(null, null, DateTime.Today,
            [new CartLineInput(1, null, 1, 100m)]);

        var result = _engine.Calculate(cart, [expired], []);

        Assert.Equal(0m, result.TotalDiscount);
    }

    [Fact]
    public void Calculate_InactiveRule_NotApplied()
    {
        var inactive = new DiscountRuleDto(1, "Inactive", "SEASONAL", DiscountScope.ProductLine,
            20m, null, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(30), 1, false);
        var cart = new CartInput(null, null, DateTime.Today,
            [new CartLineInput(1, null, 1, 100m)]);

        var result = _engine.Calculate(cart, [inactive], []);

        Assert.Equal(0m, result.TotalDiscount);
    }

    [Fact]
    public void Calculate_MultipleLines_EachLineHasCorrectTotals()
    {
        var cart = new CartInput(null, null, DateTime.Today,
        [
            new CartLineInput(1, null, 2, 100m),
            new CartLineInput(2, null, 3, 200m),
        ]);

        var result = _engine.Calculate(cart, [], []);

        Assert.Equal(2, result.Lines.Count);
        Assert.Equal(200m, result.Lines[0].LineTotal);
        Assert.Equal(600m, result.Lines[1].LineTotal);
        Assert.Equal(800m, result.SubTotal);
        Assert.Equal(800m, result.GrandTotal);
    }

    [Fact]
    public void Calculate_EmptyCart_ReturnsZeroGrandTotal()
    {
        var cart = new CartInput(null, null, DateTime.Today, []);

        var result = _engine.Calculate(cart, [], []);

        Assert.Equal(0m, result.GrandTotal);
        Assert.Empty(result.Lines);
    }
}

[Trait("Category", "Unit")]
public sealed class PricingManagementServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly IPricingEngine _engine = new PricingEngine();
    private readonly SqliteConnection _sqlite;
    private readonly PricingManagementService _sut;

    public PricingManagementServiceTests()
    {
        var ctx = new StubTenantContext(1L);
        _sqlite = new SqliteConnection("DataSource=:memory:");
        _sqlite.Open();
        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqlite)
            .Options;
        _db = new PricingTenantDbContext(opts, ctx, new AuditSaveChangesInterceptor(ctx), new TenantSaveChangesInterceptor(ctx));
        _db.Database.EnsureCreated();

        _sut = new PricingManagementService(_db, _errorLogger, ctx, _engine,
            Substitute.For<ILogger<PricingManagementService>>());
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqlite.Dispose();
    }

    [Fact]
    public async Task CreateDiscountRuleAsync_HappyPath_PersistsRule()
    {
        var dto = new CreateDiscountRuleDto("Summer10", "SEASONAL", DiscountScope.ProductLine,
            null, null, null, 10m, null, null, null, null, DateTime.Today, DateTime.Today.AddDays(30), 1, false);

        var result = await _sut.CreateDiscountRuleAsync(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, await _db.Set<DiscountRule>().CountAsync());
    }

    [Fact]
    public async Task ToggleDiscountRuleAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.ToggleDiscountRuleAsync(9999, false);

        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Pricing.DiscountRuleNotFound, result.Errors[0]);
    }

    [Fact]
    public async Task ToggleDiscountRuleAsync_Found_UpdatesIsActive()
    {
        var create = await _sut.CreateDiscountRuleAsync(new CreateDiscountRuleDto(
            "Rule", "SEASONAL", DiscountScope.ProductLine,
            null, null, null, 5m, null, null, null, null,
            DateTime.Today, DateTime.Today.AddDays(30), 1, false));

        await _sut.ToggleDiscountRuleAsync(create.Value, false);

        var rule = await _db.Set<DiscountRule>().FindAsync(create.Value);
        Assert.False(rule!.IsActive);
    }

    [Fact]
    public async Task CreateExtraChargeAsync_HappyPath_PersistsCharge()
    {
        var dto = new CreateExtraChargeDto("Delivery", ChargeType.FixedAmount, 50m, false, null);

        var result = await _sut.CreateExtraChargeAsync(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, await _db.Set<ExtraChargeRule>().CountAsync());
    }

    [Fact]
    public async Task ToggleExtraChargeAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.ToggleExtraChargeAsync(9999, false);

        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Pricing.ExtraChargeNotFound, result.Errors[0]);
    }

    [Fact]
    public async Task CreateOfferAsync_HappyPath_PersistsOffer()
    {
        var dto = new CreateOfferDto("SUMMER2026", "Summer Sale", OfferType.FlatOff, null,
            DateTime.Today, DateTime.Today.AddDays(30));

        var result = await _sut.CreateOfferAsync(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, await _db.Set<Offer>().CountAsync());
    }

    [Fact]
    public async Task CreateOfferAsync_DuplicateCode_ReturnsConflict()
    {
        var dto = new CreateOfferDto("DUPE", "Offer", OfferType.FlatOff, null,
            DateTime.Today, DateTime.Today.AddDays(30));
        await _sut.CreateOfferAsync(dto);

        var result = await _sut.CreateOfferAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Pricing.OfferCodeExists, result.Errors[0]);
    }

    [Fact]
    public async Task ToggleOfferAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.ToggleOfferAsync(9999, false);

        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Pricing.OfferNotFound, result.Errors[0]);
    }

    [Fact]
    public async Task CalculateAsync_WithActiveRuleAndCharge_ReturnsCorrectGrandTotal()
    {
        await _sut.CreateDiscountRuleAsync(new CreateDiscountRuleDto(
            "10% Off", "SEASONAL", DiscountScope.ProductLine,
            null, null, null, 10m, null, null, null, null,
            DateTime.Today.AddDays(-1), DateTime.Today.AddDays(30), 1, false));
        await _sut.CreateExtraChargeAsync(new CreateExtraChargeDto("Fee", ChargeType.FixedAmount, 20m, false, null));

        var cart = new CartInput(null, null, DateTime.Today,
            [new CartLineInput(1, null, 1, 100m)]);
        var result = await _sut.CalculateAsync(cart);

        // 100 - 10% = 90 + 20 fee = 110
        Assert.Equal(110m, result.GrandTotal);
    }

    [Fact]
    public async Task ListDiscountRulesAsync_ReturnsOnlyNonDeleted()
    {
        await _sut.CreateDiscountRuleAsync(new CreateDiscountRuleDto(
            "Rule1", "SEASONAL", DiscountScope.ProductLine,
            null, null, null, 5m, null, null, null, null,
            DateTime.Today, DateTime.Today.AddDays(30), 1, false));

        var list = await _sut.ListDiscountRulesAsync();

        Assert.Single(list);
    }

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
