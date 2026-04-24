namespace ErpSaas.Infrastructure.Sql;

public interface ISqlObjectMigrator
{
    Task DeployAsync(CancellationToken ct = default);
}
