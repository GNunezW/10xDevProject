# Model Sal, Dostępność i Preferencje (S-03) — Plan Implementacji

## Overview

Trzeci slice danych. Koordynator definiuje sale (numer, typ, pojemność), ustawia ich tygodniową dostępność (macierz dni × sloty) i opcjonalnie przypisuje preferowaną salę do przedmiotu. Solver w S-05 użyje dostępności sal jako twardego ograniczenia i preferencji jako miękkiego sygnału.

Po tym slice'u koordynator ma pełny CRUD sal z macierzą dostępności oraz pole preferowanej sali w formularzu przedmiotu.

## Current State Analysis

- Brak encji `Room`, `RoomAvailability` ani pola preferencji na `Subject`.
- Wzorzec macierzy dostępności ustalony w S-02: `LecturerAvailability(LecturerId, DayOfWeek, TimeSlotId)` + fullscreen dialog + bulk save.
- Wzorzec CRUD ustalony w S-01: MudDataGrid + MudDialog + serwisy Scoped z `AsNoTracking()` na odczycie.
- PRD (2026-05-27): sala = numer sali (PK) + typ (wykładowa / ćwiczeniowa / laboratorium) + pojemność. Te same sale dla obu trybów studiów, inna dostępność w slotach.

### Key Discoveries

- PK sali to `RoomNumber` (string), nie int — zgodnie z PRD.
- Jedna opcjonalna preferowana sala per przedmiot — wystarczy nullable FK `Subject.PreferredRoomNumber` (bez osobnej tabeli junction).
- Npgsql nie obsługuje projekcji `ValueTuple` w SQL — lekcja z S-02: anonimowy typ → `ToListAsync()` → `ToHashSet()` w pamięci.
- Usunięcie sali: cascade delete dostępności; preferencje na przedmiotach → `SetNull` (nie usuwa przedmiotu).

## Desired End State

Koordynator na stronie Sale widzi listę sal z przyciskiem kalendarza (dostępność) przy każdej sali. Macierz Pn–Nd × sloty działa identycznie jak u prowadzących. W formularzu przedmiotu pojawia się opcjonalny MudSelect z listą sal. Dane trwale w PostgreSQL.

## What We're NOT Doing

- Walidacja pojemności vs liczebność grupy — poza MVP (PRD).
- Wiele preferowanych sal per przedmiot — tylko jedna opcjonalna.
- Powiązanie dostępności z SemesterPeriod — szablon tygodniowy globalny.
- Import sal (S-04).
- Filtrowanie preferencji po typie sali — koordynator wybiera z pełnej listy.

## Implementation Approach

Cztery fazy: model → serwisy → UI sal → UI preferencji. Fazy 1–2 bez przerwy. Faza 3 wymaga 1–2. Faza 4 wymaga 1–2 i istniejącego SubjectDialog.

Bulk save dostępności: identyczna strategia jak S-02 — `SaveAsync(roomNumber, slots)` usuwa wszystkie rekordy sali i wstawia nowe.

## Phase 1: Model Danych + Migracja EF Core

### Overview

Encje `Room`, `RoomType`, `RoomAvailability`, nullable FK na `Subject`. Migracja PostgreSQL.

### Changes Required

#### 1. Enum RoomType

**File**: `plan-zajec-uczelnia/Models/RoomType.cs`

**Intent**: Typ sali wymagany przez PRD i solver (wykładowa / ćwiczeniowa / laboratorium).

**Contract**: `public enum RoomType { Lecture = 1, Exercise = 2, Laboratory = 3 }`

#### 2. Encja Room

**File**: `plan-zajec-uczelnia/Models/Room.cs`

**Intent**: Reprezentuje salę — numer sali jest kluczem głównym.

**Contract**: `RoomNumber (string, PK, MaxLength 20)`, `Type (RoomType)`, `Capacity (int, Range 1..)`. Brak osobnego `Id`.

#### 3. Encja RoomAvailability

**File**: `plan-zajec-uczelnia/Models/RoomAvailability.cs`

**Intent**: Jeden dostępny slot sali w danym dniu tygodnia — brak rekordu = niedostępna.

**Contract**: Klucz złożony `(RoomNumber, DayOfWeek, TimeSlotId)`. Navigation: `Room`, `TimeSlot`.

#### 4. Rozszerzenie Subject

**File**: `plan-zajec-uczelnia/Models/Subject.cs`

**Intent**: Opcjonalna preferowana sala per przedmiot (FR-006).

**Contract**: `PreferredRoomNumber (string?, nullable)`, `PreferredRoom (Room?)` — navigation property.

#### 5. Aktualizacja ApplicationDbContext

**File**: `plan-zajec-uczelnia/Data/ApplicationDbContext.cs`

**Intent**: DbSet'y, klucz złożony RoomAvailability, relacje z cascade/set-null.

**Contract**:
- `DbSet<Room>`, `DbSet<RoomAvailability>`
- `RoomAvailability` PK: `(RoomNumber, DayOfWeek, TimeSlotId)`
- FK Room → cascade delete RoomAvailability
- FK TimeSlot → cascade delete RoomAvailability
- FK Subject.PreferredRoomNumber → Room, `OnDelete(DeleteBehavior.SetNull)`

