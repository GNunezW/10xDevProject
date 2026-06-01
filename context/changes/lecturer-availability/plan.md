# Dostępność Prowadzących (S-02) — Plan Implementacji

## Overview

Drugi slice danych. Umożliwia koordynatorowi wprowadzenie tygodniowego szablonu dostępności każdego prowadzącego (które dni i które sloty godzinowe są dla niego dostępne). Solver w S-05 użyje tych danych jako twardego ograniczenia przy generowaniu planu.

Po tym slice'u koordynator może dla każdego prowadzącego zaznaczyć macierz dostępności (dni Pn–Nd × sloty z siatki godzin) i zapisać jednym kliknięciem.

## Current State Analysis

- `Lecturer` istnieje w `Models/Lecturer.cs` — Id, FirstName, LastName, Email.
- `TimeSlot` istnieje w `Models/TimeSlot.cs` — Id, StartTime (TimeOnly).
- Brak encji dostępności, serwisu, ani UI dla S-02.
- Wzorzec Blazor CRUD (MudDataGrid + fullscreen MudDialog + bulk save) ustalony w S-01.

### Key Discoveries

- DayOfWeek: `System.DayOfWeek` (enum wbudowany w .NET, 0=Sunday..6=Saturday) — brak osobnego enum potrzebny.
- Klucz złożony `(LecturerId, DayOfWeek, TimeSlotId)` konfigurowany w `OnModelCreating` tak jak `SubjectLecturer` w S-01.
- Cascade delete z `TimeSlot` — zgodnie z decyzją: usunięcie slotu usuwa powiązane dostępności.
- Cascade delete z `Lecturer` — spójna zasada: usunięcie prowadzącego usuwa jego dostępności.
- Szablon tygodniowy (bez FK do SemesterPeriod) — prostszy model dla MVP.
- Domyślna dostępność: brak rekordów = prowadzący niedostępny we wszystkich slotach.

## Desired End State

Koordynator na stronie Prowadzący widzi przycisk "Dostępność" przy każdym prowadzącym. Po kliknięciu otwiera się fullscreen dialog z macierzą: wiersze = sloty siatki godzin, kolumny = dni tygodnia (Pn–Nd). Koordynator zaznacza checkboxy i klika "Zapisz" — dane trafiają do PostgreSQL. Przy ponownym otwarciu dialogu zaznaczenia są zapamiętane.

## What We're NOT Doing

- Brak wyjątków datowych (prowadzący niedostępny konkretnego dnia) — to poza MVP.
- Brak powiązania z SemesterPeriod — szablon tygodniowy jest globalny.
- Brak walidacji kompletności (solver w S-05 sprawdzi czy dane są wystarczające).
- Brak powielania dostępności między prowadzącymi.
- Brak importu dostępności (S-04).

## Implementation Approach

Trzy fazy: model → serwis → UI. Fazy 1 i 2 można implementować bez przerwy. Faza 3 wymaga działających faz 1 i 2.

Bulk save strategy: `SaveAsync(lecturerId, IEnumerable<(DayOfWeek, int timeSlotId)>)` usuwa wszystkie rekordy prowadzącego i wstawia nowe — prosta atomowa operacja, bez merge logic.

---

## Phase 1: Model Danych + Migracja EF Core

### Overview

Nowa encja `LecturerAvailability` z kluczem złożonym, konfiguracja relacji w DbContext, migracja PostgreSQL.

### Changes Required

#### 1. Encja LecturerAvailability

**File**: `plan-zajec-uczelnia/Models/LecturerAvailability.cs`

**Intent**: Reprezentuje jeden dostępny slot prowadzącego w danym dniu tygodnia — brak rekordu = niedostępny.

**Contract**: Właściwości: `LecturerId (int)`, `Lecturer (Lecturer)` — navigation, `DayOfWeek (DayOfWeek)` — `System.DayOfWeek` enum, `TimeSlotId (int)`, `TimeSlot (TimeSlot)` — navigation. Brak własnego `Id` — klucz złożony z trzech pól.

#### 2. Aktualizacja ApplicationDbContext

**File**: `plan-zajec-uczelnia/Data/ApplicationDbContext.cs`

**Intent**: Dodaj DbSet i skonfiguruj klucz złożony + cascade delete w OnModelCreating.

**Contract**: Nowy `DbSet<LecturerAvailability> LecturerAvailabilities`. W `OnModelCreating`:

