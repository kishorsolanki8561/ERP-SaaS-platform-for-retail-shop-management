using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Hr.Infrastructure;
using ErpSaas.Modules.Hr.Seeds;
using ErpSaas.Modules.Hr.Services;
using ErpSaas.Shared.Catalog;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Hr.Extensions;

public static class HrServiceExtensions
{
    public static IServiceCollection AddHrModule(this IServiceCollection services)
    {
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<IStaffActivityService, StaffActivityService>();
        services.AddScoped<IDataSeeder, HrSystemSeeder>();
        services.AddSingleton<IEntityModelConfigurator, HrConfigurator>();
        services.AddSingleton(new ServiceDescriptorEntry("HR", "Staff management, attendance, leave and payroll", "1.0"));
        return services;
    }
}

internal sealed class HrConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
        => HrModelConfiguration.Configure(modelBuilder);
}
