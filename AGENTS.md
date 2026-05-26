# Repository Guidelines

Plan Zajęć Uczelnia — MVP do planowania zajęć na uczelni (optymalizacja okienek, tryby stacjonarny/weekend). Backend: ASP.NET Core 10 w `plan-zajec-uczelnia/`; wymagania w `context/foundation/`.

## Hard rules for agents

- **Nie modyfikuj `context/`** poza jawnym poleceniem skilli 10x — `prd.md`, `tech-stack.md`, logi zmian są źródłem prawdy.
- **Przed feature:** czytaj `@context/foundation/prd.md`, `@context/foundation/tech-stack.md` i `@context/foundation/lessons.md` (przed implementacją i kodem produktowym). Zakres MVP: jedna uczelnia, auth koordynatora, import/eksport, generowanie planu z AI; bez portalu studenta i bez live SIS.
- **Feature flags w appsettings** — każda flaga musi mieć kill date; reguła w `@context/foundation/lessons.md`.
- **Nie usuwaj szablonu** `/weatherforecast` w `@plan-zajec-uczelnia/Program.cs` bez świadomej zamiany na API produktowe.
- **Nie commituj `*.scaffold`** w `plan-zajec-uczelnia/` — kopie z ponownego bootstrapa; porównaj (`diff Program.cs Program.cs.scaffold`), usuń po review.

## Project structure

- `context/foundation/` — PRD, shape-notes, tech-stack, lessons
- `context/changes/` — artefakty zmian (`/10x-new`, `/10x-archive`)
- `plan-zajec-uczelnia/` — `@plan-zajec-uczelnia/plan-zajec-uczelnia.csproj`, API
- `.cursor/skills/` — workflow 10xDevs

Bootstrap log (ostatni przebieg 2026-05-21): `@context/changes/bootstrap-verification/verification.md`.

## Build and dev commands

Z katalogu `plan-zajec-uczelnia/`:

- `dotnet restore` / `dotnet build` — obowiązkowe przed PR
- `dotnet run` — dev server; pipeline w `@plan-zajec-uczelnia/Program.cs`
- `dotnet list package --vulnerable` — audyt pakietów

Brak projektu testowego — po dodaniu xUnit dopisz `dotnet test` tutaj.

## Coding conventions

Stack i namespace: `@plan-zajec-uczelnia/plan-zajec-uczelnia.csproj`. Sekrety tylko user-secrets/env — `@plan-zajec-uczelnia/appsettings.json`. Nowy kod w `Services/`, `Endpoints/` lub `Controllers/`; nie rozrastaj `@plan-zajec-uczelnia/Program.cs`.

Planowane (PRD § Business Logic): PostgreSQL, auth, import, solver harmonogramu.

## Commits and PRs

Brak commitów w repo — przy `git init` używaj Conventional Commits (`feat:`, `fix:`). PR: który FR/change-id, wynik `dotnet build`, jawna lista edycji w `context/`.

## CI and deploy (planned)

Azure App Service + GitHub Actions (`@context/foundation/tech-stack.md`). Brak `.github/workflows/` — nie zakładaj gate’a CI, dopóki workflow nie powstanie.

Frontend koordynatora **poza repo** — na razie API-first.
