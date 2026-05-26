---
bootstrapped_at: 2026-05-21T19:53:53Z
starter_id: dotnet
starter_name: ".NET (ASP.NET Core webapi)"
project_name: plan-zajec-uczelnia
language_family: dotnet
package_manager: dotnet
cwd_strategy: subdir-then-move
bootstrapper_confidence: verified
phase_3_status: ok
audit_command: "dotnet list package --vulnerable"
---

## Hand-off

```yaml
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
  has_auth: true
  has_ai: true
```

Solo, web-app na 5 tygodni po godzinach, z logowaniem i generowaniem planu z komponentem AI — wybrano ścieżkę custom w rodzinie .NET zamiast rekomendowanego JS Astro. ASP.NET Core Web API (`dotnet`) daje silne typowanie C#, oficjalny szablon `dotnet new webapi`, konwencje znane agentom i zweryfikowany bootstrapper. PostgreSQL i integracja AI dołożą się w warstwie aplikacji; deploy domyślnie Azure App Service, CI na GitHub Actions z auto-deploy po merge. Frontend koordynatora (UI importu, podglądu planu, eksportu) będzie osobną warstwą obok API — zgodnie z PRD web-app, bez multi-tenant i bez live SIS w MVP.

## Pre-scaffold verification

| Signal      | Value                                 | Severity | Notes                                                |
| ----------- | ------------------------------------- | -------- | ---------------------------------------------------- |
| npm package | not run                               | —        | dotnet starter — no npm create CLI                   |
| GitHub repo | dotnet/aspnetcore pushed 2026-05-21   | fresh    | proxy; card docs_url is learn.microsoft.com          |

## Scaffold log

**Resolved invocation**: `dotnet new webapi -n .bootstrap-scaffold --no-restore`

**Strategy**: scaffold into a temp directory, then move files up (subdir-then-move). Because `plan-zajec-uczelnia/` already existed from a prior run, merge targeted that folder (with `.bootstrap-scaffold.*` renamed to `plan-zajec-uczelnia.*`) instead of repo root.

**Exit code**: 0

**Files moved**: 0 (all targets already present)

**Conflicts (.scaffold siblings)**: Program.cs, Properties, appsettings.json, appsettings.Development.json, plan-zajec-uczelnia.csproj, plan-zajec-uczelnia.http

**.gitignore handling**: absent in scaffold

**.bootstrap-scaffold cleanup**: deleted

**Note**: `context/` and root `AGENTS.md` preserved. Compare fresh template via `*.scaffold` siblings in `plan-zajec-uczelnia/`.

## Post-scaffold audit

**Command**: `dotnet list package --vulnerable` (from `plan-zajec-uczelnia/`)

**Exit code**: 0

**Summary**: No vulnerable packages after `dotnet restore` and `dotnet build`.

## Hints recorded but not acted on in v1

- deployment_target: azure-app-service (no deploy scaffolding in v1)
- ci_provider: github-actions, ci_default_flow: auto-deploy-on-merge (no CI files in v1)
- has_auth, has_ai: logged; not wired in template
- self_check_answers, path_taken, quality_override: logged only
- AGENTS.md / CLAUDE.md: deferred to M1L4

## Next steps

- API: `cd plan-zajec-uczelnia && dotnet run`
- Optional: diff template vs current — `diff Program.cs Program.cs.scaffold` (and other `*.scaffold` files); remove `.scaffold` siblings when done
- Agent context: `/10x-agents-md` if not already satisfied by root `AGENTS.md`
- First commit / CI workflows — on your side (bootstrapper does not init git or deploy pipelines in v1)
