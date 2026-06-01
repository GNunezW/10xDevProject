# Kierunki, Przedmioty, Prowadzący i Siatka Godzin (S-01) — Plan Implementacji

## Overview

Pierwszy slice danych. Buduje trzy warstwy jednocześnie:
1. **Infrastruktura UI** — jednorazowy setup Blazor Server + MudBlazor w istniejącym projekcie ASP.NET Core 10.
2. **Model danych** — pięć nowych encji EF Core z relacjami i migracja PostgreSQL.
3. **CRUD** — serwisy C# + cztery strony Blazor (MudDataGrid + MudDialog) dla kierunków, prowadzących, przedmiotów i siatki godzin.

Po tym slice'u koordynator może zalogować się i wprowadzić pełne dane wejściowe gotowe dla S-02 (dostępność prowadzących) i S-03 (sale).

## Current State Analysis

- `ApplicationDbContext` dziedziczy po `IdentityDbContext<AppUser>` bez własnych `DbSet`-ów (`Data/ApplicationDbContext.cs:6`).
- Dwie migracje: `InitialCreate` + `AddIdentity` — brak tabel domenowych.
- Blazor Server i MudBlazor nie są zainstalowane (`plan-zajec-uczelnia.csproj` — brak pakietów).
- Brak warstwy serwisów — logika biznesowa dotychczas nieobecna.
- `Program.cs` nie zawiera `AddRazorComponents` ani `AddMudServices`.

### Key Discoveries

- Serwisy EF Core muszą być rejestrowane jako `Scoped` — `ApplicationDbContext` jest Scoped; Singleton-y nie mogą go wstrzykiwać.
- Blazor Server w .NET 10 wymaga `AddRazorComponents().AddInteractiveServerComponents()` po stronie serwera i `app.MapRazorComponents<App>().AddInteractiveServerRenderMode()` po stronie pipeline.
- MudBlazor wymaga CSS/JS w `App.razor` (nie `wwwroot/index.html` — to Blazor WASM) oraz trzech providerów: `<MudThemeProvider>`, `<MudDialogProvider>`, `<MudSnackbarProvider>`.
- Kaskadowe usuwanie `StudyProgram → Subject` musi być skonfigurowane jawnie w `OnModelCreating` — domyślne zachowanie EF Core dla wymaganych FK to Restrict, nie Cascade.
- Relacja M:M `Subject ↔ Lecturer` przez explicit junction table `SubjectLecturer` — EF Core wymaga `HasKey` złożonego w `OnModelCreating`.

## Desired End State

Koordynator po zalogowaniu widzi aplikację Blazor z bocznym menu. Może:
- Tworzyć, edytować i usuwać **kierunki studiów** (nazwa, tryb stac/niestac, rok akademicki).
- Tworzyć, edytować i usuwać **prowadzących** (imię, nazwisko, email).
- Tworzyć, edytować i usuwać **przedmioty** z przypisaniem do kierunku, semestru (1–7) i jednego lub wielu prowadzących.
- Definiować **siatkę godzin** (lista godzin startu bloków 90-minutowych w oknie 8:00–20:00).

Dane są trwale zapisane w PostgreSQL. `dotnet build` przechodzi bez błędów.

## What We're NOT Doing

- Brak dostępności prowadzących (S-02) i sal (S-03) — to osobne slice'y.
- Brak importu CSV (S-04) — osobny slice.
- Brak generowania planu (S-05).
- Brak stylizowania UI ponad MudBlazor defaults — zero własnego CSS.
- Brak autoryzacji na poziomie Blazor pages w tym slice'u — FallbackPolicy z F-01 chroni całość; szczegółowe atrybuty `[Authorize]` w Blazor można dodać później.
- Brak testów jednostkowych (brak projektu testowego w repo).

## Implementation Approach

