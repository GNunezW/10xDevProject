---
project: "Plan Zajęć Uczelnia"
version: 1
status: draft
created: 2026-05-20
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

Produkt MVP dla **jednej uczelni** ma przyjąć te dane wejściowe i wygenerować plan zajęć zoptymalizowany pod kątem czasowy: **jak najmniej okienek i przerw** w ramach właściwego trybu studiów (stacjonarni — dni robocze; niestacjonarni — weekendy). Optymalizacja ma odciążyć dział planowania od ręcznego „układania puzzli” przy zachowaniu twardych ograniczeń — lepiej niż ręczne układanie, bo przeszukuje przestrzeń układów pod wiele ograniczeń jednocześnie.

## User & Persona

**Primary persona — koordynator planowania zajęć**

- Rola: pracownik działu planowania / koordynator harmonogramu na uczelni (MVP: jedna szkoła).
- Kontekst: przygotowanie planu zajęć na semestr — zbiera dostępności prowadzących, sale, preferencje, listę przedmiotów i przypisania; musi wydać plan akceptowalny dla studentów i prowadzących.
- Moment użycia: gdy ma komplet danych wejściowych i potrzebuje wygenerować lub poprawić plan zamiast układać go wyłącznie ręcznie.

## Success Criteria

### Primary

- Koordynator wprowadza komplet danych wejściowych (przedmioty + prowadzący + tryb studiów, dostępności prowadzących, dostępność sal, preferencje sal, siatka godzin), uruchamia generowanie i otrzymuje plan bez naruszenia twardych ograniczeń, z zminimalizowaną liczbą okienek w obrębie każdego trybu (stacjonarni w tygodniu, niestacjonarni w weekendy) — w czasie istotnie krótszym niż ręczne układanie od zera.

**MVP flow:**

1. Koordynator loguje się (e-mail + hasło).
2. Pracuje w kontekście jednej uczelni (MVP: single-tenant).
3. Wprowadza dane: przedmioty z przypisanymi prowadzącymi i trybem studiów, dostępność prowadzących, dostępność sal, preferencje sal do przedmiotów, siatkę godzin.
4. Uruchamia generowanie planu (optymalizacja pod mało okienek).
5. Przegląda wynik (plan + sygnał jakości rozkładu / okienek).
6. Eksportuje plan do użycia poza aplikacją.

### Secondary

- Koordynator może wyeksportować wygenerowany plan (format do ustalenia przy implementacji — np. PDF / Excel).

### Guardrails

- Twarde ograniczenia zawsze spełnione: dostępność prowadzącego, dostępność sali, siatka godzin, brak kolizji prowadzący/sala w tym samym slocie, zgodność z trybem studiów (stacjonarni tylko w tygodniu, niestacjonarni tylko w weekendy).
- Plan nie może optymalizować okienek kosztem naruszenia powyższych reguł.

## User Stories

### US-01: Generate optimized schedule from complete inputs

- **Given** a logged-in coordinator who has entered subjects with lecturers and study mode, lecturer availability, room availability, room preferences, and the hour grid
- **When** they trigger schedule generation
- **Then** they receive a conflict-free schedule that respects all hard constraints and shows fewer student gaps than a typical manual baseline

#### Acceptance Criteria

- No double-booking of a lecturer or room in the same time slot
- Every scheduled class uses an available lecturer, an available preferred-or-acceptable room, and a slot from the hour grid
- Full-time classes appear only on weekdays; part-time classes appear only on weekends
- The coordinator sees at least one gap-related quality signal (e.g. total gap hours or gap count) per study mode in the generated plan

## Functional Requirements

### Authentication

- FR-001: Coordinator can log in with email and password. Priority: must-have
  > Socrates: Brak silnego kontrargumentu — logowanie zostaje w MVP.

### Data input

- FR-002: Coordinator can enter and maintain the subject list with assigned lecturers and study mode (full-time / part-time). Priority: must-have
  > Socrates: Kontrargument: przy dużej liczbie przedmiotów samo ręczne wprowadzanie nie skaluje się. Rozwiązanie: FR-007 import podniesiony do must-have; ręczne wprowadzanie zostaje dla mniejszych korekt.
- FR-003: Coordinator can enter lecturer availability for the planning period. Priority: must-have
  > Socrates: Brak kontrargumentu — dostępność prowadzących jest twardym ograniczeniem.
- FR-004: Coordinator can enter room availability for full-time students. Priority: must-have
  > Socrates: Kontrargument: typ sali (laboratorium / audytorium) może być ważniejszy niż konkretna sala. Otwarte: czy MVP modeluje typ sali, konkretną salę, czy oba — do doprecyzowania przy planowaniu danych.
