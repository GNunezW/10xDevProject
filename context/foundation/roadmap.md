---
project: "Plan Zajęć Uczelnia"
version: 1
status: draft
created: 2026-05-27
updated: 2026-09-13
prd_version: 8
main_goal: speed
top_blocker: decisions
---

# Roadmap: Plan Zajęć Uczelnia

> Wygenerowano z `context/foundation/prd.md` (v1) + automatycznie zbadanego stanu kodu.
> Edytuj w miejscu; archiwizuj gdy superseded.
> Slice'y poniżej są w kolejności zależności. Tabela "At a glance" jest indeksem.

## Vision recap

Koordynator planowania na uczelni spędza tygodnie na ręcznym układaniu planu zajęć z wielu źródeł: dostępność prowadzących, sale (numer + typ), siatka godzin, przypisania prowadzących. Mimo wysiłku studenci dostają złe rozkłady — za dużo okienek i nieoptymalne przerwy. MVP ma przyjąć te dane i wygenerować plan z minimalną liczbą okienek w obrębie każdego trybu studiów (stacjonarni w tygodniu, niestacjonarni w weekendy), przy spełnieniu wszystkich twardych ograniczeń (brak kolizji prowadzący/sala, zgodność z trybem studiów).

## North star

**S-05: generate-and-view-schedule — Koordynator może uruchomić generowanie planu zajęć i zobaczyć wynik z metrykami okienek.**

> Gwiazda przewodnia to najmniejszy end-to-end przepływ, który — jeśli działa — udowadnia, że produkt realizuje swoją obietnicę. Trafia na listę tak wcześnie, jak pozwalają zależności, bo jeśli ten przepływ nie działa, reszta nie ma sensu. Dla tego projektu: jeśli solver generuje plan bez kolizji i koordynator widzi metryki okienek — teza produktu jest udowodniona (US-01 + główne kryterium sukcesu PRD).

## At a glance

| ID | Change ID | Outcome (koordynator może …) | Prerequisites | PRD refs | Status |
|---|---|---|---|---|---|
| F-01 | secure-api-routes | (foundation) wszystkie trasy API wymagają ważnego tokenu JWT | — | FR-001 | done |
| S-01 | subjects-lecturers-and-grid | definiować kierunki studiów i zarządzać listą przedmiotów przypisanych do kierunku i semestru z prowadzącymi oraz siatką godzin | F-01 | FR-002, FR-003, FR-007, FR-008, US-01 | done |
| S-02 | lecturer-availability | wprowadzać dostępność prowadzących na okres planowania | S-01 | FR-004, US-01 | done |
| S-03 | rooms-model-and-preferences | wprowadzać sale (numer + typ zajęć); bez macierzy dostępności i preferencji (wariant A) | S-01 | FR-005, US-01 | done |
| S-04 | import-scheduling-data | zaimportować dane planowania z pliku | F-01 | FR-009 | parked |
| S-05 | generate-and-view-schedule | uruchomić generowanie planów zajęć (jedno uruchomienie, siatka per kierunek) i zobaczyć wyniki z metrykami okienek | S-01, S-02, S-03 | FR-010, FR-011, US-01 | done |
| S-06 | export-schedule | wyeksportować wygenerowany plan | S-05 | FR-012 | done |
| F-02 | ci-github-actions | (foundation) `dotnet build` na GitHub Actions przy PR/push | — | tech-stack CI | planned |
| F-03 | xunit-smoke-tests | (foundation) `dotnet test` dla czystej logiki szablonu tygodnia i trybu studiów | — | lessons / agent-readiness | planned |

## Streams

Pomoc nawigacyjna — grupuje pozycje, które dzielą łańcuch zależności. Kanoniczna kolejność wciąż żyje w grafie zależności poniżej; ta tabela to proponowana kolejność czytania między równoległymi ścieżkami.

| Stream | Temat | Łańcuch | Uwaga |
|---|---|---|---|
| A | Autoryzacja + dane planowania | `F-01` → `S-01` → `S-02` / `S-03` → `S-05` → `S-06` | Krytyczna ścieżka MVP: ukończona (S-04 parked). |
| B | Import danych | `F-01` → `S-04` | Parked (PRD v7): import poza MVP; dane demo = seedy + CRUD |
| C | Jakość / CI | `F-02` ∥ `F-03` | Porządki po MVP; nie zmieniają flow login → dane → generuj → podgląd → Excel |

