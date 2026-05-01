#!/usr/bin/env bash
# ops/vps/setup-api-service.sh
# Run once on the VPS as root to prepare the staging environment.
# The deploy workflow SSH's in as root, so no sudoers rules are needed.
# Usage: bash ops/vps/setup-api-service.sh
set -euo pipefail

APP_USER="erp-staging"
APP_DIR="/opt/erp-staging/api"
WEB_DIR="/opt/erp-staging/web"
PORTAL_DIR="/opt/erp-staging/portal"
LOG_DIR="/var/log/erp-staging"
SERVICE_NAME="erp-staging"

# ── 1. Create a dedicated system user (no login shell) ──────────────────────
if ! id "$APP_USER" &>/dev/null; then
  useradd --system --no-create-home --shell /usr/sbin/nologin "$APP_USER"
  echo "Created system user: $APP_USER"
else
  echo "System user already exists: $APP_USER"
fi

# ── 2. Create directories ───────────────────────────────────────────────────
mkdir -p "$APP_DIR" "$WEB_DIR" "$PORTAL_DIR" "$LOG_DIR"
chown -R "$APP_USER":"$APP_USER" "$APP_DIR" "$LOG_DIR"
chown -R www-data:www-data "$WEB_DIR" "$PORTAL_DIR"
echo "Directories ready: $APP_DIR  $LOG_DIR"

# ── 3. Install .NET 8 runtime (skip if already installed) ──────────────────
if ! command -v dotnet &>/dev/null; then
  echo "Installing .NET 8 runtime..."
  wget -q https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb \
       -O /tmp/packages-microsoft-prod.deb
  dpkg -i /tmp/packages-microsoft-prod.deb
  apt-get update -q
  apt-get install -y dotnet-runtime-8.0
  echo ".NET 8 runtime installed."
else
  echo ".NET runtime already present: $(dotnet --version)"
fi

# ── 4. Install the systemd unit file ───────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
UNIT_SRC="${SCRIPT_DIR}/../erp-staging.service"
if [ -f "$UNIT_SRC" ]; then
  cp "$UNIT_SRC" /etc/systemd/system/${SERVICE_NAME}.service
  echo "Systemd unit installed from repo."
else
  echo "WARNING: ${UNIT_SRC} not found — writing minimal stub unit."
  cat > /etc/systemd/system/${SERVICE_NAME}.service <<'UNIT'
[Unit]
Description=ERP SaaS API — Staging
After=network.target

[Service]
Type=exec
User=erp-staging
Group=erp-staging
WorkingDirectory=/opt/erp-staging/api
ExecStart=/usr/bin/dotnet /opt/erp-staging/api/ErpSaas.Api.dll
Restart=on-failure
RestartSec=10
EnvironmentFile=/opt/erp-staging/api/.env.staging
LimitNOFILE=65536

[Install]
WantedBy=multi-user.target
UNIT
fi

systemctl daemon-reload
systemctl enable "$SERVICE_NAME"
echo "Service enabled (will start on next deploy)."

# ── 5. Install nginx if absent ─────────────────────────────────────────────
if ! command -v nginx &>/dev/null; then
  apt-get install -y nginx
  echo "nginx installed."
fi

# ── 6. Install nginx config ─────────────────────────────────────────────────
NGINX_SRC="${SCRIPT_DIR}/nginx-staging.conf"
if [ -f "$NGINX_SRC" ]; then
  cp "$NGINX_SRC" /etc/nginx/conf.d/erp-staging.conf
  nginx -t && systemctl reload nginx
  echo "nginx config applied: /etc/nginx/conf.d/erp-staging.conf"
else
  echo "WARNING: ${NGINX_SRC} not found — copy ops/vps/nginx-staging.conf manually."
fi

echo ""
echo "============================================================"
echo " Staging setup complete on 188.241.62.206."
echo " Next steps:"
echo "  1. Add DNS A records for erp-api-staging.preptm.com,"
echo "     erp-app-staging.preptm.com, erp-portal-staging.preptm.com"
echo "     → all pointing to 188.241.62.206"
echo "  2. Issue TLS certificates:"
echo "     certbot --nginx \\"
echo "       -d erp-api-staging.preptm.com \\"
echo "       -d erp-app-staging.preptm.com \\"
echo "       -d erp-portal-staging.preptm.com"
echo "  3. In the GitHub repo, go to Settings → Environments,"
echo "     create a 'staging' environment and add secrets:"
echo "       DB_HOST        → localhost,1434"
echo "       DB_USER        → erpsaas_user"
echo "       DB_PASSWORD    → (see team password manager)"
echo "       JWT_SECRET     → (generate: openssl rand -base64 32)"
echo "       TURNSTILE_SECRET_KEY, PRODUCT_OWNER_*, VPS_SSH_KEY,"
echo "       VPS_KNOWN_HOSTS, CLOUDFLARE_ZONE_ID, CLOUDFLARE_API_TOKEN"
echo "  4. Run: Actions → Deploy → Run workflow → staging"
echo "============================================================"