#### 6. Migracja EF Core

**Command**: `dotnet ef migrations add AddRoomsAndPreferences`

**Intent**: Wygeneruj migrację dla tabel `Rooms`, `RoomAvailabilities` i kolumny `Subjects.PreferredRoomNumber`.

#### 7. Aktualizacja bazy

**Command**: `dotnet ef database update`

### Success Criteria

#### Automated Verification

- `dotnet build` — 0 błędów
- `dotnet ef migrations add AddRoomsAndPreferences` — plik migracji wygenerowany
- `dotnet ef database update` — bez błędów

#### Manual Verification

- Tabele `Rooms`, `RoomAvailabilities` istnieją w bazie; kolumna `PreferredRoomNumber` w `Subjects`

---

## Phase 2: Warstwa Serwisów

### Overview

Serwisy CRUD dla sal, bulk save dostępności, rozszerzenie SubjectService o preferowaną salę. Rejestracja w DI.

### Changes Required

#### 1. IRoomService + RoomService

**File**: `plan-zajec-uczelnia/Services/IRoomService.cs`, `RoomService.cs`

**Intent**: CRUD sal — PK to RoomNumber (string).

**Contract**:
- `GetAllAsync()` → `List<Room>`, `AsNoTracking()`, sort po RoomNumber
- `CreateAsync(Room)`, `UpdateAsync(Room)`, `DeleteAsync(string roomNumber)`
- `UpdateAsync`: load existing by PK, update properties (wzorzec S-01 — unikaj tracking conflicts)

#### 2. IRoomAvailabilityService + RoomAvailabilityService

**File**: `plan-zajec-uczelnia/Services/IRoomAvailabilityService.cs`, `RoomAvailabilityService.cs`

**Intent**: Odczyt i bulk save dostępności sali — kopia wzorca S-02.

**Contract**:
- `GetByRoomAsync(string roomNumber)` → `HashSet<(DayOfWeek, int)>` — projekcja przez anonimowy typ (lekcja Npgsql)
- `SaveAsync(string roomNumber, IEnumerable<(DayOfWeek, int)> slots)` → delete-all + insert

#### 3. Rozszerzenie ISubjectService + SubjectService

**File**: `plan-zajec-uczelnia/Services/ISubjectService.cs`, `SubjectService.cs`

**Intent**: Obsługa opcjonalnej preferowanej sali przy create/update przedmiotu.

**Contract**: `CreateAsync` i `UpdateAsync` przyjmują dodatkowy parametr `string? preferredRoomNumber`. Zapis na `Subject.PreferredRoomNumber`. `GetAllAsync` / `GetByIdWithLecturersAsync` dołączają `PreferredRoom` (Include).

#### 4. Rejestracja w DI

**File**: `plan-zajec-uczelnia/Program.cs`

**Contract**: `AddScoped<IRoomService, RoomService>()`, `AddScoped<IRoomAvailabilityService, RoomAvailabilityService>()`

### Success Criteria

#### Automated Verification

- `dotnet build` — 0 błędów po dodaniu serwisów

---

## Phase 3: Blazor UI — Sale i Dostępność

### Overview

Nowa strona `/sale` z CRUD sal (MudDataGrid) i przyciskiem kalendarza otwierającym macierz dostępności. Nawigacja w NavMenu.

### Changes Required

#### 1. Nawigacja

**File**: `plan-zajec-uczelnia/Components/Layout/NavMenu.razor`

**Intent**: Link do strony sal.

**Contract**: `MudNavLink Href="/sale"` z ikoną `MeetingRoom`, label "Sale".

#### 2. Rooms.razor

**File**: `plan-zajec-uczelnia/Components/Pages/Rooms.razor`

**Intent**: Lista sal z akcjami: Dostępność, Edytuj, Usuń.

**Contract**: `@page "/sale"`. Kolumny: RoomNumber, Type (label PL), Capacity. Przycisk `CalendarMonth` → `OpenAvailabilityDialog`. CRUD przez `RoomDialog` + `ConfirmDialog` — wzorzec `Lecturers.razor`.

#### 3. RoomDialog.razor

**File**: `plan-zajec-uczelnia/Components/Pages/RoomDialog.razor`

**Intent**: Formularz dodawania/edycji sali.

**Contract**: `MudTextField` RoomNumber (readonly przy edycji), `MudSelect` RoomType, `MudNumericField` Capacity (min 1). Przy create: RoomNumber edytowalny. Przy edit: RoomNumber disabled.

#### 4. RoomAvailabilityDialog.razor

**File**: `plan-zajec-uczelnia/Components/Pages/RoomAvailabilityDialog.razor`

**Intent**: Macierz dostępności sali — kopia `LecturerAvailabilityDialog.razor` z parametrem `Room Room`.

**Contract**: `[Parameter] Room Room`. `_days` = Pn–Nd. Bulk save przez `IRoomAvailabilityService`. Komunikat gdy brak slotów w siatce.

