---
project: "Plan Zajęć Uczelnia"
version: 8
status: draft
created: 2026-05-20
updated: 2026-08-28
context_type: greenfield
product_type: web-app
target_scale:
  users: small
timeline_budget:
  mvp_weeks: 5
  hard_deadline: null
  after_hours_only: true
---

## Vision & Problem Statement

Koordynator planowania na uczelni spędza **dni albo tygodnie** na ręcznym układaniu planu z wielu źródeł (dostępność prowadzących, sale, siatka godzin, przypisania do przedmiotów). Mimo tego studenci dostają złe rozkłady — m.in. za dużo okienek albo spiętrzone zjazdy.

**Cel produktu:** dział planowania **nie układa planu sam**. Podaje komplet danych, uruchamia generowanie i dostaje **najlepszy możliwy plan w ramach twardych ograniczeń** (brak kolizji, zgodność z trybem, jak najmniej okienek). Ręczne układanie puzzli ma zejść z tygodni do jednego przebiegu.

W MVP ten plan to **powtarzalny szablon tygodnia** (dni × sloty), który koordynator stosuje w semestrze — nie kalendarz z datą przy każdym spotkaniu. Solver szuka układu z najmniejszą liczbą okienek; w 60 s zapisuje **najlepszy znaleziony** (bez matematycznego dowodu, że nie istnieje jeszcze lepszy). Jeśli w tym czasie nie ma układu bez kolizji — komunikat błędu i brak planu, nigdy „prawie dobry” plan z naruszeniami.

## User & Persona

**Primary persona — koordynator planowania zajęć**

- Rola: pracownik działu planowania / koordynator harmonogramu na uczelni (MVP: jedna szkoła).
- Kontekst: przygotowanie planu na semestr dla jednego lub wielu kierunków — zbiera dane, ale **nie chce ich układać ręcznie przez dni/tygodnie**.
- Moment użycia: gdy ma komplet danych i klika generowanie, żeby dostać gotowy plan (szablon tygodnia do stosowania w semestrze) zamiast układać go od zera.

## Success Criteria

### Primary

- Dział planowania podaje dane i w **nie więcej niż 60 sekund** dostaje plan bez kolizji, z najmniejszą liczbą okienek, jaką system zdążył znaleźć — **zamiast układać ten plan ręcznie przez dni lub tygodnie**. Wynik w MVP to szablon tygodnia per kierunek + metryki okienek. Jeśli nie da się spełnić twardych reguł, jest jasny błąd i **brak zapisanego planu**.

**MVP flow:**

1. Koordynator loguje się (e-mail + hasło).
2. Pracuje w kontekście jednej uczelni (MVP: single-tenant).
3. Definiuje kierunki studiów (nazwa, tryb: stacjonarny / niestacjonarny, rok akademicki).
4. Wprowadza dane (MVP: seedy demo + CRUD): przedmioty przypisane do kierunku i semestru z prowadzącymi; dostępność prowadzących; sale (numer + typ zajęć); siatkę godzin.
5. Uruchamia generowanie — system szuka **najlepszego możliwego** układu (mało okienek, zero kolizji), limit 60 s.
6. Przegląda wyniki — osobny szablon tygodnia per kierunek + metryki okienek.
7. Eksportuje plan do użycia poza aplikacją.

### Secondary

- Koordynator może wyeksportować wygenerowany plan (format do ustalenia przy implementacji — np. PDF / Excel).

### Guardrails

- Twarde ograniczenia zawsze spełnione: dostępność prowadzącego, sala o typie zgodnym z przedmiotem (w MVP sala danego typu jest wolna w każdym slocie siatki — solver tylko zabrania podwójnej rezerwacji), siatka godzin, brak kolizji prowadzący/sala w tym samym slocie, zgodność z trybem studiów (stacjonarni tylko w tygodniu, niestacjonarni tylko w weekendy).
- Minimalna przerwa między kolejnymi zajęciami w planie: 15 minut.
- Zajęcia nie mogą zaczynać się przed 8:00 ani kończyć po 20:00.
- Plan nie może optymalizować okienek kosztem naruszenia powyższych reguł.
- Wynik MVP to szablon tygodnia powtarzalny w semestrze — nie lista zajęć z konkretnymi datami. To i tak ma **zastąpić** ręczne układanie, nie być dodatkową pracą obok niego.
- „Najlepszy możliwy” znaczy: najlepszy układ **znaleziony** w 60 s (najmniej okienek wśród planów bez kolizji). Nie obiecujemy matematycznego dowodu, że nie istnieje jeszcze lepszy.

