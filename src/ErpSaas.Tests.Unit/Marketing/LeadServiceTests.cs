using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Marketing;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Modules.Identity.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Marketing;

// ── Test-local PlatformDbContext for SQLite ───────────────────────────────────

internal sealed class MarketingPlatformDbContext(
    DbContextOptions<PlatformDbContext> options,
    AuditSaveChangesInterceptor auditInterceptor)
    : PlatformDbContext(options, auditInterceptor)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var rv = entityType.FindProperty("RowVersion");
            if (rv is not null) { rv.IsConcurrencyToken = false; rv.SetDefaultValueSql("0"); }
        }
    }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

[Trait("Category", "Unit")]
public sealed class LeadServiceTests : IDisposable
{
    private readonly PlatformDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly LeadService _sut;
    private readonly SqliteConnection _sqlite;

    public LeadServiceTests()
    {
        _sqlite = new SqliteConnection("DataSource=:memory:");
        _sqlite.Open();

        var stubCtx = new StubTenantCtx();
        var opts = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlite(_sqlite).Options;

        _db = new MarketingPlatformDbContext(opts, new AuditSaveChangesInterceptor(stubCtx));
        _db.Database.EnsureCreated();

        _sut = new LeadService(_db, _errorLogger, Substitute.For<ILogger<LeadService>>());
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqlite.Dispose();
    }

    // ── SubmitAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitAsync_ValidLead_ReturnsSuccessWithId()
    {
        var dto = MakeDto();

        var result = await _sut.SubmitAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SubmitAsync_ValidLead_PersistsWithStatusNew()
    {
        var dto = MakeDto(email: "new@test.com");

        await _sut.SubmitAsync(dto);

        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Email == "new@test.com");
        lead.Should().NotBeNull();
        lead!.Status.Should().Be(LeadStatus.New);
        lead.Source.Should().Be(LeadSource.Website);
    }

    [Fact]
    public async Task SubmitAsync_DuplicateEmail_StillSucceeds()
    {
        var dto = MakeDto(email: "dup@test.com");

        await _sut.SubmitAsync(dto);
        var result = await _sut.SubmitAsync(dto);

        result.IsSuccess.Should().BeTrue();
        var count = await _db.Leads.CountAsync(l => l.Email == "dup@test.com");
        count.Should().Be(2);
    }

    // ── ListAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_FilterByStatus_ReturnsMatchingOnly()
    {
        await _db.Leads.AddRangeAsync(
            MakeLead("a@a.com", LeadStatus.New),
            MakeLead("b@b.com", LeadStatus.Qualified),
            MakeLead("c@c.com", LeadStatus.New));
        await _db.SaveChangesAsync();

        var (items, total) = await _sut.ListAsync(1, 20, LeadStatus.New);

        total.Should().Be(2);
        items.Should().AllSatisfy(i => i.Status.Should().Be("New"));
    }

    // ── UpdateStatusAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_ToConverted_WithoutConvertedShopId_ReturnsFail()
    {
        var lead = MakeLead("x@x.com", LeadStatus.Qualified);
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateStatusAsync(lead.Id, new UpdateLeadStatusDto("Converted", null));

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Should().Be(Errors.Lead.CannotMarkConvertedWithoutShop);
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidStatus_ReturnsFail()
    {
        var lead = MakeLead("y@y.com", LeadStatus.New);
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateStatusAsync(lead.Id, new UpdateLeadStatusDto("NotAStatus", null));

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Should().Be(Errors.Lead.InvalidStatus);
    }

    [Fact]
    public async Task UpdateStatusAsync_ToContacted_SetsLastContactedAt()
    {
        var lead = MakeLead("z@z.com", LeadStatus.New);
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateStatusAsync(lead.Id, new UpdateLeadStatusDto("Contacted", null));

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Leads.FindAsync(lead.Id);
        updated!.LastContactedAtUtc.Should().NotBeNull();
    }

    // ── ConvertAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ConvertAsync_AlreadyConverted_ReturnsConflict()
    {
        var lead = MakeLead("conv@c.com", LeadStatus.Converted, convertedShopId: 99L);
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();

        var result = await _sut.ConvertAsync(lead.Id);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Should().Be(Errors.Lead.AlreadyConverted);
    }

    [Fact]
    public async Task ConvertAsync_ValidLead_CreatesShopAndSetsConvertedShopId()
    {
        var lead = MakeLead("fresh@c.com", LeadStatus.Qualified, businessName: "Test Biz");
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();

        var result = await _sut.ConvertAsync(lead.Id);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Leads.FindAsync(lead.Id);
        updated!.Status.Should().Be(LeadStatus.Converted);
        updated.ConvertedShopId.Should().NotBeNull();

        var shop = await _db.Shops.FindAsync(result.Value);
        shop.Should().NotBeNull();
        shop!.LegalName.Should().Be("Test Biz");
    }

    // ── AssignAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AssignAsync_NonExistentLead_ReturnsNotFound()
    {
        var result = await _sut.AssignAsync(99999L, 1L);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Should().Be(Errors.Lead.NotFound);
    }

    [Fact]
    public async Task AssignAsync_NonExistentUser_ReturnsNotFound()
    {
        var lead = MakeLead("assign@test.com", LeadStatus.New);
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();

        var result = await _sut.AssignAsync(lead.Id, 99999L);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Should().Be(Errors.Lead.UserNotFound);
    }

    [Fact]
    public async Task AssignAsync_ValidUserAndLead_SetsAssignedUserId()
    {
        var user = new User
        {
            DisplayName = "Sales Rep", PasswordHash = "hash",
            IsActive = true, CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Users.Add(user);

        var lead = MakeLead("assign2@test.com", LeadStatus.New);
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();

        var result = await _sut.AssignAsync(lead.Id, user.Id);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Leads.FindAsync(lead.Id);
        updated!.AssignedUserId.Should().Be(user.Id);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SubmitLeadDto MakeDto(string email = "test@example.com") => new(
        Name: "Test User", Email: email, Phone: "9999999999",
        BusinessName: "Test Co", Message: "Interested",
        CityCode: "MUM", StateCode: "MH", VerticalCode: "Electrical",
        ShopsCount: 1, Source: "Website",
        UtmSource: null, UtmCampaign: null);

    private static Lead MakeLead(
        string email, LeadStatus status,
        long? convertedShopId = null, string? businessName = null) => new()
    {
        Name = "Lead", Email = email, Phone = "9999999999",
        CityCode = "MUM", StateCode = "MH", VerticalCode = "Electrical",
        Source = LeadSource.Website, Status = status,
        ConvertedShopId = convertedShopId,
        BusinessName = businessName,
        CreatedAtUtc = DateTime.UtcNow,
    };

    private sealed class StubTenantCtx : ITenantContext
    {
        public long ShopId => 1L;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
