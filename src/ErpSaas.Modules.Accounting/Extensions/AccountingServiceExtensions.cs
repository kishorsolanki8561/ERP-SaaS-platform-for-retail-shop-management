using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Accounting.Infrastructure;
using ErpSaas.Modules.Accounting.Jobs;
using ErpSaas.Modules.Accounting.Seeds;
using ErpSaas.Modules.Accounting.Services;
using ErpSaas.Shared.Catalog;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Accounting.Extensions;

public static class AccountingServiceExtensions
{
    public static IServiceCollection AddAccountingModule(this IServiceCollection services)
    {
        services.AddScoped<AccountingService>();
        services.AddScoped<IAccountingService>(sp => sp.GetRequiredService<AccountingService>());
        services.AddScoped<IAutoVoucherService>(sp => sp.GetRequiredService<AccountingService>());
        services.AddScoped<IBankReconciliationService, BankReconciliationService>();
        services.AddScoped<IChequeService, ChequeService>();
        services.AddScoped<IPettyCashService, PettyCashService>();
        services.AddScoped<IFixedAssetService, FixedAssetService>();
        services.AddScoped<StaleChequeJob>();
        services.AddScoped<DepreciationJob>();
        services.AddDataSeeder<AccountingSystemSeeder>();
        services.AddTenantSeeder<AccountingTenantSeeder>();
        services.AddSingleton(new ServiceDescriptorEntry(
            "Accounting",
            "Chart of Accounts, double-entry vouchers, expenses and financial year management",
            "1.0"));
        services.AddSingleton<IEntityModelConfigurator, AccountingModelConfigurator>();
        return services;
    }
}

internal sealed class AccountingModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder) => AccountingModelConfiguration.Configure(modelBuilder);
}
