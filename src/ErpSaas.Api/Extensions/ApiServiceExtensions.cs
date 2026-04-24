using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Api.Extensions;

public static class ApiServiceExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(Modules.Masters.Controllers.DdlController).Assembly);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
            c.SwaggerDoc("v1", new() { Title = "ShopEarth ERP API", Version = "v1" }));

        services.AddAuthorization();

        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(configuration.GetConnectionString("LogDb")));
        services.AddHangfireServer();

        return services;
    }
}
