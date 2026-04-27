using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Infrastructure.Data;

// Stores offline command queue and on-prem replication logs (Phase 6).
public class SyncDbContext(DbContextOptions<SyncDbContext> options) : DbContext(options)
{
}