## Baseline

Stan kodu na 2026-05-27 (automatycznie zbadany + potwierdzony przez użytkownika).
Fundamenty poniżej zakładają, że te warstwy są na miejscu i ich NIE reskaffoldowują.

- **Frontend:** Blazor Server — decyzja 2026-06-01; dodany do tego samego projektu ASP.NET Core; biblioteka MudBlazor; UI zawsze najprostszy (MudBlazor defaults, żadnych własnych komponentów)
- **Backend / API:** present — ASP.NET Core 10, `Program.cs:9`, `Endpoints/AuthEndpoints.cs`
- **Data:** present — EF Core + Npgsql, `Data/ApplicationDbContext.cs`, `Data/Migrations/`
- **Auth:** partial — Identity + JWT wdrożone (`Program.cs:17-47`), ale żadna trasa API nie wymaga jeszcze autoryzacji
- **Deploy / infra:** partial — Azure App Service wdrożony ręcznie; brak `.github/workflows/` w repo
- **Observability:** partial — tylko domyślny `Logging` z ASP.NET (`appsettings.json`); brak Serilog / Application Insights / OpenTelemetry

## Foundations

### F-01: Zabezpieczenie tras API

- **Outcome:** (foundation) wszystkie trasy API obsługujące dane planowania i generowanie wymagają ważnego tokenu JWT — brak dostępu dla niezalogowanych żądań.
- **Change ID:** secure-api-routes
- **PRD refs:** FR-001, sekcja `## Access Control` (jedna rola: koordynator, pełny dostęp)
- **Unlocks:** S-01, S-02, S-03, S-04, S-05, S-06 — żaden slice z danymi nie powinien być dostępny bez autoryzacji
- **Prerequisites:** —
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Auth jest partial — Identity i JWT wdrożone, ale trasy niezabezpieczone. Pominięcie tego fundamentu oznacza, że każdy slice z danymi zostanie wdrożony bez ochrony. Najprostszy krok do zamknięcia luki przed wdrożeniem danych produkcyjnych.
- **Status:** done

### F-02: CI — GitHub Actions (build)

- **Outcome:** (foundation) przy push/PR GitHub Actions odpala `dotnet restore` + `dotnet build` na `plan-zajec-uczelnia/plan-zajec-uczelnia.csproj` — bez deployu, bez `dotnet test`.
- **Change ID:** ci-github-actions
- **PRD refs:** — (tech-stack: `ci_provider: github-actions`; NFR nie wymaga CI do zaliczenia flow)
- **Unlocks:** —
- **Prerequisites:** — (istniejący csproj)
- **Parallel with:** F-03
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Workflow tylko buduje projekt webowy. Nie dodaje `.sln`, nie rusza `Program.cs`, appsettings, migracji ani Azure. Deploy (`az webapp up` / auto-deploy) zostaje parked.
- **UI (Blazor):** —
- **Status:** planned

### F-03: Smoke testy xUnit (bez bazy)

- **Outcome:** (foundation) repo ma projekt testowy i `dotnet test` dla czystej logiki już istniejącej: `ScheduleWeekCalculator` (tygodnie dydaktyczne) oraz `ScheduleGridData.GetDaysForMode` (stac vs niestac).
- **Change ID:** xunit-smoke-tests
- **PRD refs:** — (nie nowa funkcja produktu; pokrycie reguły trybu studiów / szablonu tygodnia)
- **Unlocks:** —
- **Prerequisites:** — (publiczne helpery w `Services/Scheduling/`)
- **Parallel with:** F-02
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Nowy katalog `plan-zajec-uczelnia.Tests/` + `ProjectReference` do web csproj. Bez InternalsVisibleTo, bez WebApplicationFactory, bez PostgreSQL, bez seedów, bez zmian solvera/UI. Nie dodaje `.sln` (unik kolizji z F-02).
- **UI (Blazor):** —
- **Status:** planned

## Slices

### S-01: Kierunki, przedmioty, prowadzący i siatka godzin

