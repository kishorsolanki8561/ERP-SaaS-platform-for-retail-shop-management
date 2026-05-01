using ErpSaas.Modules.Accounting.Entities;
using ErpSaas.Modules.Accounting.Enums;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Accounting.Infrastructure;

public static class AccountingModelConfiguration
{
    public const string Schema = "accounting";

    public static void Configure(ModelBuilder b)
    {
        b.Entity<AccountGroup>(e =>
        {
            e.ToTable("AccountGroup", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.Code }).IsUnique();
            e.Property(x => x.Nature).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Account>(e =>
        {
            e.ToTable("Account", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.Code }).IsUnique();
            e.Property(x => x.OpeningBalance).HasPrecision(18, 2);
            e.Property(x => x.OpeningBalanceType).HasConversion<string>().HasMaxLength(10).IsRequired();
            e.Property(x => x.GstNumber).HasMaxLength(15);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasOne(x => x.AccountGroup)
                .WithMany(x => x.Accounts)
                .HasForeignKey(x => x.AccountGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<FinancialYear>(e =>
        {
            e.ToTable("FinancialYear", schema: Schema);
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ShopId, x.StartYear }).IsUnique();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
        });

        b.Entity<Voucher>(e =>
        {
            e.ToTable("Voucher", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.VoucherNumber).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.VoucherNumber }).IsUnique();
            e.Property(x => x.VoucherType).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Narration).HasMaxLength(2000);
            e.Property(x => x.TotalDebit).HasPrecision(18, 2);
            e.Property(x => x.TotalCredit).HasPrecision(18, 2);
            e.Property(x => x.SourceDocumentType).HasMaxLength(50);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasMany(x => x.Entries)
                .WithOne(x => x.Voucher)
                .HasForeignKey(x => x.VoucherId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<VoucherEntry>(e =>
        {
            e.ToTable("VoucherEntry", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(10).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Narration).HasMaxLength(500);
            e.HasIndex(x => new { x.ShopId, x.VoucherId });
            e.HasIndex(x => new { x.ShopId, x.AccountId });
            e.HasOne(x => x.Account)
                .WithMany(x => x.VoucherEntries)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Expense>(e =>
        {
            e.ToTable("Expense", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.PaymentModeCode).HasMaxLength(30).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.AttachmentFileId).HasMaxLength(100);
            e.Property(x => x.RecurrenceInterval).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.ShopId, x.ExpenseDate });
            e.HasOne(x => x.Account)
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<BankAccount>(e =>
        {
            e.ToTable("BankAccount", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.BankName).HasMaxLength(200).IsRequired();
            e.Property(x => x.AccountNumber).HasMaxLength(30).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.AccountNumber }).IsUnique();
            e.Property(x => x.IfscCode).HasMaxLength(11).IsRequired();
            e.Property(x => x.BranchName).HasMaxLength(200).IsRequired();
            e.Property(x => x.AccountHolderName).HasMaxLength(200).IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasOne(x => x.Account)
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<BankStatement>(e =>
        {
            e.ToTable("BankStatement", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.OpeningBalance).HasPrecision(18, 2);
            e.Property(x => x.ClosingBalance).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.ShopId, x.BankAccountId, x.PeriodStart });
            e.HasOne(x => x.BankAccount)
                .WithMany()
                .HasForeignKey(x => x.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Lines)
                .WithOne(x => x.BankStatement)
                .HasForeignKey(x => x.BankStatementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<BankStatementLine>(e =>
        {
            e.ToTable("BankStatementLine", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.Reference).HasMaxLength(100);
            e.Property(x => x.CreditAmount).HasPrecision(18, 2);
            e.Property(x => x.DebitAmount).HasPrecision(18, 2);
            e.Property(x => x.MatchStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.BankStatementId, x.MatchStatus });
        });

        b.Entity<ReconciliationRule>(e =>
        {
            e.ToTable("ReconciliationRule", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.PatternContains).HasMaxLength(200).IsRequired();
            e.Property(x => x.VoucherType).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.ShopId, x.IsActive });
            e.HasOne(x => x.Account)
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Cheque>(e =>
        {
            e.ToTable("Cheque", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ChequeNumber).HasMaxLength(30).IsRequired();
            e.Property(x => x.DrawerName).HasMaxLength(200).IsRequired();
            e.Property(x => x.DrawerBankName).HasMaxLength(200).IsRequired();
            e.Property(x => x.BounceReasonCode).HasMaxLength(50);
            e.Property(x => x.Direction).HasConversion<string>().HasMaxLength(10).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.ShopId, x.ChequeNumber, x.BankAccountId }).IsUnique();
            e.HasIndex(x => new { x.ShopId, x.Status });
            e.HasOne(x => x.BankAccount)
                .WithMany()
                .HasForeignKey(x => x.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<PettyCashClosure>(e =>
        {
            e.ToTable("PettyCashClosure", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Narration).HasMaxLength(500).IsRequired();
            e.Property(x => x.ExpectedBalance).HasPrecision(18, 2);
            e.Property(x => x.CountedBalance).HasPrecision(18, 2);
            e.Property(x => x.Variance).HasPrecision(18, 2);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.ShopId, x.ClosureDate });
        });

        b.Entity<FixedAsset>(e =>
        {
            e.ToTable("FixedAsset", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.AssetCode).HasMaxLength(30).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.CategoryCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.LocationNotes).HasMaxLength(500);
            e.Property(x => x.Method).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.PurchaseCost).HasPrecision(18, 2);
            e.Property(x => x.SalvageValue).HasPrecision(18, 2);
            e.Property(x => x.RateOfDepreciation).HasPrecision(10, 4);
            e.Property(x => x.UsefulLifeYears).HasPrecision(5, 2);
            e.Property(x => x.AccumulatedDepreciation).HasPrecision(18, 2);
            e.Property(x => x.NetBookValue).HasPrecision(18, 2);
            e.Property(x => x.DisposalValue).HasPrecision(18, 2);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.ShopId, x.AssetCode }).IsUnique();
            e.HasIndex(x => new { x.ShopId, x.Status });
            e.HasMany(x => x.DepreciationEntries)
                .WithOne(x => x.FixedAsset)
                .HasForeignKey(x => x.FixedAssetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<DepreciationEntry>(e =>
        {
            e.ToTable("DepreciationEntry", schema: Schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.AccumulatedAfter).HasPrecision(18, 2);
            e.Property(x => x.NetBookValueAfter).HasPrecision(18, 2);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.FixedAssetId, x.PeriodDate }).IsUnique();
        });
    }
}
