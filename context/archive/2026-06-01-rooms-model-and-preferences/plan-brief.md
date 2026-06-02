# Model Sal, Dostępność i Preferencje (S-03) — Plan Brief

> Full plan: `context/changes/rooms-model-and-preferences/plan.md`

## What & Why

Solver (S-05) potrzebuje wiedzieć, które sale są dostępne w których slotach (twarde ograniczenie FR-005) i jakie sale koordynator preferuje dla przedmiotów (miękki sygnał FR-006). S-03 dostarcza te dane — ostatni brakujący kawałek wejścia przed generowaniem planu.

## Starting Point

S-01 dał CRUD i wzorce Blazor. S-02 dał macierz dostępności (LecturerAvailability + dialog checkboxów). Brak encji Room i preferencji na Subject.

## Desired End State

Koordynator na `/sale` zarządza salami i ich dostępnością (macierz Pn–Nd × sloty). W formularzu przedmiotu wybiera opcjonalnie jedną preferowaną salę. Wszystko w PostgreSQL, gotowe dla solvera.

## Key Decisions Made

| Decyzja | Wybór | Dlaczego | Source |
|---|---|---|---|
| PK sali | `RoomNumber` (string) | Zgodnie z PRD 2026-05-27 | PRD |
| Typ sali | Enum: wykładowa / ćwiczeniowa / laboratorium | PRD zamknięty | PRD |
| Preferencje per przedmiot | Jedna opcjonalna sala | Prosty MVP; nullable FK wystarczy | Plan |
| Model dostępności | Szablon tygodniowy `(RoomNumber, DayOfWeek, TimeSlotId)` | Kopia S-02, te same sale dla obu trybów | Plan |
| UI dostępności | Przycisk kalendarza na wierszu sali | Spójne z Prowadzący | Plan |
| UI preferencji | Rozszerzenie SubjectDialog | Naturalny flow — edycja przedmiotu | Plan |
| Usunięcie sali | Cascade dostępności; SetNull preferencji | Nie usuwa przedmiotów | Plan |
| Fazy | 4: Model → Serwisy → UI Sal → UI Preferencje | Izolacja ryzyk, łatwiejszy review | Plan |

## Scope

**In scope:**
- Encje `Room`, `RoomType`, `RoomAvailability` + nullable `Subject.PreferredRoomNumber`
- Serwisy CRUD + bulk save dostępności
- Strona `/sale` z macierzą dostępności
- Opcjonalny select sali w SubjectDialog

**Out of scope:**
- Walidacja pojemność vs liczebność grupy
- Wiele preferowanych sal per przedmiot
- Import sal (S-04)
- Powiązanie dostępności z SemesterPeriod

## Architecture / Approach

`Room` ma string PK (`RoomNumber`). `RoomAvailability` kopiuje wzorzec S-02. Preferencja to nullable FK na Subject — bez junction table. Bulk save dostępności: delete-all + insert. Npgsql: projekcja przez anonimowy typ (lekcja S-02).

## Phases at a Glance

| Phase | What it delivers | Key risk |
|---|---|---|
| 1. Model + Migracja | Tabele Rooms, RoomAvailabilities, FK na Subject | String PK w EF Core |
| 2. Serwisy | CRUD sal + bulk availability + Subject update | AsNoTracking / tracking conflicts |
| 3. UI Sale | `/sale` CRUD + macierz dostępności | MudBlazor defaults only |
| 4. UI Preferencje | Select sali w SubjectDialog | Nullable FK w MudSelect |

**Prerequisites:** S-01 i S-02 ukończone (TimeSlot, wzorce CRUD i macierzy)
**Estimated effort:** ~1–2 sesje, 4 fazy

## Open Risks & Assumptions

- Brak slotów w siatce godzin → pusta macierz (jak S-02).
- Pojemność sal nie jest walidowana vs liczebność grup — solver w S-05 może to ignorować.

## Success Criteria (Summary)

- Koordynator może dodać sale, ustawić dostępność i przypisać preferowaną salę do przedmiotu
- Dane trwałe w PostgreSQL; usunięcie sali nie kasuje przedmiotów
- `dotnet build` 0 błędów; regresja auth OK
