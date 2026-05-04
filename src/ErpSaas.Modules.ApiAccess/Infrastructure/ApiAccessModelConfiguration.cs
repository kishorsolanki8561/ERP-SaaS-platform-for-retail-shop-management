using ErpSaas.Modules.ApiAccess.Entities;
using ErpSaas.Modules.ApiAccess.Enums;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.ApiAccess.Infrastructure;

public static class ApiAccessModelConfiguration
{
    public const string Schema = "integration";

    public static void Configure(ModelBuilder b)
    {
        b.Entity<ShopApiKey>(e =>
        {
            e.ToTable("ShopApiKey", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.KeyPrefix).HasMaxLength(20).IsRequired();
            e.Property(x => x.KeyHashSha256).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.KeyHashSha256).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ScopesCsv).HasMaxLength(4000);
            e.Property(x => x.LastUsedIp).HasMaxLength(45);
            e.Property(x => x.RevokedReason).HasMaxLength(500);
            e.HasIndex(x => new { x.ShopId, x.IsActive });
        });

        b.Entity<WebhookEndpoint>(e =>
        {
            e.ToTable("WebhookEndpoint", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Url).HasMaxLength(500).IsRequired();
            e.Property(x => x.SigningSecret).HasMaxLength(200).IsRequired();
            e.Property(x => x.EventsCsv).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.CustomHeadersJson).HasMaxLength(2000);
            e.HasIndex(x => new { x.ShopId, x.IsActive });
            e.HasMany(x => x.Deliveries)
                .WithOne(d => d.Endpoint)
                .HasForeignKey(d => d.WebhookEndpointId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<WebhookDelivery>(e =>
        {
            e.ToTable("WebhookDelivery", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.EventCode).HasMaxLength(100).IsRequired();
            e.Property(x => x.DeliveryId).IsRequired();
            e.HasIndex(x => x.DeliveryId).IsUnique();
            e.Property(x => x.PayloadJson).HasMaxLength(-1).IsRequired();
            e.Property(x => x.ResponseBody).HasMaxLength(4000);
            e.Property(x => x.ErrorMessage).HasMaxLength(2000);
            e.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            e.HasIndex(x => new { x.ShopId, x.Status });
            e.HasIndex(x => new { x.ShopId, x.WebhookEndpointId });
        });
    }
}
