using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Crm.Configuration;
using ErpSaas.Modules.Crm.Seeds;
using ErpSaas.Modules.Crm.Services;
using ErpSaas.Shared.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Crm.Extensions;

public static class CrmServiceExtensions
{
    public static IServiceCollection AddCrmModule(this IServiceCollection services)
    {
        services.AddScoped<ICrmService, CrmService>();
        services.AddDataSeeder<CrmSystemSeeder>();
        services.AddSingleton(new ServiceDescriptorEntry("Crm", "Customer relationship management", "1.0"));
        services.AddSingleton<IEntityModelConfigurator, CrmModelConfigurator>();
        return services;
    }
}

internal sealed class CrmModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CustomerGroupEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerAddressEntityTypeConfiguration());
    }
}
