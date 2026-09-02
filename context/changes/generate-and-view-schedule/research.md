---
date: 2026-08-27T22:20:08+02:00
researcher: GNunezW
git_commit: 12e6d04cbc0ceee55de51676483ae07b6a0f4d06
branch: cursor/foundation-bootstrap-infra
repository: 10x-project
topic: "S-05 generate-and-view-schedule — czy żywy kod dostarcza najlepszy możliwy plan z podanych danych"
tags: [research, codebase, scheduling, cp-sat, s-05]
status: complete
last_updated: 2026-08-28
last_updated_by: GNunezW
---

# Research: S-05 — generowanie planu z danych (żywy kod vs PRD v5)

**Date**: 2026-08-27T22:20:08+02:00
**Researcher**: GNunezW
**Git Commit**: 12e6d04cbc0ceee55de51676483ae07b6a0f4d06
**Branch**: cursor/foundation-bootstrap-infra
**Repository**: 10x-project

## Research Question

`/10x-research S-05` (`generate-and-view-schedule`): jak działa żywy pipeline generowania, czy spełnia cel PRD v5 (dział podaje dane i dostaje najlepszy możliwy plan bez ręcznego układania przez dni/tygodnie), i co blokuje tę obietnicę.

## Summary

S-05 **jest zaimplementowane end-to-end**: koordynator na `/generuj-plan` waliduje dane, klika generowanie, czeka synchronicznie do 60 s, dostaje `ScheduleRun` + siatkę tygodnia na `/plan/{id}`. Silnik to Google OR-Tools CP-SAT (`Google.OrTools` 9.15). Zapisuje tylko układ **bez kolizji** prowadzącego i sali (globalnie). To realizuje rdzeń celu: **nie układać planu ręcznie**.

Trzy luki względem PRD v5 „najlepszy możliwy”:

1. **Funkcja celu nie stawia okienek na pierwszym miejscu.** Kara za zastępcę = 1000, puste okienko w środku dnia = 100, tie-break slotu = 1 (`ScheduleCpSatSolver.cs:323-325`). Solver woli poszarpany dzień niż zastępcę, który domknąłby dziury.
2. **FR-005 i FR-006 nie żyją w modelu.** Sale są zawsze dostępne po typie zajęć; brak macierzy dostępności i preferowanej sali (usunięte w migracji `RefactorSchedulingCapacityModel`). **PRD v6 (2026-08-28):** to jest kontrakt MVP, nie luka.
3. **Ziarno okienek jest niespójne.** Solver minimalizuje dziury **per kierunek × dzień** (wszystkie semestry i grupy razem); UI pokazuje metryki **per kierunek × semestr**; student odczuwa **grupę**.

Demo (Informatyka + Technologia chemiczna, oba stacjonarne, ~103 przedmioty) w tej sesji dało `Succeeded`, 284 sesje, zero kolizji. Nie ma seedu niestacjonarnego.

## Detailed Findings

### Pipeline: UI → walidacja → CP-SAT → persystencja

- Wejście koordynatora: `Components/Pages/GenerateSchedule.razor:1` (`/generuj-plan`). Auth przez `MainLayout.razor` `[Authorize]` + `Routes.razor` `AuthorizeRouteView` — strony generowania **nie** mają własnego `@attribute [Authorize]`.
- Walidacja i generowanie idą **bezpośrednio przez DI**, nie przez HTTP. `GenerateSchedule.razor:111-115` woła `IScheduleGenerationService.GenerateAsync()` bez `CancellationToken`.
- Spinner „do 60 s”: `GenerateSchedule.razor:40-44`. Sukces → `/plan/{run.Id}`; porażka → `ErrorMessage` na stronie, bez sesji.
- REST: `Endpoints/ScheduleEndpoints.cs:15-33` — `GET /api/scheduling/validate`, `POST /api/scheduling/generate`, **tylko JWT**. Sesja cookie z Blazora tych endpointów nie otwiera.
- Rdzeń: `ScheduleGenerationService.cs:12-21` — `SemaphoreSlim` na proces; drugi równoległy request dostaje od razu `Failed`, bez kolejki. Limit solvera: `ScheduleGenerationService.cs:11` (`60`). CP-SAT w `Task.Run` (`:72-75`).
- Startup czyści osierocone `Running` starsze niż 2 min: `Program.cs:172-193`.
- Podgląd: `ViewSchedule.razor:1` (`/plan/{RunId:int}`) — siatka, metryki, filtr semestru, podsumowanie grup.

