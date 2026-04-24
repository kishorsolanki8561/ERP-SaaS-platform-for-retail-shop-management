using ErpSaas.Api.Infrastructure;
using ErpSaas.Infrastructure.Catalog;
using ErpSaas.Infrastructure.Ddl;
using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Infrastructure.Seeds;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Infrastructure.Sql;
using ErpSaas.Shared.Catalog;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using Hangfire;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext());

    // ── Infrastructure ─────────────────────────────────────────────────────────
    builder.Services.AddInfrastructure(builder.Configuration);

    // Phase 0 stub — replaced by real middleware in Phase 1
    builder.Services.AddScoped<ITenantContext, StubTenantContext>();

    // ── Cross-cutting services ─────────────────────────────────────────────────
    builder.Services.AddSingleton<IServiceCatalog, ServiceCatalog>();
    builder.Services.AddSingleton<IErrorLogger, ErrorLogger>();
    builder.Services.AddHostedService(sp => (ErrorLogger)sp.GetRequiredService<IErrorLogger>());
    builder.Services.AddScoped<IAuditLogger, AuditLogger>();
    builder.Services.AddScoped<IDdlService, DdlService>();
    builder.Services.AddScoped<ISequenceService, SequenceService>();
    builder.Services.AddScoped<ISqlObjectMigrator, SqlObjectMigrator>();
    builder.Services.AddMemoryCache();

    // ── Hangfire ───────────────────────────────────────────────────────────────
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("LogDb")));
    builder.Services.AddHangfireServer();

    // ── MVC + Swagger ──────────────────────────────────────────────────────────
    builder.Services.AddControllers()
        .AddApplicationPart(typeof(ErpSaas.Modules.Masters.Controllers.DdlController).Assembly);
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "ShopEarth ERP API", Version = "v1" });
    });

    builder.Services.AddAuthorization();

    // ── Register services in catalog ───────────────────────────────────────────
    var app = builder.Build();

    var catalog = app.Services.GetRequiredService<IServiceCatalog>();
    catalog.Register(new ServiceDescriptorEntry("DDL", "Dropdown catalog service", "1.0"));
    catalog.Register(new ServiceDescriptorEntry("Sequence", "Document number sequencing service", "1.0"));
    catalog.Register(new ServiceDescriptorEntry("ServiceCatalog", "Registered service registry", "1.0"));
    catalog.Register(new ServiceDescriptorEntry("ErrorLogger", "Async error logging to LogDb", "1.0"));
    catalog.Register(new ServiceDescriptorEntry("AuditLogger", "Mutation audit trail to LogDb", "1.0"));

    // ── Deploy stored procedures + seed all data ──────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var sp = scope.ServiceProvider;
        try
        {
            var migrator = sp.GetRequiredService<ISqlObjectMigrator>();
            await migrator.DeployAsync();

            var platformDb = sp.GetRequiredService<ErpSaas.Infrastructure.Data.PlatformDbContext>();
            await platformDb.Database.EnsureCreatedAsync();

            var tenantDb = sp.GetRequiredService<ErpSaas.Infrastructure.Data.TenantDbContext>();
            await tenantDb.Database.EnsureCreatedAsync();

            var logDb = sp.GetRequiredService<ErpSaas.Infrastructure.Data.LogDbContext>();
            await logDb.Database.EnsureCreatedAsync();

            await sp.GetRequiredService<DatabaseSeeder>().SeedAllAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Startup migration/seed failed — continuing without it in dev");
        }
    }

    // ── Middleware ─────────────────────────────────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ShopEarth ERP v1"));
    }

    app.UseSerilogRequestLogging();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHangfireDashboard("/hangfire");

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
