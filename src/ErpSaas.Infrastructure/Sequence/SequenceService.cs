using Dapper;
using ErpSaas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Infrastructure.Sequence;

public sealed class SequenceService(TenantDbContext db) : ISequenceService
{
    public async Task<string> NextAsync(string code, long shopId, CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

        var parameters = new DynamicParameters();
        parameters.Add("@ShopId", shopId);
        parameters.Add("@Code", code);
        parameters.Add("@AllocatedNumber", dbType: System.Data.DbType.Int64,
            direction: System.Data.ParameterDirection.Output);

        await conn.ExecuteAsync(
            "sequence.usp_AllocateSequenceNumber",
            parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        var number = parameters.Get<long>("@AllocatedNumber");

        // Load formatting from SequenceDefinition
        var def = await db.SequenceDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ShopId == shopId && s.Code == code, ct);

        if (def is null)
            return number.ToString();

        var padded = number.ToString().PadLeft(def.PadLength, '0');
        return $"{def.Prefix}{padded}{def.Suffix}";
    }
}
