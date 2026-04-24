using System.Data;

namespace ErpSaas.Infrastructure.Dapper;

public interface IDapperContext
{
    IDbConnection Connection { get; }
    IDbTransaction? CurrentTransaction { get; }
}
