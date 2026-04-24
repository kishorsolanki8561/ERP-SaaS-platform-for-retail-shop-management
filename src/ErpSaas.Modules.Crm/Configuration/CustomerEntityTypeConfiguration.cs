using ErpSaas.Modules.Crm.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpSaas.Modules.Crm.Configuration;

public sealed class CustomerGroupEntityTypeConfiguration : IEntityTypeConfiguration<CustomerGroup>
{
    public void Configure(EntityTypeBuilder<CustomerGroup> b)
    {
        b.ToTable("CustomerGroup", schema: "crm");
        b.HasKey(e => e.Id);
        b.Property(e => e.Code).HasMaxLength(50).IsRequired();
        b.HasIndex(e => new { e.ShopId, e.Code }).IsUnique();
        b.Property(e => e.Name).HasMaxLength(200).IsRequired();
        b.Property(e => e.DiscountPercent).HasPrecision(5, 2);
        b.Property(e => e.RowVersion).IsRowVersion();
    }
}

public sealed class CustomerEntityTypeConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("Customer", schema: "crm");
        b.HasKey(e => e.Id);
        b.Property(e => e.CustomerCode).HasMaxLength(20).IsRequired();
        b.HasIndex(e => new { e.ShopId, e.CustomerCode }).IsUnique();
        b.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
        b.Property(e => e.CustomerType).HasMaxLength(50).IsRequired();
        b.Property(e => e.Email).HasMaxLength(256);
        b.Property(e => e.Phone).HasMaxLength(20);
        b.Property(e => e.GstNumber).HasMaxLength(15);
        b.Property(e => e.CreditLimitAmount).HasPrecision(18, 2);
        b.Property(e => e.OutstandingAmount).HasPrecision(18, 2);
        b.HasOne(e => e.CustomerGroup)
            .WithMany(g => g.Customers)
            .HasForeignKey(e => e.CustomerGroupId)
            .OnDelete(DeleteBehavior.SetNull);
        b.Property(e => e.RowVersion).IsRowVersion();
    }
}

public sealed class CustomerAddressEntityTypeConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> b)
    {
        b.ToTable("CustomerAddress", schema: "crm");
        b.HasKey(e => e.Id);
        b.Property(e => e.AddressType).HasMaxLength(20).IsRequired();
        b.Property(e => e.Line1).HasMaxLength(300).IsRequired();
        b.Property(e => e.Line2).HasMaxLength(300);
        b.Property(e => e.City).HasMaxLength(100);
        b.Property(e => e.StateCode).HasMaxLength(10);
        b.Property(e => e.PinCode).HasMaxLength(10);
        b.HasOne(e => e.Customer)
            .WithMany(c => c.Addresses)
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Property(e => e.RowVersion).IsRowVersion();
    }
}
