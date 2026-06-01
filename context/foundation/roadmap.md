---
project: "Plan Zajęć Uczelnia"
version: 1
status: draft
created: 2026-05-27
updated: 2026-05-27
prd_version: 1
main_goal: speed
top_blocker: decisions
---

# Roadmap: Plan Zajęć Uczelnia

> Wygenerowano z `context/foundation/prd.md` (v1) + automatycznie zbadanego stanu kodu.
> Edytuj w miejscu; archiwizuj gdy superseded.
> Slice'y poniżej są w kolejności zależności. Tabela "At a glance" jest indeksem.

## Vision recap

Koordynator planowania na uczelni spędza tygodnie na ręcznym układaniu planu zajęć z wielu źródeł: dostępność prowadzących, sale, preferencje sal, siatka godzin, przypisania prowadzących. Mimo wysiłku studenci dostają złe rozkłady — za dużo okienek i nieoptymalne przerwy. MVP ma przyjąć te dane i wygenerować plan z minimalną liczbą okienek w obrębie każdego trybu studiów (stacjonarni w tygodniu, niestacjonarni w weekendy), przy spełnieniu wszystkich twardych ograniczeń (brak kolizji prowadzący/sala, zgodność z trybem studiów).

## North star

**S-05: generate-and-view-schedule — Koordynator może uruchomić generowanie planu zajęć i zobaczyć wynik z metrykami okienek.**

> Gwiazda przewodnia to najmniejszy end-to-end przepływ, który — jeśli działa — udowadnia, że produkt realizuje swoją obietnicę. Trafia na listę tak wcześnie, jak pozwalają zależności, bo jeśli ten przepływ nie działa, reszta nie ma sensu. Dla tego projektu: jeśli solver generuje plan bez kolizji i koordynator widzi metryki okienek — teza produktu jest udowodniona (US-01 + główne kryterium sukcesu PRD).

## At a glance

| ID | Change ID | Outcome (koordynator może …) | Prerequisites | PRD refs | Status |
|---|---|---|---|---|---|
| F-01 | secure-api-routes | (foundation) wszystkie trasy API wymagają ważnego tokenu JWT | — | FR-001 | ready |
| S-01 | subjects-lecturers-and-grid | definiować kierunki studiów i zarządzać listą przedmiotów przypisanych do kierunku i semestru z prowadzącymi oraz siatką godzin | F-01 | FR-002, FR-003, FR-007, FR-008, US-01 | proposed |
| S-02 | lecturer-availability | wprowadzać dostępność prowadzących na okres planowania | S-01 | FR-004, US-01 | proposed |
| S-03 | rooms-model-and-preferences | wprowadzać dostępność sal i preferencje sal do przedmiotów | S-01 | FR-005, FR-006, US-01 | proposed |
| S-04 | import-scheduling-data | zaimportować dane planowania z pliku | F-01 | FR-009, US-01 | proposed |
| S-05 | generate-and-view-schedule | uruchomić generowanie planów zajęć (osobno per kierunek) i zobaczyć wyniki z metrykami okienek | S-01, S-02, S-03 | FR-010, FR-011, US-01 | proposed |
| S-06 | export-schedule | wyeksportować wygenerowany plan | S-05 | FR-012 | proposed |

## Streams

Pomoc nawigacyjna — grupuje pozycje, które dzielą łańcuch zależności. Kanoniczna kolejność wciąż żyje w grafie zależności poniżej; ta tabela to proponowana kolejność czytania między równoległymi ścieżkami.

| Stream | Temat | Łańcuch | Uwaga |
|---|---|---|---|
| A | Autoryzacja + dane planowania | `F-01` → `S-01` → `S-02` / `S-03` → `S-05` → `S-06` | Krytyczna ścieżka do gwiazdy przewodniej; S-05 zablokowane przez otwarte pytania o model sal w S-03 |
| B | Import danych | `F-01` → `S-04` | Równoległy z S-01; może ruszyć zaraz po F-01 |

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
- **Status:** ready

## Slices

### S-01: Kierunki, przedmioty, prowadzący i siatka godzin

