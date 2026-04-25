using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Shift.Infrastructure;
using ErpSaas.Modules.Shift.Seeds;
using ErpSaas.Modules.Shift.Services;
using ErpSaas.Shared.Catalog;
using ErpSaas.Shared.Seeds;
using ErpSaas.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Shift.Extensions;

public static class ShiftServiceExtensions
{
    public static IServiceCollection AddShiftModule(this IServiceCollection services)
    {
        services.AddScoped<ShiftService>();
        services.AddScoped<IShiftService>(sp => sp.GetRequiredService<ShiftService>());
        services.AddScoped<IShiftLookup>(sp => sp.GetRequiredService<ShiftService>());
        services.AddScoped<IDataSeeder, ShiftSystemSeeder>();
        services.AddSingleton<IEntityModelConfigurator, ShiftModelConfigurator>();
        services.AddSingleton(new ServiceDescriptorEntry("Shift", "POS shift management with cash movements", "1.0"));
        return services;
    }
}