```csharp
modelBuilder.Entity<LecturerAvailability>()
    .HasKey(la => new { la.LecturerId, la.DayOfWeek, la.TimeSlotId });

modelBuilder.Entity<LecturerAvailability>()
    .HasOne(la => la.TimeSlot)
    .WithMany()
    .HasForeignKey(la => la.TimeSlotId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<LecturerAvailability>()
    .HasOne(la => la.Lecturer)
    .WithMany()
    .HasForeignKey(la => la.LecturerId)
    .OnDelete(DeleteBehavior.Cascade);
```

#### 3. Migracja EF Core

**Command**: `dotnet ef migrations add AddLecturerAvailability`

**Intent**: Wygeneruj migrację dla nowej tabeli.

#### 4. Aktualizacja bazy

**Command**: `dotnet ef database update`

**Intent**: Zastosuj migrację na PostgreSQL.

### Success Criteria

#### Automated Verification

- `dotnet build` — 0 błędów
- `dotnet ef migrations add AddLecturerAvailability` — plik migracji wygenerowany
- `dotnet ef database update` — bez błędów

#### Manual Verification

- Tabela `LecturerAvailabilities` istnieje w bazie (`\dt` w psql)

---

## Phase 2: Warstwa Serwisów

### Overview

Interfejs i implementacja serwisu dostępności z operacją GetByLecturer i bulk SaveAsync. Rejestracja w DI.

### Changes Required

#### 1. ILecturerAvailabilityService

**File**: `plan-zajec-uczelnia/Services/ILecturerAvailabilityService.cs`

**Intent**: Kontrakt dla operacji na dostępności prowadzącego.

**Contract**:
```csharp
Task<HashSet<(DayOfWeek Day, int TimeSlotId)>> GetByLecturerAsync(int lecturerId);
Task SaveAsync(int lecturerId, IEnumerable<(DayOfWeek Day, int TimeSlotId)> slots);
```

#### 2. LecturerAvailabilityService

**File**: `plan-zajec-uczelnia/Services/LecturerAvailabilityService.cs`

**Intent**: Implementacja — GetByLecturer zwraca zbiór par (dzień, slot), SaveAsync atomowo zastępuje wszystkie rekordy prowadzącego.

**Contract**: `GetByLecturerAsync` → `AsNoTracking().Where(la => la.LecturerId == lecturerId).Select(la => ValueTuple<DayOfWeek, int>).ToHashSetAsync()`. `SaveAsync` → delete all for lecturer, insert new, SaveChangesAsync.

#### 3. Rejestracja w DI

**File**: `plan-zajec-uczelnia/Program.cs`

**Intent**: Zarejestruj serwis jako Scoped.

**Contract**: `builder.Services.AddScoped<ILecturerAvailabilityService, LecturerAvailabilityService>();`

### Success Criteria

#### Automated Verification

- `dotnet build` — 0 błędów po dodaniu serwisu

---

## Phase 3: Blazor UI

### Overview

Przycisk "Dostępność" na wierszu tabeli Prowadzący. Fullscreen dialog z macierzą checkboxów (wiersze = sloty, kolumny = Pn–Nd). Bulk save.

### Changes Required

#### 1. Aktualizacja Lecturers.razor — kolumna Dostępność

**File**: `plan-zajec-uczelnia/Components/Pages/Lecturers.razor`

**Intent**: Dodaj trzeci przycisk akcji "Dostępność" (ikona `CalendarMonth`) obok Edytuj/Usuń. Kliknięcie otwiera `LecturerAvailabilityDialog`.

**Contract**: W `TemplateColumn` dla Akcji, nowy `MudIconButton` z `Icon="@Icons.Material.Filled.CalendarMonth"` i `OnClick="@(() => OpenAvailabilityDialog(context.Item))"`. Metoda `OpenAvailabilityDialog` otwiera `LecturerAvailabilityDialog` przez `DialogService.ShowAsync` z `DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true }`. Po zamknięciu dialogu: snackbar "Dostępność zapisana." jeśli `!result.Canceled`.

#### 2. LecturerAvailabilityDialog

**File**: `plan-zajec-uczelnia/Components/Pages/LecturerAvailabilityDialog.razor`

**Intent**: Fullscreen dialog z macierzą dostępności. Wiersze = sloty z siatki godzin (posortowane rosnąco po StartTime), kolumny = 7 dni tygodnia (Pn–Nd). Każda komórka to `MudCheckBox`. Przy otwarciu ładuje istniejącą dostępność i pre-zaznacza checkboxy.