## User Stories

### US-01: Generate optimized schedule from complete inputs

- **Given** a logged-in coordinator who has defined study programs with assigned subjects (per semester), lecturer availability, rooms (number + instruction type), and the hour grid
- **When** they trigger schedule generation
- **Then** they receive the **best conflict-free weekly template found** (fewest mid-day gaps, all hard constraints held) — so the planning office does not have to lay the timetable out by hand over days or weeks

#### Acceptance Criteria

- The output is a repeating weekly template, not a dated session list for every week of the semester
- No double-booking of a lecturer or room in the same time slot (across all study programs)
- Every scheduled class uses an available lecturer, a room matching the subject's instruction type, and a slot from the hour grid
- Full-time classes appear only on weekdays; part-time classes appear only on weekends
- No class starts before 8:00 or ends after 20:00; at least 15 minutes between consecutive classes
- The coordinator sees at least one gap-related quality signal (e.g. empty slots between the first and last class of the day) per study program in the weekly template
- Generation finishes within 60 seconds; if no feasible template exists (or none is found in time), the coordinator sees a failure message and no schedule is persisted

## Functional Requirements

### Authentication

- FR-001: Coordinator can log in with email and password. Priority: must-have
  > Socrates: Brak silnego kontrargumentu — logowanie zostaje w MVP.

### Semester calendar

- FR-002: Coordinator can define semester calendar entries: academic year (e.g. "2025/2026"), semester ordinal (winter / summer), start date, end date. One entry per (academic year, ordinal) pair. Priority: must-have
  > Dodane 2026-06-01 podczas S-01: SemesterPeriod jest osobną encją globalną dla uczelni. Rok akademicki w kierunku jest wybierany wyłącznie z lat zdefiniowanych w kalendarzu — nie może być wpisany ręcznie.

### Study program management

- FR-003b: Coordinator can create and maintain study programs, each with a name, study mode (full-time / part-time), and academic year chosen from defined semester calendar entries. Priority: must-have
  > Poprzednio FR-002. Decyzja 2026-06-01: kierunek (StudyProgram) to osobna encja — nie etykieta tekstowa. AcademicYear jest ściśle powiązany z SemesterPeriod — koordynator wybiera z listy, nie wpisuje ręcznie.

### Data input

- FR-003: Coordinator can enter and maintain the subject list; each subject is assigned to a study program and semester number, with one or more assigned lecturers. Priority: must-have
  > Poprzednio FR-002. Rozszerzony o przypisanie do kierunku i semestru (decyzja 2026-06-01).
  > Socrates: Kontrargument: przy dużej liczbie przedmiotów samo ręczne wprowadzanie nie skaluje się. W MVP (2026-08-28): dane demo przez seedy + CRUD; import pliku (FR-009) poza MVP do pilota na realnej skali.
- FR-004: Coordinator can enter lecturer availability for the planning period. Priority: must-have
  > Poprzednio FR-003. Brak kontrargumentu — dostępność prowadzących jest twardym ograniczeniem.
- FR-005: Coordinator can enter and maintain rooms: room number (PK) + instruction type (lecture / exercise / lab). The same rooms apply to full-time weekdays and part-time weekends. In MVP a room of a matching type is available in every grid slot; the solver only forbids double-booking a room. Priority: must-have
  > Poprzednio FR-004. Decyzja 2026-05-27: te same sale dla obu trybów; model: numer + typ.
  > Decyzja 2026-06-01 (S-03 wariant A, potwierdzona 2026-08-28 pod PRD v6): **bez** macierzy dostępności sal per slot i **bez** pojemności w modelu. Solver dobiera dowolną salę pasującego typu. Per-slot availability — poza MVP (patrz Non-Goals).
- FR-006: ~~Coordinator can assign room preferences per subject.~~ Poza MVP. Priority: wont-have
  > Poprzednio FR-005. Anulowane 2026-06-01 w S-03 (`RefactorSchedulingCapacityModel` usunął `PreferredRoomNumber`). Preferowana sala nie jest wejściem solvera.
- FR-007: Coordinator can define the hour grid as a single list of start times (90 min blocks, 8:00–20:00). The same slots apply every day; full-time vs part-time only changes which days of the week are used, not a second grid. Priority: must-have
  > Poprzednio FR-006. Decyzja S-01: jedna lista `TimeSlot`, nie osobne siatki weekday/weekend.