- **Outcome:** koordynator może definiować kierunki studiów (StudyProgram) i zarządzać listą przedmiotów przypisanych do kierunku i semestru z prowadzącymi oraz definiować siatkę godzin (poprawne sloty czasowe 8:00–20:00).
- **Change ID:** subjects-lecturers-and-grid
- **PRD refs:** FR-002, FR-003, FR-007, FR-008, US-01
- **Prerequisites:** F-01
- **Parallel with:** S-04
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Dane z tego slice'a są wejściem dla każdego kolejnego slice'a. Model danych StudyProgram + Subject zamknięty (2026-06-01). Błąd w tym modelu cofnie pracę nad S-02, S-03 i S-05. Ten slice jako pierwszy wdraża Blazor Server + MudBlazor do projektu — setup jednorazowy tutaj.
- **UI (Blazor):** lista kierunków (MudDataGrid) + formularz dodawania/edycji (MudDialog); lista przedmiotów per kierunek + formularz; siatka godzin jako osobna strona.
- **Status:** proposed

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
- **Status:** proposed

### S-03: Model sal i preferencje

- **Outcome:** koordynator może wprowadzać dostępność sal i przypisywać preferencje sal do przedmiotów.
- **Change ID:** rooms-model-and-preferences
- **PRD refs:** FR-005, FR-006, US-01
- **Prerequisites:** S-01
- **Parallel with:** S-02
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Model danych sal zamknięty (2026-05-27): sala to numer sali (PK) + typ + pojemność. Te same sale dla obu trybów, inna dostępność w slotach. Pojemność vs liczebność grupy nie jest modelowana w PRD — do doprecyzowania przy planowaniu.
- **UI (Blazor):** lista sal (MudDataGrid) + formularz sali; tabela dostępności sal (sloty jako checkboxy); przypisanie preferencji sali do przedmiotu (MudSelect).
- **Status:** proposed

### S-04: Import danych planowania

- **Outcome:** koordynator może zaimportować dane planowania z pliku (przedmioty, prowadzący, dostępność, sale).
- **Change ID:** import-scheduling-data
- **PRD refs:** FR-009, US-01
- **Prerequisites:** F-01
- **Parallel with:** S-01
- **Blockers:** —
- **Unknowns:**
  - Jaki format pliku uczelnia dziś używa (Excel / CSV / inny)? — Owner: koordynator przy wdrożeniu pilota. Block: no (można zacząć od CSV jako domyślnego; dostosować po rozmowie z pilotem).
- **Risk:** Import jest must-have (PRD: bez importu MVP nieużyteczny przy realnej skali danych). Format domyślnie CSV.
- **UI (Blazor):** strona importu z MudFileUpload + podgląd błędów walidacji (MudAlert).
- **Status:** proposed

### S-05: Generowanie i podgląd planu

- **Outcome:** koordynator może uruchomić generowanie planów zajęć (osobno per kierunek) minimalizującego okienka w obrębie każdego kierunku i trybu studiów, zobaczyć wyniki z metrykami okienek (łączna liczba / czas okienek per kierunek).
- **Change ID:** generate-and-view-schedule
- **PRD refs:** FR-010, FR-011, US-01
- **Prerequisites:** S-01, S-02, S-03
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:**
  - Jaka biblioteka / algorytm solverowy (constraint programming, CP-SAT, własny greedy)? — Owner: implementacja. Block: no (decyzja przy `/10x-plan generate-and-view-schedule`).
  - Jaki jest próg "akceptowalnego czasu" generowania (NFR: rząd minut)? — Owner: produkt / koordynator. Block: no (doprecyzować przy pilotowym teście).
- **Risk:** To jest gwiazda przewodnia — jeśli solver nie generuje planu bez kolizji, cały projekt nie ma sensu. Model sal zamknięty. Decyzja o algorytmie solverowym (CP-SAT, greedy, własny) zostaje przy planowaniu tego slice'a.
- **UI (Blazor):** przycisk "Generuj plan" + spinner podczas generowania; wynik jako MudDataGrid (per kierunek) + metryki okienek (MudCard z liczbami).
- **Status:** proposed

### S-06: Eksport planu