**Contract**:
- `[Parameter] Lecturer Lecturer` — prowadzący do edycji.
- `@inject ILecturerAvailabilityService`, `@inject ITimeSlotService`, `@inject ISnackbar`.
- `OnInitializedAsync`: ładuje `_slots = await TimeSlotService.GetAllAsync()` i `_selected = await AvailabilityService.GetByLecturerAsync(Lecturer.Id)`.
- Macierz renderowana jako `<MudSimpleTable>` lub `<table>` z nagłówkami kolumn (Pon, Wt, Śr, Czw, Pt, Sb, Nd) i wierszami per slot.
- Każda komórka: `<MudCheckBox T="bool" Value="IsSelected(slot, day)" ValueChanged="v => Toggle(slot, day, v)" />`.
- `_selected`: `HashSet<(DayOfWeek, int)>` — lokalny stan przed zapisem.
- `Toggle`: dodaje/usuwa parę z `_selected`.
- Przycisk "Zapisz": `await AvailabilityService.SaveAsync(Lecturer.Id, _selected)`, `MudDialog.Close(DialogResult.Ok(true))`.
- Przycisk "Anuluj": `MudDialog.Cancel()`.
- Kolejność dni: `DayOfWeek.Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday`.

### Success Criteria

#### Automated Verification

- `dotnet build` — 0 błędów

#### Manual Verification

- Przycisk "Dostępność" widoczny przy każdym prowadzącym
- Dialog otwiera się z pustą macierzą dla nowego prowadzącego
- Zaznaczenie kilku slotów i kliknięcie "Zapisz" → snackbar sukcesu
- Ponowne otwarcie dialogu — zaznaczenia zapamiętane
- Odznaczenie wszystkich + Zapisz → prowadzący nie ma dostępności (brak rekordów w bazie)
- `dotnet run` + `POST /auth/login` nadal działa (regresja F-01)

---

## Testing Strategy

### Manual Testing Steps

1. `dotnet run` — otwórz `/prowadzacy`
2. Kliknij ikonę kalendarza przy prowadzącym → dialog otwiera się
3. Zaznacz Poniedziałek 08:00 i Środa 10:00 (jeśli sloty istnieją)
4. Kliknij "Zapisz" → snackbar "Dostępność zapisana."
5. Zamknij i otwórz ponownie dialog — checkboxy Pn 08:00 i Śr 10:00 są zaznaczone
6. Odznacz wszystko i Zapisz — dialog zamyka się bez błędu
7. Usuń slot z siatki godzin → sprawdź że `LecturerAvailabilities` nie mają osieroconych rekordów

## Migration Notes

Migracja `AddLecturerAvailability` dodaje tabelę `LecturerAvailabilities` z kluczem złożonym `(LecturerId, DayOfWeek, TimeSlotId)` i FK z cascade delete do `Lecturers` i `TimeSlots`. Rollback: `dotnet ef database update SubjectsLecturersAndGrid`.

## References

- PRD: `context/foundation/prd.md` — FR-004
- Roadmap: `context/foundation/roadmap.md` — S-02
- Poprzednia zmiana: `context/changes/subjects-lecturers-and-grid/plan.md`

---

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands.

### Phase 1: Model Danych + Migracja EF Core

#### Automated

- [x] 1.1 `dotnet build` — 0 błędów po dodaniu encji — 306dd62
- [x] 1.2 `dotnet ef migrations add AddLecturerAvailability` — migracja wygenerowana — 306dd62
- [x] 1.3 `dotnet ef database update` — bez błędów — 306dd62

#### Manual

- [x] 1.4 Tabela `LecturerAvailabilities` istnieje w bazie — 306dd62

### Phase 2: Warstwa Serwisów

#### Automated

- [x] 2.1 `dotnet build` — 0 błędów po dodaniu serwisu

### Phase 3: Blazor UI

#### Automated

- [ ] 3.1 `dotnet build` — 0 błędów po dodaniu UI

#### Manual

- [ ] 3.2 Przycisk "Dostępność" widoczny przy każdym prowadzącym
- [ ] 3.3 Zaznaczenie slotów + Zapisz → sukces
- [ ] 3.4 Ponowne otwarcie → zaznaczenia zapamiętane
- [ ] 3.5 POST /auth/login nadal działa (regresja F-01)