- FR-008: Coordinator can edit any previously entered data (subjects, lecturers, lecturer availability, rooms, hour grid, study programs). Priority: must-have
  > Nowe (2026-06-01): jawny wymóg edytowalności danych po wprowadzeniu — koordynator może zmieniać dostępność prowadzących, dodawać/usuwać przedmioty itp.
- FR-009: ~~Coordinator can import scheduling data from a file.~~ Poza MVP. Priority: wont-have
  > Poprzednio FR-007, must-have. Decyzja 2026-08-28 (PRD v7): MVP ładuje dane seedami (`ComputerScienceSeed`, `ChemicalTechnologySeed`) i CRUD-em. Import z pliku wraca przy pilocie, gdy koordynator wgrywa setki wierszy z uczelni — nie jest potrzebny do zaliczenia ani do demo solvera.

### Schedule generation & review

- FR-010: Coordinator can trigger generation of a **repeating weekly class template** — one grid per study program, produced in a single run — that minimizes mid-day gaps within each program (weekdays for full-time, weekends for part-time) among plans found within 60 seconds that satisfy hard constraints. Room allocation is global (one room cannot be double-booked across programs). Number of weekly slots per subject is derived from the subject's session count and teaching weeks in the semester (including non-working days). Priority: must-have
  > Poprzednio FR-008. Rozszerzony o generowanie per kierunek (decyzja 2026-06-01). Solver widzi wszystkie kierunki jednocześnie przy alokacji sal, optymalizuje okienka osobno per kierunek.
  > Decyzja 2026-08-27: **cel** to zastąpić dni/tygodnie ręcznego układania — dział podaje dane i dostaje najlepszy możliwy plan. W MVP ten plan jest **szablonem tygodnia**. „Najlepszy możliwy” = najmniej okienek wśród układów bez kolizji **znalezionych w 60 s**, nie dowód globalnego optimum.
- FR-011: Coordinator can view the generated **weekly template** per study program and gap-related quality indicators for that template. Priority: must-have
  > Poprzednio FR-009. Brak kontrargumentu — podgląd i metryki są niezbędne przy „best effort" optymalizacji.

### Output

- FR-012: Coordinator can export the generated schedule as an Excel file (.xlsx). Priority: must-have
  > Poprzednio FR-010. Kontrargument: bez eksportu plan nie trafia do procesu uczelni.
  > Decyzja 2026-08-28 (S-06): ClosedXML; jeden plik per wybrany kierunek z podglądu (wszystkie semestry i grupy).

## Non-Functional Requirements

- Generowanie kończy się w **nie więcej niż 60 sekund** i zwraca **najlepszy znaleziony** plan bez kolizji (solver cały czas szuka mniejszej liczby okienek, nie zatrzymuje się na pierwszym poprawnym). Brak układu albo przekroczenie limitu bez układu = błąd, bez zapisanego planu.
- Dane planowania uczelni są dostępne wyłącznie dla zalogowanego koordynatora tej uczelni (MVP: jedna szkoła).

## Business Logic

Produkt ma **zastąpić ręczne układanie planu przez dział**. Koordynator podaje dane; solver zwraca najlepszy możliwy układ w ramach twardych ograniczeń.

W MVP ten układ to **szablon jednego tygodnia** do powielenia w semestrze (nie data przy każdym spotkaniu). Liczba slotów tygodniowo na przedmiot wynika z liczby spotkań w semestrze podzielonej przez tygodnie dydaktyczne (z uwzględnieniem dni wolnych).

Przy spełnionych twardych ograniczeniach (prowadzący, sale po typie, siatka, brak kolizji) solver dobiera dni i sloty tak, aby zminimalizować puste sloty **między pierwszym a ostatnim zajęciem danego dnia** — osobno per kierunek. W limicie 60 s zapisujemy układ z najmniejszą znalezioną liczbą okienek. Zastępca prowadzącego jest dozwolony, ale tylko jako słabszy tie-break: **okienka ważą więcej niż kara za zastępcę**. Sale są zasobem globalnym: ta sama sala nie może być zajęta przez dwa kierunki w tym samym slocie szablonu.

Plan musi być dopasowany do trybu studiów przypisanego do kierunku: zajęcia stacjonarne układane są w dni robocze (tydzień); zajęcia niestacjonarne układane są w weekendy. Reguła jest twarda — optymalizacja okienek nie może jej naruszyć.

