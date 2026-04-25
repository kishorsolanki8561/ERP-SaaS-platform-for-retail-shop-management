using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Wallet.Infrastructure;
using ErpSaas.Modules.Wallet.Seeds;
using ErpSaas.Modules.Wallet.Services;
using ErpSaas.Shared.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Wallet.Extensions;

public static class WalletServiceExtensions
{
    public static IServiceCollection AddWalletModule(this IServiceCollection services)
    {
        services.AddScoped<IWalletService, WalletService>();
        services.AddDataSeeder<WalletSystemSeeder>();
        services.AddSingleton(new ServiceDescriptorEntry(
            "Wallet",
            "Customer wallet, advance payments, and payment receipts",
            "1.0"));
        services.AddSingleton<IEntityModelConfigurator, WalletModelConfigurator>();
        return services;
    }
}

internal sealed class WalletModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder) => WalletModelConfiguration.Configure(modelBuilder);
}