Setup Blazor (Phase 1) musi poprzedzać strony (Phase 4) — stąd sekwencja. Data models (Phase 2) muszą poprzedzać serwisy (Phase 3). Fazy 2 i 3 mogą być implementowane razem przez tego samego agenta bez przerwy.

Blazor pages wywołują serwisy bezpośrednio przez DI (`@inject IStudyProgramService`) — brak HttpClient, brak REST call. ApplicationDbContext jest Scoped, serwisy rejestrujemy jako Scoped.

## Critical Implementation Details

- **Blazor DI lifetime**: wszystkie serwisy domenowe rejestruj jako `AddScoped<>` — `ApplicationDbContext` jest Scoped; Singleton serwisy nie mogą go wstrzykiwać i rzucą wyjątek w runtime.
- **Kaskadowe usuwanie**: EF Core domyślnie dla wymaganych FK ustawia Restrict. Aby kaskada `StudyProgram → Subjects` działała, skonfiguruj jawnie `OnDelete(DeleteBehavior.Cascade)` w `OnModelCreating`.
- **MudBlazor providers**: `<MudThemeProvider>`, `<MudDialogProvider>`, `<MudSnackbarProvider>` muszą być w ciele `App.razor` — bez nich dialogi potwierdzenia i snackbary nie działają.
- **TimeOnly w PostgreSQL**: Npgsql od wersji 6+ obsługuje `TimeOnly` natywnie jako `time`. Brak dodatkowej konfiguracji potrzebny.

---

## Phase 1: Blazor Server + MudBlazor Setup

### Overview

Jednorazowy setup infrastruktury UI. Po tej fazie `dotnet run` serwuje Blazor app z bocznym menu i pustymi stronami placeholderowymi. Żadnych danych domenowych jeszcze.

### Changes Required

#### 1. Pakiet MudBlazor

**File**: `plan-zajec-uczelnia/plan-zajec-uczelnia.csproj`

**Intent**: Dodaj pakiet MudBlazor przez `dotnet add package MudBlazor`.

**Contract**: Nowy `<PackageReference Include="MudBlazor" Version="..." />` w `<ItemGroup>`.

#### 2. Rejestracja serwisów Blazor w DI

**File**: `plan-zajec-uczelnia/Program.cs`

**Intent**: Zarejestruj Razor Components z Interactive Server render mode i MudBlazor services.

**Contract**: Dodaj przed `var app = builder.Build()`:

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
```

#### 3. Mapowanie Blazor w pipeline

**File**: `plan-zajec-uczelnia/Program.cs`

**Intent**: Dodaj `MapRazorComponents` do pipeline HTTP (po `UseAuthorization`).

**Contract**: Dodaj po `app.UseAuthorization()`:

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
```

#### 4. Komponent główny App.razor

**File**: `plan-zajec-uczelnia/Components/App.razor`

**Intent**: Stwórz root komponent Blazor z HTML shell, MudBlazor CSS/JS i providerami. Renderuje `<Routes>`.

**Contract**: Plik zawiera pełny `<!DOCTYPE html>` z `<head>` (MudBlazor CSS, Google Fonts Roboto) i `<body>` (`<Routes @rendermode="InteractiveServer">`, `<MudThemeProvider>`, `<MudDialogProvider>`, `<MudSnackbarProvider>`, MudBlazor JS script).

#### 5. Router

**File**: `plan-zajec-uczelnia/Components/Routes.razor`

**Intent**: Standardowy Blazor router wskazujący na `MainLayout`.

**Contract**: `<Router AppAssembly="typeof(App).Assembly">` z `<Found>` renderującym `<RouteView DefaultLayout="typeof(MainLayout)">` i `<NotFound>` z komunikatem 404.

#### 6. Główny layout z MudBlazor

**File**: `plan-zajec-uczelnia/Components/Layout/MainLayout.razor`

**Intent**: Layout z `MudLayout` — górny pasek (`MudAppBar`) i boczna nawigacja (`MudDrawer` z `NavMenu`). Treść strony w `MudMainContent`.

