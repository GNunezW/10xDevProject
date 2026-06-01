---
deployed_at: 2026-05-26T21:36:50Z
platform: Azure App Service
region: polandcentral
app_name: plan-zajec-gabi-01
resource_group: rg-plan-zajec-uczelnia
runtime: DOTNETCORE:10.0
sku: F1
postgres: skipped (Opcja B)
verification: ok
---

# Deploy Plan — Pierwsze wdrożenie na Azure App Service

**Cel:** MVP API (ASP.NET Core 10) na Azure App Service, bez PostgreSQL (Opcja B — baza w fazie 2).

**URL:** `https://plan-zajec-gabi-01.azurewebsites.net`

---

## Wykonane kroki

| # | Krok | Komenda / akcja | Status | Uwagi |
|---|------|-----------------|--------|-------|
| 1 | Bramki ręczne | `az login`, nazwy zasobów, Opcja B | OK | Solo, bez Postgres w tej fazie |
| 2 | Build weryfikacyjny | `dotnet restore && dotnet build --configuration Release` | OK | 0 błędów, 0 ostrzeżeń, net10.0 |
| 3 | Resource Group | `az group create --name rg-plan-zajec-uczelnia --location polandcentral` | OK | Pierwszy RG w westeurope usunięty — policy subskrypcji blokuje ten region |
| 4 | App Service (F1, Linux) | `az webapp up --sku F1 --name plan-zajec-gabi-01 --resource-group rg-plan-zajec-uczelnia --location polandcentral --os-type linux --runtime "DOTNETCORE:10.0"` | OK | Auto-created App Service Plan |
| 5 | Budget alert ($20/mies.) | Portal: Cost Management → Budgets → rg-plan-zajec-uczelnia | OK (ręcznie) | `az consumption budget create` zwraca błąd 400 preview API — portal działa |
| 6 | Postgres | POMINIĘTY (Opcja B) | SKIP | Dodać przed FR-001 auth / FR-008 solver |
| 7 | Connection string | POMINIĘTY (Opcja B) | SKIP | — |
| 8 | Weryfikacja | `curl -v https://plan-zajec-gabi-01.azurewebsites.net/weatherforecast` | OK | HTTP 200, JSON z 5 prognozami |

---

## Zasoby Azure

| Zasób | Nazwa | Region | SKU / Tier |
|-------|-------|--------|------------|
| Resource Group | `rg-plan-zajec-uczelnia` | Poland Central | — |
| App Service Plan | `gnunezwietrzynska_asp_*` (auto) | Poland Central | F1 Free |
| Web App | `plan-zajec-gabi-01` | Poland Central | F1 Linux |

---

## Korekta regionu

Plan zakładał `westeurope`. Subskrypcja ma policy `listOfAllowedLocations` ograniczające
do: `polandcentral`, `swedencentral`, `germanywestcentral`, `spaincentral`, `francecentral`.

**Użyto:** `polandcentral` — optymalny wybór dla polskiej uczelni.

Plik `context/foundation/infrastructure.md` zaktualizowany: region `westeurope` → `polandcentral`.

---

## Sekrety wpisane ręcznie

Brak w tej fazie (Opcja B — bez Postgres). Sekrety DB dodać przed Opcją A:
- `DefaultConnection` — connection string do PostgreSQL (App Service → Configuration, nigdy w repo)

---

## Granice bezpieczeństwa

| Akcja | Wykonano przez |
|-------|---------------|
| `az group create`, `az webapp up` | Ty (terminal) |
| Weryfikacja curl | Ty (terminal) |
| Budget alert | Ty (portal) |
| Stop/delete PostgreSQL, drop RG | Tylko Ty ręcznie (gdy dodasz) |
| Rotacja sekretów | Tylko Ty ręcznie |

---

## Następne kroki (faza 2)

1. **PostgreSQL** — `az postgres flexible-server create` w `polandcentral` + connection string w App Settings
2. **GitHub Actions CI/CD** — OIDC federated credentials, auto-deploy po merge do `main`
3. **Staging app** — drugi F1 `plan-zajec-gabi-01-staging` w tym samym RG
4. **Implementacja FR** — auth (FR-001), import (FR-007), solver (FR-008), eksport (FR-010)

---

## Poza zakresem tego wdrożenia

- EF Core migrations (przed FR-001)
- Azure OpenAI wiring (przed FR-008)
- Production SLA / upgrade do B1+ (przed pilotem)
