using System.Text;
using ErpSaas.Infrastructure.Authorization;
using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Identity.Seeds;
using ErpSaas.Modules.Identity.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Catalog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ErpSaas.Modules.Identity.Extensions;

public static class IdentityServiceExtensions
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // JWT authentication
        var jwtSecret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret not configured");
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "shopearth-erp";
        var jwtAudience = configuration["Jwt:Audience"] ?? "shopearth-erp-clients";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        // Authorization policy provider + handlers
        services.AddSingleton<IAuthorizationPolicyProvider, DynamicAuthorizationPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, FeatureAuthorizationHandler>();

        // Identity services
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBootstrapService, BootstrapService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IShopOnboardingService, ShopOnboardingService>();
        services.AddScoped<IMenuService, MenuService>();

        // Seeders
        services.AddDataSeeder<IdentityDataSeeder>();
        services.AddDataSeeder<MenuDataSeeder>();

        // Service catalog entry
        services.AddSingleton(new ServiceDescriptorEntry("Identity", "JWT auth, RBAC, shop/user management", "1.0"));

        return services;
    }
}
