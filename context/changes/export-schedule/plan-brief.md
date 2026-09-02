# Eksport planu (S-06) — Plan Brief

> Full plan: `context/changes/export-schedule/plan.md`

## What & Why

Koordynator musi wynieść wygenerowany szablon tygodnia poza aplikację (FR-012). Bez eksportu plan nie wchodzi w proces uczelni.

## Starting Point

S-05 daje `ScheduleRun` i siatkę per kierunek na `/plan/{id}`. Brak pliku wyjściowego.

## Desired End State

Przycisk **Pobierz Excel** obok wyboru kierunku. Jeden `.xlsx` = cały wybrany kierunek (semestry i grupy w wierszach).

## Key Decisions Made

| Decision | Choice | Why | Source |
|---|---|---|---|
| Format | Excel `.xlsx` (ClosedXML) | Domyślna roadmapy / tech-stack | Plan |
| Zakres pliku | Wybrany kierunek, wszystkie grupy | Zgodnie z podglądem per kierunek | Plan |
| Auth | Cookie Identity | Ten sam kanał co Blazor | Plan |
| UI | MudButton Href | Lekcja: najprostszy MudBlazor | Plan |

## Scope

**In scope:** ClosedXML, serwis, GET `/plan/{runId}/eksport`, przycisk na podglądzie.

**Out of scope:** import, PDF, JWT export, plik per grupa.

## Architecture / Approach

`IScheduleExportService` czyta sesje przez `IScheduleViewService`, składa `XLWorkbook`, endpoint zwraca `Results.File`.

## Phases at a Glance

| Phase | What it delivers | Key risk |
|---|---|---|
| 1. Serwis | Bajty xlsx | Nazwy arkusza Excel (31 znaków, zakazane znaki) |
| 2. UI + GET | Przycisk i download | Cookie vs JWT; `forceLoad` vs Href |

**Prerequisites:** S-05 (udany `ScheduleRun`)
**Estimated effort:** jedna sesja, 2 fazy

## Open Risks & Assumptions

- Demo seedy muszą mieć choć jeden Succeeded run, żeby kliknąć przycisk.
- Nawigacja `Href` na GET z `Content-Disposition: attachment` nie powinna zrywać sidła Blazora.

## Success Criteria (Summary)

- Koordynator pobiera Excel wybranego kierunku z grupami w kolumnie.
- Niezalogowany nie dostaje pliku.
