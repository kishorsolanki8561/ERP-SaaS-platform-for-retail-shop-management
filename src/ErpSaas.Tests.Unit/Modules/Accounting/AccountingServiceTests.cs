using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Modules.Accounting.Entities;
using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Modules.Accounting.Infrastructure;
using ErpSaas.Modules.Accounting.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Accounting;

internal sealed class AccountingTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        AccountingModelConfiguration.Configure(modelBuilder);
    }
}

[Trait("Category", "Unit")]
public class AccountingServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly ISequenceService _sequence  = Substitute.For<ISequenceService>();
    private readonly ITenantContext _tenant      = Substitute.For<ITenantContext>();
    private readonly AccountingService _sut;
    private readonly SqliteConnection _sqliteConnection;
    private int _seqCounter;

    public AccountingServiceTests()
    {
        var stubCtx = new StubTenantContext(shopId: 1L);
        _tenant.ShopId.Returns(1L);

        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        var auditInterceptor  = new AuditSaveChangesInterceptor(stubCtx);
        var tenantInterceptor = new TenantSaveChangesInterceptor(stubCtx);
        _db = new AccountingTenantDbContext(opts, stubCtx, auditInterceptor, tenantInterceptor);
        _db.Database.EnsureCreated();

        _sequence.NextAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult($"VJ-2526-{++_seqCounter:000000}"));

        _sut = new AccountingService(_db, _errorLogger, _sequence, _tenant);
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqliteConnection.Dispose();
    }

    // ── ListAccountGroupsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ListAccountGroupsAsync_WhenGroupsExist_ReturnsAllNonDeleted()
    {
        _db.Set<AccountGroup>().Add(new AccountGroup
        {
            ShopId = 1L, Name = "Assets", Code = "ASSETS",
            Nature = AccountNature.Asset, IsSystem = true, CreatedAtUtc = DateTime.UtcNow
        });
        _db.Set<AccountGroup>().Add(new AccountGroup
        {
            ShopId = 1L, Name = "Deleted", Code = "DEL",
            Nature = AccountNature.Expense, IsDeleted = true, CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _sut.ListAccountGroupsAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Assets");
    }

    // ── CreateAccountAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAccountAsync_ValidDto_ReturnsSuccessWithId()
    {
        var group = SeedAccountGroup();
        var dto   = new CreateAccountDto("Cash", "1010", group.Id, 0, DebitCredit.Debit);

        var result = await _sut.CreateAccountAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAccountAsync_DuplicateCode_ReturnsConflict()
    {
        var group = SeedAccountGroup();
        _db.Set<Account>().Add(new Account
        {
            ShopId = 1L, Code = "1010", Name = "Cash", AccountGroupId = group.Id,
            OpeningBalanceType = DebitCredit.Debit, IsActive = true, CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var dto    = new CreateAccountDto("Cash Duplicate", "1010", group.Id, 0, DebitCredit.Debit);
        var result = await _sut.CreateAccountAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Shared.Messages.Errors.Accounting.AccountCodeExists);
    }

    // ── CreateVoucherAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateVoucherAsync_BalancedEntries_ReturnsSuccessWithId()
    {
        var (debitAcc, creditAcc) = SeedTwoAccounts();
        var dto = new CreateVoucherDto(
            DateTime.Today, VoucherType.Journal, "Test",
            null, null,
            [
                new(debitAcc.Id,  DebitCredit.Debit,  1000m, null),
                new(creditAcc.Id, DebitCredit.Credit, 1000m, null),
            ]);

        var result = await _sut.CreateVoucherAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateVoucherAsync_ImbalancedEntries_ReturnsConflict()
    {
        var (debitAcc, creditAcc) = SeedTwoAccounts();
        var dto = new CreateVoucherDto(
            DateTime.Today, VoucherType.Journal, null, null, null,
            [
                new(debitAcc.Id,  DebitCredit.Debit,  1000m, null),
                new(creditAcc.Id, DebitCredit.Credit,  900m, null),
            ]);

        var result = await _sut.CreateVoucherAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Shared.Messages.Errors.Accounting.VoucherImbalanced);
    }

    // ── PostVoucherAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task PostVoucherAsync_DraftVoucher_TransitionsToPosted()
    {
        var voucher = SeedDraftVoucher();

        var result = await _sut.PostVoucherAsync(voucher.Id);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Set<Voucher>().FindAsync(voucher.Id);
        updated!.Status.Should().Be(VoucherStatus.Posted);
        updated.IsPosted.Should().BeTrue();
    }

    [Fact]
    public async Task PostVoucherAsync_AlreadyPosted_ReturnsConflict()
    {
        var voucher = SeedDraftVoucher();
        voucher.Status = VoucherStatus.Posted;
        voucher.IsPosted = true;
        await _db.SaveChangesAsync();

        var result = await _sut.PostVoucherAsync(voucher.Id);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Shared.Messages.Errors.Accounting.VoucherAlreadyPosted);
    }

    [Fact]
    public async Task PostVoucherAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.PostVoucherAsync(99999L);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Shared.Messages.Errors.Accounting.VoucherNotFound);
    }

    // ── ReverseVoucherAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ReverseVoucherAsync_PostedVoucher_CreatesReversalAndMarksReversed()
    {
        var voucher = SeedDraftVoucher();
        voucher.Status = VoucherStatus.Posted;
        voucher.IsPosted = true;
        await _db.SaveChangesAsync();

        var result = await _sut.ReverseVoucherAsync(voucher.Id, "Test reversal");

        result.IsSuccess.Should().BeTrue();
        var original = await _db.Set<Voucher>().FindAsync(voucher.Id);
        original!.Status.Should().Be(VoucherStatus.Reversed);
    }

    [Fact]
    public async Task ReverseVoucherAsync_DraftVoucher_ReturnsConflict()
    {
        var voucher = SeedDraftVoucher();

        var result = await _sut.ReverseVoucherAsync(voucher.Id, "reason");

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Shared.Messages.Errors.Accounting.VoucherNotPosted);
    }

    // ── CreateFinancialYearAsync ──────────────────────────────────────────────

    [Fact]
    public async Task CreateFinancialYearAsync_NewYear_ReturnsSuccess()
    {
        var result = await _sut.CreateFinancialYearAsync(new CreateFinancialYearDto(2026));

        result.IsSuccess.Should().BeTrue();
        var fy = await _db.Set<FinancialYear>().FindAsync(result.Value);
        fy!.StartYear.Should().Be(2026);
        fy.StartDate.Should().Be(new DateTime(2026, 4, 1));
    }

    [Fact]
    public async Task CreateFinancialYearAsync_DuplicateYear_ReturnsConflict()
    {
        _db.Set<FinancialYear>().Add(new FinancialYear
        {
            ShopId = 1L, StartYear = 2026,
            StartDate = new DateTime(2026, 4, 1), EndDate = new DateTime(2027, 3, 31),
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _sut.CreateFinancialYearAsync(new CreateFinancialYearDto(2026));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Shared.Messages.Errors.Accounting.FinancialYearExists);
    }

    // ── CloseFinancialYearAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CloseFinancialYearAsync_OpenFyNoOpenVouchers_ReturnsSuccess()
    {
        var fy = new FinancialYear
        {
            ShopId = 1L, StartYear = 2025,
            StartDate = new DateTime(2025, 4, 1), EndDate = new DateTime(2026, 3, 31),
            IsClosed = false, CreatedAtUtc = DateTime.UtcNow
        };
        _db.Set<FinancialYear>().Add(fy);
        await _db.SaveChangesAsync();

        var result = await _sut.CloseFinancialYearAsync(fy.Id);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CloseFinancialYearAsync_AlreadyClosed_ReturnsConflict()
    {
        var fy = new FinancialYear
        {
            ShopId = 1L, StartYear = 2025,
            StartDate = new DateTime(2025, 4, 1), EndDate = new DateTime(2026, 3, 31),
            IsClosed = true, ClosedAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow
        };
        _db.Set<FinancialYear>().Add(fy);
        await _db.SaveChangesAsync();

        var result = await _sut.CloseFinancialYearAsync(fy.Id);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Shared.Messages.Errors.Accounting.FinancialYearAlreadyClosed);
    }

    // ── UpdateAccountAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAccountAsync_SystemAccount_ReturnsConflict()
    {
        var group   = SeedAccountGroup();
        var account = new Account
        {
            ShopId = 1L, Code = "SYS", Name = "System Account",
            AccountGroupId = group.Id, OpeningBalanceType = DebitCredit.Debit,
            IsSystem = true, IsActive = true, CreatedAtUtc = DateTime.UtcNow
        };
        _db.Set<Account>().Add(account);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateAccountAsync(account.Id, new UpdateAccountDto("New Name", null, true));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(Shared.Messages.Errors.Accounting.SystemAccountReadOnly);
    }

    // ── Cancellation token propagation ────────────────────────────────────────

    [Fact]
    public async Task CreateAccountAsync_CancelledToken_ReturnsCancelledResult()
    {
        var group = SeedAccountGroup();
        var dto   = new CreateAccountDto("Cash", "2000", group.Id, 0, DebitCredit.Debit);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await _sut.CreateAccountAsync(dto, cts.Token);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Gone);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private AccountGroup SeedAccountGroup()
    {
        var g = new AccountGroup
        {
            ShopId = 1L, Name = "Assets", Code = "ASSETS",
            Nature = AccountNature.Asset, IsSystem = true, CreatedAtUtc = DateTime.UtcNow
        };
        _db.Set<AccountGroup>().Add(g);
        _db.SaveChanges();
        return g;
    }

    private (Account debit, Account credit) SeedTwoAccounts()
    {
        var group = SeedAccountGroup();
        var a1 = new Account
        {
            ShopId = 1L, Code = "A001", Name = "Cash",
            AccountGroupId = group.Id, OpeningBalanceType = DebitCredit.Debit,
            IsActive = true, CreatedAtUtc = DateTime.UtcNow
        };
        var a2 = new Account
        {
            ShopId = 1L, Code = "A002", Name = "Sales",
            AccountGroupId = group.Id, OpeningBalanceType = DebitCredit.Credit,
            IsActive = true, CreatedAtUtc = DateTime.UtcNow
        };
        _db.Set<Account>().AddRange(a1, a2);
        _db.SaveChanges();
        return (a1, a2);
    }

    private Voucher SeedDraftVoucher()
    {
        var (d, c) = SeedTwoAccounts();
        var v = new Voucher
        {
            ShopId = 1L, VoucherNumber = "VJ-TEST-000001",
            VoucherDate = DateTime.Today, VoucherType = VoucherType.Journal,
            Status = VoucherStatus.Draft, TotalDebit = 500m, TotalCredit = 500m,
            IsPosted = false, CreatedAtUtc = DateTime.UtcNow,
            Entries =
            [
                new() { ShopId = 1L, AccountId = d.Id, Type = DebitCredit.Debit,  Amount = 500m, CreatedAtUtc = DateTime.UtcNow },
                new() { ShopId = 1L, AccountId = c.Id, Type = DebitCredit.Credit, Amount = 500m, CreatedAtUtc = DateTime.UtcNow },
            ]
        };
        _db.Set<Voucher>().Add(v);
        _db.SaveChanges();
        return v;
    }
}

