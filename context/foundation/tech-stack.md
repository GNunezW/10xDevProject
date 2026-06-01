---
starter_id: dotnet
package_manager: dotnet
project_name: plan-zajec-uczelnia
hints:
  language_family: dotnet
  team_size: solo
  deployment_target: azure-app-service
  ci_provider: github-actions
  ci_default_flow: auto-deploy-on-merge
  bootstrapper_confidence: verified
  path_taken: custom
  quality_override: false
  self_check_answers:
    typed: true
    from_official_starter: false
    conventions: true
    docs_current: true
    can_judge_agent: true
  has_auth: true
  has_payments: false
  has_realtime: true
  has_ai: true
  has_background_jobs: false
---

## Why this stack

Solo, web-app na 5 tygodni po godzinach, z logowaniem i generowaniem planu z komponentem AI — wybrano ścieżkę custom w rodzinie .NET zamiast rekomendowanego JS Astro. ASP.NET Core Web API (`dotnet`) daje silne typowanie C#, oficjalny szablon `dotnet new webapi`, konwencje znane agentom i zweryfikowany bootstrapper. PostgreSQL i integracja AI dołożą się w warstwie aplikacji; deploy domyślnie Azure App Service, CI na GitHub Actions z auto-deploy po merge.

Decyzja 2026-06-01: **Blazor Server** (w tym samym projekcie ASP.NET Core) zastępuje planowany osobny frontend. Jeden użytkownik (koordynator) — Blazor Server (SignalR) jest optymalny: brak osobnego repo, C# po obu stronach, serwisy i EF Core dostępne bezpośrednio. Biblioteka komponentów: **MudBlazor** — dostarcza gotowe DataGrid, formularze i nawigację bez własnego CSS. Zasada MVP: UI jest zawsze najprostszy jak to możliwe — MudBlazor defaults, żadnych własnych komponentów, żadnego stylizowania.

## Layers

| Warstwa | Technologia | Uwagi |
|---|---|---|
| API / backend | ASP.NET Core 10 Web API | JWT auth, Minimal API + Controllers |
| UI | Blazor Server (w tym samym projekcie) | Razor Components, SignalR |
| Komponenty UI | MudBlazor | DataGrid, MudForm, MudDialog — defaults only |
| ORM | EF Core 10 + Npgsql | Code-first, migrations |
| Baza danych | PostgreSQL (Azure Flexible Server) | |
| Auth | ASP.NET Core Identity + JWT Bearer | |
| Eksport | ClosedXML lub EPPlus | Excel (.xlsx) — FR-012 |
| Deploy | Azure App Service (Linux, F1 SKU) | `az webapp up` |
| CI | GitHub Actions | auto-deploy on merge (planned) |
