using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.ServiceJobs.Infrastructure;
using ErpSaas.Modules.ServiceJobs.Seeds;
using ErpSaas.Modules.ServiceJobs.Services;
using ErpSaas.Shared.Catalog;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.ServiceJobs.Extensions;

public static class ServiceJobServiceExtensions
{
    public static IServiceCollection AddServiceJobsModule(this IServiceCollection services)
    {
        services.AddScoped<IServiceJobService, ServiceJobService>();
        services.AddScoped<IDataSeeder, ServiceJobSystemSeeder>();
        services.AddSingleton<IEntityModelConfigurator, ServiceJobConfigurator>();
        services.AddSingleton(new ServiceDescriptorEntry("ServiceJobs", "Service and repair job tracking", "1.0"));
        return services;
    }
}

internal sealed class ServiceJobConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
        => ServiceJobModelConfiguration.Configure(modelBuilder);
}
