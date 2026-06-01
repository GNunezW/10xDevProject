---
project: "Plan Zajęć Uczelnia"
version: 3
status: draft
created: 2026-05-20
updated: 2026-06-01
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

Koordynator planowania na uczelni spędza tygodnie na ręcznym układaniu planu zajęć z wielu źródeł (dostępność prowadzących, sale, preferencje sal do przedmiotów, siatka godzin, przypisania prowadzących do przedmiotów). Mimo tego wysiłku studenci stacjonarni dostają złe rozkłady czasowe — np. trzy tygodnie bez zjazdów, potem dwa weekendy zjazdów po ~12 h każdy.

Produkt MVP dla **jednej uczelni** ma przyjąć te dane wejściowe i wygenerować plan zajęć zoptymalizowany pod kątem czasowy: **jak najmniej okienek i przerw** w ramach każdego kierunku i trybu studiów (stacjonarni — dni robocze; niestacjonarni — weekendy). Optymalizacja ma odciążyć dział planowania od ręcznego „układania puzzli" przy zachowaniu twardych ograniczeń — lepiej niż ręczne układanie, bo przeszukuje przestrzeń układów pod wiele ograniczeń jednocześnie.

## User & Persona

**Primary persona — koordynator planowania zajęć**

- Rola: pracownik działu planowania / koordynator harmonogramu na uczelni (MVP: jedna szkoła).
- Kontekst: przygotowanie planu zajęć na semestr dla jednego lub wielu kierunków — zbiera dostępności prowadzących, sale, preferencje, listę przedmiotów z przypisaniami do kierunków i semestrów.
- Moment użycia: gdy ma komplet danych wejściowych i potrzebuje wygenerować lub poprawić plan zamiast układać go wyłącznie ręcznie.

## Success Criteria

### Primary

- Koordynator wprowadza komplet danych wejściowych (kierunki studiów z przypisanymi przedmiotami, prowadzącymi i semestrami; dostępności prowadzących; dostępność sal; preferencje sal; siatka godzin), uruchamia generowanie i otrzymuje osobne plany dla każdego kierunku — bez naruszenia twardych ograniczeń, z zminimalizowaną liczbą okienek w obrębie każdego kierunku i trybu — w czasie istotnie krótszym niż ręczne układanie od zera.

**MVP flow:**

1. Koordynator loguje się (e-mail + hasło).
2. Pracuje w kontekście jednej uczelni (MVP: single-tenant).
3. Definiuje kierunki studiów (nazwa, tryb: stacjonarny / niestacjonarny, rok akademicki).
4. Wprowadza dane: przedmioty przypisane do kierunku i semestru z prowadzącymi; dostępność prowadzących; dostępność sal; preferencje sal do przedmiotów; siatkę godzin.
5. Uruchamia generowanie planu (optymalizacja pod mało okienek per kierunek).
6. Przegląda wyniki — osobny plan dla każdego kierunku + metryki okienek.
7. Eksportuje plan do użycia poza aplikacją.

### Secondary

- Koordynator może wyeksportować wygenerowany plan (format do ustalenia przy implementacji — np. PDF / Excel).

### Guardrails

- Twarde ograniczenia zawsze spełnione: dostępność prowadzącego, dostępność sali, siatka godzin, brak kolizji prowadzący/sala w tym samym slocie, zgodność z trybem studiów (stacjonarni tylko w tygodniu, niestacjonarni tylko w weekendy).
- Minimalna przerwa między kolejnymi zajęciami w planie: 15 minut.
- Zajęcia nie mogą zaczynać się przed 8:00 ani kończyć po 20:00.
- Plan nie może optymalizować okienek kosztem naruszenia powyższych reguł.

## User Stories

### US-01: Generate optimized schedule from complete inputs

- **Given** a logged-in coordinator who has defined study programs with assigned subjects (per semester), lecturer availability, room availability, room preferences, and the hour grid
- **When** they trigger schedule generation
- **Then** they receive a separate conflict-free schedule per study program that respects all hard constraints and shows fewer student gaps than a typical manual baseline

#### Acceptance Criteria

- No double-booking of a lecturer or room in the same time slot (across all study programs)
- Every scheduled class uses an available lecturer, an available preferred-or-acceptable room, and a slot from the hour grid
- Full-time classes appear only on weekdays; part-time classes appear only on weekends
- No class starts before 8:00 or ends after 20:00; at least 15 minutes between consecutive classes
- The coordinator sees at least one gap-related quality signal (e.g. total gap hours or gap count) per study program in the generated plan

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
  > Socrates: Kontrargument: przy dużej liczbie przedmiotów samo ręczne wprowadzanie nie skaluje się. Rozwiązanie: FR-009 import podniesiony do must-have; ręczne wprowadzanie zostaje dla mniejszych korekt.
- FR-004: Coordinator can enter lecturer availability for the planning period. Priority: must-have
  > Poprzednio FR-003. Brak kontrargumentu — dostępność prowadzących jest twardym ograniczeniem.
- FR-005: Coordinator can enter room availability (same rooms apply to full-time weekdays and part-time weekends — availability differs per slot). Priority: must-have
  > Poprzednio FR-004. Decyzja 2026-05-27: te same sale, inna dostępność slotów w weekendy vs tygodniu. Model sali: numer sali (PK) + typ (wykładowa / ćwiczeniowa / laboratorium) + pojemność.
- FR-006: Coordinator can assign room preferences per subject. Priority: must-have
  > Poprzednio FR-005. Brak kontrargumentu — preferencje sal zostają; mogą być ważone miękko w optymalizacji jeśli twarde blokują układ.
