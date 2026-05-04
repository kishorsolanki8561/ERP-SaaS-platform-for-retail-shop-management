using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Modules.Hr.Entities;
using ErpSaas.Modules.Hr.Enums;
using ErpSaas.Modules.Hr.Infrastructure;
using ErpSaas.Modules.Hr.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Hr;

// ── Test-local DbContext ──────────────────────────────────────────────────────

internal sealed class HrTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        HrModelConfiguration.Configure(modelBuilder);
    }
}

internal sealed class HrStubTenantContext(long shopId) : ITenantContext
{
    public long ShopId => shopId;
    public long CurrentUserId => 1L;
    public IReadOnlyList<string> CurrentUserRoles => [];
}

// ── EmployeeService tests ─────────────────────────────────────────────────────

[Trait("Category", "Unit")]
public class EmployeeServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly ISequenceService _sequence = Substitute.For<ISequenceService>();
    private readonly EmployeeService _sut;
    private readonly SqliteConnection _connection;
    private const long ShopId = 1L;

    public EmployeeServiceTests()
    {
        var stubCtx = new HrStubTenantContext(ShopId);
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_connection).Options;

        _db = new HrTenantDbContext(opts, stubCtx,
            new AuditSaveChangesInterceptor(stubCtx),
            new TenantSaveChangesInterceptor(stubCtx));
        _db.Database.EnsureCreated();

        _sequence.NextAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns("EMP0001");

        _sut = new EmployeeService(_db, _errorLogger, _sequence, stubCtx);
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsSuccessWithId()
    {
        var result = await _sut.CreateAsync(MakeDto());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_SetsEmployeeCode_FromSequence()
    {
        var result = await _sut.CreateAsync(MakeDto());
        var emp = await _db.Set<Employee>().FindAsync(result.Value);
        emp!.EmployeeCode.Should().Be("EMP0001");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingEmployee_ReturnsDto()
    {
        var created = await _sut.CreateAsync(MakeDto());
        var result = await _sut.GetByIdAsync(created.Value);
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Ravi");
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(9999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ValidPatch_UpdatesFields()
    {
        var created = await _sut.CreateAsync(MakeDto());
        var patch = new UpdateEmployeeDto("9988776655", null, "Senior Dev", null, null, null, null, null, null, null);

        var result = await _sut.UpdateAsync(created.Value, patch);

        result.IsSuccess.Should().BeTrue();
        var updated = await _sut.GetByIdAsync(created.Value);
        updated!.Designation.Should().Be("Senior Dev");
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsNotFound()
    {
        var result = await _sut.UpdateAsync(9999, new UpdateEmployeeDto(null, null, null, null, null, null, null, null, null, null));
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ListAsync_ReturnsAllEmployees()
    {
        await _sut.CreateAsync(MakeDto());
        var list = await _sut.ListAsync();
        list.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddDocumentAsync_ValidEmployee_ReturnsSuccess()
    {
        var emp = await _sut.CreateAsync(MakeDto());
        var result = await _sut.AddDocumentAsync(emp.Value, new AddDocumentDto("PAN", 42, null));
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AddDocumentAsync_UnknownEmployee_ReturnsNotFound()
    {
        var result = await _sut.AddDocumentAsync(9999, new AddDocumentDto("PAN", 42, null));
        result.IsSuccess.Should().BeFalse();
    }

    private static CreateEmployeeDto MakeDto() => new(
        "Ravi", "Kumar", "9876543210", "ravi@test.com",
        new DateTime(1990, 1, 1), new DateTime(2023, 6, 1),
        "Developer", "Engineering", 50000m,
        null, null, null, null);
}

// ── AttendanceService tests ───────────────────────────────────────────────────

[Trait("Category", "Unit")]
public class AttendanceServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly AttendanceService _sut;
    private readonly SqliteConnection _connection;
    private readonly HrStubTenantContext _stubCtx;
    private const long ShopId = 1L;
    private const long EmpId = 1L;

    public AttendanceServiceTests()
    {
        _stubCtx = new HrStubTenantContext(ShopId);
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_connection).Options;

        _db = new HrTenantDbContext(opts, _stubCtx,
            new AuditSaveChangesInterceptor(_stubCtx),
            new TenantSaveChangesInterceptor(_stubCtx));
        _db.Database.EnsureCreated();

        SeedEmployee();
        _sut = new AttendanceService(_db, _errorLogger, _stubCtx);
    }

    private void SeedEmployee()
    {
        _db.Set<Employee>().Add(new Employee
        {
            Id = EmpId, ShopId = ShopId, EmployeeCode = "EMP0001",
            FirstName = "Ravi", LastName = "Kumar",
            DateOfBirth = new DateTime(1990, 1, 1),
            DateOfJoining = new DateTime(2023, 1, 1),
            Designation = "Dev", Department = "Eng",
            BasicSalary = 50000m, IsActive = true, CreatedAtUtc = DateTime.UtcNow,
        });
        _db.SaveChanges();
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    [Fact]
    public async Task CheckInAsync_FirstCheckIn_ReturnsSuccess()
    {
        var result = await _sut.CheckInAsync(new CheckInDto(EmpId, null, null, null));
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CheckInAsync_DuplicateSameDay_ReturnsConflict()
    {
        await _sut.CheckInAsync(new CheckInDto(EmpId, null, null, null));
        var result = await _sut.CheckInAsync(new CheckInDto(EmpId, null, null, null));
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CheckOutAsync_AfterCheckIn_ReturnsSuccess()
    {
        await _sut.CheckInAsync(new CheckInDto(EmpId, null, null, null));
        var result = await _sut.CheckOutAsync(EmpId);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CheckOutAsync_WithoutCheckIn_ReturnsNotFound()
    {
        var result = await _sut.CheckOutAsync(EmpId);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task BulkMarkAsync_MultipleEmployees_AllMarked()
    {
        var dto = new BulkAttendanceDto(DateTime.Today,
        [
            new BulkAttendanceEntry(EmpId, "Present", null)
        ]);
        var result = await _sut.BulkMarkAsync(dto);
        result.IsSuccess.Should().BeTrue();
    }
}

// ── LeaveService tests ────────────────────────────────────────────────────────

[Trait("Category", "Unit")]
public class LeaveServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly LeaveService _sut;
    private readonly SqliteConnection _connection;
    private readonly HrStubTenantContext _stubCtx;
    private const long ShopId = 1L;
    private const long EmpId = 1L;

    public LeaveServiceTests()
    {
        _stubCtx = new HrStubTenantContext(ShopId);
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_connection).Options;

        _db = new HrTenantDbContext(opts, _stubCtx,
            new AuditSaveChangesInterceptor(_stubCtx),
            new TenantSaveChangesInterceptor(_stubCtx));
        _db.Database.EnsureCreated();

        SeedEmployee();
        _sut = new LeaveService(_db, _errorLogger, _stubCtx);
    }

    private void SeedEmployee()
    {
        _db.Set<Employee>().Add(new Employee
        {
            Id = EmpId, ShopId = ShopId, EmployeeCode = "EMP0001",
            FirstName = "Ravi", LastName = "Kumar",
            DateOfBirth = new DateTime(1990, 1, 1),
            DateOfJoining = new DateTime(2023, 1, 1),
            Designation = "Dev", Department = "Eng",
            BasicSalary = 50000m, IsActive = true, CreatedAtUtc = DateTime.UtcNow,
        });
        _db.SaveChanges();
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    [Fact]
    public async Task CreateLeaveTypeAsync_ValidDto_ReturnsSuccess()
    {
        var result = await _sut.CreateLeaveTypeAsync(new CreateLeaveTypeDto("CL", "Casual Leave", 12, true, false));
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CreateLeaveTypeAsync_DuplicateCode_ReturnsConflict()
    {
        await _sut.CreateLeaveTypeAsync(new CreateLeaveTypeDto("CL", "Casual Leave", 12, true, false));
        var result = await _sut.CreateLeaveTypeAsync(new CreateLeaveTypeDto("CL", "Casual Leave 2", 6, true, false));
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RequestLeaveAsync_ValidRequest_ReturnsSuccess()
    {
        var lt = await _sut.CreateLeaveTypeAsync(new CreateLeaveTypeDto("SL", "Sick Leave", 10, true, false));
        var result = await _sut.RequestLeaveAsync(new CreateLeaveRequestDto(
            EmpId, lt.Value,
            DateTime.Today, DateTime.Today.AddDays(1), 1m, "Fever"));
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ApproveAsync_PendingRequest_ApprovesAndUpdatesBalance()
    {
        var lt = await _sut.CreateLeaveTypeAsync(new CreateLeaveTypeDto("EL", "Earned Leave", 20, true, true));
        _db.Set<LeaveBalance>().Add(new LeaveBalance
        {
            ShopId = ShopId, EmployeeId = EmpId, LeaveTypeId = lt.Value,
            Year = DateTime.Today.Year, EntitledDays = 20, UsedDays = 0, CarryForwardDays = 0,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();

        var req = await _sut.RequestLeaveAsync(new CreateLeaveRequestDto(
            EmpId, lt.Value, DateTime.Today, DateTime.Today, 1m, "Test"));

        var result = await _sut.ApproveAsync(req.Value, approverUserId: 99L);
        result.IsSuccess.Should().BeTrue();

        var balances = await _sut.GetBalancesAsync(EmpId, DateTime.Today.Year);
        balances.First().UsedDays.Should().Be(1m);
    }

    [Fact]
    public async Task RejectAsync_PendingRequest_Rejects()
    {
        var lt = await _sut.CreateLeaveTypeAsync(new CreateLeaveTypeDto("CL", "Casual Leave", 12, true, false));
        var req = await _sut.RequestLeaveAsync(new CreateLeaveRequestDto(
            EmpId, lt.Value, DateTime.Today, DateTime.Today, 1m, "Personal"));
        var result = await _sut.RejectAsync(req.Value);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ApproveAsync_AlreadyApproved_ReturnsConflict()
    {
        var lt = await _sut.CreateLeaveTypeAsync(new CreateLeaveTypeDto("CL", "Casual Leave", 12, true, false));
        var req = await _sut.RequestLeaveAsync(new CreateLeaveRequestDto(
            EmpId, lt.Value, DateTime.Today, DateTime.Today, 1m, "Test"));
        await _sut.ApproveAsync(req.Value, 99L);
        var result = await _sut.ApproveAsync(req.Value, 99L);
        result.IsSuccess.Should().BeFalse();
    }
}

// ── PayrollService tests ──────────────────────────────────────────────────────

[Trait("Category", "Unit")]
public class PayrollServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly PayrollService _sut;
    private readonly SqliteConnection _connection;
    private readonly HrStubTenantContext _stubCtx;
    private const long ShopId = 1L;
    private const long EmpId = 1L;

    public PayrollServiceTests()
    {
        _stubCtx = new HrStubTenantContext(ShopId);
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_connection).Options;

        _db = new HrTenantDbContext(opts, _stubCtx,
            new AuditSaveChangesInterceptor(_stubCtx),
            new TenantSaveChangesInterceptor(_stubCtx));
        _db.Database.EnsureCreated();

        SeedEmployeeWithComponents();
        _sut = new PayrollService(_db, _errorLogger, _stubCtx);
    }

    private void SeedEmployeeWithComponents()
    {
        var emp = new Employee
        {
            Id = EmpId, ShopId = ShopId, EmployeeCode = "EMP0001",
            FirstName = "Ravi", LastName = "Kumar",
            DateOfBirth = new DateTime(1990, 1, 1),
            DateOfJoining = new DateTime(2023, 1, 1),
            Designation = "Dev", Department = "Eng",
            BasicSalary = 50000m, IsActive = true, CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Set<Employee>().Add(emp);
        _db.SaveChanges();

        _db.Set<SalaryComponent>().AddRange(
            new SalaryComponent { ShopId = ShopId, EmployeeId = EmpId, ComponentCode = "Basic", Type = ComponentType.Earning, Amount = 50000m, IsRecurring = true, CreatedAtUtc = DateTime.UtcNow },
            new SalaryComponent { ShopId = ShopId, EmployeeId = EmpId, ComponentCode = "PF",    Type = ComponentType.Deduction, Amount = 1800m, IsRecurring = true, CreatedAtUtc = DateTime.UtcNow }
        );

        // Seed all 31 days of May 2026 as Present so gross is not pro-rated
        var daysInMay = DateTime.DaysInMonth(2026, 5);
        for (int d = 1; d <= daysInMay; d++)
        {
            _db.Set<Attendance>().Add(new Attendance
            {
                ShopId = ShopId, EmployeeId = EmpId,
                Date = new DateTime(2026, 5, d),
                StatusCode = "Present", CreatedAtUtc = DateTime.UtcNow,
            });
        }

        _db.SaveChanges();
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    [Fact]
    public async Task GenerateAsync_ValidMonth_ReturnsDraftPayroll()
    {
        var result = await _sut.GenerateAsync(new GeneratePayrollDto(EmpId, 2026, 5));
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateAsync_DuplicateMonth_ReturnsConflict()
    {
        await _sut.GenerateAsync(new GeneratePayrollDto(EmpId, 2026, 5));
        var result = await _sut.GenerateAsync(new GeneratePayrollDto(EmpId, 2026, 5));
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateAsync_CalculatesNetPay()
    {
        var result = await _sut.GenerateAsync(new GeneratePayrollDto(EmpId, 2026, 5));
        var payroll = await _db.Set<Payroll>().FindAsync(result.Value);
        payroll!.GrossEarnings.Should().Be(50000m);
        payroll.TotalDeductions.Should().Be(1800m);
        payroll.NetPay.Should().Be(48200m);
    }

    [Fact]
    public async Task ApproveAsync_DraftPayroll_ReturnsSuccess()
    {
        var gen = await _sut.GenerateAsync(new GeneratePayrollDto(EmpId, 2026, 5));
        var result = await _sut.ApproveAsync(gen.Value);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ApproveAsync_AlreadyApproved_ReturnsConflict()
    {
        var gen = await _sut.GenerateAsync(new GeneratePayrollDto(EmpId, 2026, 5));
        await _sut.ApproveAsync(gen.Value);
        var result = await _sut.ApproveAsync(gen.Value);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task PayAsync_ApprovedPayroll_ReturnsSuccess()
    {
        var gen = await _sut.GenerateAsync(new GeneratePayrollDto(EmpId, 2026, 5));
        await _sut.ApproveAsync(gen.Value);
        var result = await _sut.PayAsync(gen.Value);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task PayAsync_DraftPayroll_ReturnsConflict()
    {
        var gen = await _sut.GenerateAsync(new GeneratePayrollDto(EmpId, 2026, 5));
        var result = await _sut.PayAsync(gen.Value);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetPayslipAsync_ExistingPayroll_ReturnsPayslip()
    {
        var gen = await _sut.GenerateAsync(new GeneratePayrollDto(EmpId, 2026, 5));
        var payslip = await _sut.GetPayslipAsync(gen.Value);
        payslip.Should().NotBeNull();
        payslip!.NetPay.Should().Be(48200m);
    }
}
