using ErpSaas.Infrastructure.Data;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Infrastructure;

/// <summary>
/// Guards against two classes of migration mistake:
///
/// 1. Pending model changes — a developer modified an entity but forgot to run
///    <c>dotnet ef migrations add</c>. <c>HasPendingModelChanges()</c> returns
///    <c>true</c> in this case. No database connection is required; the check
///    compares the current EF model against the model snapshot stored in the
///    last migration file.
///
/// 2. Un-applied migrations — migration files exist but have not been run
///    against the database. <c>GetPendingMigrationsAsync()</c> returns the names
///    of missing migrations. This list should be empty because
///    <c>IntegrationTestFixture.InitializeAsync()</c> calls <c>MigrateAsync()</c>
///    for every context before any test class runs.
///
/// Both checks run inside the shared Testcontainers fixture so they use the
/// fully-configured DI container (all module configurators included). This
/// ensures the <c>TenantDbContext</c> model compared here matches the model
/// that was used when the migration snapshot was generated.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class PendingMigrationsTests(IntegrationTestFixture fixture)
{
    // ── HasPendingModelChanges ────────────────────────────────────────────────
    // These tests fail the build if a developer pushes an entity change
    // without a corresponding migration file.

    [Fact]
    public async Task PlatformDbContext_HasNoPendingModelChanges()
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        db.Database.HasPendingModelChanges()
            .Should().BeFalse(
                "all model changes to PlatformDbContext must have a corresponding migration — " +
                "run: dotnet ef migrations add <Name> --project src/ErpSaas.Infrastructure " +
                "--startup-project src/ErpSaas.Api --context PlatformDbContext");
    }

    [Fact]
    public async Task TenantDbContext_HasNoPendingModelChanges()
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        db.Database.HasPendingModelChanges()
            .Should().BeFalse(
                "all model changes to TenantDbContext must have a corresponding migration — " +
                "run: dotnet ef migrations add <Name> --project src/ErpSaas.Infrastructure " +
                "--startup-project src/ErpSaas.Api --context TenantDbContext");
    }

    [Fact]
    public async Task AnalyticsDbContext_HasNoPendingModelChanges()
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        db.Database.HasPendingModelChanges()
            .Should().BeFalse(
                "all model changes to AnalyticsDbContext must have a corresponding migration — " +
                "run: dotnet ef migrations add <Name> --project src/ErpSaas.Infrastructure " +
                "--startup-project src/ErpSaas.Api --context AnalyticsDbContext");
    }

    [Fact]
    public async Task LogDbContext_HasNoPendingModelChanges()
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LogDbContext>();
        db.Database.HasPendingModelChanges()
            .Should().BeFalse(
                "all model changes to LogDbContext must have a corresponding migration — " +
                "run: dotnet ef migrations add <Name> --project src/ErpSaas.Infrastructure " +
                "--startup-project src/ErpSaas.Api --context LogDbContext");
    }

    [Fact]
    public async Task NotificationsDbContext_HasNoPendingModelChanges()
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        db.Database.HasPendingModelChanges()
            .Should().BeFalse(
                "all model changes to NotificationsDbContext must have a corresponding migration — " +
                "run: dotnet ef migrations add <Name> --project src/ErpSaas.Infrastructure " +
                "--startup-project src/ErpSaas.Api --context NotificationsDbContext");
    }

    // ── GetPendingMigrationsAsync ─────────────────────────────────────────────
    // These tests confirm that every migration file has been applied to the
    // Testcontainers database. They catch the case where a migration file exists
    // but MigrateAsync() silently skipped it during fixture initialisation.

    [Fact]
    public async Task PlatformDbContext_AllMigrationsApplied()
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var pending = await db.Database.GetPendingMigrationsAsync();
        pending.Should().BeEmpty(
            "IntegrationTestFixture.InitializeAsync() calls MigrateAsync() — " +
            "no migration should remain un-applied against the Testcontainers database");
    }

    [Fact]
    public async Task TenantDbContext_AllMigrationsApplied()
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var pending = await db.Database.GetPendingMigrationsAsync();
        pending.Should().BeEmpty(
            "IntegrationTestFixture.InitializeAsync() calls MigrateAsync() — " +
            "no migration should remain un-applied against the Testcontainers database");
    }

    [Fact]
    public async Task AnalyticsDbContext_AllMigrationsApplied()
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        var pending = await db.Database.GetPendingMigrationsAsync();
        pending.Should().BeEmpty(
            "IntegrationTestFixture.InitializeAsync() calls MigrateAsync() — " +
            "no migration should remain un-applied against the Testcontainers database");
    }

    [Fact]
    public async Task LogDbContext_AllMigrationsApplied()
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LogDbContext>();
        var pending = await db.Database.GetPendingMigrationsAsync();
        pending.Should().BeEmpty(
            "IntegrationTestFixture.InitializeAsync() calls MigrateAsync() — " +
            "no migration should remain un-applied against the Testcontainers database");
    }

    [Fact]
    public async Task NotificationsDbContext_AllMigrationsApplied()
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var pending = await db.Database.GetPendingMigrationsAsync();
        pending.Should().BeEmpty(
            "IntegrationTestFixture.InitializeAsync() calls MigrateAsync() — " +
            "no migration should remain un-applied against the Testcontainers database");
    }
}
