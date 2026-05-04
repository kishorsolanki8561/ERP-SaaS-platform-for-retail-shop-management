using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Modules.ApiAccess.Infrastructure;
using ErpSaas.Modules.ApiAccess.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.ApiAccess;

internal sealed class ApiAccessTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ApiAccessModelConfiguration.Configure(modelBuilder);
    }
}

[Trait("Category", "Unit")]
public sealed class ApiAccessServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly StubTenantContext _ctx = new(1L);
    private readonly SqliteConnection _sqlite;
    private readonly ShopApiKeyService _sut;

    public ApiAccessServiceTests()
    {
        _sqlite = new SqliteConnection("DataSource=:memory:");
        _sqlite.Open();
        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqlite)
            .Options;
        _db = new ApiAccessTenantDbContext(opts, _ctx,
            new AuditSaveChangesInterceptor(_ctx),
            new TenantSaveChangesInterceptor(_ctx));
        _db.Database.EnsureCreated();
        _sut = new ShopApiKeyService(_db, _errorLogger, _ctx);
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqlite.Dispose();
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsRawKey()
    {
        var dto = new CreateApiKeyDto("My Integration", null, null);
        var result = await _sut.CreateAsync(dto, createdByUserId: 1);
        Assert.True(result.IsSuccess);
        Assert.StartsWith("sk_live_", result.Value.RawKey);
        Assert.Equal(12, result.Value.KeyPrefix.Length);
    }

    [Fact]
    public async Task ListAsync_AfterCreate_ReturnsKeys()
    {
        await _sut.CreateAsync(new CreateApiKeyDto("Key A", null, null), 1);
        await _sut.CreateAsync(new CreateApiKeyDto("Key B", null, null), 1);

        var result = await _sut.ListAsync();
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task RevokeAsync_ActiveKey_SetsInactive()
    {
        var created = await _sut.CreateAsync(new CreateApiKeyDto("Key", null, null), 1);
        var result = await _sut.RevokeAsync(created.Value.Id, new RevokeApiKeyDto("Test"), 1);
        Assert.True(result.IsSuccess);

        var list = await _sut.ListAsync();
        Assert.False(list.Value[0].IsActive);
    }

    [Fact]
    public async Task RevokeAsync_AlreadyRevoked_ReturnsConflict()
    {
        var created = await _sut.CreateAsync(new CreateApiKeyDto("Key", null, null), 1);
        await _sut.RevokeAsync(created.Value.Id, new RevokeApiKeyDto(null), 1);

        var result = await _sut.RevokeAsync(created.Value.Id, new RevokeApiKeyDto(null), 1);
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.ApiAccess.KeyAlreadyRevoked, result.Errors[0]);
    }

    [Fact]
    public async Task RevokeAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.RevokeAsync(9999, new RevokeApiKeyDto(null), 1);
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.ApiAccess.KeyNotFound, result.Errors[0]);
    }

    [Fact]
    public async Task RotateAsync_ActiveKey_RevokesOldIssuesNew()
    {
        var created = await _sut.CreateAsync(new CreateApiKeyDto("Key", null, null), 1);
        var rotated = await _sut.RotateAsync(created.Value.Id, 1);

        Assert.True(rotated.IsSuccess);
        Assert.NotEqual(created.Value.RawKey, rotated.Value.RawKey);

        var list = await _sut.ListAsync();
        var active = list.Value.Where(k => k.IsActive).ToList();
        Assert.Single(active);
    }

    [Fact]
    public async Task RotateAsync_RevokedKey_ReturnsConflict()
    {
        var created = await _sut.CreateAsync(new CreateApiKeyDto("Key", null, null), 1);
        await _sut.RevokeAsync(created.Value!.Id, new RevokeApiKeyDto(null), 1);

        var result = await _sut.RotateAsync(created.Value!.Id, 1);
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.ApiAccess.KeyAlreadyRevoked, result.Errors[0]);
    }

    [Fact]
    public async Task RotateAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.RotateAsync(9999, 1);
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.ApiAccess.KeyNotFound, result.Errors[0]);
    }

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
