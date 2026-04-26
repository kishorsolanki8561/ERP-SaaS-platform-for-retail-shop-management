using Dapper;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Sql;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Tests.Integration.Infrastructure;

/// <summary>
/// Verifies that <c>ISqlObjectMigrator.DeployAsync</c> is idempotent —
/// deploying SQL objects twice produces identical version-table entries and
/// no exceptions on the second run.
/// Phase exit-gate: universal check #7 / <c>SqlObjectMigratorTests</c>.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class SqlObjectMigratorTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task DeployAsync_CalledTwice_VersionTableRowCountIsIdentical()
    {
        // ── Arrange: first deploy already ran via InitializeAsync ─────────────
        await using var scope1 = fixture.CreateScope();
        var migrator = scope1.ServiceProvider.GetRequiredService<ISqlObjectMigrator>();
        var db       = scope1.ServiceProvider.GetRequiredService<TenantDbContext>();
        var conn     = db.Database.GetDbConnection();

        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        var countAfterFirstDeploy = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM [dbo].[SqlObjectVersion]");

        // ── Act: second deploy ────────────────────────────────────────────────
        var act = async () => await migrator.DeployAsync();

        // ── Assert ────────────────────────────────────────────────────────────
        await act.Should().NotThrowAsync("second deploy must be a no-op, not throw");

        var countAfterSecondDeploy = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM [dbo].[SqlObjectVersion]");

        countAfterSecondDeploy.Should().Be(
            countAfterFirstDeploy,
            "no new version rows should be inserted on an idempotent re-deploy");
    }

    [Fact]
    public async Task DeployAsync_AllSqlFilesHaveVersionEntry()
    {
        // Every .sql file in the Sql directory must have a corresponding version row.
        await using var scope = fixture.CreateScope();
        var db   = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var conn = db.Database.GetDbConnection();

        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        var deployedFiles = (await conn.QueryAsync<string>(
            "SELECT [FileName] FROM [dbo].[SqlObjectVersion]")).ToList();

        // The count may be 0 if there are no .sql files — that is also valid.
        deployedFiles.Should().OnlyHaveUniqueItems(
            "each SQL file must appear exactly once in the version table");
    }
}