**Contract**: Dziedziczy po `LayoutComponentBase`. Używa `MudLayout`, `MudAppBar` (tytuł "Plan Zajęć"), `MudDrawer` z `<NavMenu />`, `MudMainContent` z `@Body`.

#### 7. Menu nawigacyjne

**File**: `plan-zajec-uczelnia/Components/Layout/NavMenu.razor`

**Intent**: `MudNavMenu` z czterema pozycjami prowadzącymi do stron CRUD.

**Contract**: `MudNavLink` dla: `/study-programs` (Kierunki), `/lecturers` (Prowadzący), `/subjects` (Przedmioty), `/hour-grid` (Siatka godzin).

#### 8. Globalne importy

**File**: `plan-zajec-uczelnia/Components/_Imports.razor`

**Intent**: Globalne `@using` dla przestrzeni nazw MudBlazor i projektu — bez tego każda strona Blazor musiałaby je importować osobno.

**Contract**: `@using MudBlazor`, `@using plan_zajec_uczelnia.Models`, `@using plan_zajec_uczelnia.Services`.

#### 9. Placeholder strony

**Files**: `Components/Pages/StudyPrograms.razor`, `Lecturers.razor`, `Subjects.razor`, `HourGrid.razor`

**Intent**: Puste strony z `@page` dyrektywami — żeby nawigacja działała po Phase 1. Treść zastąpiona w Phase 4.

**Contract**: Każda: `@page "/[route]"`, `@rendermode InteractiveServer`, `<MudText Typo="Typo.h5">[Nazwa sekcji]</MudText>`.

### Success Criteria

#### Automated Verification

- `dotnet build` — 0 błędów, 0 ostrzeżeń
- `dotnet run` uruchamia się bez wyjątków

#### Manual Verification

- Przeglądarka na `http://localhost:<port>` pokazuje Blazor app z MudBlazor layout i bocznym menu
- Nawigacja między stronami (Kierunki, Prowadzący, Przedmioty, Siatka godzin) działa bez 404
- `POST /auth/login` nadal zwraca token (regresja F-01)

**Implementation Note**: Zatrzymaj się po Phase 1 i potwierdź manual testing przed przejściem do Phase 2.

---

## Phase 2: Data Models + EF Core Migration

### Overview

Pięć nowych encji domenowych, konfiguracja relacji w DbContext i migracja PostgreSQL. Po tej fazie tabele istnieją w bazie.

### Changes Required

#### 1. Enum trybu studiów

**File**: `plan-zajec-uczelnia/Models/StudyMode.cs`

**Intent**: Enum reprezentujący tryb studiów — używany w `StudyProgram` i przy filtracji solvera.

**Contract**: `public enum StudyMode { FullTime = 1, PartTime = 2 }`

#### 2. Model StudyProgram

**File**: `plan-zajec-uczelnia/Models/StudyProgram.cs`

**Intent**: Encja kierunku studiów — klucz dla wszystkich przedmiotów i przyszłych planów.

**Contract**: Właściwości: `Id (int)`, `Name (string, required, max 200)`, `StudyMode (StudyMode)`, `AcademicYear (string, required, max 20, np. "2025/2026")`, `Subjects (ICollection<Subject>)` — navigation property.

#### 3. Model Lecturer

**File**: `plan-zajec-uczelnia/Models/Lecturer.cs`

**Intent**: Encja prowadzącego — zasób dla solvera i wyświetlania planu.

**Contract**: Właściwości: `Id (int)`, `FirstName (string, required, max 100)`, `LastName (string, required, max 100)`, `Email (string, required, max 200, unikalne)`, `SubjectLecturers (ICollection<SubjectLecturer>)`.

#### 4. Model Subject

**File**: `plan-zajec-uczelnia/Models/Subject.cs`

