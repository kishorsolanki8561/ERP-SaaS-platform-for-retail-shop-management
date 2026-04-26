using ErpSaas.Api.Middleware;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Serilog;

namespace ErpSaas.Api.Extensions;

public static class ErpPipelineExtensions
{
    public static WebApplication UseErpPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "ShopEarth ERP v1"));
        }

        app.UseSerilogRequestLogging();
        app.UseAuthentication();
        app.UseTenantContext();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHangfireDashboard("/hangfire");

        return app;
    }
}
