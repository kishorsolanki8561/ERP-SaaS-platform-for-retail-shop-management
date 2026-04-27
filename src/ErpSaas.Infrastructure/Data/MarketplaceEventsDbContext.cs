using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Infrastructure.Data;

// Stores raw webhook payloads from Amazon, Flipkart, etc. (Phase 4).
public class MarketplaceEventsDbContext(DbContextOptions<MarketplaceEventsDbContext> options) : DbContext(options)
{
}