- **Outcome:** koordynator może definiować kierunki studiów (StudyProgram) i zarządzać listą przedmiotów przypisanych do kierunku i semestru z prowadzącymi oraz definiować siatkę godzin (poprawne sloty czasowe 8:00–20:00).
- **Change ID:** subjects-lecturers-and-grid
- **PRD refs:** FR-002, FR-003, FR-007, FR-008, US-01
- **Prerequisites:** F-01
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Dane z tego slice'a są wejściem dla każdego kolejnego slice'a. Model danych StudyProgram + Subject zamknięty (2026-06-01). Błąd w tym modelu cofnie pracę nad S-02, S-03 i S-05. Ten slice jako pierwszy wdraża Blazor Server + MudBlazor do projektu — setup jednorazowy tutaj.
- **UI (Blazor):** lista kierunków (MudDataGrid) + formularz dodawania/edycji (MudDialog); lista przedmiotów per kierunek + formularz; siatka godzin jako osobna strona.
- **Status:** done

### S-02: Dostępność prowadzących

- **Outcome:** koordynator może wprowadzać dostępność prowadzących na okres planowania.
- **Change ID:** lecturer-availability
- **PRD refs:** FR-004, US-01
- **Prerequisites:** S-01
- **Parallel with:** S-03
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Dostępność prowadzących jest twardym ograniczeniem solvera. Brak tych danych = solver nie może generować poprawnego planu. Sekwencjonowane przed S-05.
- **UI (Blazor):** lista prowadzących (MudDataGrid) + formularz dostępności (sloty jako checkboxy lub MudTimePicker).
- **Status:** done

### S-03: Model sal (wariant A)

- **Outcome:** koordynator może wprowadzać sale (numer + typ zajęć). Solver dobiera salę pasującego typu; brak macierzy dostępności per slot i brak preferencji sal (PRD v6, S-03 wariant A).
- **Change ID:** rooms-model-and-preferences
- **PRD refs:** FR-005, US-01
- **Prerequisites:** S-01
- **Parallel with:** S-02
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Świadome cięcie vs wczesny PRD: sale zawsze wolne po typie. Kolizja sali nadal twarda. Preferencje (dawne FR-006) — poza MVP.
- **UI (Blazor):** lista sal (MudDataGrid) + formularz sali (numer, typ zajęć).
- **Status:** done

### S-04: Import danych planowania

- **Outcome:** koordynator może zaimportować dane planowania z pliku (przedmioty, prowadzący, dostępność, sale).
- **Change ID:** import-scheduling-data
- **PRD refs:** FR-009
- **Prerequisites:** F-01
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Świadomie odłożone. MVP ładuje dane seedami + CRUD. Import wraca przy pilocie na realnej skali.
- **UI (Blazor):** — (poza MVP)
- **Status:** parked

### S-05: Generowanie i podgląd planu

- **Outcome:** koordynator może uruchomić jedno generowanie planu (wszystkie kierunki naraz) minimalizujące okienka w obrębie każdego kierunku i trybu studiów, zobaczyć szablon tygodnia z metrykami okienek.
- **Change ID:** generate-and-view-schedule
- **PRD refs:** FR-010, FR-011, US-01
- **Prerequisites:** S-01, S-02, S-03
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Gwiazda przewodnia — solver CP-SAT, limit 60 s, szablon tygodnia. Model sal wariant A (PRD v6).
- **UI (Blazor):** `/generuj-plan` + spinner; `/plan/{id}` siatka + metryki okienek (MudBlazor defaults).
- **Status:** done

### S-06: Eksport planu

- **Outcome:** koordynator może wyeksportować wygenerowany plan do pliku.
- **Change ID:** export-schedule
- **PRD refs:** FR-012
- **Prerequisites:** S-05
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Bez eksportu plan nie trafia do procesu uczelni. Format: Excel `.xlsx` (ClosedXML), jeden plik per kierunek.
- **UI (Blazor):** przycisk "Pobierz Excel" na `/plan/{runId}` obok wyboru kierunku; GET `/plan/{runId}/eksport` zwraca `.xlsx`.
- **Status:** done

## Backlog Handoff

