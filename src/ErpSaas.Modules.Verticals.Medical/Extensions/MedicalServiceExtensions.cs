using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Verticals.Medical.Infrastructure;
using ErpSaas.Modules.Verticals.Medical.Seeds;
using ErpSaas.Modules.Verticals.Medical.Services;
using ErpSaas.Shared.Catalog;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Verticals.Medical.Extensions;

public static class MedicalServiceExtensions
{
    public static IServiceCollection AddMedicalModule(this IServiceCollection services)
    {
        services.AddScoped<IMedicalInventoryService, MedicalInventoryService>();
        services.AddScoped<IDataSeeder, MedicalSystemSeeder>();
        services.AddSingleton<IEntityModelConfigurator, MedicalConfigurator>();
        services.AddSingleton(new ServiceDescriptorEntry("Medical", "Medical vertical — batch/expiry tracking, schedule-H, prescriptions", "1.0"));
        return services;
    }
}

internal sealed class MedicalConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
        => MedicalModelConfiguration.Configure(modelBuilder);
}
