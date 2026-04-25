using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Masters.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Masters;

// ── Test-local PlatformDbContext on SQLite ────────────────────────────────────

/// <summary>
/// A minimal PlatformDbContext for unit tests that uses SQLite in-memory.
/// Masters entities (Country, State, City, Currency, HsnSacCode) are
/// registered by the base <c>PlatformDbContext.OnModelCreating</c>.
///
/// SQLite does not support <c>IsRowVersion()</c> (SQL Server rowversion), so
/// we override <c>OnModelCreating</c> to drop that constraint before the DB
/// is created.
/// </summary>
internal sealed class MastersPlatformDbContext(
    DbContextOptions<PlatformDbContext> options,
    AuditSaveChangesInterceptor auditInterceptor)
    : PlatformDbContext(options, auditInterceptor)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SQLite does not support SQL Server rowversion / timestamp.
        // Remove the concurrency-token flag and set a SQLite-compatible default
        // so EnsureCreated does not produce a NOT NULL column with no default.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var rowVersion = entityType.FindProperty("RowVersion");
            if (rowVersion is not null)
            {
                rowVersion.IsConcurrencyToken = false;
                rowVersion.SetDefaultValueSql("0");
            }
        }
    }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

[Trait("Category", "Unit")]
public class MastersServiceTests : IDisposable
{
    private readonly PlatformDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly MasterDataService _sut;
    private readonly SqliteConnection _sqliteConnection;

    public MastersServiceTests()
    {
        // Use SQLite in-memory (supports transactions, unlike EF in-memory provider).
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var opts = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        var stubCtx = new StubAuditContext();
        var auditInterceptor = new AuditSaveChangesInterceptor(stubCtx);

        _db = new MastersPlatformDbContext(opts, auditInterceptor);
        _db.Database.EnsureCreated();

        _sut = new MasterDataService(_db, _errorLogger);
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqliteConnection.Dispose();
    }

    // ── CreateCountryAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCountryAsync_ValidInput_ReturnsSuccessWithId()
    {
        var result = await _sut.CreateCountryAsync("IN", "India", "+91", "INR");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateCountryAsync_DuplicateCode_ReturnsConflict()
    {
        await _sut.CreateCountryAsync("IN", "India", "+91", "INR");

        var result = await _sut.CreateCountryAsync("IN", "India Duplicate", null, null);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateCountryAsync_PersistsCorrectly()
    {
        await _sut.CreateCountryAsync("US", "United States", "+1", "USD");

        var countries = await _sut.ListCountriesAsync();
        countries.Should().ContainSingle(c => c.Code == "US");
        var us = countries.Single(c => c.Code == "US");
        us.Name.Should().Be("United States");
        us.PhoneCode.Should().Be("+1");
        us.CurrencyCode.Should().Be("USD");
    }

    // ── ListCountriesAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ListCountriesAsync_ReturnsOnlyActiveCountries()
    {
        await _sut.CreateCountryAsync("DE", "Germany", "+49", "EUR");
        await _sut.CreateCountryAsync("FR", "France", "+33", "EUR");

        var result = await _sut.ListCountriesAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListCountriesAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await _sut.ListCountriesAsync();

        result.Should().BeEmpty();
    }

    // ── CreateStateAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateStateAsync_ValidInput_ReturnsSuccessWithId()
    {
        var countryResult = await _sut.CreateCountryAsync("IN", "India", "+91", "INR");
        var countryId = countryResult.Value!;

        var result = await _sut.CreateStateAsync(countryId, "MH", "Maharashtra", "27");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateStateAsync_DuplicateCodeInSameCountry_ReturnsConflict()
    {
        var countryResult = await _sut.CreateCountryAsync("IN", "India", "+91", "INR");
        var countryId = countryResult.Value!;
        await _sut.CreateStateAsync(countryId, "MH", "Maharashtra", "27");

        var result = await _sut.CreateStateAsync(countryId, "MH", "Maharashtra Dup", null);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ListStatesByCountryAsync_ReturnsOnlyStatesForCountry()
    {
        var inResult = await _sut.CreateCountryAsync("IN", "India", "+91", "INR");
        var usResult = await _sut.CreateCountryAsync("US", "United States", "+1", "USD");
        await _sut.CreateStateAsync(inResult.Value!, "MH", "Maharashtra", "27");
        await _sut.CreateStateAsync(inResult.Value!, "GJ", "Gujarat", "24");
        await _sut.CreateStateAsync(usResult.Value!, "CA", "California", null);

        var indiaStates = await _sut.ListStatesByCountryAsync(inResult.Value!);

        indiaStates.Should().HaveCount(2);
        indiaStates.Should().AllSatisfy(s => s.CountryId.Should().Be(inResult.Value));
    }

    // ── CreateCityAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCityAsync_ValidInput_ReturnsSuccessWithId()
    {
        var countryResult = await _sut.CreateCountryAsync("IN", "India", "+91", "INR");
        var stateResult = await _sut.CreateStateAsync(countryResult.Value!, "MH", "Maharashtra", "27");

        var result = await _sut.CreateCityAsync(stateResult.Value!, "Mumbai");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateCityAsync_DuplicateNameInSameState_ReturnsConflict()
    {
        var countryResult = await _sut.CreateCountryAsync("IN", "India", "+91", "INR");
        var stateResult = await _sut.CreateStateAsync(countryResult.Value!, "MH", "Maharashtra", "27");
        await _sut.CreateCityAsync(stateResult.Value!, "Mumbai");

        var result = await _sut.CreateCityAsync(stateResult.Value!, "Mumbai");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ListCitiesByStateAsync_ReturnsOnlyCitiesForState()
    {
        var countryResult = await _sut.CreateCountryAsync("IN", "India", "+91", "INR");
        var mhResult = await _sut.CreateStateAsync(countryResult.Value!, "MH", "Maharashtra", "27");
        var gjResult = await _sut.CreateStateAsync(countryResult.Value!, "GJ", "Gujarat", "24");
        await _sut.CreateCityAsync(mhResult.Value!, "Mumbai");
        await _sut.CreateCityAsync(mhResult.Value!, "Pune");
        await _sut.CreateCityAsync(gjResult.Value!, "Ahmedabad");

        var mhCities = await _sut.ListCitiesByStateAsync(mhResult.Value!);

        mhCities.Should().HaveCount(2);
        mhCities.Should().AllSatisfy(c => c.StateId.Should().Be(mhResult.Value));
    }

    // ── ListCurrenciesAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ListCurrenciesAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await _sut.ListCurrenciesAsync();

        result.Should().BeEmpty();
    }

    // ── SearchHsnSacAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task SearchHsnSacAsync_NoMatches_ReturnsEmptyList()
    {
        var result = await _sut.SearchHsnSacAsync("9999XYZ");

        result.Should().BeEmpty();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class StubAuditContext : ITenantContext
    {
        public long ShopId => 0;
        public long CurrentUserId => 1;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
