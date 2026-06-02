# Generowanie i podgląd planu (S-05) Implementation Plan

## Overview

Piąty slice — gwiazda przewodnia roadmapy. Koordynator uruchamia generowanie planów dla **wszystkich kierunków** jednym przebiegiem solvera CP-SAT (Google OR-Tools), otrzymuje persystowany wynik bez kolizji prowadzących i sal oraz przegląda plan per kierunek z metrykami okienek per dzień (FR-010, FR-011).

Decyzje z planowania: wariant A sal (bez `RoomAvailability`), `ScheduleRun` + `ScheduledSession`, synchroniczny UX ze spinnerem (60 s), przy INFEASIBLE — run `Failed`, główny prowadzący przez `SubjectLecturer.IsPrimary`.

## Current State Analysis

- Dane wejściowe z S-01–S-03: `StudyProgram`, `Subject`, `SubjectLecturer`, `LecturerAvailability`, `TimeSlot`, `Room`, `InstructionType`, `StudyProgramEnrollment` — `plan-zajec-uczelnia/Data/ApplicationDbContext.cs`.
- Brak encji wyniku harmonogramu, brak pakietu optymalizacji w `plan-zajec-uczelnia.csproj`.
- Brak stron `/generuj-plan` / podglądu — `Components/Layout/NavMenu.razor` kończy się na `/siatka`, `/semestry`.
- S-03 wdrożył wariant A: sale zawsze dostępne, solver dobiera `Room` po `InstructionTypeId` — `context/archive/2026-06-01-rooms-model-and-preferences/change.md`.

### Key Discoveries

- `Subject.NumberOfSessions` — liczba bloków 90 min do zaplanowania per przedmiot (nie per grupa) — `Models/Subject.cs:19-21`.
- Liczba grup: `InstructionType.EstimateGroupCount(studentCount, type)` — `Models/InstructionType.cs:16-20`.
- Siatka: jedna globalna lista `TimeSlot`, start 08:00–18:30 — `Services/TimeSlotService.cs:9-19`.
- Dostępność prowadzący: `(LecturerId, DayOfWeek, TimeSlotId)` — wzorzec S-02.
- PRD wymaga ≥15 min między kolejnymi zajęciami — przy slotach 90 min realizowane jako **zakaz dwóch zajęć tego samego kierunku w sąsiednich slotach tego samego dnia** (koniec bloku 9:30 + start 9:30 = 0 min przerwy).

## Desired End State

1. Koordynator na `/generuj-plan` widzi walidację wejścia, klika „Generuj plan”, czeka do 60 s (MudProgress).
2. Przy sukcesie: `ScheduleRun` status `Succeeded`, sesje w `ScheduledSession`, redirect/podgląd na `/plan/{runId}`.
3. Podgląd: MudDataGrid lub tabela per kierunek (dzień × slot) + MudCard z okienkami per dzień (liczba luk + suma minut pustych slotów między zajęciami).
4. Przy INFEASIBLE: run `Failed`, komunikat w UI, brak sesji (lub rollback transakcji).
5. `dotnet build` — 0 błędów; regresja logowania F-01.

## What We're NOT Doing

- Przywracanie `RoomAvailability` / preferencji sal (osobny change jeśli PRD ma być domknięty).
- Eksport planu (S-06), import (S-04).
- Generowanie w tle / Hangfire / polling.
- Wybór prowadzącego przez solver (tylko `IsPrimary`).
- Osobne siatki weekday vs weekend (jedna `TimeSlot` lista).
- Filtr semestru lub roku akademickiego przy triggerze — MVP bierze wszystkie `Subjects` w bazie.
- Gwarancja globalnego optimum — best effort w limicie 60 s.
- Test project xUnit (brak w repo) — weryfikacja manualna + `dotnet build`.

## Implementation Approach

