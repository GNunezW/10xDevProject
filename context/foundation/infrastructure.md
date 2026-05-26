---
project: plan-zajec-uczelnia
researched_at: 2026-05-21T20:00:00Z
recommended_platform: Azure App Service
runner_up: Render
context_type: mvp
tech_stack:
  language: C#
  framework: ASP.NET Core Web API
  runtime: .NET 10 (net10.0)
---

## Recommendation

**Deploy on Azure App Service (Linux, .NET 10 runtime).**

This matches the hand-off (`deployment_target: azure-app-service`), your hyperscaler familiarity, single-region (West Europe) needs, and co-location preference: API and Azure Database for PostgreSQL Flexible Server live in one subscription. Compute can start on **F1 Free** for learning/MVP trials; PostgreSQL has no free tier (~$12+/month on Burstable B1ms) but supports **stop/start** to limit dev cost. Scoring favors Azure on native .NET support, `az` CLI deploy/rollback, Learn docs, and **Azure MCP Server** (GA, checked 2026-05-21). Cost sensitivity is addressed by F1 for the app and stopping the DB when idle—not by switching to JS-only PaaS that cannot host this API.

## Platform Comparison

Hard filters applied first: **Cloudflare Workers, Vercel, and Netlify** dropped for this stack—they do not host a first-class ASP.NET Core 10 Web API without contrived workarounds. Interview Q1 (persistent connections) was **unknown**; platforms that support always-on containers (Azure, Fly.io, Railway, Render) remain in the pool.

| Platform | CLI-first | Managed / serverless | Agent-readable docs | Stable deploy API | MCP / integration | Total |
|---|---|---|---|---|---|---|
| **Azure App Service** | Pass | Pass | Pass | Pass | Pass | **5/5** |
| Render | Partial | Pass | Partial | Pass | Fail | 3/5 |
| Railway | Pass | Pass | Partial | Pass | Partial | 4/5 |
| Fly.io | Pass | Pass | Pass | Pass | Partial | 4/5 |
| Cloudflare | — | — | — | — | — | *filtered* |
| Vercel | — | — | — | — | — | *filtered* |
| Netlify | — | — | — | — | — | *filtered* |

**Weights from interview:** minimize cost → favor F1 + DB stop/start; Azure familiarity → tie-break Azure; single region → West Europe pairing; co-location → Azure Postgres in same RG/VNet.

### Shortlisted Platforms

#### 1. Azure App Service (Recommended)

Native `dotnet publish` / ZIP deploy / GitHub Actions to App Service; F1 free tier for compute (60 CPU minutes/day, no SLA, not for production—checked 2026-05-21). PostgreSQL Flexible Server (Burstable) co-located in region. `az webapp` + `@azure/mcp` for agent operations.

#### 2. Render

Free Docker web service (15-minute idle spin-down, checked Render docs 2026-05-21) and free Postgres **30 days**—cheapest short experiment, but cold starts hurt coordinator UX and co-location is weaker (separate managed Postgres product).

#### 3. Railway

Dockerfile required (Railpack does not support .NET yet—Railway docs 2026-05-21); managed Postgres via `DATABASE_URL`; $5 trial credit then Hobby **$5/month** minimum—good DX and co-location, higher baseline cost than Azure F1 compute.

## Anti-Bias Cross-Check: Azure App Service

### Devil's Advocate — Weaknesses

1. **F1 is not production-grade** — shared CPU, 60 CPU-minutes/day cap, no SLA; schedule-generation spikes may hit limits before you add a paid SKU.
2. **PostgreSQL costs immediately** — Flexible Server has no free tier (~$12+/month for smallest Burstable; checked 2026-05-21); “minimize cost” conflicts with co-location unless you stop the server off-hours.
3. **Billing surface area** — App Service plan + DB + storage + egress can produce surprise line items if resources are left running in multiple RGs.
4. **Long-running work is unclear** — FR-008 optimization may need background jobs or queue + worker; App Service alone may need Azure Functions or Container Apps later (unknown from Q1).
5. **Auth secrets sprawl** — connection strings, JWT keys, and Azure OpenAI endpoints multiply across App Settings, Key Vault, and GitHub Actions—easy to misconfigure without a checklist.

### Pre-Mortem — How This Could Fail

The team deployed the coordinator API to Azure App Service F1 in West Europe and Azure Database for PostgreSQL in the same subscription. They assumed the free web tier would carry MVP traffic and that one Burstable DB SKU would stay cheap. By month three, coordinators complained that the first request after idle was slow (they had confused App Service with Render’s spin-down—but real pain came from **CPU throttling on F1** during schedule generation). A nightly load test exceeded the daily CPU quota and returned 503s during demo week. Nobody had enabled **stop/start** on Postgres for dev environments, so three duplicate databases billed $40/month. GitHub Actions deployed successfully to staging but production still used a manual publish profile secret that expired. The AI scheduling call used an Azure OpenAI key in App Settings while local dev used a different key in `user-secrets`, so “works on my machine” blocked FR-008 in production. The post-mortem lesson: F1 is for learning, not solver workloads; tag resources; one Key Vault; one deployment path.

