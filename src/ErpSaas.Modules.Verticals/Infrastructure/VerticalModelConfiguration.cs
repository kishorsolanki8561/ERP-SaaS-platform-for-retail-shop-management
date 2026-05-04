using ErpSaas.Modules.Verticals.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Verticals.Infrastructure;

public static class VerticalModelConfiguration
{
    public const string Schema = "verticals";

    public static void Configure(ModelBuilder b)
    {
        b.Entity<ShopVertical>(e =>
        {
            e.ToTable("ShopVertical", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.VerticalPackCode).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.ShopId).IsUnique();
        });
    }
}
