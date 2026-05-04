using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Replication;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Identity.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.OnPrem;

[Trait("Category", "Unit")]
public class OnPremDeploymentServiceTests : IDisposable
{
    private sealed class StubTenantContext(long shopId, long userId = 42L) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => userId;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }

    private readonly PlatformDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly OnPremDeploymentService _sut;
    private readonly SqliteConnection _sqliteConnection;
    private readonly StubTenantContext _tenant;

    private const long ShopId = 1L;

    public OnPremDeploymentServiceTests()
    {
        _tenant = new StubTenantContext(shopId: ShopId);
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var opts = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        _db = new OnPremPlatformDbContext(opts, new AuditSaveChangesInterceptor(_tenant));
        _db.Database.EnsureCreated();

        _sut = new OnPremDeploymentService(
            _db, _errorLogger, _tenant, Substitute.For<ILogger<OnPremDeploymentService>>());
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqliteConnection.Dispose();
    }

    // ── RegisterAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_NewDeployment_CreatesWithStatusActive()
    {
        var dto = new RegisterDeploymentDto("DEP-001", "http://local:5000", "pk_test", "1.0.0", ReplicationMode.Bidirectional);

        var result = await _sut.RegisterAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DeploymentId.Should().Be("DEP-001");
        result.Value.Status.Should().Be(OnPremDeploymentStatus.Active);
        result.Value.Mode.Should().Be(ReplicationMode.Bidirectional);
    }

    [Fact]
    public async Task RegisterAsync_ExistingDeployment_UpdatesInsteadOfDuplicate()
    {
        var dto = new RegisterDeploymentDto("DEP-002", "http://old:5000", "pk_test", "1.0.0", ReplicationMode.CloudToOnPrem);
        await _sut.RegisterAsync(dto);

        var updateDto = new RegisterDeploymentDto("DEP-002", "http://new:5000", "pk_test", "1.1.0", ReplicationMode.Bidirectional);
        var result = await _sut.RegisterAsync(updateDto);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ShopLocalEndpoint.Should().Be("http://new:5000");
        result.Value.SoftwareVersion.Should().Be("1.1.0");
        _db.OnPremDeployments.Count(d => d.ShopId == ShopId && d.DeploymentId == "DEP-002").Should().Be(1);
    }

    // ── ListAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_MultipleDeployments_ReturnsAllForShop()
    {
        await _sut.RegisterAsync(new RegisterDeploymentDto("DEP-A", "http://a:5000", "pk_a", "1.0.0", ReplicationMode.Bidirectional));
        await _sut.RegisterAsync(new RegisterDeploymentDto("DEP-B", "http://b:5000", "pk_b", "1.0.0", ReplicationMode.CloudToOnPrem));

        var items = await _sut.ListAsync();

        items.Should().HaveCount(2);
        items.Select(d => d.DeploymentId).Should().Contain(["DEP-A", "DEP-B"]);
    }

    // ── GetAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_ExistingId_ReturnsDeployment()
    {
        var reg = await _sut.RegisterAsync(new RegisterDeploymentDto("DEP-GET", "http://x:5000", "pk", "1.0.0", ReplicationMode.Bidirectional));

        var result = await _sut.GetAsync(reg.Value!.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DeploymentId.Should().Be("DEP-GET");
    }

    [Fact]
    public async Task GetAsync_NonExistentId_ReturnsNotFound()
    {
        var result = await _sut.GetAsync(999L);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Errors.OnPrem.NotFound);
    }

    // ── UpdateStatusAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_ValidDeployment_ChangesStatus()
    {
        var reg = await _sut.RegisterAsync(new RegisterDeploymentDto("DEP-STATUS", "http://s:5000", "pk", "1.0.0", ReplicationMode.Bidirectional));

        var result = await _sut.UpdateStatusAsync(reg.Value!.Id, OnPremDeploymentStatus.Paused);

        result.IsSuccess.Should().BeTrue();
        var updated = await _sut.GetAsync(reg.Value.Id);
        updated.Value!.Status.Should().Be(OnPremDeploymentStatus.Paused);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonExistentId_ReturnsNotFound()
    {
        var result = await _sut.UpdateStatusAsync(9999L, OnPremDeploymentStatus.Paused);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Errors.OnPrem.NotFound);
    }

    // ── ResolveConflictAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ResolveConflictAsync_PendingConflict_ResolvesSuccessfully()
    {
        _db.ConflictArchives.Add(new ConflictArchive
        {
            ShopId = ShopId,
            DeploymentId = "DEP-CONFLICT",
            EntityName = "Invoice",
            EntityId = 1L,
            CloudSnapshotJson = "{}",
            OnPremSnapshotJson = "{}",
            Strategy = ConflictResolutionStrategy.ManualResolution,
            Outcome = ConflictResolutionOutcome.Pending,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();
        var conflictId = _db.ConflictArchives.First(c => c.ShopId == ShopId).Id;

        var result = await _sut.ResolveConflictAsync(conflictId,
            new ResolveConflictDto(ConflictResolutionOutcome.ManuallyResolved, "Cloud wins"));

        result.IsSuccess.Should().BeTrue();
        var resolved = _db.ConflictArchives.First(c => c.Id == conflictId);
        resolved.Outcome.Should().Be(ConflictResolutionOutcome.ManuallyResolved);
        resolved.ResolutionNote.Should().Be("Cloud wins");
        resolved.ResolvedByUserId.Should().Be(42L);
    }

    [Fact]
    public async Task ResolveConflictAsync_AlreadyResolved_ReturnsConflict()
    {
        _db.ConflictArchives.Add(new ConflictArchive
        {
            ShopId = ShopId,
            DeploymentId = "DEP-RESOLVED",
            EntityName = "Invoice",
            EntityId = 2L,
            CloudSnapshotJson = "{}",
            OnPremSnapshotJson = "{}",
            Strategy = ConflictResolutionStrategy.LastWriteWins,
            Outcome = ConflictResolutionOutcome.AutoResolved,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();
        var conflictId = _db.ConflictArchives.First(c => c.DeploymentId == "DEP-RESOLVED").Id;

        var result = await _sut.ResolveConflictAsync(conflictId,
            new ResolveConflictDto(ConflictResolutionOutcome.ManuallyResolved, null));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Errors.OnPrem.AlreadyResolved);
    }

    // ── Test-local PlatformDbContext ──────────────────────────────────────────

    private sealed class OnPremPlatformDbContext(
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

                // SQLite doesn't support partial indexes
                foreach (var index in entityType.GetIndexes().ToList())
                {
                    index.SetFilter(null);
                }
            }
        }
    }
}
