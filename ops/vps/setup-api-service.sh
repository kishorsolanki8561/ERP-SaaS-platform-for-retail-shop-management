#!/usr/bin/env bash
# ops/vps/setup-api-service.sh
# Run once on the VPS as root (or sudo) to prepare the staging environment.
# Usage: sudo bash ops/vps/setup-api-service.sh
set -euo pipefail

APP_USER="erp-staging"
APP_DIR="/opt/erp-staging/api"
LOG_DIR="/var/log/erp-staging"
SERVICE_NAME="erp-staging"

# ── 1. Create a dedicated system user (no login shell) ──────────────────────
if ! id "$APP_USER" &>/dev/null; then
  useradd --system --no-create-home --shell /usr/sbin/nologin "$APP_USER"
  echo "Created system user: $APP_USER"
fi

# ── 2. Create directories ───────────────────────────────────────────────────
mkdir -p "$APP_DIR" "$LOG_DIR"
chown -R "$APP_USER":"$APP_USER" "$APP_DIR" "$LOG_DIR"
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

# ── 4. Write the systemd unit file ─────────────────────────────────────────
cat > /etc/systemd/system/${SERVICE_NAME}.service <<'UNIT'
[Unit]
Description=ErpSaas API (staging)
After=network.target

[Service]
Type=simple
User=erp-staging
WorkingDirectory=/opt/erp-staging/api
ExecStart=/usr/bin/dotnet /opt/erp-staging/api/ErpSaas.Api.dll
Restart=on-failure
RestartSec=5
KillSignal=SIGINT
SyslogIdentifier=erp-staging

# ── Runtime configuration ─────────────────────────────────────────────────
# These EnvironmentFile values are written by the deploy workflow
# using GitHub Secrets; never hard-code credentials here.
EnvironmentFile=/opt/erp-staging/api/.env.staging

# ASP.NET Core
Environment=ASPNETCORE_ENVIRONMENT=Staging
Environment=ASPNETCORE_URLS=http://localhost:5100

[Install]
WantedBy=multi-user.target
UNIT

echo "Systemd unit written: /etc/systemd/system/${SERVICE_NAME}.service"

# ── 5. Enable the service (don't start yet — no binaries yet) ───────────────
systemctl daemon-reload
systemctl enable "$SERVICE_NAME"
echo "Service enabled (will start on next deploy)."

# ── 6. Install nginx if absent ─────────────────────────────────────────────
if ! command -v nginx &>/dev/null; then
  apt-get install -y nginx
fi

# ── 7. Sudoers rule for the deploy user ─────────────────────────────────────
# The GitHub Actions workflow SSH's in as 'administrator' and needs to
# run exactly these privileged commands — nothing broader.
SUDOERS_FILE="/etc/sudoers.d/erp-staging-deploy"
cat > "$SUDOERS_FILE" <<'SUDOERS'
# Allow the deploy user to manage the erp-staging service and copy files
# into the deployment directories without an interactive password prompt.
administrator ALL=(root) NOPASSWD: /bin/systemctl stop erp-staging
administrator ALL=(root) NOPASSWD: /bin/systemctl start erp-staging
administrator ALL=(root) NOPASSWD: /bin/systemctl is-active erp-staging
administrator ALL=(root) NOPASSWD: /bin/rm -rf /opt/erp-staging/*
administrator ALL=(root) NOPASSWD: /bin/cp -r * /opt/erp-staging/*
administrator ALL=(root) NOPASSWD: /bin/mkdir -p /opt/erp-staging/*
administrator ALL=(root) NOPASSWD: /bin/chown -R erp-staging\:erp-staging /opt/erp-staging/api
administrator ALL=(root) NOPASSWD: /bin/chown -R www-data\:www-data /opt/erp-staging/web
administrator ALL=(root) NOPASSWD: /bin/chown -R www-data\:www-data /opt/erp-staging/portal
administrator ALL=(root) NOPASSWD: /bin/chmod 600 /opt/erp-staging/api/.env.staging
administrator ALL=(erp-staging) NOPASSWD: /usr/bin/dotnet *
SUDOERS
chmod 0440 "$SUDOERS_FILE"
echo "Sudoers rule written: $SUDOERS_FILE"

echo ""
echo "============================================================"
echo " Setup complete.  Next steps:"
echo "  1. Add DNS A records for erp-api-staging.preptm.com,"
echo "     erp-app-staging.preptm.com, erp-portal-staging.preptm.com"
echo "     → all pointing to 204.12.245.106"
echo "  2. Copy ops/vps/nginx-staging.conf to"
echo "     /etc/nginx/sites-available/erp-staging"
echo "     and symlink: ln -s /etc/nginx/sites-available/erp-staging"
echo "                          /etc/nginx/sites-enabled/"
echo "  3. Run: certbot --nginx -d erp-api-staging.preptm.com"
echo "                          -d erp-app-staging.preptm.com"
echo "                          -d erp-portal-staging.preptm.com"
echo "  4. Push to main → GitHub Actions deploy workflow fires."
echo "============================================================"