- FR-007: Coordinator can define the hour grid (valid time slots, 8:00–20:00, separate for weekdays and weekends). Priority: must-have
  > Poprzednio FR-006. Brak kontrargumentu — siatka godzin jest podstawą slotów. Okno 8:00–20:00 jest twardą regułą.
- FR-008: Coordinator can edit any previously entered data (subjects, lecturers, availability, rooms, preferences, hour grid, study programs). Priority: must-have
  > Nowe (2026-06-01): jawny wymóg edytowalności danych po wprowadzeniu — koordynator może zmieniać dostępność prowadzących, dodawać/usuwać przedmioty itp.
- FR-009: Coordinator can import scheduling data from a file. Priority: must-have
  > Poprzednio FR-007. Kontrargument: bez importu MVP nieużyteczne przy realnej skali danych. Rozwiązanie: podniesione do must-have.

### Schedule generation & review

- FR-010: Coordinator can trigger generation of class schedules — one per study program — that minimize gaps within each program (weekdays for full-time, weekends for part-time) among all plans satisfying hard constraints. Room allocation is global (one room cannot be double-booked across programs). Priority: must-have
  > Poprzednio FR-008. Rozszerzony o generowanie per kierunek (decyzja 2026-06-01). Solver widzi wszystkie kierunki jednocześnie przy alokacji sal, optymalizuje okienka osobno per kierunek.
  > Socrates: obietnica produktu to **najlepszy znaleziony układ w ramach ograniczeń** + widoczne metryki okienek (FR-011), nie gwarancja globalnego optimum.
- FR-011: Coordinator can view the generated schedule per study program and gap-related quality indicators. Priority: must-have
  > Poprzednio FR-009. Brak kontrargumentu — podgląd i metryki są niezbędne przy „best effort" optymalizacji.

### Output

- FR-012: Coordinator can export the generated schedule. Priority: must-have
  > Poprzednio FR-010. Kontrargument: bez eksportu plan nie trafia do procesu uczelni. Rozwiązanie: podniesione do must-have.

## Non-Functional Requirements

- Koordynator otrzymuje wygenerowany plan w czasie akceptowalnym do pracy operacyjnej (docelowo rząd minut, nie godzin) — dokładny próg do ustalenia przy implementacji.
- Dane planowania uczelni są dostępne wyłącznie dla zalogowanego koordynatora tej uczelni (MVP: jedna szkoła).

## Business Logic

Przy spełnionych twardych ograniczeniach (prowadzący, sale, siatka, brak kolizji) produkt dobiera terminy zajęć tak, aby zminimalizować łączny czas okienek między zajęciami — osobno w obrębie każdego kierunku studiów. Sale są zasobem globalnym: ta sama sala nie może być zajęta przez dwa kierunki w tym samym slocie.

Plan musi być dopasowany do trybu studiów przypisanego do kierunku: zajęcia stacjonarne układane są w dni robocze (tydzień); zajęcia niestacjonarne układane są w weekendy. Reguła jest twarda — optymalizacja okienek nie może jej naruszyć.

Dodatkowe twarde reguły czasowe: żadne zajęcia nie mogą zacząć się przed 8:00 ani zakończyć po 20:00; między kolejnymi zajęciami w planie musi być co najmniej 15 minut przerwy.

Wejścia konsumowane przez reguły: kierunki studiów (nazwa, tryb, rok akademicki), przedmioty z przypisaniem do kierunku i semestru oraz prowadzącymi, dostępność prowadzących, dostępność sal, preferencje sal do przedmiotów, siatka godzin (8:00–20:00, z podziałem na okna tygodniowe vs weekendowe).

## Access Control

- **Model:** logowanie e-mail + hasło.
- **Role w MVP:** jedna rola — koordynator planowania (pełny dostęp do wprowadzania danych i generowania planu). Brak osobnych ról dziekana / studenta / prowadzącego w pierwszej wersji.

## Non-Goals

- **Wiele uczelni (multi-tenant)** — MVP obsługuje wyłącznie jedną szkołę; brak izolacji i onboardingu dla kolejnych uczelni.
- **Portal studencki** — studenci nie logują się ani nie przeglądają planu w aplikacji; koordynator eksportuje wynik na zewnątrz.
- **Samodzielna edycja przez prowadzących** — prowadzący nie wprowadzają własnej dostępności w MVP; robi to koordynator (lub import).
- **Synchronizacja na żywo z systemem dziekanackim** — brak dwukierunkowej integracji z systemem dziekanackim w pierwszej wersji; dane przez import lub ręcznie.

## Open Questions

1. ~~**Model sal dla niestacjonarnych**~~ — ✓ rozstrzygnięte 2026-05-27: te same sale, inna dostępność w weekendy.
2. ~~**Typ sali vs konkretna sala**~~ — ✓ rozstrzygnięte 2026-05-27: numer sali (PK) + typ (wykładowa / ćwiczeniowa / laboratorium) + pojemność.
3. ~~**Model kierunku studiów**~~ — ✓ rozstrzygnięte 2026-06-01: StudyProgram jako osobna encja (nazwa, tryb, rok akademicki); przedmiot przypisany do kierunku + numer semestru.
4. **Format importu i eksportu** — jakie pliki uczelnia dziś używa (Excel, CSV, inny)? Owner: koordynator przy wdrożeniu pilota. Block: FR-009 (nie — można domyślnie CSV), FR-012 (nie — można domyślnie Excel).
5. **target_scale (qps, data_volume)** — ballpark dla obciążenia i wolumenu danych nie został określony w shape; Owner: produkt / zespół.