Cztery fazy z pauzą manualną po każdej (konwencja `/10x-implement`). Logika solvera w `Services/Scheduling/` (nie rozrastać `Program.cs` — tylko rejestracja DI). UI — MudBlazor defaults, bez własnego CSS (lekcja z `lessons.md`).

Kolejność: model → walidacja + pakiet → solver → UI. Faza 3 jest największa; implementer buduje model CP-SAT inkrementalnie (najpierw feasibility + kolizje, potem cel okienek).

## Critical Implementation Details

**Zakaz sąsiednich slotów (15 min PRD):** Dla danego `StudyProgramId`, tego samego `DayOfWeek`, jeśli dwa `ScheduledSession` mają `TimeSlotId` kolejne w sortowaniu `StartTime`, traktuj jako naruszenie — w CP-SAT dodaj constraint `ForbiddenAssignments` lub nie pozwalaj na parę sąsiednich slotów dla tego samego kierunku w tym dniu (dotyczy wszystkich grup — student na kierunku nie może mieć zajęć w 8:00 i 9:30 bez przerwy).

**Zadania solvera (placement unit):** Dla każdej kombinacji `(SubjectId, GroupIndex 1..G, SessionIndex 1..NumberOfSessions)` jedna zmienna przypisania do `(DayOfWeek, TimeSlotId, RoomNumber)` z domyślnym `LecturerId` = główny prowadzący przedmiotu.

**Globalne kolizje:** Ten sam `(LecturerId, Day, Slot)` lub `(RoomNumber, Day, Slot)` nie może wystąpić w dwóch sesjach (wszystkie kierunki).

**Dni trybu:** `StudyMode.FullTime` → tylko `Monday`–`Friday`; `PartTime` → tylko `Saturday`, `Sunday`.

**Cel okienek:** Per `StudyProgramId`, per `DayOfWeek`, minimalizuj sumę „pustych slotów” między pierwszym a ostatnim zajęciem dnia (luki w środku dnia). Agreguj w funkcji celu CP-SAT (wagi równe w MVP).

## Phase 1: Model wyniku + główny prowadzący

### Overview

Encje persystencji harmonogramu i flaga głównego prowadzącego na przedmiocie. Migracja + minimalna aktualizacja UI przedmiotu.

### Changes Required

#### 1. ScheduleRunStatus enum

**File**: `plan-zajec-uczelnia/Models/ScheduleRunStatus.cs`

**Intent**: Status przebiegu generowania.

**Contract**: `Running`, `Succeeded`, `Failed`.

#### 2. Encja ScheduleRun

**File**: `plan-zajec-uczelnia/Models/ScheduleRun.cs`

**Intent**: Nagłówek jednego uruchomienia solvera.

**Contract**: `Id` (int PK), `StartedAt` (UTC), `CompletedAt` (nullable), `Status`, `ErrorMessage` (nullable, MaxLength 2000), `SolverTimeLimitSeconds` (int, default 60). Kolekcja `ScheduledSessions`.

#### 3. Encja ScheduledSession

**File**: `plan-zajec-uczelnia/Models/ScheduledSession.cs`

**Intent**: Jedna zaplanowana sesja 90 min.

**Contract**: `Id`, `ScheduleRunId` (FK), `StudyProgramId`, `SubjectId`, `GroupIndex` (1-based), `SessionIndex` (1-based, ≤ Subject.NumberOfSessions), `DayOfWeek`, `TimeSlotId` (FK), `RoomNumber` (FK string), `LecturerId` (FK). Indeks unikalności logicznego: unikalne `(ScheduleRunId, SubjectId, GroupIndex, SessionIndex)`.

#### 4. SubjectLecturer.IsPrimary

**File**: `plan-zajec-uczelnia/Models/SubjectLecturer.cs`

**Intent**: Dokładnie jeden główny prowadzący per przedmiot (decyzja planowania).

**Contract**: `bool IsPrimary` (default false). W `ApplicationDbContext` — brak wymuszenia DB unique partial w MVP; walidacja w `SubjectService` przy zapisie.

#### 5. ApplicationDbContext

