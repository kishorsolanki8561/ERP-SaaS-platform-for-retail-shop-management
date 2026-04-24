namespace ErpSaas.Api.Middleware;

public sealed class TenantContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, RequestTenantContext tenantContext)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            if (long.TryParse(user.FindFirst("sub")?.Value, out var userId))
                tenantContext.CurrentUserId = userId;

            if (long.TryParse(user.FindFirst("shop_id")?.Value, out var shopId))
                tenantContext.ShopId = shopId;
        }

        await next(context);
    }
}

public static class TenantContextMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantContext(this IApplicationBuilder app)
        => app.UseMiddleware<TenantContextMiddleware>();
}
