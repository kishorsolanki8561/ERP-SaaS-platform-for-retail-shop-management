using ErpSaas.Modules.Hr.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Hr.Controllers;

[Route("api")]
[Authorize]
public sealed class HrController(
    IEmployeeService employeeService,
    IAttendanceService attendanceService,
    ILeaveService leaveService,
    IPayrollService payrollService,
    IStaffActivityService activityService) : BaseController
{
    // ── Employees ─────────────────────────────────────────────────────────────

    [HttpGet("employees")]
    [RequirePermission("HR.View")]
    public async Task<IActionResult> ListEmployees(CancellationToken ct = default)
        => Ok(await employeeService.ListAsync(ct));

    [HttpPost("employees")]
    [RequirePermission("HR.Manage")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto, CancellationToken ct = default)
        => Ok(await employeeService.CreateAsync(dto, ct));

    [HttpPatch("employees/{id:long}")]
    [RequirePermission("HR.Manage")]
    public async Task<IActionResult> UpdateEmployee(long id, [FromBody] UpdateEmployeeDto dto, CancellationToken ct = default)
        => Ok(await employeeService.UpdateAsync(id, dto, ct));

    [HttpGet("employees/{id:long}")]
    [RequirePermission("HR.View")]
    public async Task<IActionResult> GetEmployee(long id, CancellationToken ct = default)
    {
        var result = await employeeService.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("employees/{id:long}/documents")]
    [RequirePermission("HR.Manage")]
    public async Task<IActionResult> AddDocument(long id, [FromBody] AddDocumentDto dto, CancellationToken ct = default)
        => Ok(await employeeService.AddDocumentAsync(id, dto, ct));

    [HttpGet("employees/{id:long}/documents")]
    [RequirePermission("HR.View")]
    public async Task<IActionResult> ListDocuments(long id, CancellationToken ct = default)
        => Ok(await employeeService.ListDocumentsAsync(id, ct));

    // ── Attendance ────────────────────────────────────────────────────────────

    [HttpGet("attendance")]
    [RequirePermission("HR.View")]
    public async Task<IActionResult> ListAttendance([FromQuery] int year, [FromQuery] int month, CancellationToken ct = default)
        => Ok(await attendanceService.ListAsync(year, month, ct));

    [HttpPost("attendance/check-in")]
    [RequirePermission("HR.Attendance")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInDto dto, CancellationToken ct = default)
        => Ok(await attendanceService.CheckInAsync(dto, ct));

    [HttpPost("attendance/check-out")]
    [RequirePermission("HR.Attendance")]
    public async Task<IActionResult> CheckOut([FromBody] long employeeId, CancellationToken ct = default)
        => Ok(await attendanceService.CheckOutAsync(employeeId, ct));

    [HttpPost("attendance/bulk")]
    [RequirePermission("HR.Manage")]
    public async Task<IActionResult> BulkMarkAttendance([FromBody] BulkAttendanceDto dto, CancellationToken ct = default)
        => Ok(await attendanceService.BulkMarkAsync(dto, ct));

    // ── Leave Types ───────────────────────────────────────────────────────────

    [HttpGet("leave-types")]
    [RequirePermission("HR.View")]
    public async Task<IActionResult> ListLeaveTypes(CancellationToken ct = default)
        => Ok(await leaveService.ListLeaveTypesAsync(ct));

    [HttpPost("leave-types")]
    [RequirePermission("HR.Manage")]
    public async Task<IActionResult> CreateLeaveType([FromBody] CreateLeaveTypeDto dto, CancellationToken ct = default)
        => Ok(await leaveService.CreateLeaveTypeAsync(dto, ct));

    // ── Leave Requests ────────────────────────────────────────────────────────

    [HttpGet("leave-requests")]
    [RequirePermission("HR.View")]
    public async Task<IActionResult> ListLeaveRequests(CancellationToken ct = default)
        => Ok(await leaveService.ListRequestsAsync(ct));

    [HttpPost("leave-requests")]
    [RequirePermission("HR.Attendance")]
    public async Task<IActionResult> RequestLeave([FromBody] CreateLeaveRequestDto dto, CancellationToken ct = default)
        => Ok(await leaveService.RequestLeaveAsync(dto, ct));

    [HttpPost("leave-requests/{id:long}/approve")]
    [RequirePermission("HR.Manage")]
    public async Task<IActionResult> ApproveLeave(long id, [FromQuery] long approverUserId, CancellationToken ct = default)
        => Ok(await leaveService.ApproveAsync(id, approverUserId, ct));

    [HttpPost("leave-requests/{id:long}/reject")]
    [RequirePermission("HR.Manage")]
    public async Task<IActionResult> RejectLeave(long id, CancellationToken ct = default)
        => Ok(await leaveService.RejectAsync(id, ct));

    [HttpGet("leave-balances/{employeeId:long}")]
    [RequirePermission("HR.View")]
    public async Task<IActionResult> GetLeaveBalances(long employeeId, [FromQuery] int year, CancellationToken ct = default)
        => Ok(await leaveService.GetBalancesAsync(employeeId, year, ct));

    // ── Payroll ───────────────────────────────────────────────────────────────

    [HttpGet("payroll")]
    [RequirePermission("HR.Payroll")]
    public async Task<IActionResult> ListPayroll([FromQuery] int year, [FromQuery] int month, CancellationToken ct = default)
        => Ok(await payrollService.ListAsync(year, month, ct));

    [HttpPost("payroll/generate")]
    [RequirePermission("HR.Payroll")]
    public async Task<IActionResult> GeneratePayroll([FromBody] GeneratePayrollDto dto, CancellationToken ct = default)
        => Ok(await payrollService.GenerateAsync(dto, ct));

    [HttpPost("payroll/{id:long}/approve")]
    [RequirePermission("HR.Payroll")]
    public async Task<IActionResult> ApprovePayroll(long id, CancellationToken ct = default)
        => Ok(await payrollService.ApproveAsync(id, ct));

    [HttpPost("payroll/{id:long}/pay")]
    [RequirePermission("HR.Payroll")]
    public async Task<IActionResult> PayPayroll(long id, CancellationToken ct = default)
        => Ok(await payrollService.PayAsync(id, ct));

    [HttpGet("payroll/{id:long}/payslip")]
    [RequirePermission("HR.Payroll")]
    public async Task<IActionResult> GetPayslip(long id, CancellationToken ct = default)
    {
        var result = await payrollService.GetPayslipAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // ── Staff Activities ──────────────────────────────────────────────────────

    [HttpGet("staff-activities")]
    [RequirePermission("HR.View")]
    public async Task<IActionResult> ListActivities([FromQuery] long? employeeId = null, CancellationToken ct = default)
        => Ok(await activityService.ListAsync(employeeId, ct));
}