**File**: `plan-zajec-uczelnia/Data/ApplicationDbContext.cs`

**Intent**: DbSety, relacje, cascade delete sesji przy usunięciu run.

**Contract**: `DbSet<ScheduleRun>`, `DbSet<ScheduledSession>`; FK z `OnDelete` Cascade dla sesji od run; Restrict/NoAction tam gdzie usuwanie kierunku nie powinno kasować historii (sesje zachowają FK do programu — `Restrict` na StudyProgram).

#### 6. SubjectService + SubjectDialog

**Files**: `Services/SubjectService.cs`, `Services/ISubjectService.cs`, `Components/Pages/SubjectDialog.razor`

**Intent**: Zapis wymaga dokładnie jednego `IsPrimary` wśród wybranych prowadzących; UI — MudRadioGroup lub checkbox „Główny” przy jednym prowadzącym w multiselect (najprościej: po wyborze prowadzących jeden MudSelect „Główny prowadzący” z listy wybranych).

**Contract**: `CreateAsync` / `UpdateAsync` przyjmują `primaryLecturerId`; rzuca `InvalidOperationException` gdy brak lub nie należy do listy.

#### 7. Migracja danych istniejących

**File**: migracja EF `AddScheduleRunAndPrimaryLecturer`

**Intent**: Dla istniejących `SubjectLecturer` ustaw `IsPrimary = true` na pierwszym prowadzącym per `SubjectId` (SQL w migracji lub data seed step), żeby walidacja S-05 nie blokowała starych danych.

**Contract**: `UPDATE` per subject: jeden wiersz `IsPrimary = true` (min `LecturerId`).

#### 8. Migracja EF

**Command** (z `plan-zajec-uczelnia/`): `dotnet ef migrations add AddScheduleRunAndPrimaryLecturer` → `dotnet ef database update`

### Success Criteria

#### Automated Verification

- `dotnet build` — 0 błędów
- `dotnet ef migrations add AddScheduleRunAndPrimaryLecturer` — plik migracji wygenerowany
- `dotnet ef database update` — bez błędów

#### Manual Verification

- Tabele `ScheduleRuns`, `ScheduledSessions` w bazie; kolumna `IsPrimary` na `SubjectLecturers`
- Formularz przedmiotu wymusza wybór głównego prowadzącego przy ≥1 prowadzącym

**Implementation Note**: Po fazie 1 — pauza na potwierdzenie manualne przed fazą 2.

---

## Phase 2: Walidacja wejścia + OR-Tools

### Overview

Dodać pakiet Google OR-Tools i serwis walidacji danych przed uruchomieniem solvera.

### Changes Required

#### 1. Pakiet NuGet

**File**: `plan-zajec-uczelnia/plan-zajec-uczelnia.csproj`

**Intent**: CP-SAT w .NET.

**Contract**: `<PackageReference Include="Google.OrTools" Version="9.11.4210" />` (lub najnowsza stabilna kompatybilna z `net10.0` — zweryfikować `dotnet add package`).

#### 2. ScheduleValidationResult DTO

**File**: `plan-zajec-uczelnia/Services/Scheduling/ScheduleValidationResult.cs`

**Intent**: Wynik pre-check dla UI.

**Contract**: `bool CanRun`, `IReadOnlyList<string> Issues` (komunikaty po polsku).

#### 3. IScheduleValidationService

**File**: `plan-zajec-uczelnia/Services/Scheduling/IScheduleValidationService.cs`

**Intent**: Kontrakt walidacji.

**Contract**: `Task<ScheduleValidationResult> ValidateAsync(CancellationToken ct)`.

#### 4. ScheduleValidationService

**File**: `plan-zajec-uczelnia/Services/Scheduling/ScheduleValidationService.cs`

**Intent**: Podstawowa lista braków (decyzja planowania).

