# Generowanie i podgląd planu (S-05) — Plan Brief

> Full plan: `context/changes/generate-and-view-schedule/plan.md`

## What & Why

Koordynator ma uruchomić generowanie planów dla wszystkich kierunków jednym przebiegiem solvera, otrzymać układ bez kolizji prowadzących i sal (globalnie) oraz zobaczyć plan per kierunek z metrykami okienek — to realizacja obietnicy produktu z PRD (FR-010, FR-011).

## Starting Point

S-01–S-03 dostarczają dane wejściowe: kierunki (`StudyMode`), przedmioty (`NumberOfSessions`, `InstructionType`), prowadzący + dostępność, sale po typie zajęć (wariant A — bez macierzy dostępności sal), siatka `TimeSlot` (90 min, 08:00–20:00), liczebność `StudyProgramEnrollment`. Brak encji wyniku, brak OR-Tools, brak stron generowania/podglądu.

## Desired End State

Na `/generuj-plan` koordynator uruchamia generowanie (spinner do 60 s), widzi status runu (sukces / failed). Na podglądzie — siatka zajęć per kierunek + karta z okienkami per dzień. Wynik zapisany w `ScheduleRun` + `ScheduledSession` w PostgreSQL.

## Key Decisions Made

| Decision | Choice | Why | Source |
| --- | --- | --- | --- |
| Silnik | Google OR-Tools CP-SAT | Twarde ograniczenia + minimalizacja okienek | Plan |
| Sale | Wariant A (bez RoomAvailability) | Zgodne z S-03 po refaktorze | Plan |
| Persystencja | ScheduleRun + ScheduledSession | Historia, podgląd, S-06 | Plan |
| Zakres uruchomienia | Wszystkie kierunki naraz | Globalna alokacja sal (FR-010) | Plan |
| Grupy | Osobne sesje per grupa (EstimateGroupCount) | Zgodne z liczebnością S-03 | Plan |
| Dni trybu | Twarde (stac pn–pt, niestac sob–nd) | Reguła biznesowa PRD | Plan |
| Okienka | Metryki per dzień per kierunek | FR-011, intuicyjny podgląd | Plan |
| Limit solvera | 60 s best effort | NFR rząd minut | Plan |
| UX generowania | Synchroniczny request + MudProgress | Najprostszy Blazor MVP | Plan |
| INFEASIBLE | Run Failed + MudAlert | Audyt bez częściowych planów | Plan |
| Prowadzący | `SubjectLecturer.IsPrimary` | Wybór koordynatora, prostszy model niż zmienna CP | Plan |
| Walidacja | Podstawowa lista braków przed startem | Szybki feedback | Plan |

## Scope

**In scope:** Pakiet OR-Tools; encje wyniku; CP-SAT (kolizje, availability, typ sali, dni trybu, brak sąsiednich slotów = 15 min); obiektywna minimalizacja okienek per kierunek; walidacja wejścia; UI `/generuj-plan` + podgląd; `IsPrimary` w UI przedmiotu.

**Out of scope:** RoomAvailability (FR-005 odroczone); preferencje sal (FR-006); eksport (S-06); import (S-04); tło/Hangfire; optymalizacja obciążenia prowadzących; filtr semestru/ROK w triggerze (MVP: wszystkie przedmioty w DB); osobne siatki weekday/weekend.

## Architecture / Approach

```
[Blazor /generuj-plan] → IScheduleValidationService (pre-check)
                      → IScheduleGenerationService
                            → load inputs (EF)
                            → build CP-SAT model (tasks = subject × group × session#)
                            → solve (60s) → persist ScheduleRun + ScheduledSession
[Blazor /plan/{runId}] → IScheduleViewService (sessions + gap metrics per program/day)
```

Solver przypisuje każdą sesję do `(DayOfWeek, TimeSlotId, RoomNumber)`; prowadzący = główny z `SubjectLecturer.IsPrimary`; sale z puli `Room.InstructionTypeId == Subject.InstructionTypeId`.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Model wyniku + główny prowadzący | Tabele run/sesji, IsPrimary, migracja | Migracja danych istniejących przedmiotów bez primary |
| 2. Walidacja + OR-Tools | Pakiet, IScheduleValidationService | Brak testów jednostkowych w repo — weryfikacja ręczna |
| 3. Solver CP-SAT | ScheduleGenerationService, metryki | Złożoność modelu przy wielu grupach; INFEASIBLE przy słabych danych |
| 4. UI generowania i podglądu | Strony Blazor, NavMenu | Timeout HTTP >60 s na Azure (monitorować) |

**Prerequisites:** S-01, S-02, S-03 zarchiwizowane / wdrożone; `dotnet build` zielony.

**Estimated effort:** ~3–4 sesje implementacji (4 fazy z pauzą manualną po każdej).

## Open Risks & Assumptions

- MVP planuje **wszystkie** wiersze `Subjects` — koordynator utrzymuje tylko aktualny semestr w bazie lub akceptuje mieszankę semestrów.
- PRD FR-005/FR-006 nie są spełnione w danych — świadome odchylenie (wariant A).
- Jakość okienek zależy od gęstości siatki i limitu 60 s — brak gwarancji optimum (zgodnie z PRD).
- Pierwszy przedmiot bez oznaczonego głównego prowadzącego blokuje walidację — wymaga jednorazowego uzupełnienia danych.

## Success Criteria (Summary)

- Koordynator generuje plan jednym przyciskiem; przy poprawnych danych otrzymuje run `Succeeded` bez kolizji sal/prowadzących.
- Podgląd per kierunek pokazuje zajęcia i metryki okienek per dzień.
- Przy braku rozwiązania — run `Failed` i czytelny komunikat, bez częściowego planu.