**Intent**: Encja przedmiotu — przypisana do kierunku i semestru, z wieloma prowadzącymi.

**Contract**: Właściwości: `Id (int)`, `Name (string, required, max 200)`, `StudyProgramId (int)`, `StudyProgram (StudyProgram)` — navigation, `Semester (int, range 1–7)`, `NumberOfSessions (int, required, min 1)` — liczba bloków 90-minutowych do zaplanowania w semestrze; solver używa tej wartości bezpośrednio bez przeliczania, `SubjectLecturers (ICollection<SubjectLecturer>)`.

#### 5. Tabela pośrednicząca SubjectLecturer

**File**: `plan-zajec-uczelnia/Models/SubjectLecturer.cs`

**Intent**: Explicit junction entity dla relacji M:M Subject ↔ Lecturer — wymagana przez EF Core przy złożonym PK.

**Contract**: Właściwości: `SubjectId (int)`, `Subject (Subject)`, `LecturerId (int)`, `Lecturer (Lecturer)`. Brak własnego `Id` — klucz złożony konfigurowany w DbContext.

#### 6. Model TimeSlot

**File**: `plan-zajec-uczelnia/Models/TimeSlot.cs`

**Intent**: Slot siatki godzin — godzina startu 90-minutowego bloku zajęć.

**Contract**: Właściwości: `Id (int)`, `StartTime (TimeOnly, required)`. Blok trwa zawsze 90 min (stała `TimeSpan.FromMinutes(90)` w logice serwisu, nie przechowywana w bazie). Constraint: `StartTime >= 08:00` i `StartTime + 90 min <= 20:00` — walidacja w serwisie, nie constraint DB.

#### 7. Aktualizacja ApplicationDbContext

**File**: `plan-zajec-uczelnia/Data/ApplicationDbContext.cs`

**Intent**: Dodaj `DbSet`-y dla nowych encji i skonfiguruj relacje w `OnModelCreating`.

**Contract**:
- Nowe DbSet-y: `DbSet<StudyProgram>`, `DbSet<Lecturer>`, `DbSet<Subject>`, `DbSet<SubjectLecturer>`, `DbSet<TimeSlot>`.
- W `OnModelCreating`: złożony PK na SubjectLecturer, kaskadowe usuwanie StudyProgram→Subject, unikalny index na `Lecturer.Email`.

Konfiguracja relacji w `OnModelCreating` (nieoczywista — snippet wymagany):

```csharp
modelBuilder.Entity<SubjectLecturer>()
    .HasKey(sl => new { sl.SubjectId, sl.LecturerId });

modelBuilder.Entity<StudyProgram>()
    .HasMany(sp => sp.Subjects)
    .WithOne(s => s.StudyProgram)
    .HasForeignKey(s => s.StudyProgramId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<Lecturer>()
    .HasIndex(l => l.Email)
    .IsUnique();
```

#### 8. Migracja EF Core

**Command**: `dotnet ef migrations add SubjectsLecturersAndGrid`

**Intent**: Wygeneruj migrację dla pięciu nowych encji.

#### 9. Aktualizacja bazy

**Command**: `dotnet ef database update`

**Intent**: Zastosuj migrację na PostgreSQL (Azure).

### Success Criteria

#### Automated Verification

- `dotnet build` — 0 błędów
- `dotnet ef migrations add` tworzy plik migracji bez błędów
- `dotnet ef database update` kończy się bez błędów

#### Manual Verification

- W bazie PostgreSQL istnieją tabele: `StudyPrograms`, `Lecturers`, `Subjects`, `SubjectLecturers`, `TimeSlots`

**Implementation Note**: Zatrzymaj się i potwierdź manual testing (sprawdzenie tabel w bazie) przed Phase 3.

---

## Phase 3: Warstwa Serwisów

### Overview

Cztery interfejsy serwisów + implementacje z operacjami CRUD. Rejestracja w DI. Po tej fazie Blazor pages mogą wstrzykiwać serwisy.