**Contract**: Sprawdź (każdy brak = osobny issue, `CanRun = false`):
- co najmniej jeden `StudyProgram`
- co najmniej jeden `TimeSlot`
- co najmniej jeden `Subject` z `NumberOfSessions >= 1`
- każdy `Subject` ma dokładnie jednego `SubjectLecturer` z `IsPrimary`
- każdy `Subject` ma co najmniej jednego prowadzącego
- dla każdego subject: `StudyProgramEnrollment` dla `(StudyProgramId, Semester)` z `StudentCount > 0` OR `EstimateGroupCount > 0` (jeśli 0 grup — issue)
- co najmniej jedna `Room` per używany `InstructionTypeId` w przedmiotach
- główny prowadzący każdego przedmiotu ma ≥1 wpis `LecturerAvailability` (ostrzeżenie jako issue — nie blokuje jeśli chcesz tylko warn; **blokuj** w MVP jeśli zero availability dla primary)

#### 5. Rejestracja DI

**File**: `plan-zajec-uczelnia/Program.cs`

**Intent**: Scoped serwis walidacji.

**Contract**: `AddScoped<IScheduleValidationService, ScheduleValidationService>()`.

### Success Criteria

#### Automated Verification

- `dotnet build` — 0 błędów po dodaniu pakietu
- `dotnet list package --vulnerable` — brak krytycznych na nowym pakiecie (jeśli są — udokumentować w PR)

#### Manual Verification

- Wywołanie walidacji z tymczasowego endpointu lub z fazy 4 UI zwraca sensowne komunikaty przy pustej bazie vs pełnych danych testowych

**Implementation Note**: Po fazie 2 — pauza manualna.

---

## Phase 3: Solver CP-SAT

### Overview

`ScheduleGenerationService` buduje model CP-SAT, rozwiązuje (limit 60 s), zapisuje run i sesje, liczy metryki okienek (serwis podglądu może liczyć on read).

### Changes Required

#### 1. Scheduling DTOs (wewnętrzne)

**File**: `plan-zajec-uczelnia/Services/Scheduling/SchedulingProblem.cs` (i powiązane)

**Intent**: Kanoniczna lista zadań do ułożenia w pamięci po odczycie EF.

**Contract**: `PlacementTask` z polami: ProgramId, SubjectId, InstructionTypeId, GroupIndex, SessionIndex, PrimaryLecturerId, AllowedDays (z StudyMode), AllowedSlots (z availability primary + global TimeSlots), AllowedRooms (RoomNumber[] matching type).

#### 2. IScheduleGenerationService

**File**: `plan-zajec-uczelnia/Services/Scheduling/IScheduleGenerationService.cs`

**Intent**: API generowania.

**Contract**: `Task<ScheduleRun> GenerateAsync(CancellationToken ct)` — tworzy run `Running`, po solve ustawia `Succeeded`/`Failed`, zapisuje sesje tylko przy sukcesie w transakcji.

#### 3. ScheduleGenerationService

**File**: `plan-zajec-uczelnia/Services/Scheduling/ScheduleGenerationService.cs`

**Intent**: Rdzeń CP-SAT.

**Contract** (kolejność implementacji):
1. Załaduj programy, przedmioty, enrollment, availability, rooms, slots — `AsNoTracking`.
2. Zbuduj listę `PlacementTask` (pętla grup × sesji).
3. Utwórz `CpModel`; dla każdego taska zmienne `Day`, `Slot`, `Room` (lub jedna zmienna indeksowana na dozwolone triplety — zalecane **lista dozwolonych tuple** z przefiltrowanych kombinacji dla redukcji rozmiaru).
4. Constraints: kolizje lecturer/room; allowed days; allowed availability; room type match; **no adjacent slots same program same day**.
5. Objective: minimize weighted sum of intra-day gaps per program (patrz Critical Implementation Details).
6. `CpSolver` — `parameters.MaxTimeInSeconds = 60`.
7. Jeśli `OPTIMAL` lub `FEASIBLE` — mapuj rozwiązanie na `ScheduledSession` entities; inaczej `Failed` + `ErrorMessage` np. „Nie znaleziono planu spełniającego ograniczenia”.
8. Zapisz jedną transakcją.

