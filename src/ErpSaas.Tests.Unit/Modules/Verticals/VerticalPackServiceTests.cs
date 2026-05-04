using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Verticals;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Modules.Verticals.Entities;
using ErpSaas.Modules.Verticals.Infrastructure;
using ErpSaas.Modules.Verticals.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Verticals;

internal sealed class VerticalPlatformDbContext(
    DbContextOptions<PlatformDbContext> options,
    AuditSaveChangesInterceptor auditInterceptor)
    : PlatformDbContext(options, auditInterceptor)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var rowVersion = entityType.FindProperty("RowVersion");
            if (rowVersion is not null)
            {
                rowVersion.IsConcurrencyToken = false;
                rowVersion.SetDefaultValueSql("0");
            }
            foreach (var index in entityType.GetIndexes().ToList())
                index.SetFilter(null);
        }
    }
}

internal sealed class VerticalTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        VerticalModelConfiguration.Configure(modelBuilder);
    }
}

[Trait("Category", "Unit")]
public sealed class VerticalPackServiceTests : IDisposable
{
    private readonly PlatformDbContext _platformDb;
    private readonly TenantDbContext _tenantDb;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly SqliteConnection _platformSqlite;
    private readonly SqliteConnection _tenantSqlite;
    private readonly VerticalPackService _sut;
    private readonly StubTenantContext _ctx = new(shopId: 1L);

    public VerticalPackServiceTests()
    {
        _platformSqlite = new SqliteConnection("DataSource=:memory:");
        _platformSqlite.Open();
        _tenantSqlite = new SqliteConnection("DataSource=:memory:");
        _tenantSqlite.Open();

        var auditInterceptor = new AuditSaveChangesInterceptor(_ctx);

        _platformDb = new VerticalPlatformDbContext(
            new DbContextOptionsBuilder<PlatformDbContext>().UseSqlite(_platformSqlite).Options,
            auditInterceptor);
        _platformDb.Database.EnsureCreated();

        _tenantDb = new VerticalTenantDbContext(
            new DbContextOptionsBuilder<TenantDbContext>().UseSqlite(_tenantSqlite).Options,
            _ctx, auditInterceptor, new TenantSaveChangesInterceptor(_ctx));
        _tenantDb.Database.EnsureCreated();

        _sut = new VerticalPackService(_platformDb, _tenantDb, _errorLogger, _ctx);
    }

    public void Dispose()
    {
        _platformDb.Dispose();
        _tenantDb.Dispose();
        _platformSqlite.Dispose();
        _tenantSqlite.Dispose();
    }

    private async Task<VerticalPack> SeedPack(string code, bool isActive = true)
    {
        var pack = new VerticalPack
        {
            Code = code,
            Name = $"Pack {code}",
            FeatureFlagsCsv = "Flag.A",
            IsActive = isActive,
            SortOrder = 1,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _platformDb.VerticalPacks.Add(pack);
        await _platformDb.SaveChangesAsync();
        return pack;
    }

    [Fact]
    public async Task ListPacksAsync_ReturnsOnlyActiveNonDeleted()
    {
        await SeedPack("ELECTRICAL");
        var inactive = await SeedPack("MEDICAL", isActive: false);
        var deleted = await SeedPack("GROCERY");
        deleted.IsDeleted = true;
        await _platformDb.SaveChangesAsync();

        var list = await _sut.ListPacksAsync();
        Assert.Single(list);
        Assert.Equal("ELECTRICAL", list[0].Code);
    }

    [Fact]
    public async Task GetPackAsync_Found_ReturnsDto()
    {
        await SeedPack("ELECTRICAL");
        var result = await _sut.GetPackAsync("ELECTRICAL");
        Assert.NotNull(result);
        Assert.Equal("ELECTRICAL", result!.Code);
    }

    [Fact]
    public async Task GetPackAsync_NotFound_ReturnsNull()
    {
        var result = await _sut.GetPackAsync("DOESNOTEXIST");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetShopVerticalAsync_NoVerticalInstalled_ReturnsNull()
    {
        var result = await _sut.GetShopVerticalAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task InstallForShopAsync_PackNotFound_ReturnsNotFound()
    {
        var result = await _sut.InstallForShopAsync("NONEXISTENT");
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Verticals.PackNotFound, result.Errors[0]);
    }

    [Fact]
    public async Task InstallForShopAsync_ValidPack_CreatesShopVertical()
    {
        await SeedPack("ELECTRICAL");
        var result = await _sut.InstallForShopAsync("ELECTRICAL");
        Assert.True(result.IsSuccess);

        var sv = await _tenantDb.Set<ShopVertical>().FirstAsync();
        Assert.Equal("ELECTRICAL", sv.VerticalPackCode);
        Assert.Equal(1L, sv.ShopId);
    }

    [Fact]
    public async Task InstallForShopAsync_Twice_UpdatesExistingRow()
    {
        await SeedPack("ELECTRICAL");
        await SeedPack("MEDICAL");

        await _sut.InstallForShopAsync("ELECTRICAL");
        await _sut.InstallForShopAsync("MEDICAL");

        var count = await _tenantDb.Set<ShopVertical>().CountAsync();
        Assert.Equal(1, count);
        var sv = await _tenantDb.Set<ShopVertical>().FirstAsync();
        Assert.Equal("MEDICAL", sv.VerticalPackCode);
    }

    [Fact]
    public async Task GetShopVerticalAsync_AfterInstall_ReturnsDto()
    {
        await SeedPack("ELECTRICAL");
        await _sut.InstallForShopAsync("ELECTRICAL");

        var result = await _sut.GetShopVerticalAsync();
        Assert.NotNull(result);
        Assert.Equal("ELECTRICAL", result!.VerticalPackCode);
    }

    [Fact]
    public async Task InstallForShopAsync_InactivePack_ReturnsNotFound()
    {
        await SeedPack("INACTIVE_PACK", isActive: false);
        var result = await _sut.InstallForShopAsync("INACTIVE_PACK");
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Verticals.PackNotFound, result.Errors[0]);
    }

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
