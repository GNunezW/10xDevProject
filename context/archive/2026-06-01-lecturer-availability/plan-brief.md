# Dostępność Prowadzących (S-02) — Plan Brief

> Full plan: `context/changes/lecturer-availability/plan.md`

## What & Why

Koordynator potrzebuje wskazać dla każdego prowadzącego, w które dni tygodnia i o jakich godzinach może prowadzić zajęcia. Bez tych danych solver (S-05) nie może wygenerować planu respektującego dostępność kadry — twarde ograniczenie PRD FR-004.

## Starting Point

S-01 dostarczył `Lecturer`, `TimeSlot` i wzorzec CRUD (MudDataGrid + fullscreen dialog + bulk save). S-02 buduje na tym — dodaje encję łączącą i dialog z macierzą checkboxów.

## Desired End State

Na stronie Prowadzący pojawia się ikona kalendarza przy każdym wierszu. Po kliknięciu otwiera się pełnoekranowy dialog z macierzą 7 dni × N slotów godzinowych. Koordynator zaznacza dostępne pola i klika "Zapisz" — dane trwale w PostgreSQL, zapamiętane przy kolejnym otwarciu.

## Key Decisions Made

| Decyzja | Wybór | Dlaczego | Source |
|---|---|---|---|
| Granularność modelu | `(LecturerId, DayOfWeek, TimeSlotId)` | Solver dostaje gotowe pary bez mapowania | Plan |
| Dni tygodnia | Pełny tydzień Pn–Nd | Pokrywa stacjonarnych (dni robocze) i niestacjonarnych (weekendy) | Plan |
| Zakres czasowy | Szablon tygodniowy (bez SemesterPeriod) | Prostszy model MVP — wystarczy jeden szablon | Plan |
| UI entry | Przycisk na wierszu Prowadzący | Naturalny flow — lista → szczegół | Plan |
| Zapis | Bulk save (przycisk "Zapisz") | Spójne z resztą UI, mniej zapytań | Plan |
| Domyślna dostępność | Brak (wszystko odznaczone) | Bezpieczne — solver nie przypisze bez danych | Plan |
| Usunięcie slotu | Cascade delete do LecturerAvailability | Brak osieroconych rekordów | Plan |

## Scope

**In scope:**
- Encja `LecturerAvailability(LecturerId, DayOfWeek, TimeSlotId)` + migracja
- Serwis `ILecturerAvailabilityService` (GetByLecturer, SaveAsync bulk)
- Przycisk "Dostępność" w tabeli Prowadzący
- Fullscreen dialog z macierzą Pn–Nd × sloty

**Out of scope:**
- Wyjątki datowe (konkretne daty niedostępności)
- Powiązanie z SemesterPeriod
- Walidacja kompletności danych
- Import dostępności (S-04)

## Architecture / Approach

Encja bez własnego Id — klucz złożony `(LecturerId, DayOfWeek, TimeSlotId)`. Bulk save: usuń wszystkie rekordy prowadzącego i wstaw nowe — atomowa operacja. Dialog: `HashSet<(DayOfWeek, int)>` jako lokalny stan, synchronizowany z checkboxami przez `Toggle()`.

## Phases at a Glance

| Phase | What it delivers | Key risk |
|---|---|---|
| 1. Model + Migracja | Tabela `LecturerAvailabilities` w PostgreSQL | Konfiguracja klucza złożonego w EF Core |
| 2. Serwis | `GetByLecturerAsync` + `SaveAsync` bulk replace | AsNoTracking — lekcja z S-01 |
| 3. Blazor UI | Macierz checkboxów + zapis | MudSimpleTable w fullscreen dialog |

**Prerequisites:** S-01 ukończony (Lecturer, TimeSlot, wzorzec CRUD w Blazor)
**Estimated effort:** ~1 sesja, 3 fazy

## Open Risks & Assumptions

- Jeśli brak slotów w siatce godzin, dialog wyświetli pustą macierz — koordynator musi najpierw dodać sloty.
- `System.DayOfWeek` w EF Core mapuje się do `int` w PostgreSQL — brak dodatkowej konfiguracji.

## Success Criteria (Summary)

- Koordynator może zaznaczyć dostępność prowadzącego i zapisać
- Dane są trwałe — przy ponownym otwarciu dialogu zaznaczenia są zapamiętane
- `dotnet build` 0 błędów, regresja F-01 (auth) nie wystąpiła
