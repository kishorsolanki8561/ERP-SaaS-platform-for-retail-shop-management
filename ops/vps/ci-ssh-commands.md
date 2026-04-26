# CI/CD SSH Commands Reference — Staging VPS

**VPS:** `204.12.245.106`  
**User:** `administrator`  
**Workflow file:** `.github/workflows/deploy-staging.yml`

All commands below run non-interactively from GitHub Actions. The runner authenticates with the SSH private key stored in the `VPS_SSH_KEY` GitHub secret. No password is used — only key-based authentication.

---

## Directory structure on the VPS

```
/opt/erp-staging/
├── api/            API publish artefacts + .env.staging
├── web/            Angular staff app (served by nginx)
└── portal/         Angular customer portal (served by nginx)

/tmp/               Temporary staging area during deploys (cleaned up after swap)
├── erp-staging-api-deploy/
├── erp-staging-web-deploy/
└── erp-staging-portal-deploy/
```

---

## 1. Deploy API

### File transfer (rsync — runs on GitHub Actions runner, not on VPS)
```bash
rsync -avz --delete ./api-publish/ \
  administrator@204.12.245.106:/tmp/erp-staging-api-deploy/
```
- `-a` — archive mode (preserves permissions, timestamps, symlinks)
- `-v` — verbose output
- `-z` — compress during transfer
- `--delete` — remove files on the VPS that no longer exist in the source

### Remote commands (run via SSH heredoc)
```bash
ssh administrator@204.12.245.106 bash << 'REMOTE'
set -euo pipefail

# Stop the running service
sudo systemctl stop erp-staging || true

# Replace API files
sudo rm -rf /opt/erp-staging/api/*
sudo cp -r /tmp/erp-staging-api-deploy/. /opt/erp-staging/api/

# Fix ownership and permissions
sudo chown -R erp-staging:erp-staging /opt/erp-staging/api
sudo chmod 600 /opt/erp-staging/api/.env.staging   # restrict secrets file

# Start the service
sudo systemctl start erp-staging

# Poll until active (up to 90 seconds — migrations may need time)
for i in $(seq 1 18); do
  STATUS=$(systemctl is-active erp-staging 2>/dev/null || true)
  if [ "$STATUS" = "active" ]; then break; fi
  if [ "$STATUS" = "failed" ]; then
    sudo -n journalctl -u erp-staging --no-pager -n 80
    exit 1
  fi
  sleep 5
done

# Clean up temp files
rm -rf /tmp/erp-staging-api-deploy
REMOTE
```

### Required sudoers rules (NOPASSWD)
```
administrator ALL=(ALL) NOPASSWD: /bin/systemctl stop erp-staging
administrator ALL=(ALL) NOPASSWD: /bin/systemctl start erp-staging
administrator ALL=(ALL) NOPASSWD: /bin/rm -rf /opt/erp-staging/api/*
administrator ALL=(ALL) NOPASSWD: /bin/cp -r /tmp/erp-staging-api-deploy/. /opt/erp-staging/api/
administrator ALL=(ALL) NOPASSWD: /bin/chown -R erp-staging:erp-staging /opt/erp-staging/api
administrator ALL=(ALL) NOPASSWD: /bin/chmod 600 /opt/erp-staging/api/.env.staging
administrator ALL=(ALL) NOPASSWD: /usr/bin/journalctl -u erp-staging *
```

---

## 2. Deploy Staff App (Angular)

### File transfer
```bash
rsync -avz --delete ./web-publish/ \
  administrator@204.12.245.106:/tmp/erp-staging-web-deploy/
```

### Remote commands
```bash
ssh administrator@204.12.245.106 bash << 'REMOTE'
set -euo pipefail

sudo mkdir -p /opt/erp-staging/web
sudo rm -rf /opt/erp-staging/web/*
sudo cp -r /tmp/erp-staging-web-deploy/. /opt/erp-staging/web/
sudo chown -R www-data:www-data /opt/erp-staging/web

rm -rf /tmp/erp-staging-web-deploy
REMOTE
```

### Required sudoers rules (NOPASSWD)
```
administrator ALL=(ALL) NOPASSWD: /bin/mkdir -p /opt/erp-staging/web
administrator ALL=(ALL) NOPASSWD: /bin/rm -rf /opt/erp-staging/web/*
administrator ALL=(ALL) NOPASSWD: /bin/cp -r /tmp/erp-staging-web-deploy/. /opt/erp-staging/web/
administrator ALL=(ALL) NOPASSWD: /bin/chown -R www-data:www-data /opt/erp-staging/web
```

---

## 3. Deploy Customer Portal (Angular)

### File transfer
```bash
rsync -avz --delete ./portal-publish/ \
  administrator@204.12.245.106:/tmp/erp-staging-portal-deploy/
```