- FR-005: Coordinator can assign room preferences per subject. Priority: must-have
  > Socrates: Brak kontrargumentu — preferencje sal zostają; mogą być ważone miękko w optymalizacji jeśli twarde blokują układ.
- FR-006: Coordinator can define the hour grid (valid time slots). Priority: must-have
  > Socrates: Brak kontrargumentu — siatka godzin jest podstawą slotów.
- FR-007: Coordinator can import scheduling data from a file. Priority: must-have
  > Socrates: Kontrargument: bez importu MVP nieużyteczne przy realnej skali danych. Rozwiązanie: podniesione do must-have.

### Schedule generation & review

- FR-008: Coordinator can trigger generation of a class schedule that minimizes gaps within each study mode (weekdays for full-time, weekends for part-time) among all plans satisfying hard constraints. Priority: must-have
  > Socrates: Kontrargument: „optymalny” plan nie da się obiektywnie udowodnić. Rozwiązanie: obietnica produktu to **najlepszy znaleziony układ w ramach ograniczeń** + widoczne metryki okienek (FR-009), nie gwarancja globalnego optimum.
  > Socrates (doprecyzowanie): tryb studiów jest twardą regułą — zajęcia niestacjonarne nie mogą lądować w tygodniu i odwrotnie.
- FR-009: Coordinator can view the generated schedule and gap-related quality indicators. Priority: must-have
  > Socrates: Brak kontrargumentu — podgląd i metryki są niezbędne przy „best effort” optymalizacji.

### Output

- FR-010: Coordinator can export the generated schedule. Priority: must-have
  > Socrates: Kontrargument: bez eksportu plan nie trafia do procesu uczelni. Rozwiązanie: podniesione do must-have.

## Non-Functional Requirements

- Koordynator otrzymuje wygenerowany plan w czasie akceptowalnym do pracy operacyjnej (docelowo rząd minut, nie godzin) — dokładny próg do ustalenia przy implementacji.
- Dane planowania uczelni są dostępne wyłącznie dla zalogowanego koordynatora tej uczelni (MVP: jedna szkoła).

## Business Logic

Przy spełnionych twardych ograniczeniach (prowadzący, sale, siatka, brak kolizji) produkt dobiera terminy zajęć tak, aby zminimalizować łączny czas okienek między zajęciami — osobno w obrębie kohorty stacjonarnej i osobno w obrębie kohorty niestacjonarnej.

Plan musi być dopasowany do trybu studiów przypisanego do przedmiotu/grupy: zajęcia stacjonarne układane są w dni robocze (tydzień); zajęcia niestacjonarne układane są w weekendy. Reguła jest twarda — optymalizacja okienek nie może jej naruszyć.

Wejścia konsumowane przez reguły: dostępność prowadzących, dostępność sal, preferencje sal do przedmiotów, lista przedmiotów z prowadzącymi i trybem studiów, siatka godzin (z podziałem na okna tygodniowe vs weekendowe).

## Access Control

- **Model:** logowanie e-mail + hasło.
- **Role w MVP:** jedna rola — koordynator planowania (pełny dostęp do wprowadzania danych i generowania planu). Brak osobnych ról dziekana / studenta / prowadzącego w pierwszej wersji.

## Non-Goals

- **Wiele uczelni (multi-tenant)** — MVP obsługuje wyłącznie jedną szkołę; brak izolacji i onboardingu dla kolejnych uczelni.
- **Portal studencki** — studenci nie logują się ani nie przeglądają planu w aplikacji; koordynator eksportuje wynik na zewnątrz.
- **Samodzielna edycja przez prowadzących** — prowadzący nie wprowadzają własnej dostępności w MVP; robi to koordynator (lub import).
- **Synchronizacja na żywo z systemem dziekanackim** — brak dwukierunkowej integracji z systemem dziekanackim w pierwszej wersji; dane przez import lub ręcznie.

## Open Questions

1. **Model sal dla niestacjonarnych** — czy dostępność sal (FR-004) dotyczy osobnych zasobów weekendowych, czy tych samych sal z inną dostępnością? Owner: produkt / koordynator uczelni.
2. **Typ sali vs konkretna sala** (z Socrates FR-004) — co jest minimalnym modelem w MVP? Owner: produkt.
3. **Format importu i eksportu** — jakie pliki uczelnia już dziś używa (Excel, CSV, inny)? Owner: koordynator przy wdrożeniu pilota.
4. **target_scale (qps, data_volume)** — ballpark dla obciążenia i wolumenu danych nie został określony w shape; Owner: produkt / zespół.
