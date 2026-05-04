using ErpSaas.Modules.Payment.Entities;
using ErpSaas.Modules.Payment.Enums;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Payment.Infrastructure;

public static class PaymentModelConfiguration
{
    public const string Schema = "payment";

    public static void Configure(ModelBuilder b)
    {
        b.Entity<PaymentGatewayTransaction>(e =>
        {
            e.ToTable("PaymentGatewayTransaction", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.GatewayCode).HasMaxLength(30).IsRequired();
            e.Property(x => x.GatewayTxnId).HasMaxLength(200).IsRequired();
            e.Property(x => x.OurReferenceNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.Method).HasMaxLength(50);
            e.Property(x => x.Vpa).HasMaxLength(100);
            e.Property(x => x.CardLast4).HasMaxLength(4);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.FailureCode).HasMaxLength(50);
            e.Property(x => x.FailureMessage).HasMaxLength(500);
            e.Property(x => x.SettlementReference).HasMaxLength(100);
            e.Property(x => x.PaymentUrl).HasMaxLength(2000);
            e.Property(x => x.RefundGatewayTxnId).HasMaxLength(200);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.GatewayFee).HasPrecision(18, 4);
            e.Property(x => x.GatewayGst).HasPrecision(18, 4);
            e.Property(x => x.NetSettled).HasPrecision(18, 2);
            e.HasIndex(x => new { x.ShopId, x.GatewayCode, x.GatewayTxnId }).IsUnique()
                .HasFilter("[GatewayTxnId] != ''");
            e.HasIndex(x => new { x.ShopId, x.OurReferenceNumber }).IsUnique();
            e.HasIndex(x => new { x.ShopId, x.Status });
            e.HasIndex(x => new { x.ShopId, x.InitiatedAtUtc });
            e.HasIndex(x => new { x.ShopId, x.Purpose });
        });

        b.Entity<PaymentGatewayAccount>(e =>
        {
            e.ToTable("PaymentGatewayAccount", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.GatewayCode).HasMaxLength(30).IsRequired();
            e.Property(x => x.CredentialsJsonEncrypted).HasMaxLength(4000).IsRequired();
            e.Property(x => x.WebhookSecretEncrypted).HasMaxLength(500);
            e.HasIndex(x => new { x.ShopId, x.GatewayCode }).IsUnique();
        });

        b.Entity<ReconciliationException>(e =>
        {
            e.ToTable("ReconciliationException", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.GatewayCode).HasMaxLength(30).IsRequired();
            e.Property(x => x.GatewayTxnId).HasMaxLength(200);
            e.Property(x => x.OurReferenceNumber).HasMaxLength(100);
            e.Property(x => x.ExceptionType).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.ResolutionNotes).HasMaxLength(1000);
            e.Property(x => x.OurAmount).HasPrecision(18, 2);
            e.Property(x => x.GatewayAmount).HasPrecision(18, 2);
            e.Property(x => x.OurFee).HasPrecision(18, 4);
            e.Property(x => x.GatewayFee).HasPrecision(18, 4);
            e.HasIndex(x => new { x.ShopId, x.Status });
            e.HasIndex(x => new { x.ShopId, x.GatewayCode, x.DetectedAtUtc });
        });
    }
}
