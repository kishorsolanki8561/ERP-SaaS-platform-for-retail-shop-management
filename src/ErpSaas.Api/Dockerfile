# ─────────────────────────────────────────────────────────────────────────────
#  ErpSaas API — multi-stage Dockerfile
#
#  Build context must be the /src directory (one level above this file).
#  docker-compose sets:  context: ../src  dockerfile: ErpSaas.Api/Dockerfile
#  Visual Studio sets the context to the solution root automatically.
#
#  Stages:
#    dev      – SDK image + dotnet watch for local hot-reload development
#    build    – full Release publish (used by CI and the runtime stage)
#    runtime  – minimal aspnet image for staging / production
# ─────────────────────────────────────────────────────────────────────────────

# ── dev ───────────────────────────────────────────────────────────────────────
# Source code is bind-mounted at /src at runtime (see docker-compose.yml).
# dotnet restore runs on every container start so packages are always fresh.
# DOTNET_USE_POLLING_FILE_WATCHER lets dotnet watch detect saves from the
# Windows host (inotify events don't cross the WSL2 boundary reliably).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dev

WORKDIR /src

ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_WATCH_SUPPRESS_MSBUILD_INCREMENTALISM=true
ENV DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true
# Poll every 2 s — fast enough to feel instant, cheap enough not to thrash.
ENV DOTNET_WATCH_POLL_INTERVAL_MS=2000

EXPOSE 8080

CMD ["sh", "-c", "dotnet restore ErpSaas.sln && dotnet watch run --project ErpSaas.Api/ErpSaas.Api.csproj --non-interactive"]


# ── build ─────────────────────────────────────────────────────────────────────
# Layer-cache NuGet restore by copying only .csproj / .sln first, then the
# full source.  A code-only change skips the restore layer entirely.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ErpSaas.sln .

COPY ErpSaas.Api/ErpSaas.Api.csproj                               ErpSaas.Api/
COPY ErpSaas.Core/ErpSaas.Core.csproj                             ErpSaas.Core/
COPY ErpSaas.Shared/ErpSaas.Shared.csproj                         ErpSaas.Shared/
COPY ErpSaas.Infrastructure/ErpSaas.Infrastructure.csproj         ErpSaas.Infrastructure/
COPY ErpSaas.Modules.Masters/ErpSaas.Modules.Masters.csproj       ErpSaas.Modules.Masters/
COPY ErpSaas.Modules.Identity/ErpSaas.Modules.Identity.csproj     ErpSaas.Modules.Identity/
COPY ErpSaas.Modules.Crm/ErpSaas.Modules.Crm.csproj               ErpSaas.Modules.Crm/
COPY ErpSaas.Modules.Inventory/ErpSaas.Modules.Inventory.csproj   ErpSaas.Modules.Inventory/
COPY ErpSaas.Modules.Wallet/ErpSaas.Modules.Wallet.csproj         ErpSaas.Modules.Wallet/
COPY ErpSaas.Modules.Shift/ErpSaas.Modules.Shift.csproj           ErpSaas.Modules.Shift/
COPY ErpSaas.Modules.Billing/ErpSaas.Modules.Billing.csproj       ErpSaas.Modules.Billing/
COPY ErpSaas.Modules.Accounting/ErpSaas.Modules.Accounting.csproj ErpSaas.Modules.Accounting/
COPY ErpSaas.Modules.Purchasing/ErpSaas.Modules.Purchasing.csproj ErpSaas.Modules.Purchasing/
COPY ErpSaas.Modules.SalesReturns/ErpSaas.Modules.SalesReturns.csproj ErpSaas.Modules.SalesReturns/
COPY ErpSaas.Modules.Reports/ErpSaas.Modules.Reports.csproj       ErpSaas.Modules.Reports/
COPY ErpSaas.Modules.Warranty/ErpSaas.Modules.Warranty.csproj     ErpSaas.Modules.Warranty/
COPY ErpSaas.Modules.Pricing/ErpSaas.Modules.Pricing.csproj       ErpSaas.Modules.Pricing/
COPY ErpSaas.Modules.Transport/ErpSaas.Modules.Transport.csproj   ErpSaas.Modules.Transport/
COPY ErpSaas.Modules.Quotations/ErpSaas.Modules.Quotations.csproj ErpSaas.Modules.Quotations/
COPY ErpSaas.Tests.Unit/ErpSaas.Tests.Unit.csproj                 ErpSaas.Tests.Unit/
COPY ErpSaas.Tests.Integration/ErpSaas.Tests.Integration.csproj   ErpSaas.Tests.Integration/
COPY ErpSaas.Tests.Arch/ErpSaas.Tests.Arch.csproj                 ErpSaas.Tests.Arch/

RUN dotnet restore ErpSaas.sln

COPY . .

RUN dotnet publish ErpSaas.Api/ErpSaas.Api.csproj \
    -c ${BUILD_CONFIGURATION} \
    -o /app/publish \
    --no-restore


# ── runtime ───────────────────────────────────────────────────────────────────
# Minimal ASP.NET runtime image — no SDK, no source code.
# All config comes in via environment variables at deployment time.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ErpSaas.Api.dll"]