### Remote commands
```bash
ssh administrator@204.12.245.106 bash << 'REMOTE'
set -euo pipefail

sudo mkdir -p /opt/erp-staging/portal
sudo rm -rf /opt/erp-staging/portal/*
sudo cp -r /tmp/erp-staging-portal-deploy/. /opt/erp-staging/portal/
sudo chown -R www-data:www-data /opt/erp-staging/portal

rm -rf /tmp/erp-staging-portal-deploy
REMOTE
```

### Required sudoers rules (NOPASSWD)
```
administrator ALL=(ALL) NOPASSWD: /bin/mkdir -p /opt/erp-staging/portal
administrator ALL=(ALL) NOPASSWD: /bin/rm -rf /opt/erp-staging/portal/*
administrator ALL=(ALL) NOPASSWD: /bin/cp -r /tmp/erp-staging-portal-deploy/. /opt/erp-staging/portal/
administrator ALL=(ALL) NOPASSWD: /bin/chown -R www-data:www-data /opt/erp-staging/portal
```

---

## 4. Post-deploy cache purge (Cloudflare — no SSH)

These run on the GitHub Actions runner via `curl`, not over SSH:

```bash
# Purge API cache
curl -sf -X POST \
  "https://api.cloudflare.com/client/v4/zones/${CLOUDFLARE_ZONE_ID}/purge_cache" \
  -H "Authorization: Bearer ${CLOUDFLARE_API_TOKEN}" \
  -H "Content-Type: application/json" \
  --data '{"files":["https://erp-api-staging.preptm.com/health","https://erp-api-staging.preptm.com/api/services"]}'

# Purge staff app cache
curl -sf -X POST \
  "https://api.cloudflare.com/client/v4/zones/${CLOUDFLARE_ZONE_ID}/purge_cache" \
  -H "Authorization: Bearer ${CLOUDFLARE_API_TOKEN}" \
  -H "Content-Type: application/json" \
  --data '{"files":["https://erp-app-staging.preptm.com/","https://erp-app-staging.preptm.com/index.html"]}'

# Purge portal cache
curl -sf -X POST \
  "https://api.cloudflare.com/client/v4/zones/${CLOUDFLARE_ZONE_ID}/purge_cache" \
  -H "Authorization: Bearer ${CLOUDFLARE_API_TOKEN}" \
  -H "Content-Type: application/json" \
  --data '{"files":["https://erp-portal-staging.preptm.com/","https://erp-portal-staging.preptm.com/index.html"]}'
```

---

## 5. GitHub Secrets required

| Secret name          | Used for                                      |
|----------------------|-----------------------------------------------|
| `VPS_SSH_KEY`        | Private key for SSH/rsync authentication      |
| `VPS_KNOWN_HOSTS`    | VPS host fingerprint (prevents MITM)          |
| `DB_PASSWORD`        | SQL Server `sa` password in `.env.staging`    |
| `JWT_SECRET`         | JWT signing key in `.env.staging`             |
| `TURNSTILE_SECRET_KEY` | Cloudflare Turnstile in `.env.staging`      |
| `CLOUDFLARE_ZONE_ID` | Cloudflare zone for cache purge               |
| `CLOUDFLARE_API_TOKEN` | Cloudflare API bearer token for cache purge |

---

## 6. One-time VPS setup commands

Run these once manually when setting up a new VPS or reinstalling:

```bash
# Create service user (no login shell)
sudo useradd -r -s /sbin/nologin erp-staging

# Create deploy directories
sudo mkdir -p /opt/erp-staging/{api,web,portal}
sudo chown erp-staging:erp-staging /opt/erp-staging/api
sudo chown www-data:www-data /opt/erp-staging/{web,portal}

# Install .NET 8 runtime
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
sudo dpkg -i /tmp/packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-8.0

# Copy and enable nginx config
sudo cp ops/vps/nginx-staging.conf /etc/nginx/conf.d/erp-staging.conf
sudo nginx -t && sudo systemctl reload nginx

# Copy and enable systemd service
sudo cp ops/vps/erp-staging.service /etc/systemd/system/erp-staging.service
sudo systemctl daemon-reload
sudo systemctl enable erp-staging

# Add authorized key for GitHub Actions CI
echo "ssh-ed25519 AAAA... github-actions-ci" >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
```

---

## 7. Useful manual diagnostic commands

```bash
# Check service status
systemctl status erp-staging

# Tail live logs
journalctl -u erp-staging -f

# Last 100 log lines
journalctl -u erp-staging --no-pager -n 100

# Check which ports are listening
ss -tlnp | grep 5100

# Check nginx config
sudo nginx -t
sudo nginx -T | grep -A5 erp-staging

# Check .env.staging (read as erp-staging user)
sudo -u erp-staging cat /opt/erp-staging/api/.env.staging

# Restart service manually
sudo systemctl restart erp-staging

# Reload nginx after config change
sudo systemctl reload nginx
```