### Changes Required

#### 1. IStudyProgramService + StudyProgramService

**File**: `plan-zajec-uczelnia/Services/IStudyProgramService.cs` + `StudyProgramService.cs`

**Intent**: CRUD dla kierunków. Delete sprawdza czy kierunek ma przedmioty (mimo kaskady — dla czytelnego komunikatu błędu w UI przed faktycznym usunięciem).

**Contract**:
```csharp
Task<List<StudyProgram>> GetAllAsync();
Task<StudyProgram?> GetByIdAsync(int id);
Task<StudyProgram> CreateAsync(StudyProgram program);
Task UpdateAsync(StudyProgram program);
Task DeleteAsync(int id);
Task<bool> HasSubjectsAsync(int programId);
```

#### 2. ILecturerService + LecturerService

**File**: `plan-zajec-uczelnia/Services/ILecturerService.cs` + `LecturerService.cs`

**Intent**: CRUD dla prowadzących. Email musi być unikalny — serwis rzuca wyjątek jeśli istnieje duplikat.

**Contract**:
```csharp
Task<List<Lecturer>> GetAllAsync();
Task<Lecturer?> GetByIdAsync(int id);
Task<Lecturer> CreateAsync(Lecturer lecturer);
Task UpdateAsync(Lecturer lecturer);
Task DeleteAsync(int id);
```

#### 3. ISubjectService + SubjectService

**File**: `plan-zajec-uczelnia/Services/ISubjectService.cs` + `SubjectService.cs`

**Intent**: CRUD dla przedmiotów z zarządzaniem relacją M:M z prowadzącymi. Create/Update przyjmuje listę LecturerId.

**Contract**:
```csharp
Task<List<Subject>> GetAllAsync(int? studyProgramId = null);
Task<Subject?> GetByIdWithLecturersAsync(int id);
Task<Subject> CreateAsync(Subject subject, IEnumerable<int> lecturerIds);
Task UpdateAsync(Subject subject, IEnumerable<int> lecturerIds);
Task DeleteAsync(int id);
```

#### 4. ITimeSlotService + TimeSlotService

**File**: `plan-zajec-uczelnia/Services/ITimeSlotService.cs` + `TimeSlotService.cs`

**Intent**: CRUD dla slotów siatki godzin. Walidacja: `StartTime >= 08:00` i `StartTime + 90min <= 20:00`. Brak duplikatów StartTime.

**Contract**:
```csharp
Task<List<TimeSlot>> GetAllAsync();
Task<TimeSlot> CreateAsync(TimeSlot slot);
Task DeleteAsync(int id);
```

#### 5. Rejestracja w DI

**File**: `plan-zajec-uczelnia/Program.cs`

**Intent**: Zarejestruj wszystkie cztery serwisy jako Scoped przed `var app = builder.Build()`.

**Contract**: `builder.Services.AddScoped<IStudyProgramService, StudyProgramService>()` (i analogicznie dla pozostałych trzech).

### Success Criteria

#### Automated Verification

- `dotnet build` — 0 błędów, 0 ostrzeżeń

#### Manual Verification

- Brak — weryfikacja serwisów nastąpi przez UI w Phase 4.

---

## Phase 4: Blazor CRUD Pages

### Overview

Cztery strony Blazor zastępują placeholdery z Phase 1. Każda: MudDataGrid (lista) + MudDialog (formularz add/edit) + potwierdzenie usunięcia.

### Changes Required

#### 1. StudyPrograms.razor

**File**: `plan-zajec-uczelnia/Components/Pages/StudyPrograms.razor`

**Intent**: Strona listy kierunków z możliwością dodawania, edytowania i usuwania. Usunięcie kierunku z przedmiotami wymaga potwierdzenia w MudDialog z ostrzeżeniem o kaskadzie.

