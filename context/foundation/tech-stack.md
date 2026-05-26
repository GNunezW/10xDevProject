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
  has_realtime: false
  has_ai: true
  has_background_jobs: false
---

## Why this stack

Solo, web-app na 5 tygodni po godzinach, z logowaniem i generowaniem planu z komponentem AI — wybrano ścieżkę custom w rodzinie .NET zamiast rekomendowanego JS Astro. ASP.NET Core Web API (`dotnet`) daje silne typowanie C#, oficjalny szablon `dotnet new webapi`, konwencje znane agentom i zweryfikowany bootstrapper. PostgreSQL i integracja AI dołożą się w warstwie aplikacji; deploy domyślnie Azure App Service, CI na GitHub Actions z auto-deploy po merge. Frontend koordynatora (UI importu, podglądu planu, eksportu) będzie osobną warstwą obok API — zgodnie z PRD web-app, bez multi-tenant i bez live SIS w MVP.
