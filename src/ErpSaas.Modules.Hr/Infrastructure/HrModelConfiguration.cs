using ErpSaas.Modules.Hr.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Hr.Infrastructure;

public static class HrModelConfiguration
{
    public const string Schema = "hr";

    public static void Configure(ModelBuilder b)
    {
        b.Entity<Employee>(e =>
        {
            e.ToTable("Employees", schema: Schema);
            e.Property(x => x.EmployeeCode).HasMaxLength(30).IsRequired();
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Designation).HasMaxLength(100).IsRequired();
            e.Property(x => x.Department).HasMaxLength(100).IsRequired();
            e.Property(x => x.BasicSalary).HasPrecision(18, 2);
            e.Property(x => x.BankAccountNumber).HasMaxLength(30);
            e.Property(x => x.BankIfsc).HasMaxLength(11);
            e.Property(x => x.PanNumber).HasMaxLength(10);
            e.Property(x => x.AadhaarNumberEncrypted).HasMaxLength(500);
            e.HasIndex(x => new { x.ShopId, x.EmployeeCode }).IsUnique();
            e.HasMany(x => x.Documents).WithOne(x => x.Employee).HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.SalaryComponents).WithOne(x => x.Employee).HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<EmployeeDocument>(e =>
        {
            e.ToTable("EmployeeDocuments", schema: Schema);
            e.Property(x => x.DocumentType).HasMaxLength(50).IsRequired();
        });

        b.Entity<SalaryComponent>(e =>
        {
            e.ToTable("SalaryComponents", schema: Schema);
            e.Property(x => x.ComponentCode).HasMaxLength(30).IsRequired();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
        });

        b.Entity<Attendance>(e =>
        {
            e.ToTable("Attendance", schema: Schema);
            e.Property(x => x.StatusCode).HasMaxLength(20).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasIndex(x => new { x.ShopId, x.EmployeeId, x.Date }).IsUnique();
            e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<LeaveType>(e =>
        {
            e.ToTable("LeaveTypes", schema: Schema);
            e.Property(x => x.Code).HasMaxLength(10).IsRequired();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.Code }).IsUnique();
        });

        b.Entity<LeaveRequest>(e =>
        {
            e.ToTable("LeaveRequests", schema: Schema);
            e.Property(x => x.Days).HasPrecision(5, 1);
            e.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.LeaveType).WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<LeaveBalance>(e =>
        {
            e.ToTable("LeaveBalances", schema: Schema);
            e.Property(x => x.EntitledDays).HasPrecision(5, 1);
            e.Property(x => x.UsedDays).HasPrecision(5, 1);
            e.Property(x => x.CarryForwardDays).HasPrecision(5, 1);
            e.HasIndex(x => new { x.ShopId, x.EmployeeId, x.LeaveTypeId, x.Year }).IsUnique();
            e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.LeaveType).WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Payroll>(e =>
        {
            e.ToTable("Payrolls", schema: Schema);
            e.Property(x => x.GrossEarnings).HasPrecision(18, 2);
            e.Property(x => x.TotalDeductions).HasPrecision(18, 2);
            e.Property(x => x.NetPay).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.DetailsJson).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.EmployeeId, x.Year, x.Month }).IsUnique();
            e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<StaffActivity>(e =>
        {
            e.ToTable("StaffActivities", schema: Schema);
            e.Property(x => x.ActivityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.RelatedEntityType).HasMaxLength(100);
            e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