**Contract**: `@page "/study-programs"`, `@rendermode InteractiveServer`. `@inject IStudyProgramService`. Tabela: kolumny Nazwa, Tryb, Rok Akademicki + przyciski Edytuj/Usuń. Przycisk "Dodaj kierunek" otwiera MudDialog z MudForm: `MudTextField` (Nazwa), `MudSelect<StudyMode>` (Tryb), `MudTextField` (Rok Akademicki). DataAnnotations walidacja. Usunięcie: najpierw `HasSubjectsAsync` — jeśli true, MudDialog ostrzega "Ten kierunek ma przedmioty — zostaną usunięte kaskadowo".

#### 2. Lecturers.razor

**File**: `plan-zajec-uczelnia/Components/Pages/Lecturers.razor`

**Intent**: Strona listy prowadzących z CRUD. Email musi być unikalny — formularz pokazuje błąd serwera przy duplikacie.

**Contract**: `@page "/lecturers"`. Tabela: Imię, Nazwisko, Email + Edytuj/Usuń. MudDialog z MudForm: `MudTextField` dla FirstName, LastName, Email (type="email"). Błąd duplikatu email obsłużony jako snackbar/błąd formularza.

#### 3. Subjects.razor

**File**: `plan-zajec-uczelnia/Components/Pages/Subjects.razor`

**Intent**: Strona listy przedmiotów z filtrem po kierunku. Formularz add/edit zawiera wybór kierunku, semestru i prowadzących (multi-select).

**Contract**: `@page "/subjects"`. `@inject ISubjectService`, `@inject IStudyProgramService`, `@inject ILecturerService`. Filtr: `MudSelect<int?>` wybór kierunku (filtruje tabelę). Tabela: Nazwa, Kierunek, Semestr, Liczba zajęć, Prowadzący (concat) + Edytuj/Usuń. MudDialog: `MudTextField` (Nazwa), `MudSelect<int>` (Kierunek), `MudNumericField<int>` (Semestr, Min=1, Max=7), `MudNumericField<int>` (Liczba zajęć w sem., Min=1, label z podpowiedzią "× 90 min"), `MudSelect<int>` z `MultiSelection=true` (Prowadzący).

#### 4. HourGrid.razor

**File**: `plan-zajec-uczelnia/Components/Pages/HourGrid.razor`

**Intent**: Strona definicji siatki godzin — lista slotów startowych (wyświetlana jako "08:00 – 09:30") z możliwością dodawania i usuwania. Uproszczony UI: brak edycji, tylko add/delete.

**Contract**: `@page "/hour-grid"`. `@inject ITimeSlotService`. Lista: `MudChip` lub `MudList` wyświetla każdy slot jako "HH:mm – HH:mm" (StartTime + 90 min). Przycisk "+ Dodaj slot" otwiera prosty MudDialog z `MudTimePicker` (wybór StartTime, minuty co 15). Walidacja: StartTime w zakresie 08:00–18:30 (bo 18:30+90min=20:00). Usuń: ikona kosza przy każdym slocie + potwierdzenie MudDialog.

### Success Criteria

#### Automated Verification

- `dotnet build` — 0 błędów, 0 ostrzeżeń

#### Manual Verification

- Koordynator może dodać kierunek studiów (np. "Informatyka", Stacjonarny, "2025/2026") → widoczny w tabeli
- Koordynator może dodać prowadzącego (Jan Kowalski, jan@uczelnia.pl)
- Koordynator może dodać przedmiot (np. "Matematyka", kierunek Informatyka, semestr 2, prowadzący Jan Kowalski) → widoczny z prowadzącym w tabeli
- Koordynator może zdefiniować slot siatki godzin (np. 08:00 → wyświetlany jako "08:00 – 09:30")
- Usunięcie kierunku z przedmiotami pokazuje ostrzeżenie, po potwierdzeniu usuwa kaskadowo
- Email duplikatu prowadzącego pokazuje błąd zamiast wyjątku serwera
- `POST /auth/login` nadal działa (regresja F-01)

