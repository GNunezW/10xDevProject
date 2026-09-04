# Test Plan

> Phased test rollout for this project. Strategy is frozen at the top
> (§1–§5); cookbook patterns at the bottom (§6) fill in as phases ship.
> Read before writing any new test.
>
> Refresh: re-run `/10x-test-plan --refresh` when stale (see §8).
>
> Last updated: 2026-09-04

## 1. Strategy

Tests follow three non-negotiable principles for this project:

1. **Cost × signal.** The cheapest test that gives a real signal for the
   risk wins. Do not promote to e2e because e2e "feels safer." Do not put a
   vision model on top of a deterministic visual diff that already catches
   the regression.
2. **User concerns are first-class evidence.** Risks anchored in "<the
   team is worried about X, and the failure would surface somewhere in
   <area>>" carry the same weight as PRD lines or hot-spot data.
3. **Risks are scenarios, not code locations.** This plan documents *what
   could fail* and *why we believe it's likely* — drawn from documents,
   interview, and codebase *signal* (churn, structure, test base). It does
   NOT claim to know which line owns the failure. That knowledge is
   produced by `/10x-research` during each rollout phase. If the plan and
   research disagree about where the failure lives, research is the
   ground truth.

Hot-spot scope used for likelihood weighting: `plan-zajec-uczelnia/Services/`, `plan-zajec-uczelnia/Endpoints/`, `plan-zajec-uczelnia/Components/`, `plan-zajec-uczelnia/Models/`, `plan-zajec-uczelnia/Data/` (excluding Migrations). Skan 30 dni: insufficient git history — likelihood opiera się na roadmapie i wywiadzie.

## 2. Risk Map