### Success Criteria

#### Automated Verification

- `dotnet build` — 0 błędów

#### Manual Verification

- Strona `/sale` dostępna z menu
- CRUD sal działa (dodaj, edytuj, usuń)
- Macierz dostępności otwiera się, zapisuje i zapamiętuje zaznaczenia
- Usunięcie sali usuwa jej dostępność (cascade)

---

## Phase 4: Blazor UI — Preferencje Sal przy Przedmiotach

### Overview

Rozszerzenie formularza przedmiotu o opcjonalny wybór preferowanej sali. Wyświetlanie preferencji w tabeli przedmiotów.

### Changes Required

#### 1. SubjectDialog.razor

**File**: `plan-zajec-uczelnia/Components/Pages/SubjectDialog.razor`

**Intent**: Opcjonalny MudSelect z listą sal.

**Contract**: Nowy parametr `List<Room> AllRooms`, `string? PreferredRoomNumber`. `MudSelect T="string?"` z opcją pustą "— brak —" + lista sal (`RoomNumber` + typ). Przekazanie do `SubjectService.CreateAsync` / `UpdateAsync`.

#### 2. Subjects.razor

**File**: `plan-zajec-uczelnia/Components/Pages/Subjects.razor`

**Intent**: Ładowanie listy sal do dialogu; kolumna preferowanej sali w gridzie.

**Contract**: `@inject IRoomService`. `OnInitializedAsync`: ładuje `_rooms`. `OpenAddDialog` / `OpenEditDialog`: przekazuje `AllRooms` i `PreferredRoomNumber`. Nowa kolumna `PreferredRoomNumber` (lub nazwa sali jeśli Include).

#### 3. SubjectService — weryfikacja Include

**File**: `plan-zajec-uczelnia/Services/SubjectService.cs`

**Intent**: Upewnij się że odczyt zwraca PreferredRoom dla wyświetlania w gridzie.

**Contract**: `.Include(s => s.PreferredRoom)` w `GetAllAsync`.

### Success Criteria

#### Automated Verification

- `dotnet build` — 0 błędów

#### Manual Verification

- W dialogu przedmiotu widać opcjonalny select sal
- Zapis preferencji działa; ponowne otwarcie dialogu pokazuje wybraną salę
- Odznaczenie preferencji (— brak —) + Zapisz → `PreferredRoomNumber` null w bazie
- Usunięcie sali z preferencją → przedmiot zostaje, preferencja wyczyszczona (SetNull)
- `POST /auth/login` nadal działa (regresja F-01)

---

## Testing Strategy

### Manual Testing Steps

1. `dotnet run` — otwórz `/sale`, dodaj salę A101 (wykładowa, 120 miejsc)
2. Kliknij kalendarz → zaznacz Pt 10:00 → Zapisz → ponownie otwórz, checkbox zaznaczony
3. Otwórz `/przedmioty` → edytuj przedmiot → wybierz A101 jako preferowaną salę → Zapisz
4. Grid przedmiotów pokazuje A101 w kolumnie preferencji
5. Usuń salę A101 → przedmiot bez preferencji, brak rekordów w RoomAvailabilities
6. `curl POST /auth/login` — token nadal zwracany

## Migration Notes

Migracja `AddRoomsAndPreferences` dodaje tabele `Rooms` (PK string), `RoomAvailabilities` (klucz złożony) i nullable FK `Subjects.PreferredRoomNumber`. Rollback: `dotnet ef database update AddLecturerAvailability`.

## References

- PRD: `context/foundation/prd.md` — FR-005, FR-006
- Roadmap: `context/foundation/roadmap.md` — S-03
- Wzorzec dostępności: `context/changes/lecturer-availability/plan.md`

---

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands.

### Phase 1: Model Danych + Migracja EF Core

#### Automated

- [x] 1.1 `dotnet build` — 0 błędów po dodaniu encji — f244eac
- [x] 1.2 `dotnet ef migrations add AddRoomsAndPreferences` — migracja wygenerowana — f244eac
- [x] 1.3 `dotnet ef database update` — bez błędów — f244eac

#### Manual

- [x] 1.4 Tabele `Rooms`, `RoomAvailabilities` i kolumna `Subjects.PreferredRoomNumber` istnieją w bazie — f244eac

### Phase 2: Warstwa Serwisów

#### Automated

- [x] 2.1 `dotnet build` — 0 błędów po dodaniu serwisów — cf29003

### Phase 3: Blazor UI — Sale i Dostępność

#### Automated

- [x] 3.1 `dotnet build` — 0 błędów po dodaniu UI

#### Manual

- [x] 3.2 Strona `/sale` — CRUD sal działa
- [x] 3.3 Typy zajęć — CRUD w `/ustawienia-planowania`
- [x] 3.4 Liczebność kierunku (ikona grup) + typ zajęć na przedmiocie + szac. grup
- [x] 3.5 POST /auth/login nadal działa (regresja F-01)

### Phase 4: Blazor UI — Preferencje Sal

> Anulowane — wariant A: tylko typ zajęć, bez preferowanej sali.