**Uwaga:** Przy bardzo dużej liczbie tupleów rozważ pre-pruning slotów bez availability — już w `AllowedSlots`.

#### 4. IScheduleViewService + ScheduleViewService

**Files**: `plan-zajec-uczelnia/Services/Scheduling/IScheduleViewService.cs`, `ScheduleViewService.cs`

**Intent**: Odczyt runu, sesji, metryk okienek dla UI.

**Contract**:
- `GetRunAsync(runId)`
- `GetProgramsForRunAsync(runId)`
- `GetSessionsAsync(runId, studyProgramId)` — ordered by day, slot
- `GetGapMetricsAsync(runId, studyProgramId)` → lista `{ DayOfWeek, GapCount, GapMinutes }` — liczone z zajęć tego dnia: posortuj sloty zajęć, między kolejnymi indeksami slotów w siatce globalnej policz puste sloty × 90 min

#### 5. DI

**File**: `plan-zajec-uczelnia/Program.cs`

**Contract**: `AddScoped<IScheduleGenerationService, ScheduleGenerationService>()`, `AddScoped<IScheduleViewService, ScheduleViewService>()`.

### Success Criteria

#### Automated Verification

- `dotnet build` — 0 błędów

#### Manual Verification

- Na zestawie testowym (1–2 kierunki, mała siatka): generowanie kończy się `Succeeded`; brak podwójnej sali/prowadzącego w tym samym slocie
- Celowniczo słabe dane (brak availability): run `Failed`
- Okienka: ręcznie zweryfikuj metryki dla prostego planu 2 zajęcia z luką w środku dnia

**Implementation Note**: Po fazie 3 — pauza manualna (kluczowa).

---

## Phase 4: UI generowania i podglądu

### Overview

Strony Blazor: uruchomienie, spinner, lista runów, podgląd per kierunek.

### Changes Required

#### 1. GenerateSchedule.razor

**File**: `plan-zajec-uczelnia/Components/Pages/GenerateSchedule.razor`

**Route**: `/generuj-plan`

**Intent**: Walidacja + przycisk generuj + MudProgress podczas `GenerateAsync`.

**Contract**: `@attribute [Authorize]` jeśli projekt używa auth na stronach Blazor (sprawdź wzorzec z istniejących stron); wyświetl `Issues` z walidacji; przycisk disabled gdy `!CanRun`; po sukcesie `NavigationManager.NavigateTo($"/plan/{run.Id}")`; po failed — MudAlert z `ErrorMessage`.

#### 2. ViewSchedule.razor

**File**: `plan-zajec-uczelnia/Components/Pages/ViewSchedule.razor`

**Route**: `/plan/{RunId:int}`

**Intent**: Podgląd wyniku FR-011.

**Contract**: MudSelect kierunku; tabela dzień × slot (komórka: przedmiot, grupa, sala, prowadzący skrót); MudCard z metrykami okienek per dzień; link „Wróć” do `/generuj-plan`.

#### 3. ScheduleRuns.razor (opcjonalnie lekka lista)

**File**: `plan-zajec-uczelnia/Components/Pages/ScheduleRuns.razor` lub sekcja na `GenerateSchedule.razor`

**Intent**: Ostatnie runy (status, data) — bez osobnej fazy jeśli lista na stronie generowania wystarczy.

**Contract**: MudTable: StartedAt, Status, link do podglądu gdy Succeeded.

#### 4. NavMenu

**File**: `plan-zajec-uczelnia/Components/Layout/NavMenu.razor`

**Intent**: Nawigacja do generowania.

**Contract**: Pozycja „Generuj plan” → `/generuj-plan` (np. po `/siatka`).

#### 5. Regresja auth

**Intent**: `POST /auth/login` nadal działa (F-01).

### Success Criteria

#### Automated Verification

