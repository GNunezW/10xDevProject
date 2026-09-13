<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: Generowanie i podgląd planu (S-05)

- **Plan**: context/changes/generate-and-view-schedule/plan.md
- **Scope**: Phases 1–4 (automated complete; manual verification pending) + uncommitted working tree
- **Date**: 2026-08-26
- **Verdict**: REJECTED
- **Findings**: 2 critical, 5 warnings, 2 observations

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | FAIL |
| Scope Discipline | WARNING |
| Safety & Quality | FAIL |
| Architecture | WARNING |
| Pattern Consistency | WARNING |
| Success Criteria | WARNING |

## Findings

### F1 — Generowanie planu dostępne bez uwierzytelnienia

- **Severity**: ❌ CRITICAL
- **Impact**: 🔬 HIGH — architectural stakes; think carefully before deciding
- **Dimension**: Safety & Quality
- **Location**: plan-zajec-uczelnia/Program.cs:100
- **Detail**: `MapRazorComponents(...).AllowAnonymous()` udostępnia `/generuj-plan` bez logowania. Każdy może wywołać 60-sekundowy solver CP-SAT (DoS). Plan wymaga regresji F-01, ale UI omija FallbackPolicy.
- **Fix A ⭐ Recommended**: Usuń `.AllowAnonymous()` z mapowania Blazor (lub dodaj `[Authorize]` na `GenerateSchedule.razor` / `ViewSchedule.razor`) i zweryfikuj flow logowania koordynatora.
  - Strength: Spójne z FallbackPolicy i F-01; blokuje nieautoryzowany DoS.
  - Tradeoff: Wymaga działającego auth Blazor Server (cookie/JWT) — może ujawnić braki w konfiguracji auth UI.
  - Confidence: HIGH — wzorzec auth jest już w projekcie dla API.
  - Blind spot: Nie zweryfikowano, czy demo środowisko celowo omija auth.
- **Fix B**: Zostaw UI anonimowe, ale dodaj `[Authorize]` tylko na endpointach generowania + rate limit.
  - Strength: Mniejsza zmiana w mapowaniu Blazor.
  - Tradeoff: UI nadal dostępne; ochrona tylko na warstwie serwisu.
  - Confidence: MED — częściowa ochrona.
  - Blind spot: Bezpośrednie wywołanie serwisu z komponentu nadal możliwe.
- **Decision**: FIXED via Fix A — cookie auth + AuthorizeRouteView + `/login`, usunięto `.AllowAnonymous()` z MapRazorComponents, `[Authorize]` na stronach planu

### F2 — Funkcja celu solvera nie minimalizuje okienek

- **Severity**: ❌ CRITICAL
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Plan Adherence
- **Location**: plan-zajec-uczelnia/Services/Scheduling/ScheduleCpSatSolver.cs:316-342
- **Detail**: Plan (Critical Implementation Details + Phase 3) wymaga minimalizacji luk w środku dnia per kierunek. Implementacja używa `AddCompactnessObjective`: `slotIndex * 10 + day` + kara za zastępcę — preferuje wczesne sloty, nie liczy pustych slotów między zajęciami. Metryki okienek w `ScheduleViewService` liczą poprawnie, ale solver ich nie optymalizuje.
- **Fix A ⭐ Recommended**: Zastąp `AddCompactnessObjective` modelem luk z planu — dla każdego (ProgramId, Day) zmienne „slot zajęty” i kara za puste sloty między min a max indeksem.
  - Strength: Zgodność z FR-010/PRD i planem; metryki podglądu będą odzwierciedlać to, co solver optymalizuje.
  - Tradeoff: Większy model CP-SAT; może wydłużyć czas rozwiązywania.
  - Confidence: MED — wzór jest w planie, ale wymaga implementacji constraintów.
  - Blind spot: Nie zmierzono wpływu na czas solvera przy pełnym seedzie Informatyki.
- **Fix B**: Zaktualizuj plan jako „MVP: kompaktowość slotów zamiast okienek” i usuń obietnicę optymalizacji okienek z PRD tego slice'a.
  - Strength: Brak pracy solverowej.
  - Tradeoff: Rozjazd z PRD FR-010; koordynator dostaje metryki, których solver nie minimalizuje.
  - Confidence: LOW — wymaga akceptacji product.
  - Blind spot: Stakeholder PRD nie skonsultowany.