- **Outcome:** koordynator może wyeksportować wygenerowany plan do pliku.
- **Change ID:** export-schedule
- **PRD refs:** FR-012
- **Prerequisites:** S-05
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:**
  - Jaki format eksportu (PDF / Excel / CSV)? — Owner: koordynator przy wdrożeniu pilota. Block: no (można wybrać Excel jako domyślny; dostosować po rozmowie z pilotem).
- **Risk:** Bez eksportu plan nie trafia do procesu uczelni. Format: Excel (.xlsx) przez ClosedXML lub EPPlus.
- **UI (Blazor):** przycisk "Pobierz Excel" na stronie podglądu planu — endpoint zwraca plik, Blazor triggeruje download przez JS interop.
- **Status:** proposed

## Backlog Handoff

| Roadmap ID | Change ID | Sugerowany tytuł issue | Gotowe do `/10x-plan` | Uwagi |
|---|---|---|---|---|
| F-01 | secure-api-routes | Zabezpiecz trasy API tokenem JWT | tak | Zaimplementowane ✓ |
| S-01 | subjects-lecturers-and-grid | CRUD: kierunki (StudyProgram), przedmioty (kierunek + semestr), prowadzący, siatka godzin | tak | Czeka — F-01 done |
| S-02 | lecturer-availability | CRUD: dostępność prowadzących | nie | Czeka na S-01 |
| S-03 | rooms-model-and-preferences | CRUD: model sal, dostępność i preferencje | nie | Czeka na S-01; model sal zamknięty (2026-05-27) |
| S-04 | import-scheduling-data | Import danych planowania z pliku (CSV) | nie | Czeka na F-01; może startować równolegle z S-01 |
| S-05 | generate-and-view-schedule | Generowanie planów per kierunek + podgląd z metrykami okienek | nie | Czeka na S-01, S-02, S-03 |
| S-06 | export-schedule | Eksport wygenerowanego planu (Excel/CSV) | nie | Czeka na S-05 |

## Open Roadmap Questions

1. ~~**Model sal dla niestacjonarnych**~~ — ✓ rozstrzygnięte 2026-05-27: te same sale, inna dostępność w weekendy.
2. ~~**Typ sali vs konkretna sala**~~ — ✓ rozstrzygnięte 2026-05-27: numer sali (PK) + typ (wykładowa / ćwiczeniowa / laboratorium) + pojemność.
3. ~~**Model kierunku studiów**~~ — ✓ rozstrzygnięte 2026-06-01: StudyProgram jako osobna encja (nazwa, tryb stac/niestac, rok akademicki); Subject przypisany do StudyProgram + semestr (int).
4. **Format importu i eksportu** — jakie pliki uczelnia dziś używa (Excel, CSV, inny)? Owner: koordynator przy wdrożeniu pilota. Block: S-04 (nie — można domyślnie CSV), S-06 (nie — można domyślnie Excel).

## Parked

- **Multi-tenant (wiele uczelni)** — PRD §Poza zakresem; MVP obsługuje jedną szkołę.
- **Portal studencki** — PRD §Poza zakresem; koordynator eksportuje wynik na zewnątrz.
- **Samodzielna edycja przez prowadzących** — PRD §Poza zakresem; koordynator wprowadza dane w MVP.
- **Synchronizacja z systemem dziekanackim (live SIS)** — PRD §Poza zakresem; dane przez import lub ręcznie.
- **CI/CD GitHub Actions** — decyzja użytkownika; wdrożenie ręczne przez `az webapp up` na MVP; CI/CD nie blokuje żadnego slice'a funkcjonalnego.
- **Observability (Application Insights / OpenTelemetry)** — `main_goal: speed`; domyślny logging ASP.NET wystarcza na MVP.
- **Frontend UI koordynatora** — poza repo; API-first w bieżącej fazie (`AGENTS.md`, `tech-stack.md`).

## Done

(Puste przy pierwszej generacji. `/10x-archive` dopisuje wpis tutaj — i zmienia Status danej pozycji na `done` — gdy zmiana o pasującym Change ID zostanie zarchiwizowana. Format:)

- **<Slice ID>: <Outcome>** — Zarchiwizowano <YYYY-MM-DD> → `context/archive/<YYYY-MM-DD-change-id>/`. Lekcja: <wskaźnik do lessons.md lub `—`>.