- `dotnet build` — 0 błędów

#### Manual Verification

- Pełny flow: login → uzupełnij dane testowe → generuj → podgląd z metrykami
- Run Failed pokazuje komunikat bez sesji w podglądzie
- Regresja: login API

**Implementation Note**: Po fazie 4 — gotowe do `/10x-archive` po review.

---

## Testing Strategy

### Unit Tests

- Brak projektu testowego — nie dodawać w tym slice bez osobnego zlecenia.

### Integration Tests

- Brak — weryfikacja manualna end-to-end.

### Manual Testing Steps

1. Utwórz 2 kierunki (stac + niestac), siatkę slotów pn–nd, availability prowadzących w odpowiednich dniach.
2. Przedmioty z głównym prowadzącym, enrollment, sale pasujące typy.
3. Generuj — oczekuj `Succeeded`; sprawdź kolizje sal między kierunkami (ten sam slot, różne kierunki — ta sama sala tylko raz).
4. Sprawdź metryki okienek po dodaniu celowego rozstrzelenia zajęć w jednym dniu.
5. Usuń availability głównego — walidacja lub Failed.
6. `curl` login JWT — 200.

## Performance Considerations

- Limit CP-SAT 60 s; rozmiar modelu rośnie z `Σ(groups × sessions)` — przy >~500 zadań monitorować czas.
- Ładowanie dozwolonych triplets zamiast pełnego kartezjańskiego produktu.
- Blazor synchroniczny request — App Service domyślny timeout ~230 s, ale UX blokuje UI do 60 s.

## Migration Notes

- Migracja `IsPrimary` na istniejących danych — pierwszy prowadzący per przedmiot.
- Stare przedmioty bez prowadząców — walidacja zablokuje generowanie do uzupełnienia.

## References

- PRD FR-010, FR-011: `context/foundation/prd.md`
- Roadmap S-05: `context/foundation/roadmap.md`
- S-03 wariant A: `context/archive/2026-06-01-rooms-model-and-preferences/change.md`
- Siatka i przedmioty: `context/changes/subjects-lecturers-and-grid/plan.md`
- Dostępność prowadzących: `context/changes/lecturer-availability/plan.md`
- OR-Tools .NET: https://developers.google.com/optimization

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: Model wyniku + główny prowadzący

#### Automated

- [x] 1.1 `dotnet build` — 0 błędów po modelu i migracji — 4e65f1e
- [x] 1.2 `dotnet ef migrations add AddScheduleRunAndPrimaryLecturer` — migracja wygenerowana — 4e65f1e
- [x] 1.3 `dotnet ef database update` — bez błędów — 4e65f1e

#### Manual

- [x] 1.4 Tabele run/sesji i IsPrimary w bazie; formularz przedmiotu z głównym prowadzącym — 4e65f1e

### Phase 2: Walidacja wejścia + OR-Tools

#### Automated

- [x] 2.1 `dotnet build` — 0 błędów po Google.OrTools i walidacji
- [x] 2.2 `dotnet list package --vulnerable` — brak nowych krytycznych

#### Manual

- [ ] 2.3 Walidacja zwraca czytelne issue przy niekompletnych danych

### Phase 3: Solver CP-SAT

#### Automated

- [x] 3.1 `dotnet build` — 0 błędów po solverze

#### Manual

- [ ] 3.2 Generowanie Succeeded na zestawie testowym bez kolizji sal/prowadzących
- [ ] 3.3 Generowanie Failed przy celowo niespójnych danych
- [ ] 3.4 Metryki okienek zgodne z ręcznym policzeniem dla prostego przypadku

### Phase 4: UI generowania i podglądu

#### Automated

- [x] 4.1 `dotnet build` — 0 błędów po UI

#### Manual

- [ ] 4.2 Flow E2E: generuj → podgląd z metrykami
- [ ] 4.3 Run Failed — komunikat w UI
- [ ] 4.4 POST /auth/login — regresja F-01
