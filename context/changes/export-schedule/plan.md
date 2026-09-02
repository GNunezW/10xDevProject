# Eksport planu (S-06) Implementation Plan

## Overview

Koordynator na podglądzie wygenerowanego planu pobiera plik Excel (`.xlsx`) dla **wybranego kierunku** — wszystkie semestry i grupy w jednym arkuszu. FR-012.

## Current State Analysis

S-05 zapisuje `ScheduleRun` + `ScheduledSession`. `ViewSchedule.razor` filtruje siatkę per kierunek i semestr. Brak pakietu Excel i brak endpointu pliku. Blazor używa cookie Identity; REST generate/validate jest JWT-only.

## Desired End State

Na `/plan/{runId}` przycisk **Pobierz Excel** (MudBlazor default) pobiera `.xlsx` z kolumnami: dzień, godzina, przedmiot, semestr, grupa, blok, sala, prowadzący. Plik dotyczy aktualnie wybranego kierunku. Niezalogowany request — 401/redirect. Brak udanego runu lub pusty kierunek — 404.

### Key Discoveries:

- Sesje per kierunek: `IScheduleViewService.GetSessionsAsync` z `semester: null`
- Lekcja UI: MudBlazor defaults, bez własnego CSS
- Tech-stack: ClosedXML lub EPPlus — wybór ClosedXML 0.105

## What We're NOT Doing

- Import (S-04 / FR-009 — poza MVP)
- PDF
- Osobny plik per grupa
- Eksport JWT (Blazor = cookie)
- Osobne generowanie per kierunek

## Implementation Approach

Serwis buduje workbook ClosedXML z sesji widoku. GET `/plan/{runId}/eksport?programId=` z cookie. Przycisk to `MudButton` z `Href` (bez JS interop).

## Phase 1: Pakiet i serwis eksportu

### Overview

ClosedXML + `IScheduleExportService` zwraca bajty i nazwę pliku.

### Changes Required:

#### 1. Pakiet

**File**: `plan-zajec-uczelnia/plan-zajec-uczelnia.csproj`

**Intent**: Dodać ClosedXML bez ruszania Google.OrTools.

**Contract**: `PackageReference` ClosedXML 0.105.0 oraz Google.OrTools 9.15.6755.

#### 2. Serwis

**File**: `plan-zajec-uczelnia/Services/Scheduling/ScheduleExportService.cs`

**Intent**: Zbudować arkusz z sesji kierunku; zsanityzować nazwę arkusza i pliku.

**Contract**: `ExportProgramAsync(runId, studyProgramId)` → `ScheduleExportFile?` (null gdy run nie Succeeded albo brak sesji).

### Success Criteria:

#### Automated Verification:

- `dotnet build` bez błędów

#### Manual Verification:

- (pokryte w fazie 2)

---

## Phase 2: Endpoint i przycisk

### Overview

Cookie GET + przycisk na podglądzie.

### Changes Required:

#### 1. Endpoint

**File**: `plan-zajec-uczelnia/Endpoints/ScheduleExportEndpoints.cs`

**Intent**: Pobieranie pliku tym samym auth co UI.

**Contract**: `GET /plan/{runId}/eksport?programId=` — `RequireAuthorization` na `IdentityConstants.ApplicationScheme`; `Results.File`.

#### 2. UI

**File**: `plan-zajec-uczelnia/Components/Pages/ViewSchedule.razor`

**Intent**: Przycisk obok wyboru kierunku.

**Contract**: `MudButton` „Pobierz Excel”, `Href` na endpoint, disabled gdy brak kierunku.

### Success Criteria:

#### Automated Verification:

- `dotnet build` bez błędów

#### Manual Verification:

- Zalogowany koordynator na `/plan/{id}` klika Pobierz Excel i dostaje `.xlsx` wybranego kierunku
- Kolumny zawierają grupę; zmiana kierunku zmienia zawartość pliku
- Bez sesji cookie endpoint nie oddaje pliku

## Testing Strategy

### Unit Tests:

Brak projektu testowego — weryfikacja `dotnet build` + ręczny download.

### Manual Testing Steps:

1. Zaloguj się, otwórz udany run.
2. Wybierz kierunek, Pobierz Excel — otwórz plik, sprawdź wiersze i grupy.
3. Drugi kierunek — inna nazwa pliku i inne przedmioty.
4. Wylogowany GET `/plan/{id}/eksport` — brak pliku.

## References

- PRD FR-012: `context/foundation/prd.md`
- Roadmapa S-06: `context/foundation/roadmap.md`
- Podgląd: `plan-zajec-uczelnia/Components/Pages/ViewSchedule.razor`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: Pakiet i serwis eksportu

#### Automated

- [x] 1.1 `dotnet build` bez błędów

### Phase 2: Endpoint i przycisk

#### Automated

- [x] 2.1 `dotnet build` bez błędów

#### Manual

- [ ] 2.2 Zalogowany koordynator pobiera `.xlsx` wybranego kierunku
- [ ] 2.3 Kolumny zawierają grupę; zmiana kierunku zmienia plik
- [x] 2.4 Bez cookie endpoint nie oddaje pliku