- **Decision**: FIXED via Fix A — `AddGapMinimizationObjective` minimalizuje puste sloty między zajęciami per (ProgramId, Day)

### F3 — Model tygodniowy zamiast semestralnego (niezacommitowane)

- **Severity**: ⚠️ WARNING
- **Impact**: 🔬 HIGH — architectural stakes; think carefully before deciding
- **Dimension**: Plan Adherence
- **Location**: plan-zajec-uczelnia/Services/Scheduling/ScheduleGenerationService.cs:200-208
- **Detail**: Plan: jedno zadanie per `(Subject, Group, SessionIndex 1..NumberOfSessions)`. Kod (uncommitted): `ScheduleWeekCalculator.ResolveWeeklySlotCount()` dzieli sesje semestralne przez tygodnie — generuje szablon tygodniowy powtarzalny, nie pełny semestr. Zmiana semantyki bez aktualizacji planu.
- **Fix A ⭐ Recommended**: Dodaj addendum do plan.md opisujące model tygodniowy i uzasadnienie (żądanie użytkownika); zaktualizuj Success Criteria manualne.
  - Strength: Utrzymuje zaimplementowaną logikę; synchronizuje źródło prawdy.
  - Tradeoff: Plan S-05 rozszerza zakres poza oryginalny opis.
  - Confidence: HIGH — zmiana jest świadoma (historia rozmowy).
  - Blind spot: PRD może nadal opisywać plan semestralny.
- **Fix B**: Cofnij do pętli `1..NumberOfSessions` z planu.
  - Strength: Strict plan adherence.
  - Tradeoff: Utrata modelu tygodniowego, który użytkownik prosił o wdrożenie.
  - Confidence: MED.
  - Blind spot: UI podglądu i metryki są dostosowane do tygodnia.
- **Decision**: FIXED via Fix A — addendum „model tygodniowy” w plan.md

### F4 — Solver wybiera zastępców (sprzeczne z planem)

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Plan Adherence
- **Location**: plan-zajec-uczelnia/Services/Scheduling/ScheduleCpSatSolver.cs:322-331
- **Detail**: Plan i „What We're NOT Doing”: tylko `IsPrimary`, solver nie wybiera prowadzącego. Uncommitted kod: `AllowedAssignments` obejmuje wszystkich prowadzących z dostępnością; `nonPrimaryLecturerPenalty = 1000` pozwala solverowi wybrać zastępcę.
- **Fix**: Ogranicz `AllowedAssignments` do głównego prowadzącego (plan) albo zaktualizuj plan i walidację, jeśli zastępcy są wymagani biznesowo.
- **Decision**: FIXED — plan zaktualizowany (addendum: zastępcy dozwoleni z karą w celu)

### F5 — Rozległy scope creep poza S-05 (niezacommitowane)

- **Severity**: ⚠️ WARNING
- **Impact**: 🔬 HIGH — architectural stakes; think carefully before deciding
- **Dimension**: Scope Discipline
- **Location**: N/A (wiele plików)
- **Detail**: Poza planem S-05 w working tree: `StudyDegree` + migracja, seedy (`ComputerScienceSeed`, `ChemicalTechnologySeed`), `ScheduleInfeasibilityDiagnostics`, filtr semestru w `ViewSchedule.razor`, rozszerzenie `ErrorMessage` do 8000 znaków. Brak osobnego change-id ani addendum w plan.md.
- **Fix A ⭐ Recommended**: Commitnij jako osobny change (`/10x-new`) z krótkim planem/addendum albo dopisz sekcję „Addenda” do plan.md S-05.
  - Strength: Traceability; review i archiwizacja per change.
  - Tradeoff: Overhead dokumentacji.
  - Confidence: HIGH — zgodne z workflow 10x.
  - Blind spot: Część zmian może być ad-hoc demo data.
- **Fix B**: Wycofaj extras z tego brancha; zostaw tylko S-05.
  - Strength: Czysty scope slice.
  - Tradeoff: Utrata seedów i StudyDegree potrzebnych do testów Informatyki.
  - Confidence: MED.
  - Blind spot: Demo bez seedów może być puste.
- **Decision**: FIXED via Fix A — addendum w plan.md (StudyDegree, seedy, diagnostyka, filtr semestru)

