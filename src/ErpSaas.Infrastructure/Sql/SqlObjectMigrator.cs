using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using ErpSaas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Infrastructure.Sql;

public sealed class SqlObjectMigrator(TenantDbContext tenantDb, ILogger<SqlObjectMigrator> logger)
    : ISqlObjectMigrator
{
    public async Task DeployAsync(CancellationToken ct = default)
    {
        var conn = tenantDb.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

        await EnsureVersionTableAsync(conn, ct);

        var sqlDir = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "Sql");

        if (!Directory.Exists(sqlDir))
        {
            logger.LogWarning("Sql directory not found at {Path}; skipping sproc deployment", sqlDir);
            return;
        }

        foreach (var file in Directory.GetFiles(sqlDir, "*.sql", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(file);
            var content = await File.ReadAllTextAsync(file, ct);
            var sha = ComputeSha256(content);

            var existing = await conn.QuerySingleOrDefaultAsync<string>(
                "SELECT [Sha256] FROM [dbo].[SqlObjectVersion] WHERE [FileName] = @fileName",
                new { fileName });

            if (existing == sha)
            {
                logger.LogDebug("Sql object {File} is up-to-date", fileName);
                continue;
            }

            logger.LogInformation("Deploying sql object {File}", fileName);

            // Execute each GO-separated batch
            foreach (var batch in SplitBatches(content))
            {
                if (!string.IsNullOrWhiteSpace(batch))
                    await conn.ExecuteAsync(batch);
            }

            await conn.ExecuteAsync(
                """
                MERGE [dbo].[SqlObjectVersion] AS target
                USING (SELECT @fileName AS [FileName], @sha AS [Sha256]) AS source
                ON target.[FileName] = source.[FileName]
                WHEN MATCHED THEN UPDATE SET [Sha256] = source.[Sha256], [DeployedAtUtc] = GETUTCDATE()
                WHEN NOT MATCHED THEN INSERT ([FileName], [Sha256], [DeployedAtUtc]) VALUES (source.[FileName], source.[Sha256], GETUTCDATE());
                """,
                new { fileName, sha });
        }
    }

    private static async Task EnsureVersionTableAsync(System.Data.IDbConnection conn, CancellationToken ct)
    {
        await conn.ExecuteAsync(
            """
            IF OBJECT_ID('dbo.SqlObjectVersion', 'U') IS NULL
            CREATE TABLE [dbo].[SqlObjectVersion] (
                [FileName]      NVARCHAR(200) NOT NULL PRIMARY KEY,
                [Sha256]        NVARCHAR(64)  NOT NULL,
                [DeployedAtUtc] DATETIME2     NOT NULL DEFAULT GETUTCDATE()
            );
            """);
    }

    private static string ComputeSha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static IEnumerable<string> SplitBatches(string sql)
        => sql.Split(["\nGO", "\r\nGO"], StringSplitOptions.RemoveEmptyEntries);
}