**Implementation Note**: Zatrzymaj się po Phase 4 i potwierdź manual testing przed commitem.

---

## Testing Strategy

### Manual Testing Steps

1. `dotnet run` — otwórz `http://localhost:<port>`
2. Sprawdź że layout Blazor się ładuje z bocznym menu
3. Zaloguj się przez `POST /auth/login` (token JWT) — regresja
4. Dodaj kierunek: Informatyka, Stacjonarny, 2025/2026
5. Dodaj prowadzącego: Jan Kowalski, jan@uczelnia.pl
6. Dodaj przedmiot: Matematyka, Informatyka, sem. 2, prowadzący: Jan Kowalski
7. Dodaj slot siatki: 08:00 → widoczny jako "08:00 – 09:30"
8. Edytuj przedmiot — zmień semestr na 3
9. Usuń kierunek z przedmiotami — potwierdź ostrzeżenie, sprawdź kaskadę
10. Próba dodania prowadzącego z duplikatem email → błąd

## Migration Notes

Migracja `SubjectsLecturersAndGrid` dodaje tabele: `StudyPrograms`, `Lecturers`, `Subjects`, `SubjectLecturers`, `TimeSlots`. Rollback: `dotnet ef database update AddIdentity` cofa do stanu przed S-01.

## References

- PRD: `context/foundation/prd.md` — FR-002, FR-003, FR-007, FR-008
- Roadmap: `context/foundation/roadmap.md` — S-01
- Poprzednia zmiana: `context/changes/secure-api-routes/plan.md`
- Tech stack: `context/foundation/tech-stack.md` (Blazor Server + MudBlazor)

---

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands.

### Phase 1: Blazor Server + MudBlazor Setup

#### Automated

- [x] 1.1 `dotnet build` — 0 błędów po dodaniu Blazor + MudBlazor
- [x] 1.2 `dotnet run` uruchamia się bez wyjątków

#### Manual

- [x] 1.3 Przeglądarka pokazuje Blazor app z MudBlazor layout i bocznym menu
- [x] 1.4 Nawigacja między czterema stronami działa bez 404
- [x] 1.5 POST /auth/login nadal zwraca token (regresja F-01)

### Phase 2: Data Models + EF Core Migration

#### Automated

- [x] 2.1 `dotnet build` — 0 błędów po dodaniu modeli i aktualizacji DbContext
- [x] 2.2 `dotnet ef migrations add SubjectsLecturersAndGrid` — plik migracji wygenerowany
- [x] 2.3 `dotnet ef database update` — bez błędów

#### Manual

- [x] 2.4 Tabele StudyPrograms, Lecturers, Subjects, SubjectLecturers, TimeSlots istnieją w bazie

### Phase 3: Warstwa Serwisów

#### Automated

- [x] 3.1 `dotnet build` — 0 błędów po dodaniu serwisów i rejestracji DI

### Phase 4: Blazor CRUD Pages

#### Automated

- [x] 4.1 `dotnet build` — 0 błędów po dodaniu stron CRUD

#### Manual

- [x] 4.2 Koordynator może dodać kierunek studiów → widoczny w tabeli
- [x] 4.3 Koordynator może dodać prowadzącego (Jan Kowalski, jan@uczelnia.pl)
- [x] 4.4 Koordynator może dodać przedmiot z prowadzącym → widoczny w tabeli
- [x] 4.5 Koordynator może dodać slot siatki godzin (08:00 → "08:00 – 09:30")
- [x] 4.6 Usunięcie kierunku z przedmiotami → ostrzeżenie + kaskada po potwierdzeniu
- [x] 4.7 Duplikat email prowadzącego → błąd (nie wyjątek serwera)
- [x] 4.8 POST /auth/login nadal działa (regresja F-01)
