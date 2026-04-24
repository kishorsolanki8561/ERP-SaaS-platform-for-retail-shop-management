# ADR 0001 — Infrastructure Decisions

**Date:** 2026-04-24
**Status:** Decided
**Reference:** docs/PHASE_0_STARTER.md — "Before you write any code — the eight decisions"

---

## Decisions

| # | Decision | Choice | Notes |
|---|---|---|---|
| 1 | Cloud host | **Azure** | Best fit for .NET 8 + SQL Server stack |
| 2 | Primary domain | **shopearth.in** | Production domain |
| 3 | Subdomain strategy | **app.shopearth.in** (staff SPA) + **api.shopearth.in** (Web API) + **portal.shopearth.in** (customer portal) + **docs.shopearth.in** (documentation) | Standard recommended layout |
| 4 | SQL Server edition for prod | **Azure SQL Managed Instance** | Full SQL Server compatibility, managed service |
| 5 | Payment gateway | **Razorpay** (primary, India) + **Stripe** (international, Phase 5+) | Razorpay for Indian UPI/Card/Wallet; Stripe deferred |
| 6 | SMS provider | **MSG91** | DLT-compliant for India transactional SMS |
| 7 | Email provider | **Postmark** | Transactional email, strong deliverability |
| 8 | Product Owner email | **kishorsolanki2012@gmail.com** | Seeds the first platform account on bootstrap |

---

## Local development (Phase 0)

- SQL Server: Docker container (SQL Server 2022 Developer Edition)
- Redis: Docker container
- Email: MailHog (local SMTP trap, Docker)
- Blob storage: Azurite (Azure Storage emulator, Docker)
- Log aggregation: Seq (Docker)
- No staging deployment until Phase 0 exit gate is confirmed locally

## Deferred decisions (Phase 5+)

- Azure Bicep / Terraform IaC for cloud provisioning
- Azure Key Vault integration (using `dotnet user-secrets` locally)
- Custom domain SSL certs (Azure Front Door / Let's Encrypt)
- Stripe integration for international payments
- Azure Cache for Redis (using local Redis container until Phase 5)
