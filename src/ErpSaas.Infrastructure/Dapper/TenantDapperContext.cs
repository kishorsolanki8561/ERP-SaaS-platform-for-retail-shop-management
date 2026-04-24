using System.Data;
using ErpSaas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace ErpSaas.Infrastructure.Dapper;

/// <summary>
/// Exposes TenantDbContext's underlying connection + current transaction for Dapper queries.
/// Use this inside BaseService.ExecuteAsync to share the EF transaction with Dapper.
/// </summary>
public sealed class TenantDapperContext(TenantDbContext db) : IDapperContext
{
    public IDbConnection Connection => db.Database.GetDbConnection();

    public IDbTransaction? CurrentTransaction =>
        db.Database.CurrentTransaction?.GetDbTransaction();
}
