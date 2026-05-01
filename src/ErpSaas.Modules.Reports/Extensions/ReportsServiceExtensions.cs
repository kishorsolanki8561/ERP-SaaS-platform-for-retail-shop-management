using ErpSaas.Modules.Reports.Seeds;
using ErpSaas.Modules.Reports.Services;
using ErpSaas.Shared.Seeds;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Reports.Extensions;

public static class ReportsServiceExtensions
{
    public static IServiceCollection AddReportsModule(this IServiceCollection services)
    {
        services.AddScoped<IReportQueryRepository, ReportQueryRepository>();
        services.AddScoped<IReportBuilderService, ReportBuilderService>();
        services.AddScoped<IDataSeeder, ReportsSystemSeeder>();
        return services;
    }
}
