#!/usr/bin/env bash
# ops/vps/setup-production-service.sh
# Run once on the VPS as root to prepare the production environment.
# The staging environment must already exist (setup-api-service.sh was run first).
# Usage: bash ops/vps/setup-production-service.sh
set -euo pipefail

APP_USER="erp-production"
APP_DIR="/opt/erp-production/api"
WEB_DIR="/opt/erp-production/web"
PORTAL_DIR="/opt/erp-production/portal"
LOG_DIR="/var/log/erp-production"
SERVICE_NAME="erp-production"

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
echo "Directories ready."

# ── 3. .NET 8 runtime is already installed alongside staging ────────────────
echo ".NET runtime present: $(dotnet --version)"

# ── 4. Install the systemd unit file ───────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
UNIT_SRC="${SCRIPT_DIR}/../erp-production.service"
if [ -f "$UNIT_SRC" ]; then
  cp "$UNIT_SRC" /etc/systemd/system/${SERVICE_NAME}.service
  echo "Systemd unit installed from repo."
else
  echo "WARNING: ${UNIT_SRC} not found — writing minimal stub unit."
  cat > /etc/systemd/system/${SERVICE_NAME}.service <<'UNIT'
[Unit]
Description=ERP SaaS API — Production
After=network.target

[Service]
Type=exec
User=erp-production
Group=erp-production
WorkingDirectory=/opt/erp-production/api
ExecStart=/usr/bin/dotnet /opt/erp-production/api/ErpSaas.Api.dll
Restart=on-failure
RestartSec=10
EnvironmentFile=/opt/erp-production/api/.env.production
LimitNOFILE=65536

[Install]
WantedBy=multi-user.target
UNIT
fi

systemctl daemon-reload
systemctl enable "$SERVICE_NAME"
echo "Service enabled (will start on next deploy)."

# ── 5. Install nginx config ─────────────────────────────────────────────────
NGINX_SRC="${SCRIPT_DIR}/nginx-production.conf"
if [ -f "$NGINX_SRC" ]; then
  cp "$NGINX_SRC" /etc/nginx/conf.d/erp-production.conf
  nginx -t && systemctl reload nginx
  echo "nginx config applied: /etc/nginx/conf.d/erp-production.conf"
else
  echo "WARNING: ${NGINX_SRC} not found — copy ops/vps/nginx-production.conf manually."
fi

echo ""
echo "============================================================"
echo " Production setup complete on 188.241.62.206."
echo " Next steps:"
echo "  1. Add DNS A records for erp-api.preptm.com,"
echo "     erp-app.preptm.com, erp-portal.preptm.com"
echo "     → all pointing to 188.241.62.206"
echo "  2. Issue TLS certificates:"
echo "     certbot --nginx \\"
echo "       -d erp-api.preptm.com \\"
echo "       -d erp-app.preptm.com \\"
echo "       -d erp-portal.preptm.com"
echo "  3. In the GitHub repo, go to Settings → Environments,"
echo "     create a 'production' environment and add:"
echo "       - Required reviewers (yourself or a team)"
echo "       - Secrets: DB_HOST, DB_USER, DB_PASSWORD, JWT_SECRET,"
echo "         TURNSTILE_SECRET_KEY, PRODUCT_OWNER_* (production values)"
echo "  4. Trigger: Actions → Deploy → Run workflow → production"
echo "============================================================"
