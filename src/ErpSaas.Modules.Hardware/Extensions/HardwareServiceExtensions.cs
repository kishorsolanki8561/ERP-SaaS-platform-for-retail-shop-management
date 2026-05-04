using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Hardware.Infrastructure;
using ErpSaas.Modules.Hardware.Seeds;
using ErpSaas.Modules.Hardware.Services;
using ErpSaas.Shared.Catalog;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Hardware.Extensions;

public static class HardwareServiceExtensions
{
    public static IServiceCollection AddHardwareModule(this IServiceCollection services)
    {
        services.AddScoped<IDeviceProfileService, DeviceProfileService>();
        services.AddScoped<ILabelTemplateService, LabelTemplateService>();
        services.AddScoped<IReceiptTemplateService, ReceiptTemplateService>();
        services.AddScoped<IDataSeeder, HardwareSystemSeeder>();
        services.AddSingleton<IEntityModelConfigurator, HardwareConfigurator>();
        services.AddSingleton(new ServiceDescriptorEntry("Hardware", "Device profiles, label templates, receipt templates, and print rendering", "1.0"));
        return services;
    }
}

internal sealed class HardwareConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
        => HardwareModelConfiguration.Configure(modelBuilder);
}