Dodatkowe twarde reguły czasowe: żadne zajęcia nie mogą zacząć się przed 8:00 ani zakończyć po 20:00; między kolejnymi zajęciami w szablonie musi być co najmniej 15 minut przerwy (przy slotach 90 min: zakaz dwóch zajęć **różnych przedmiotów** tej samej grupy w sąsiednich slotach tego samego dnia). Ten sam przedmiot może iść w sąsiednich slotach, ale **nie więcej niż dwa bloki z rzędu** w jednym dniu.

Jeśli twarde ograniczenia nie dają się spełnić, produkt nie zapisuje planu — pokazuje przyczynę niepowodzenia.

Wejścia konsumowane przez reguły: kierunki studiów (nazwa, tryb, rok akademicki), przedmioty z przypisaniem do kierunku i semestru oraz prowadzącymi, liczba spotkań w semestrze, dostępność prowadzących, sale (dobór po typie zajęć), siatka godzin (8:00–20:00), kalendarz semestru i dni wolne (do wyliczenia tygodni dydaktycznych).

## Access Control

- **Model:** logowanie e-mail + hasło.
- **Role w MVP:** jedna rola — koordynator planowania (pełny dostęp do wprowadzania danych i generowania planu). Brak osobnych ról dziekana / studenta / prowadzącego w pierwszej wersji.

## Non-Goals

- **Wiele uczelni (multi-tenant)** — MVP obsługuje wyłącznie jedną szkołę; brak izolacji i onboardingu dla kolejnych uczelni.
- **Portal studencki** — studenci nie logują się ani nie przeglądają planu w aplikacji; koordynator eksportuje wynik na zewnątrz.
- **Samodzielna edycja przez prowadzących** — prowadzący nie wprowadzają własnej dostępności w MVP; robi to koordynator (CRUD lub seedy).
- **Synchronizacja na żywo z systemem dziekanackim** — brak dwukierunkowej integracji z systemem dziekanackim w pierwszej wersji; dane przez CRUD lub seedy.
- **Import danych planowania z pliku** — poza MVP (FR-009). Skala demo: seedy + ręczne korekty w UI.
- **Plan z datami na cały semestr** — MVP nie rozpisuje każdego spotkania na konkretny dzień kalendarza; wynik to szablon tygodnia, który **jest** planem do stosowania, nie szkicem obok ręcznej pracy.
- **Matematyczny dowód globalnego optimum** — nie obiecujemy, że po 60 s nie istnieje jeszcze lepszy układ; obiecujemy że zapisany plan jest najlepszym **znalezionym** (najmniej okienek, zero kolizji).
- **Macierz dostępności sal per slot** — MVP (S-03 wariant A): sala danego typu jest zawsze wolna; ograniczeniem jest tylko kolizja (jedna sala = jeden kierunek w slocie).
- **Preferencje sal per przedmiot** — anulowane w S-03 (2026-06-01); solver nie dostaje preferowanej sali.
- **Generowanie w tle** — MVP czeka synchronicznie do 60 s; brak kolejki / jobów.

## Open Questions

1. ~~**Model sal dla niestacjonarnych**~~ — ✓ rozstrzygnięte 2026-05-27, doprecyzowane 2026-08-28 (PRD v6): te same sale dla obu trybów. Tryb studiów steruje **dniami** (tydzień vs weekend), nie macierzą dostępności sal. W MVP sale nie mają osobnej dostępności weekendowej.
2. ~~**Typ sali vs konkretna sala**~~ — ✓ rozstrzygnięte 2026-05-27 / S-03 wariant A: numer sali (PK) + typ (wykładowa / ćwiczeniowa / laboratorium). Pojemność i preferowana sala **nie** wchodzą do modelu MVP.
3. ~~**Model kierunku studiów**~~ — ✓ rozstrzygnięte 2026-06-01: StudyProgram jako osobna encja (nazwa, tryb, rok akademicki); przedmiot przypisany do kierunku + numer semestru.
4. ~~**Format eksportu**~~ — ✓ 2026-08-28 (S-06): Excel `.xlsx` (ClosedXML), jeden plik per kierunek. Import odłożony z FR-009.
5. **target_scale (qps, data_volume)** — ballpark dla obciążenia i wolumenu danych nie został określony w shape; Owner: produkt / zespół.
6. ~~**Próg czasu generowania**~~ — ✓ rozstrzygnięte 2026-08-27: 60 sekund; w tym czasie solver szuka najmniejszej liczby okienek i zapisuje najlepszy znaleziony poprawny układ.