| Roadmap ID | Change ID | Sugerowany tytuł issue | Gotowe do `/10x-plan` | Uwagi |
|---|---|---|---|---|
| F-01 | secure-api-routes | Zabezpiecz trasy API tokenem JWT | — | done (zarchiwizowane) |
| S-01 | subjects-lecturers-and-grid | CRUD: kierunki, przedmioty, prowadzący, siatka godzin | — | done (zarchiwizowane) |
| S-02 | lecturer-availability | CRUD: dostępność prowadzących | — | done (zarchiwizowane) |
| S-03 | rooms-model-and-preferences | CRUD: sale numer + typ (wariant A) | — | done (zarchiwizowane) |
| S-04 | import-scheduling-data | Import danych planowania z pliku | nie | Parked — FR-009 poza MVP (PRD v7) |
| S-05 | generate-and-view-schedule | Generowanie + podgląd z metrykami okienek | — | done (zarchiwizowane) |
| S-06 | export-schedule | Eksport wygenerowanego planu (Excel) | — | done (zarchiwizowane) |
| F-02 | ci-github-actions | GitHub Actions: restore + build (bez deploy, bez test) | tak | Parallel z F-03; tylko `.github/workflows/` |
| F-03 | xunit-smoke-tests | xUnit: WeekCalculator + GetDaysForMode (bez DB) | tak | Parallel z F-02; tylko nowy projekt testowy |

## Open Roadmap Questions

1. ~~**Model sal dla niestacjonarnych**~~ — ✓ PRD v6: te same sale; tryb studiów steruje dniami, nie macierzą sal.
2. ~~**Typ sali vs konkretna sala**~~ — ✓ S-03 wariant A: numer + typ; bez pojemności i preferencji w MVP.
3. ~~**Model kierunku studiów**~~ — ✓ rozstrzygnięte 2026-06-01: StudyProgram jako osobna encja (nazwa, tryb stac/niestac, rok akademicki); Subject przypisany do StudyProgram + semestr (int).
4. ~~**Format eksportu**~~ — ✓ 2026-08-28: Excel `.xlsx` (ClosedXML). Import (S-04) parked.

## Parked

- **Multi-tenant (wiele uczelni)** — PRD §Poza zakresem; MVP obsługuje jedną szkołę.
- **Portal studencki** — PRD §Poza zakresem; koordynator eksportuje wynik na zewnątrz.
- **Samodzielna edycja przez prowadzących** — PRD §Poza zakresem; koordynator wprowadza dane w MVP.
- **Import danych planowania z pliku (S-04 / FR-009)** — PRD v7; MVP = seedy + CRUD. Wróci przy pilocie na realnej skali.
- **Synchronizacja z systemem dziekanackim (live SIS)** — PRD §Poza zakresem; dane przez CRUD lub seedy.
- **CI/CD GitHub Actions (deploy)** — auto-deploy on merge i `az webapp up` zostają parked. Sam **build na Actions** to F-02 (`planned`).
- **Observability (Application Insights / OpenTelemetry)** — `main_goal: speed`; domyślny logging ASP.NET wystarcza na MVP.
- **Frontend UI koordynatora** — poza repo; API-first w bieżącej fazie (`AGENTS.md`, `tech-stack.md`).

## Done

- **F-01: (foundation) wszystkie trasy API obsługujące dane planowania i generowanie wymagają ważnego tokenu JWT** — Zarchiwizowano 2026-09-13 → `context/archive/2026-05-31-secure-api-routes/`. Lekcja: —.
- **S-01: koordynator może definiować kierunki studiów (StudyProgram) i zarządzać listą przedmiotów** — Zarchiwizowano 2026-09-13 → `context/archive/2026-06-01-subjects-lecturers-and-grid/`. Lekcja: —.
- **S-02: koordynator może wprowadzać dostępność prowadzących na okres planowania** — Zarchiwizowano 2026-09-13 → `context/archive/2026-06-01-lecturer-availability/`. Lekcja: —.
- **S-03: koordynator może wprowadzać sale (numer + typ zajęć), bez dostępności per slot i bez preferencji** — Zarchiwizowano 2026-06-02 → `context/archive/2026-06-01-rooms-model-and-preferences/`. PRD v6 (2026-08-28) zrównał FR-005/FR-006 z tym wariantem.
- **S-05: koordynator może uruchomić jedno generowanie planu i zobaczyć szablon tygodnia z metrykami okienek** — Zarchiwizowano 2026-09-13 → `context/archive/2026-06-02-generate-and-view-schedule/`. Lekcja: —.
- **S-06: koordynator może wyeksportować wygenerowany plan do pliku** — Zarchiwizowano 2026-09-13 → `context/archive/2026-08-28-export-schedule/`. Lekcja: —.
- **testing-critical-path-coverage: Phase 1 test rollout (obsada, kolizje, tryb studiów)** — Zarchiwizowano 2026-09-13 → `context/archive/2026-09-03-testing-critical-path-coverage/`. Lekcja: —.