internal sealed class StubTenantContext(long shopId) : ITenantContext
{
    public long ShopId => shopId;
    public long CurrentUserId => 42L;
    public IReadOnlyList<string> CurrentUserRoles => [];
}

[Trait("Category", "Unit")]
public class BankReconciliationServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly ISequenceService _sequence  = Substitute.For<ISequenceService>();
    private readonly ITenantContext _tenant      = Substitute.For<ITenantContext>();
    private readonly BankReconciliationService _sut;
    private readonly SqliteConnection _sqliteConnection;
    private int _seqCounter;

    public BankReconciliationServiceTests()
    {
        var stubCtx = new StubTenantContext(shopId: 1L);
        _tenant.ShopId.Returns(1L);
        _tenant.CurrentUserId.Returns(42L);

        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        var auditInterceptor  = new AuditSaveChangesInterceptor(stubCtx);
        var tenantInterceptor = new TenantSaveChangesInterceptor(stubCtx);
        _db = new AccountingTenantDbContext(opts, stubCtx, auditInterceptor, tenantInterceptor);
        _db.Database.EnsureCreated();

        _sequence.NextAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => $"VJ-{++_seqCounter:D6}");

        _sut = new BankReconciliationService(_db, _errorLogger, _sequence, _tenant);
    }

    public void Dispose()
    {
        _db.Dispose();
        _sqliteConnection.Dispose();
    }

    [Fact]
    public async Task CreateBankStatementAsync_HappyPath_ReturnsId()
    {
        var acct = SeedAccount("CA-001");
        var ba = SeedBankAccount(acct.Id);
        var dto = new CreateBankStatementDto(ba.Id, DateTime.Today.AddDays(-30), DateTime.Today,
            1000m, 1500m);

        var result = await _sut.CreateBankStatementAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateBankStatementAsync_WhenBankAccountNotFound_ReturnsNotFound()
    {
        var dto = new CreateBankStatementDto(9999L, DateTime.Today.AddDays(-30), DateTime.Today,
            1000m, 1500m);

        var result = await _sut.CreateBankStatementAsync(dto);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ImportLinesAsync_HappyPath_ReturnsLineCount()
    {
        var stmt = SeedStatement();
        var lines = new List<ImportBankStatementLineDto>
        {
            new(DateTime.Today.AddDays(-10), "Payment received", "REF001", 500m, 0m),
            new(DateTime.Today.AddDays(-5),  "Expense paid",     "REF002", 0m, 200m),
        };

        var result = await _sut.ImportLinesAsync(stmt.Id, lines);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
    }

    [Fact]
    public async Task ImportLinesAsync_WhenStatementCompleted_ReturnsConflict()
    {
        var stmt = SeedStatement(isCompleted: true);
        var lines = new List<ImportBankStatementLineDto>
        {
            new(DateTime.Today, "Test", null, 100m, 0m),
        };

        var result = await _sut.ImportLinesAsync(stmt.Id, lines);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ManualMatchLineAsync_HappyPath_SetsMatchedStatus()
    {
        var stmt = SeedStatement();
        var line = SeedLine(stmt.Id);
        var voucher = SeedVoucher();
        var dto = new ManualMatchLineDto(voucher.Id);

        var result = await _sut.ManualMatchLineAsync(line.Id, dto);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Set<BankStatementLine>().FindAsync(line.Id);
        updated!.MatchStatus.Should().Be(BankStatementLineStatus.Matched);
        updated.MatchedVoucherId.Should().Be(voucher.Id);
    }

    [Fact]
    public async Task ManualMatchLineAsync_WhenAlreadyMatched_ReturnsConflict()
    {
        var stmt = SeedStatement();
        var line = SeedLine(stmt.Id, BankStatementLineStatus.Matched);
        var voucher = SeedVoucher();

        var result = await _sut.ManualMatchLineAsync(line.Id, new ManualMatchLineDto(voucher.Id));

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task IgnoreLineAsync_HappyPath_SetsIgnoredStatus()
    {
        var stmt = SeedStatement();
        var line = SeedLine(stmt.Id);

        var result = await _sut.IgnoreLineAsync(line.Id);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Set<BankStatementLine>().FindAsync(line.Id);
        updated!.MatchStatus.Should().Be(BankStatementLineStatus.Ignored);
    }

    [Fact]
    public async Task CompleteReconciliationAsync_WhenUnmatchedLinesExist_ReturnsConflict()
    {
        var stmt = SeedStatement();
        SeedLine(stmt.Id, BankStatementLineStatus.Unmatched);

        var result = await _sut.CompleteReconciliationAsync(stmt.Id);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CompleteReconciliationAsync_WhenAllLinesMatched_Succeeds()
    {
        var stmt = SeedStatement();
        SeedLine(stmt.Id, BankStatementLineStatus.Matched);
        SeedLine(stmt.Id, BankStatementLineStatus.Ignored);

        var result = await _sut.CompleteReconciliationAsync(stmt.Id);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Set<BankStatement>().FindAsync(stmt.Id);
        updated!.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task CreateReconciliationRuleAsync_HappyPath_ReturnsId()
    {
        var acct = SeedAccount("BANK-001");
        var dto = new CreateReconciliationRuleDto("Bank Fee", "bank charge", acct.Id, VoucherType.Journal);

        var result = await _sut.CreateReconciliationRuleAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ToggleReconciliationRuleAsync_TogglesActiveState()
    {
        var acct = SeedAccount("BANK-002");
        var rule = new ReconciliationRule
        {
            ShopId = 1L, Name = "Test Rule", PatternContains = "test",
            AccountId = acct.Id, VoucherType = VoucherType.Journal,
            IsActive = true, CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Set<ReconciliationRule>().Add(rule);
        await _db.SaveChangesAsync();

        var result = await _sut.ToggleReconciliationRuleAsync(rule.Id);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Set<ReconciliationRule>().FindAsync(rule.Id);
        updated!.IsActive.Should().BeFalse();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Account SeedAccount(string code)
    {
        var group = new AccountGroup
        {
            ShopId = 1L, Name = "Assets", Code = "ASSETS",
            Nature = AccountNature.Asset, SortOrder = 1,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Set<AccountGroup>().Add(group);
        _db.SaveChanges();

        var acct = new Account
        {
            ShopId = 1L, Name = code, Code = code, AccountGroupId = group.Id,
            OpeningBalance = 0m, OpeningBalanceType = DebitCredit.Debit,
            IsActive = true, CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Set<Account>().Add(acct);
        _db.SaveChanges();
        return acct;
    }

    private BankAccount SeedBankAccount(long accountId)
    {
        var ba = new BankAccount
        {
            ShopId = 1L, AccountId = accountId, BankName = "Test Bank",
            AccountNumber = $"ACC{accountId:D10}", IfscCode = "TEST0000001",
            BranchName = "Main Branch", AccountHolderName = "Test Shop",
            IsActive = true, CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Set<BankAccount>().Add(ba);
        _db.SaveChanges();
        return ba;
    }

    private BankStatement SeedStatement(bool isCompleted = false)
    {
        var acct = SeedAccount($"BA{_seqCounter++:D3}");
        var ba = SeedBankAccount(acct.Id);
        var stmt = new BankStatement
        {
            ShopId = 1L, BankAccountId = ba.Id,
            PeriodStart = DateTime.Today.AddDays(-30),
            PeriodEnd = DateTime.Today,
            OpeningBalance = 1000m, ClosingBalance = 1500m,
            IsCompleted = isCompleted,
            CompletedAtUtc = isCompleted ? DateTime.UtcNow : null,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Set<BankStatement>().Add(stmt);
        _db.SaveChanges();
        return stmt;
    }

    private BankStatementLine SeedLine(long statementId,
        BankStatementLineStatus status = BankStatementLineStatus.Unmatched)
    {
        var line = new BankStatementLine
        {
            BankStatementId = statementId,
            TransactionDate = DateTime.Today.AddDays(-5),
            Description = "Test transaction",
            CreditAmount = 100m, DebitAmount = 0m,
            MatchStatus = status,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Set<BankStatementLine>().Add(line);
        _db.SaveChanges();
        return line;
    }

    private Voucher SeedVoucher()
    {
        var group = new AccountGroup
        {
            ShopId = 1L, Name = "TestG", Code = $"G{_seqCounter++:D3}",
            Nature = AccountNature.Asset, SortOrder = 1, CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Set<AccountGroup>().Add(group);
        _db.SaveChanges();

        var d = new Account { ShopId = 1L, Name = "D", Code = $"D{_seqCounter++:D3}", AccountGroupId = group.Id, OpeningBalance = 0m, OpeningBalanceType = DebitCredit.Debit, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        var c = new Account { ShopId = 1L, Name = "C", Code = $"C{_seqCounter++:D3}", AccountGroupId = group.Id, OpeningBalance = 0m, OpeningBalanceType = DebitCredit.Credit, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        _db.Set<Account>().AddRange(d, c);
        _db.SaveChanges();

        var v = new Voucher
        {
            ShopId = 1L, VoucherNumber = $"VJ-{_seqCounter++:D6}",
            VoucherDate = DateTime.Today, VoucherType = VoucherType.Journal,
            Status = VoucherStatus.Posted, TotalDebit = 500m, TotalCredit = 500m,
            IsPosted = true, CreatedAtUtc = DateTime.UtcNow,
            Entries =
            [
                new() { ShopId = 1L, AccountId = d.Id, Type = DebitCredit.Debit,  Amount = 500m, CreatedAtUtc = DateTime.UtcNow },
                new() { ShopId = 1L, AccountId = c.Id, Type = DebitCredit.Credit, Amount = 500m, CreatedAtUtc = DateTime.UtcNow },
            ]
        };
        _db.Set<Voucher>().Add(v);
        _db.SaveChanges();
        return v;
    }
}
