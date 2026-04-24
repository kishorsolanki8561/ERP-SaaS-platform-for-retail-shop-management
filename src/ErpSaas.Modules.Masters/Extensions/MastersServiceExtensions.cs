using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Masters.Seeds;
using ErpSaas.Modules.Masters.Services;
using ErpSaas.Shared.Catalog;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Masters.Extensions;

public static class MastersServiceExtensions
{
    public static IServiceCollection AddMastersModule(this IServiceCollection services)
    {
        services.AddScoped<IMasterDataService, MasterDataService>();
        services.AddDataSeeder<MasterDataSeeder>();
        services.AddSingleton(new ServiceDescriptorEntry("MasterData", "Country/State/City/Currency/HSN-SAC catalog", "1.0"));
        return services;
    }
}
