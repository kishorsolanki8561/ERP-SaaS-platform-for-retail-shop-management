using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Verticals.Infrastructure;
using ErpSaas.Modules.Verticals.Seeds;
using ErpSaas.Modules.Verticals.Services;
using ErpSaas.Shared.Catalog;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Verticals.Extensions;

public static class VerticalServiceExtensions
{
    public static IServiceCollection AddVerticalsModule(this IServiceCollection services)
    {
        services.AddScoped<IVerticalPackService, VerticalPackService>();
        services.AddScoped<IDataSeeder, VerticalSystemSeeder>();
        services.AddSingleton<IEntityModelConfigurator, VerticalConfigurator>();
        services.AddSingleton(new ServiceDescriptorEntry("Verticals", "Industry vertical packs — electrical, medical, grocery", "1.0"));
        return services;
    }
}

internal sealed class VerticalConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
        => VerticalModelConfiguration.Configure(modelBuilder);
}
