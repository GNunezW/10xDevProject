# Kierunki, Przedmioty, Prowadzący i Siatka Godzin — Plan Brief

> Full plan: `context/changes/subjects-lecturers-and-grid/plan.md`

## What & Why

Pierwszy slice danych: setup Blazor Server + MudBlazor (jednorazowy) + pięć encji domenowych + cztery strony CRUD. Bez tych danych solver (S-05) nie ma co optymalizować — kierunki, przedmioty z prowadzącymi i siatka godzin to fundament każdego kolejnego slice'a.

## Starting Point

`ApplicationDbContext` ma tylko tabele Identity, brak DbSet-ów domenowych. Blazor i MudBlazor nie są zainstalowane. Projekt jest czystym Web API bez warstwy UI.

## Desired End State

Koordynator loguje się i widzi Blazor app z bocznym menu. Może tworzyć/edytować/usuwać kierunki studiów, prowadzących, przedmioty (przypisane do kierunku + semestru, z wieloma prowadzącymi) i definiować siatkę godzin (godziny startu bloków 90-minutowych). Dane trwałe w PostgreSQL.

## Key Decisions Made

| Decision | Choice | Why |
|---|---|---|
| UI framework | Blazor Server + MudBlazor | Jeden projekt C#, brak JS, jeden użytkownik |
| Lecturer-Subject | M:M (SubjectLecturer junction) | Jeden przedmiot może mieć wielu prowadzących |
| Slot czasowy | Stały 90 min, koordynator definiuje start | Prostszy solver (dyskretna przestrzeń) |
| Rok akademicki | String "2025/2026" | Naturalny format uczelniany |
| Dostęp do danych | DI serwisy bezpośrednio (nie HttpClient) | Prostszy kod, brak overhead JWT w Blazor Server |
| Usuwanie | Hard delete + kaskada StudyProgram→Subjects | Prostszy model; dialog ostrzega przed kaskadą |
| Walidacja | MudForm + DataAnnotations | Built-in MudBlazor, inline feedback |
| Siatka godzin | Globalna (jedna dla całej uczelni) | Prostsze MVP |

## Scope

**In scope:**
- Blazor Server + MudBlazor setup (jednorazowy)
- Encje: StudyProgram, Lecturer, Subject, SubjectLecturer, TimeSlot
- EF Core migration + update
- Serwisy: IStudyProgramService, ILecturerService, ISubjectService, ITimeSlotService
- 4 strony CRUD: /study-programs, /lecturers, /subjects, /hour-grid

**Out of scope:**
- Dostępność prowadzących (S-02), sale (S-03), import (S-04)
- Custom CSS / stylizowanie
- Autoryzacja na poziomie Blazor pages (FallbackPolicy z F-01 wystarcza)
- Testy jednostkowe

## Architecture / Approach

Blazor Server w tym samym projekcie ASP.NET Core 10. Strony wstrzykują serwisy C# przez DI (nie HTTP calls). Serwisy są Scoped (wymagane przez ApplicationDbContext). MudDataGrid + MudDialog na każdej stronie — zero własnych komponentów.

## Phases at a Glance

| Phase | What it delivers | Key risk |
|---|---|---|
| 1. Blazor Setup | App.razor, layout, menu, placeholder pages | Nowe zależności mogą kolidować z istniejącym pipeline |
| 2. Data Models + Migration | 5 encji w bazie PostgreSQL | Błąd w modelu danych cofa S-02, S-03, S-05 |
| 3. Service Layer | IXxxService + implementacje, DI | Scoped lifetime — Singleton nie może wstrzykiwać DbContext |
| 4. CRUD Pages | 4 strony Blazor z formularzami | MudBlazor providers muszą być w App.razor |

**Prerequisites:** F-01 (secure-api-routes) ukończone ✓  
**Estimated effort:** ~3-4 sesje po godzinach (Phase 1: ~2h, Phase 2: ~1h, Phase 3: ~1.5h, Phase 4: ~3h)

## Open Risks & Assumptions

- .NET 10 Blazor Server API może różnić się od dokumentacji .NET 8 — sprawdź przy Phase 1.
- MudBlazor najnowsza wersja kompatybilna z .NET 10 — zweryfikuj NuGet przed instalacją.
- Pojemność sali vs liczebność grupy nie jest modelowana (brak pola `GroupSize` w Subject) — do doprecyzowania przy S-03 lub S-05.

## Success Criteria (Summary)

- `dotnet build` 0 błędów po każdej fazie
- Koordynator może dodać kierunek → przedmiot z prowadzącym → slot siatki godzin przez UI
- Dane są trwałe po restarcie serwera (PostgreSQL)