### Unknown Unknowns

- **App Service plan SKU is shared** — multiple apps in one plan share compute billing; a second experiment app can double cost unexpectedly.
- **Linux vs Windows** — .NET 10 LTS is supported on both; Linux is usually cheaper but Dockerfile/custom startup differs from Visual Studio publish defaults.
- **Outbound to Azure OpenAI** — FR-008 may need Azure OpenAI in the same tenant; network restrictions and managed identity are easier than raw API keys but take setup time.
- **GitHub Actions OIDC to Azure** — preferred over long-lived publish profiles; not visible on the App Service “quickstart” page alone.
- **MCP is powerful but beta-channel** — `@azure/mcp` ships frequent beta releases (3.0.0-beta.x, checked npm 2026-05-21); pin a version for reproducible agent sessions.

## Operational Story

- **Preview deploys**: Use a **staging App Service slot** (Standard S1+ required for slots) or a separate F1/B1 app `plan-zajec-uczelnia-staging` in the same RG; GitHub Actions workflow on `pull_request` deploys to staging URL. F1 does not support slots—use a second free/low app for MVP previews. Protect staging with Entra ID easy auth or IP restriction if exposed.
- **Secrets**: Production secrets in **App Service → Configuration → Application settings** (Key Vault references when enabled); CI uses **GitHub Actions secrets** or OIDC federated credentials to Azure—never commit `appsettings.Production.json` with secrets.
- **Rollback**: `az webapp deployment list-publishing-profiles` / Deployment Center → redeploy previous ZIP or swap slots; typical revert minutes if artifact retained; **database migrations do not roll back** with app swap—keep EF migrations forward-only.
- **Approval**: Agent may deploy to **staging** unattended; **production slot swap** and **Key Vault secret rotation** require human confirmation in AGENTS.md/CI rules.
- **Logs**: `az webapp log tail --name plan-zajec-uczelnia --resource-group <rg>`; Application Insights optional; GitHub Actions run logs for build/deploy audit.

## Risk Register

| Risk | Source | Likelihood | Impact | Mitigation |
|---|---|---|---|---|
| F1 CPU quota exhausted during schedule generation | Devil's advocate | M | H | Move to B1+ before pilot; offload long jobs to queue/worker; load-test early |
| Postgres cost without free tier | Research finding | H | M | Burstable B1ms + stop server nights/weekends; one DB per environment |
| Surprise Azure bill from orphan resources | Devil's advocate | M | M | Single RG, tags, budget alert, monthly resource review |
| Long-running solver needs background processing | Unknown unknowns / Q1 | M | H | Design FR-008 as async job early; plan Functions or Container Apps spike |
| Secret mismatch local vs Azure | Pre-mortem | M | H | Key Vault or single App Settings matrix documented in change plan |
| MCP package drift breaks agent deploy scripts | Unknown unknowns | L | M | Pin `@azure/mcp` version in project MCP config |

## Getting Started

1. Install Azure CLI if missing: `brew install azure-cli` (macOS) and `az login`.
2. Register resource providers: `az provider register --namespace Microsoft.Web --wait` and `Microsoft.DBforPostgreSQL`.
3. Create RG in West Europe: `az group create --name rg-plan-zajec-uczelnia --location westeurope`.
4. Scaffold web app (from repo root): `cd plan-zajec-uczelnia && az webapp up --sku F1 --name <unique-app-name> --os-type linux --runtime "DOTNET:10"`.
5. Create PostgreSQL Flexible Server (Burstable, dev): `az postgres flexible-server create --resource-group rg-plan-zajec-uczelnia --name <unique-pg-name> --location westeurope --tier Burstable --sku-name Standard_B1ms --version 16 --admin-user <admin> --admin-password <pw> --storage-size 32`.
6. Wire connection string to App Service: `az webapp config connection-string set --resource-group rg-plan-zajec-uczelnia --name <unique-app-name> --settings DefaultConnection="Host=<pg>.postgres.database.azure.com;..." --connection-string-type PostgreSQL`.
7. Add GitHub Actions deploy workflow via App Service Deployment Center (OIDC) when ready—matches `hints.ci_provider: github-actions` in tech-stack.

## Out of Scope

The following were not evaluated in this research:

- Docker image configuration beyond App Service Linux container option
- CI/CD pipeline authoring (only platform capability noted)
- Production-scale architecture (multi-region HA, DR, private endpoints)
