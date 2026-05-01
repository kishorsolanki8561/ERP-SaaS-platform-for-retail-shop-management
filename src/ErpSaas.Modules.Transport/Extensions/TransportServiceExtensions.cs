using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Transport.Infrastructure;
using ErpSaas.Modules.Transport.Seeds;
using ErpSaas.Modules.Transport.Services;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Transport.Extensions;

public static class TransportServiceExtensions
{
    public static IServiceCollection AddTransportModule(this IServiceCollection services)
    {
        services.AddScoped<ITransportService, TransportService>();
        services.AddScoped<IDataSeeder, TransportSystemSeeder>();
        services.AddSingleton<IEntityModelConfigurator, TransportModelConfigurator>();
        return services;
    }
}

internal sealed class TransportModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
        => TransportModelConfiguration.Configure(modelBuilder);
}
