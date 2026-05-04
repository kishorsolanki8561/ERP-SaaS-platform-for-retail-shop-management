using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Modules.Crm.Configuration;
using ErpSaas.Modules.Crm.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Crm;

internal sealed class CrmTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor audit,
    TenantSaveChangesInterceptor tenant)
    : TenantDbContext(options, tenantContext, audit, tenant, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new CustomerGroupEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerAddressEntityTypeConfiguration());
    }
}

[Trait("Category", "Unit")]
[Trait("Module", "Crm")]
public class CrmServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly CrmService _sut;
    private readonly SqliteConnection _conn;

    public CrmServiceTests()
    {
        var stub = new StubTenantContext(shopId: 1L);

        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_conn)
            .Options;

        _db = new CrmTenantDbContext(opts, stub,
            new AuditSaveChangesInterceptor(stub),
            new TenantSaveChangesInterceptor(stub));
        _db.Database.EnsureCreated();

        _sut = new CrmService(_db, _errorLogger);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    // ── CreateCustomerAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCustomerAsync_ValidDto_ReturnsSuccessWithId()
    {
        var dto = new CreateCustomerDto("Alice Retail", "Retail", "alice@test.com", "9876543210", null, 5000m, null);

        var result = await _sut.CreateCustomerAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateCustomerAsync_DuplicatePhone_ReturnsConflict()
    {
        var dto = new CreateCustomerDto("Alice", "Retail", null, "9999999999", null, 0, null);
        await _sut.CreateCustomerAsync(dto);

        var result = await _sut.CreateCustomerAsync(
            new CreateCustomerDto("Bob", "Retail", null, "9999999999", null, 0, null));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateCustomerAsync_GeneratesSequentialCode()
    {
        await _sut.CreateCustomerAsync(new CreateCustomerDto("First", "Retail", null, null, null, 0, null));
        var result = await _sut.CreateCustomerAsync(new CreateCustomerDto("Second", "Wholesale", null, null, null, 0, null));

        result.IsSuccess.Should().BeTrue();
        var customer = await _sut.GetCustomerAsync(result.Value);
        customer!.CustomerCode.Should().Be("CUST00002");
    }

    // ── GetCustomerAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetCustomerAsync_ExistingId_ReturnsCustomer()
    {
        var created = await _sut.CreateCustomerAsync(
            new CreateCustomerDto("Test Customer", "Retail", "t@t.com", null, null, 1000m, null));

        var dto = await _sut.GetCustomerAsync(created.Value);

        dto.Should().NotBeNull();
        dto!.DisplayName.Should().Be("Test Customer");
        dto.CustomerType.Should().Be("Retail");
    }

    [Fact]
    public async Task GetCustomerAsync_MissingId_ReturnsNull()
    {
        var dto = await _sut.GetCustomerAsync(9999L);
        dto.Should().BeNull();
    }

    // ── UpdateCustomerAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCustomerAsync_ValidUpdate_ReturnsSuccess()
    {
        var id = (await _sut.CreateCustomerAsync(
            new CreateCustomerDto("Original", "Retail", null, null, null, 0, null))).Value;

        var result = await _sut.UpdateCustomerAsync(id,
            new UpdateCustomerDto("Updated Name", "new@test.com", null, null, 9999m, null));

        result.IsSuccess.Should().BeTrue();
        var updated = await _sut.GetCustomerAsync(id);
        updated!.DisplayName.Should().Be("Updated Name");
        updated.CreditLimit.Should().Be(9999m);
    }

    [Fact]
    public async Task UpdateCustomerAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.UpdateCustomerAsync(9999L,
            new UpdateCustomerDto("X", null, null, null, 0, null));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // ── DeactivateCustomerAsync ─────────────────────────────────────────────

    [Fact]
    public async Task DeactivateCustomerAsync_ExistingCustomer_SetsInactive()
    {
        var id = (await _sut.CreateCustomerAsync(
            new CreateCustomerDto("Active Customer", "Retail", null, null, null, 0, null))).Value;

        var result = await _sut.DeactivateCustomerAsync(id);

        result.IsSuccess.Should().BeTrue();
        var dto = await _sut.GetCustomerAsync(id);
        dto!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateCustomerAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.DeactivateCustomerAsync(9999L);
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // ── CreateGroupAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateGroupAsync_ValidData_ReturnsSuccessWithId()
    {
        var result = await _sut.CreateGroupAsync("WHOLESALE", "Wholesale Customers", 5m);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateGroupAsync_DuplicateCode_ReturnsConflict()
    {
        await _sut.CreateGroupAsync("VIP", "VIP Customers", 10m);
        var result = await _sut.CreateGroupAsync("VIP", "VIP Customers 2", 15m);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    // ── ListGroupsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListGroupsAsync_ReturnsOnlyActiveGroups()
    {
        await _sut.CreateGroupAsync("G1", "Group 1", 5m);
        await _sut.CreateGroupAsync("G2", "Group 2", 10m);

        var groups = await _sut.ListGroupsAsync();

        groups.Should().HaveCount(2);
        groups.All(g => g.IsActive).Should().BeTrue();
    }

    // ── ListCustomersAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ListCustomersAsync_SearchByName_FiltersCorrectly()
    {
        await _sut.CreateCustomerAsync(new CreateCustomerDto("Alpha Corp", "Wholesale", null, "1111111111", null, 0, null));
        await _sut.CreateCustomerAsync(new CreateCustomerDto("Beta Ltd",   "Retail",    null, "2222222222", null, 0, null));

        var result = await _sut.ListCustomersAsync(1, 10, "Alpha");

        result.TotalCount.Should().Be(1);
        result.Items[0].DisplayName.Should().Be("Alpha Corp");
    }

    [Fact]
    public async Task ListCustomersAsync_NullSearch_ReturnsAll()
    {
        await _sut.CreateCustomerAsync(new CreateCustomerDto("C1", "Retail", null, "3111111111", null, 0, null));
        await _sut.CreateCustomerAsync(new CreateCustomerDto("C2", "Retail", null, "3222222222", null, 0, null));

        var result = await _sut.ListCustomersAsync(1, 10, null);

        result.TotalCount.Should().BeGreaterThanOrEqualTo(2);
    }

    // ── Tenant isolation ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCustomerAsync_CustomerBelongsToCurrentShop()
    {
        var dto = new CreateCustomerDto("Shop1 Customer", "Retail", null, null, null, 0, null);
        var id = (await _sut.CreateCustomerAsync(dto)).Value;

        var list = await _sut.ListCustomersAsync(1, 100, null);
        list.Items.Should().Contain(c => c.Id == id);
    }

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