The top failure scenarios this project must protect against, ordered by
risk = impact × likelihood. Risks are failure scenarios in user / business
terms, not test names. The Source column cites the *evidence that surfaced
this risk* — never a specific file as "where the failure lives" (that is
research's job, see §1 principle #3).

| # | Risk (failure scenario) | Impact | Likelihood | Source (evidence — not anchor) |
|---|-------------------------|--------|------------|--------------------------------|
| 1 | Koordynator nie może wygenerować planu, bo obsada wymaga N różnych prowadzących na N grup, zamiast rozłożyć grupy na inne sloty/dni tak, by jeden wolny prowadzący objął więcej grup | High | High | interview Q2, Q3; PRD § Business Logic; roadmap S-05 |
| 2 | Zapisany szablon tygodnia ma podwójną rezerwację prowadzącego albo sali w tym samym slocie | High | Medium | interview Q1; PRD US-01 AC; FR-010 |
| 3 | Brak układu (twarde reguły niespełnione albo 60 s bez rozwiązania), a plan i tak zostaje zapisany | High | Medium | PRD Vision + US-01 AC + NFR |
| 4 | Zajęcia stacjonarne lądują w weekend (albo niestacjonarne w tygodniu) w zapisanym lub eksportowanym szablonie | High | Medium | PRD Guardrails; FR-010; roadmap F-03 |
| 5 | Niezalogowany caller uruchamia generowanie (~60 s) albo czyta/eksportuje dane planu | High | Medium | PRD Access Control + NFR; roadmap F-01; abuse lens |
| 6 | Excel pomija grupy, weekendy albo wybrany kierunek | Medium | Medium | PRD FR-012; roadmap S-06; change export-schedule (manual only) |
| 7 | Liczba slotów tygodniowo na przedmiot jest zła, bo źle policzono tygodnie dydaktyczne / dni wolne — plan „valid”, ale niedoucza albo przeucza | Medium | Medium | PRD FR-010 + Business Logic; roadmap F-03 |

### Risk Response Guidance

| Risk | What would prove protection | Must challenge | Context `/10x-research` must ground | Likely cheapest layer | Anti-pattern to avoid |
|------|-----------------------------|----------------|--------------------------------------|-----------------------|-----------------------|
| #1 | Fixture: 1 wolny prowadzący, 4 grupy tego przedmiotu, wystarczające sloty → generowanie **możliwe**; system rozłoży grupy na różne godziny/dni | **Fałszywe założenie:** „4 grupy ⇒ 4 różnych prowadzących”. Prawidłowo: jeden prowadzący może poprowadzić wiele grup, jeśli sloty się nie nakładają | Jak liczone są grupy vs wymagania obsady vs dostępność w czasie; oracle z interview Q2, nie z obecnego kodu | unit jeśli czysta reguła obsady; inaczej integracja solvera na małym zestawie | test utrwalający dzisiejsze „za mało prowadzących” jako oczekiwany wynik |
| #2 | Po sukcesie: ten sam prowadzący/sala nie występuje dwa razy w tym samym slocie (wszystkie kierunki) | Status `Succeeded` ⇒ brak kolizji | Kontrakt zapisu po generate; definicja „ten sam slot” | unit/integracja na wyniku szablonu | mirror implementacji solvera; happy-path bez asercji kolizji |
| #3 | Dane niewykonalne albo timeout → komunikat błędu, **brak** zapisanego planu | Pusty/error UI ⇒ nic nie poszło do bazy | Stan ScheduleRun / sesji po fail i po timeout | integracja generate + persystencja | asercja tylko HTTP 200/4xx bez sprawdzenia zapisu |
| #4 | Stac: tylko Pn–Pt; niestac: tylko Sb–Nd w zapisanym szablonie | Helper dni tygodnia wystarczy, jeśli zapisany plan może się rozjechać | Tryb kierunku → dni w wyniku, nie tylko w helperze | unit GetDaysForMode + 1 asercja na zapisanym szablonie | pokrycie helpera bez sprawdzenia wyniku generate |
| #5 | Bez JWT: generate/export/dane → 401/403 i **brak** startu solvera | Login page istnieje ⇒ API jest zamknięte | Które trasy mają FallbackPolicy; side-effect generate | WebApplicationFactory + test auth scheme | mock auth wewnętrzny tak, że nie sprawdzasz anonima |
| #6 | Pobrany `.xlsx` ma wszystkie semestry/grupy wybranego kierunku, w tym weekendy niestac | Plik się pobiera ⇒ treść kompletna | Jeden plik per kierunek (FR-012); źródło wierszy | integracja ClosedXML na znanym runie | snapshot binarny xlsx |
| #7 | Przy znanych datach semestru i dniach wolnych liczba slotów/przedmiot zgadza się z PRD (spotkania / tygodnie dydaktyczne) | Seedy demo ⇒ wzór jest poprawny | Wejścia: sesje, tygodnie, non-working days | unit kalkulatora tygodni (F-03) | expected przepisane z produkcji |

## 3. Phased Rollout

Each row is a discrete rollout phase that will open its own change folder
via `/10x-new`. Status moves left-to-right through the values below; the
orchestrator updates Status as artifacts appear on disk.

| # | Phase name | Goal (one line) | Risks covered | Test types | Status | Change folder |
|---|------------|-----------------|---------------|------------|--------|---------------|
| 1 | Critical-path coverage | Wystawić xUnit i bronić obsady grup, kolizji oraz trybu studiów najtańszą warstwą | #1, #2, #4 | unit (+ mała integracja solvera jeśli #1 nie jest czystą funkcją) | change opened | context/changes/testing-critical-path-coverage/ |
| 2 | Generation persist contract | Fail/timeout nie zapisuje planu; wzór tygodni dydaktycznych ma niezależny oracle | #3, #7 | integration + unit | not started | — |
| 3 | Auth and costly-op gate | Anonim nie odpala 60 s generate ani nie eksportuje | #5 | integration (WebApplicationFactory) | not started | — |
| 4 | Export completeness + quality gates | Excel kompletny; `dotnet test` na PR | #6, cross-cutting | integration + CI gates | not started | — |

## 4. Stack

The classic test base for this project. AI-native tools (if any) carry a
`checked:` date so future readers can see which lines need re-verification.

| Layer | Tool | Version | Notes |
|-------|------|---------|-------|
| unit + integration | xUnit | v3 (`xunit.v3.mtp-v2` 4.0.0) | `plan-zajec-uczelnia.Tests/`; `dotnet test --project …`; checked: 2026-09-04 |
| API integration | Microsoft.AspNetCore.Mvc.Testing | 10.x (with web csproj) | WebApplicationFactory + test auth scheme; checked: 2026-09-03 |
| API mocking | — | — | none yet; mock tylko na krawędzi sieci |
| e2e | — | — | none yet; Blazor e2e tylko gdy integracja nie łapie regresji |
| accessibility | — | — | none yet |
| (optional) AI-native | — | n/a | not used — solver/reguły wymagają deterministycznego orakla |

**Stack grounding tools (current session):**
- Docs: Context7 — xUnit v3 (`dotnet new xunit3`), ASP.NET Core WebApplicationFactory + test auth scheme; checked: 2026-09-03
- Search: not available in current session; checked: 2026-09-03
- Runtime/browser: cursor-ide-browser — możliwy smoke Blazor; nie tańszy niż test deterministyczny solvera; checked: 2026-09-03
- Provider/platform: none — brak GitHub/Azure MCP w sesji; checked: 2026-09-03

## 5. Quality Gates

The full set of gates that must pass before a change reaches production.
"Required for §3 Phase <N>" means the gate is enforced once that rollout
phase lands; before that, the gate is `planned`.

| Gate | Where | Required? | Catches |
|------|-------|-----------|---------|
| lint + typecheck (`dotnet build`) | local + CI | required | syntactic / type drift |
| unit + integration (`dotnet test`) | local + CI | required after §3 Phase 4 | logic regressions (obsada, kolizje, persist) |
| e2e on critical flows | CI on PR | planned | broken critical user paths — defer until integration gaps proven |
| post-edit hook | local (agent loop) | planned | regressions at edit time |
| visual diff (deterministic) | CI on PR | optional | rendering regressions |
| multimodal visual review | CI on PR | optional | visual issues classic diff misses |
| pre-prod smoke | between merge + prod | optional | environment-specific failures |

## 6. Cookbook Patterns

How to add new tests in this project. Each sub-section is filled in once
the relevant rollout phase ships; before that, the sub-section reads
"TBD — see §3 Phase <N>."

### 6.1 Adding a unit test

- **Location**: `plan-zajec-uczelnia.Tests/` (sibling of the web project; no `.sln`).
- **Naming**: `<Type>Tests.cs` (e.g. `SubjectStaffingEvaluatorTests.cs`).
- **Reference test**: `plan-zajec-uczelnia.Tests/SubjectStaffingEvaluatorTests.cs`.
- **Run locally**: `dotnet test --project plan-zajec-uczelnia.Tests/plan-zajec-uczelnia.Tests.csproj` (SDK 10 requires `--project`).
- **Oracle**: assert user/business behavior (1 lecturer can cover 4 groups when weekly *slots* suffice). Do not copy production message strings or week-calculator internals as expected values.

### 6.2 Adding an integration test

- TBD — see §3 Phase 2 (generate persist contract) and Phase 3 (auth gate).

### 6.3 Adding an e2e test

- TBD — not planned in initial rollout; prefer integration until gap proven.

### 6.4 Adding a test for a new API endpoint

- TBD — see §3 Phase 3 (auth + costly-op gate pattern).

### 6.5 Adding a test for scheduling / solver behavior

- **Staffing (risk #1)**: hand-built `Subject` + `SubjectStaffingContext`; 120 students / 30 per group → 4 groups; `NumberOfSessions = 1` so weekly need is 4. See `SubjectStaffingEvaluatorTests.cs`.
- **Collisions (risk #2)**: test-only `ScheduleCollisionOracle` groups by `(LecturerId|RoomNumber, Day, TimeSlotId)`. Include a deliberate clash. Apply the same oracle to `ScheduleCpSatSolver.Solve` placements. See `ScheduleCollisionOracleTests.cs` and `ScheduleCpSatSolverTests.cs`.
- **Study mode (risk #4)**: unit `GetDaysForMode`, then assert every placement `Day` is in that set for FullTime- and PartTime-shaped domains. Generator must call `ScheduleGridData.GetDaysForMode` (no inline day arrays). See `ScheduleGridDataTests.cs`.
- **Anti-patterns**: do not expect “too few lecturers” for 1 lecturer + 4 groups + enough slots; do not treat `Succeeded` as a collision proof without the oracle; do not test the helper without an output/domain assertion.

### 6.6 Per-rollout-phase notes

Phase 1 (`testing-critical-path-coverage`): xUnit v3 (`xunit.v3.mtp-v2` 4.0.0), `global.json` sets `test.runner` to Microsoft.Testing.Platform. Template `UnitTest1` removed. No Postgres in this suite.

## 7. What We Deliberately Don't Test

Exclusions agreed during the rollout (Phase 2 interview, Q5). Future
contributors should respect these unless the underlying assumption changes.

- **Import danych z pliku (S-04 / FR-009)** — parked poza MVP; brak implementacji do testowania. Re-evaluate if S-04 wraca z roadmapy. (Source: Phase 2 interview Q5.)
- **Szablon `/weatherforecast` i pliki `*.scaffold`** — artefakty bootstrapa, nie produkt. (Source: AGENTS.md hard rules.)
- **Wygląd MudBlazor (kolory, marginesy, snapshoty UI)** — łapią szum, nie kolizje ani obsadę. (Source: lessons.md — UI najprostszy.)

## 8. Freshness Ledger

- Strategy (§1–§5) last reviewed: 2026-09-03
- Stack versions last verified: 2026-09-03
- AI-native tool references last verified: 2026-09-03

Refresh (`/10x-test-plan --refresh`) when:

- a new top-3 risk surfaces from the roadmap or archive,
- a recommended tool's `checked:` date is older than three months,
- the project's tech stack changes (new framework, new test runner),
- §7 negative-space no longer matches what the team believes.