### F6 — Walidacja blokuje mimo komunikatu o zastępcach

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: plan-zajec-uczelnia/Services/Scheduling/ScheduleValidationService.cs:89-90
- **Detail**: Gdy główny prowadzący nie ma dostępności, ale zastępcy tak — issue mówi „solver użyje zastępców”, ale `issues.Add` blokuje `CanRun`. Sprzeczny UX; plan mówił o blokowaniu tylko przy zero availability dla primary.
- **Fix**: Jeśli zastępcy mają dostępność — nie dodawaj issue (lub osobna lista ostrzeżeń nieblokujących). Blokuj tylko gdy `lecturersWithAvailabilityOnSubject.Count == 0`.
- **Decision**: FIXED — usunięto blokujący issue przy braku dostępności primary, gdy zastępcy dostępni

### F7 — DbContext i brak blokady równoległego generowania

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Safety & Quality
- **Location**: plan-zajec-uczelnia/Services/Scheduling/ScheduleGenerationService.cs:21-55, Components/Pages/GenerateSchedule.razor:115
- **Detail**: Scoped DbContext żyje przez całe `GenerateAsync` (do 60 s). Brak globalnej blokady — dwa równoległe generowania obciążają CPU i mogą zostawić osierocone runy `Running` po crashu.
- **Fix**: Rozdziel scope (create run → dispose → solve → nowy scope persist); singleton `SemaphoreSlim` lub flaga `Running` w DB; startup cleanup starych `Running`.
- **Decision**: FIXED — `GenerationLock` (SemaphoreSlim) w ScheduleGenerationService + cleanup osieroconych Running przy starcie

### F8 — Manualne kryteria sukcesu niezweryfikowane

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Success Criteria
- **Location**: context/changes/generate-and-view-schedule/plan.md (Progress §2.3, 3.2–3.4, 4.2–4.4)
- **Detail**: Wszystkie manualne kroki faz 2–4 pozostają `- [ ]`. Automated (`dotnet build`) przechodzi; E2E, Failed run UI, regresja login i metryki okienek nie potwierdzone w Progress.
- **Fix**: Przeprowadź checklistę manualną z planu (generuj na seedzie Informatyki, sprawdź Failed, curl login) i odznacz Progress lub zostaw jako pending przed archive.
- **Decision**: FIXED — testy API 2026-08-26: POST /auth/login 200+token (4.4); validate CanRun=true 0 issues (2.3); generate run #33 Succeeded 360 sesji, 0 kolizji prowadzący/sala (3.2); validate/login bez auth → 401. **Pending:** flow Blazor cookie (4.2) wymaga restartu serwera z nowym kodem auth; metryki okienek ręcznie (3.4); scenariusz Failed (3.3/4.3) — do sprawdzenia w UI po restarcie.

### F9 — Endpointy API schedulingu poza planem

- **Severity**: 👁 OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Scope Discipline
- **Location**: plan-zajec-uczelnia/Endpoints/ScheduleEndpoints.cs
- **Detail**: `GET /api/scheduling/validate` i `POST /api/scheduling/generate` nie były w planie (walidacja tylko przez UI). Commit 12e6d04.
- **Fix**: Udokumentuj w plan.md jako bonus API-first albo usuń jeśli nieużywane.
- **Decision**: FIXED — udokumentowano w addendum plan.md (API schedulingu)

### F10 — Inline style w UI (lekcja Blazor)

- **Severity**: 👁 OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Pattern Consistency
- **Location**: plan-zajec-uczelnia/Components/Pages/ViewSchedule.razor:28,74,97
- **Detail**: `Style="white-space: pre-wrap"`, `overflow-x: auto`, `min-width: 10rem` — lekcja z lessons.md: MudBlazor defaults, bez własnego CSS.
- **Fix**: Usuń inline style tam, gdzie MudTable/MudAlert wystarczą.
- **Decision**: FIXED — usunięto inline Style z ViewSchedule.razor; wieloliniowe błędy przez Split('\n') + MudText

## Automated Verification Log

| Command | Result | Notes |
|---------|--------|-------|
| `dotnet build` | PASS | 0 errors; NU1903 on Microsoft.OpenApi 2.0.0 (transitive, not OrTools) |
| `dotnet list package --vulnerable` | INCONCLUSIVE | Sandbox permission denied on NuGet audit endpoint; build restore showed no OrTools vulnerability |