### Solver vs „najlepszy możliwy plan”

**Twarde (żywe):**

| Reguła | Gdzie | vs PRD |
|---|---|---|
| 1 prowadzący / 1 sala na (dzień, slot), globalnie | `ScheduleCpSatSolver.cs:107-138` `AddAtMostOne` | Zgodne z FR-010 |
| Kohorta studentów: max 1 przedmiot na (kierunek, semestr, grupa, slot) | `ScheduleCpSatSolver.cs:158-184` | Drobniejsze niż „kierunek” |
| Zakaz sąsiednich slotów | `ScheduleCpSatSolver.cs:186-223` — ten sam program **i** semestr **i** grupa; **ten sam przedmiot jest wyjątkiem** | PRD: program, wszystkie przedmioty |
| Max 2 bloki tego samego przedmiotu z rzędu | `ScheduleCpSatSolver.cs:242-280` | **Nie ma w PRD** — może powodować INFEASIBLE |
| Tryb stac/niestac | filtr dni przy budowie zadań (`ScheduleGenerationService.cs:201-203`), nie constraint CP | Zgodne jako filtr |
| Dostępność prowadzącego | tylko krotki z `LecturerAvailabilities` wchodzą do `AllowedAssignments` | Zgodne |

**Cel (żywy):** `AddGapMinimizationObjective` (`ScheduleCpSatSolver.cs:316-398`):

- luka w środku dnia (program × dzień) × **100**
- zastępca × **1000**
- `slotIndex + (int)Day` × **1**

PRD v5 (`prd.md` NFR / FR-010): wśród planów bez kolizji **okienka są metryką jakości**. Żywy kod stawia **zastępców przed okienkami**.

**Szablon tygodnia:** `ScheduleWeekCalculator.ResolveWeeklySlotCount` (`ScheduleWeekCalculator.cs:71-83`) — `ceil(NumberOfSessions / tygodnie dydaktyczne)`. Zgodne z PRD v5. `SessionIndex` w `ScheduledSession` to indeks bloku tygodniowego, nie numer spotkania w semestrze.

**Status:** `Optimal` i `Feasible` → `Succeeded` + sesje; reszta → `Failed` bez sesji (`ScheduleCpSatSolver.cs:34-55`, `ScheduleGenerationService.cs:77-114`). Zgodne z „nigdy prawie-dobry plan z kolizjami”. `Optimal` vs `Feasible` nie jest zapisywane.

### Wejścia, walidacja, seedy

- `ScheduleValidationResult` ma tylko `CanRun` + `Issues` — **brak ostrzeżeń**. Każdy issue blokuje przycisk (`ScheduleValidationService.cs:156`).
- Dostępność: wystarczy **jakikolwiek** przypisany prowadzący z availability, niekoniecznie primary (`ScheduleValidationService.cs:89-94`).
- Sale: `Room` = numer + `InstructionTypeId` (`Models/Room.cs:5-12`). Brak `RoomAvailability` i `PreferredRoomNumber`.
- Seedy: `ComputerScienceSeed` (FullTime, 59 przedmiotów, 33 prowadzących) + `ChemicalTechnologySeed` (FullTime, 44 przedmioty, 35 prowadzących). **Brak seedu niestacjonarnego.**
- Generowanie bierze **wszystkie** `Subjects` w bazie (plan: brak filtra semestru przy triggerze).

### Auth i operacje

- UI: cookie Identity. API: JWT. Dwa kanały.
- `MapRazorComponents().AllowAnonymous()` (`Program.cs:118-120`) omija `FallbackPolicy`; ochrona UI stoi na layoucie.
- Brak ról — każdy zalogowany user może generować i oglądać dowolne `runId`.
- Brak cancel z UI; Blazor czeka na cały solve.

## Code References

