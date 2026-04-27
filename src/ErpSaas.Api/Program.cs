using ErpSaas.Api.Extensions;
using ErpSaas.Api.Middleware;
using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Shared.Data;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext());

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApiServices(builder.Configuration);
    builder.Services.AddScoped<RequestTenantContext>();
    builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<RequestTenantContext>());

    var app = builder.Build();

    await app.InitializeAsync();

    if (args.Contains("--seed-and-exit"))
    {
        Log.Information("Seed-and-exit: all migrations and seeds complete — shutting down");
        return;
    }

    app.UseErpPipeline();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    Environment.Exit(1); // non-zero so systemd Restart=on-failure fires
}
finally
{
    await Log.CloseAndFlushAsync();
}
