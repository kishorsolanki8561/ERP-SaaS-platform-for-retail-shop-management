using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Hr.Entities;

[Auditable("HR.Employee")]
public class Employee : TenantEntity
{
    [AuditField("Employee Code")]
    public string EmployeeCode { get; set; } = default!;

    public long? LinkedUserId { get; set; }

    [AuditField("First Name")]
    public string FirstName { get; set; } = default!;

    [AuditField("Last Name")]
    public string LastName { get; set; } = default!;

    [AuditField("Phone")]
    public string? Phone { get; set; }

    [AuditField("Email")]
    public string? Email { get; set; }

    [AuditField("Date of Birth")]
    public DateTime DateOfBirth { get; set; }

    [AuditField("Date of Joining")]
    public DateTime DateOfJoining { get; set; }

    [AuditField("Date of Leaving")]
    public DateTime? DateOfLeaving { get; set; }

    [AuditField("Designation")]
    public string Designation { get; set; } = default!;

    [AuditField("Department")]
    public string Department { get; set; } = default!;

    [AuditField("Basic Salary")]
    public decimal BasicSalary { get; set; }

    [AuditField("Bank Account")]
    public string? BankAccountNumber { get; set; }

    [AuditField("Bank IFSC")]
    public string? BankIfsc { get; set; }

    [AuditField("PAN Number")]
    public string? PanNumber { get; set; }

    // AadhaarNumberEncrypted intentionally excluded from audit display (sensitive)
    public string? AadhaarNumberEncrypted { get; set; }

    [AuditField("Active")]
    public bool IsActive { get; set; } = true;

    public ICollection<EmployeeDocument> Documents { get; set; } = [];
    public ICollection<SalaryComponent> SalaryComponents { get; set; } = [];
}
