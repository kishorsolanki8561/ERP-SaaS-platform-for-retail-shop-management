# HR Module (§6.9)

Staff management, attendance, leave management and payroll for shops.

## Entities (TenantDB — `hr` schema)

| Entity | Purpose |
|---|---|
| `Employee` | Staff registry with salary, bank and identity details |
| `EmployeeDocument` | Documents attached to an employee (Aadhaar, PAN, offer letter) |
| `SalaryComponent` | Per-employee salary structure (Basic, HRA, PF deduction, etc.) |
| `Attendance` | Daily check-in/out with geofence support |
| `LeaveType` | Configurable leave types per shop (CL, SL, EL) |
| `LeaveRequest` | Leave requests with Pending → Approved/Rejected flow |
| `LeaveBalance` | Per-employee, per-type annual leave balance |
| `Payroll` | Monthly payroll: Draft → Approved → Paid |
| `StaffActivity` | Lightweight activity feed (auto-tracked from other modules) |

## Services

| Service | Responsibility |
|---|---|
| `IEmployeeService` | Onboarding, update, document upload |
| `IAttendanceService` | Check-in/out, bulk marking |
| `ILeaveService` | Leave types, request/approve/reject, balance queries |
| `IPayrollService` | Generate (attendance × components), approve, pay, payslip |
| `IStaffActivityService` | Track and list staff activities |

## Endpoints

| Method | Route | Permission |
|---|---|---|
| GET | `/api/employees` | `HR.View` |
| POST | `/api/employees` | `HR.Manage` |
| PATCH | `/api/employees/{id}` | `HR.Manage` |
| GET | `/api/employees/{id}` | `HR.View` |
| POST | `/api/employees/{id}/documents` | `HR.Manage` |
| GET | `/api/employees/{id}/documents` | `HR.View` |
| GET | `/api/attendance?year=&month=` | `HR.View` |
| POST | `/api/attendance/check-in` | `HR.Attendance` |
| POST | `/api/attendance/check-out` | `HR.Attendance` |
| POST | `/api/attendance/bulk` | `HR.Manage` |
| GET | `/api/leave-types` | `HR.View` |
| POST | `/api/leave-types` | `HR.Manage` |
| GET | `/api/leave-requests` | `HR.View` |
| POST | `/api/leave-requests` | `HR.Attendance` |
| POST | `/api/leave-requests/{id}/approve` | `HR.Manage` |
| POST | `/api/leave-requests/{id}/reject` | `HR.Manage` |
| GET | `/api/leave-balances/{employeeId}?year=` | `HR.View` |
| GET | `/api/payroll?year=&month=` | `HR.Payroll` |
| POST | `/api/payroll/generate` | `HR.Payroll` |
| POST | `/api/payroll/{id}/approve` | `HR.Payroll` |
| POST | `/api/payroll/{id}/pay` | `HR.Payroll` |
| GET | `/api/payroll/{id}/payslip` | `HR.Payroll` |
| GET | `/api/staff-activities` | `HR.View` |

## Feature Flags

- `hr.payroll` — Enabled on Growth and Enterprise plans

## DDL Keys

- `ATTENDANCE_STATUS` — Present, Absent, HalfDay, Leave, Holiday
- `SALARY_COMPONENT` — Basic, HRA, DA, PF, Bonus, Deduction