- `plan-zajec-uczelnia/Components/Pages/GenerateSchedule.razor:1-138` — walidacja, spinner, `GenerateAsync`, redirect
- `plan-zajec-uczelnia/Components/Pages/ViewSchedule.razor:1-260` — podgląd, filtr semestru, metryki
- `plan-zajec-uczelnia/Services/Scheduling/ScheduleGenerationService.cs:11-277` — lock, 60 s, budowa zadań, persist
- `plan-zajec-uczelnia/Services/Scheduling/ScheduleCpSatSolver.cs:12-398` — constrainty + funkcja celu
- `plan-zajec-uczelnia/Services/Scheduling/ScheduleWeekCalculator.cs:71-83` — szablon tygodnia
- `plan-zajec-uczelnia/Services/Scheduling/ScheduleViewService.cs:107-148` — metryki okienek on-read
- `plan-zajec-uczelnia/Services/Scheduling/ScheduleValidationService.cs:12-157` — pre-check all-or-nothing
- `plan-zajec-uczelnia/Endpoints/ScheduleEndpoints.cs:15-33` — JWT validate/generate
- `plan-zajec-uczelnia/Program.cs:92-120` — DI, seedy, AllowAnonymous na Razor
- `plan-zajec-uczelnia/Models/Room.cs:5-12` — sala bez dostępności slotowej

## Architecture Insights

- **Dwa wejścia, jeden serwis.** Blazor nie używa REST. JWT jest do testów/regresji, nie do UI.
- **Domenowy pruning zamiast constraintów.** Tryb, availability, typ sali wycinają `AllowedAssignments` zanim powstanie `CpModel`. Pusty zbiór = INFEASIBLE bez solve.
- **Jedno lotnisko na proces.** `static SemaphoreSlim` — drugi klik = Failed, nie kolejka. Zgodne z Non-Goal „brak jobów w tle”.
- **Ziarno „program” w celu vs „grupa” w adjacency.** Solver może „domknąć” dzień inną grupą/semestrem; student nadal widzi dziurę. UI licząc per semestr też miesza grupy.
- **Lekcja UI** (`lessons.md`): MudBlazor defaults — strony generowania/podglądu trzymają się tego.

## Historical Context (from prior changes)

- `context/changes/generate-and-view-schedule/plan.md` — CP-SAT, 60 s, wariant A sal, addendum tygodniowy (2026-08-26), zastępcy z karą.
- `context/changes/generate-and-view-schedule/plan-brief.md:38,69` — FR-005 = katalog sal; FR-006 poza MVP (PRD v6).
- `context/changes/generate-and-view-schedule/reviews/impl-review.md` — F2 (cel okienek) oznaczone FIXED; wagi 1000 vs 100 **nie były** przedmiotem tej poprawki. F1 auth UI FIXED przez layout. F3 model tygodniowy udokumentowany w planie.
- `context/archive/2026-06-01-rooms-model-and-preferences/change.md` — S-03 wariant A; preferencje sal anulowane.
- `context/changes/lecturer-availability/` — availability jako `(LecturerId, Day, Slot)`, twarde wejście solvera.
- `context/changes/subjects-lecturers-and-grid/` — slot 90 min, żeby przestrzeń CP była dyskretna.
- `context/foundation/prd.md` v5 (2026-08-27) — cel: dział nie układa ręcznie; „najlepszy” = najmniej okienek wśród znalezionych w 60 s.

## Related Research

Brak wcześniejszych `research.md` w `context/changes/` ani `context/archive/`. Ten plik jest pierwszym artefaktem research dla S-05.

## Open Questions

1. **Wagi celu** — czy kara zastępcy (1000) ma zejść poniżej wagi okienka (100), żeby PRD „najmniej okienek wśród planów bez kolizji” było prawdziwe w CP-SAT?
2. ~~**FR-005 / FR-006**~~ — ✓ PRD v6 (2026-08-28): wariant A jest kontraktem MVP; macierz sal i preferencje w Non-Goals.
3. **Ziarno okienek** — optymalizować i raportować per (kierunek, semestr, grupa), czy zostawić per kierunek jak FR-010?
4. **Niestacjonarne** — brak seedu weekendowego; żywy test S-05 nie pokrył `StudyMode.PartTime`.
5. **Max 2 bloki z rzędu** — zostawić jako twardą regułę (ryzyko INFEASIBLE) czy przenieść do PRD / zmiękczyć?
6. **Manual Progress w plan.md** — kroki 2.3, 3.2–3.4, 4.2–4.4 nadal odznaczone; bieg demo w tej sesji pokrywa 3.2 (Succeeded, zero kolizji) na seedach stacjonarnych, nie pokrywa niestac / Failed-path UI.
