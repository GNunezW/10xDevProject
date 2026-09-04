---
date: 2026-09-03T16:38:05+02:00
researcher: GNunezW
git_commit: 39b4dfaafc3dfbfdb168098c20a2da1151640f0b
branch: cursor/foundation-bootstrap-infra
repository: 10xDevProject
topic: "Ground rollout Phase 1 — Critical-path coverage (risks #1 staffing, #2 collisions, #4 study mode)"
tags: [research, codebase, scheduling, staffing, solver, xunit]
status: complete
last_updated: 2026-09-03
last_updated_by: GNunezW
---

# Research: Ground rollout Phase 1 — Critical-path coverage (risks #1, #2, #4)

**Date**: 2026-09-03T16:38:05+02:00
**Researcher**: GNunezW
**Git Commit**: 39b4dfaafc3dfbfdb168098c20a2da1151640f0b
**Branch**: cursor/foundation-bootstrap-infra
**Repository**: 10xDevProject

## Research Question

Ground rollout Phase 1 of `context/foundation/test-plan.md`.

Risks to verify: #1 (obsada grup — fałszywe „N grup ⇒ N prowadzących”), #2 (kolizja prowadzący/sala), #4 (tryb studiów: stac vs niestac).

Risk response guidance to verify, not blindly accept:
- #1: prove 1 wolny prowadzący + 4 grupy + wystarczające sloty → generowanie możliwe; challenge fałszywe „4 grupy ⇒ 4 prowadzących”; avoid test utrwalający „za mało prowadzących” jako expected.
- #2: prove po sukcesie brak podwójnej rezerwacji prowadzącego/sali w tym samym slocie; challenge Succeeded ⇒ brak kolizji; avoid mirror solvera bez asercji kolizji.
- #4: prove stac tylko Pn–Pt, niestac tylko Sb–Nd w zapisanym szablonie; challenge helper wystarczy bez sprawdzenia wyniku; avoid pokrycie GetDaysForMode bez generate.

The test plan carries evidence and response intent, not code anchors. For each risk: ground the real failure path, quote relevant lines, verify or correct the response guidance, locate existing tests, identify the cheapest useful test layer.

## Summary

Phase 1 can start with a new xUnit project (none exists in the main tree). All three risks are real, but **#1 is a regression/UX risk, not a current headcount bug**.

- **#1:** Current code counts **weekly time slots**, not people. `SubjectStaffingEvaluator` requires `weeklyCapacity >= groups × weeklySlotsPerGroup`. The solver allows one lecturer across many groups at different `(Day, TimeSlotId)`. Oracle from the interview remains correct: 1 lecturer + 4 groups + enough slots **must stay feasible**. Do not encode “too few lecturers” as expected failure.
- **#2:** CP-SAT enforces global `AddAtMostOne` on lecturer and room per `(Day, TimeSlotId)`. Only `Optimal`/`Feasible` solutions are persisted as `Succeeded`. There is **no post-save collision check** and **no DB unique index** on those keys. `Succeeded` is an assumed invariant, not an independent proof. Cheapest test: oracle on a `ScheduledSession` list (group-by count ≤ 1), not a solver-mirror.
- **#4:** `GetDaysForMode` is **not** used by the generator. `ScheduleGenerationService` duplicates Mon–Fri / Sat–Sun inline when building `AllowedAssignments`. The solver has no `StudyMode` constraint. UI can hide wrong-day rows; Excel exports them. Unit-testing the helper alone is **not** enough.

Cheapest Phase 1 layer: **xUnit units** against public statics (`SubjectStaffingEvaluator`, `ScheduleGridData.GetDaysForMode`, collision oracle on a session list) plus a **small in-process CP-SAT call** if we want #1/#2 on solver output without PostgreSQL. Full `GenerateAsync` + DB is Phase 2 territory (persist contract). F-03’s planned smoke (`WeekCalculator` + `GetDaysForMode`) is a subset; Phase 1 is wider.

## Detailed Findings

### Risk #1 — Obsada grup vs prowadzący

**Failure path (user-lived):** generation blocked with a staffing message that *feels* like “not enough people” when 4 groups exist.

**What the code actually does:** groups come from `InstructionType.EstimateGroupCount(studentCount, type)` (ceiling of students / max per group). Each group × weekly slots becomes a `PlacementTask`. Every group shares the **same** `AllowedAssignments` list built from **all** assigned lecturers (`ScheduleGenerationService.cs:192-226`).

`SubjectStaffingEvaluator.Evaluate` (public static) never compares `assigned` to `groups`:

```55:62:plan-zajec-uczelnia/Services/SubjectStaffingEvaluator.cs
        var weeklySessionsNeeded = groups * weeklySlotsPerGroup;
        var allowedDays = ScheduleGridData.GetDaysForMode(subject.StudyProgram.StudyMode).ToHashSet();
        var weeklyCapacity = CountWeeklyCapacity(subject, context, allowedDays);

        var sufficient = weeklyCapacity >= weeklySessionsNeeded;
        var message = sufficient
            ? $"OK — {assigned} prowadzących, {weeklyCapacity} slotów/tydz. (potrzeba {weeklySessionsNeeded}, {groups} grup)"
            : $"Niewystarczająca obsada — {weeklyCapacity} slotów/tydz. na {weeklySessionsNeeded} spotkań ({groups} grup × {weeklySlotsPerGroup}/tydz.)";
```

Solver: one lecturer cannot occupy two tasks in the same `(LecturerId, Day, TimeSlotId)`, but **can** teach group 1 and group 2 in different slots (`ScheduleCpSatSolver.cs:107-138`).

**Response guidance verdict:** Keep the oracle (1 lecturer + 4 groups + enough slots → feasible). Correct the implication that current code still requires N people — it does not. Risk remains: a future change could reintroduce headcount, or messages (`Niewystarczająca obsada`, `Przeciążenie prowadzącego`) can be misread. Anti-pattern still holds: do not assert failure for that fixture.

**Cheapest layer:** unit on `SubjectStaffingEvaluator.Evaluate` with a hand-built `SubjectStaffingContext` (no DB). Optional second unit: `ScheduleCpSatSolver.Solve` with 4 `PlacementTask`s, same lecturer, 4+ distinct slots → `Success`. Full generate+Postgres is **not** required for Phase 1.

### Risk #2 — Kolizja prowadzący/sala po Succeeded

**Same slot** = `(DayOfWeek, TimeSlotId)` — discrete 90-minute grid cell, shared by solver, diagnostics, and UI (`ScheduleGridCellKey`).

Constraints are **global** (no `StudyProgramId` in the key):

```107:138:plan-zajec-uczelnia/Services/Scheduling/ScheduleCpSatSolver.cs
    private static void AddAtMostOneResourceConstraints(...)
    {
        var lecturerSlotLiterals = new Dictionary<(int LecturerId, DayOfWeek Day, int SlotId), List<BoolVar>>();
        var roomSlotLiterals = new Dictionary<(string Room, DayOfWeek Day, int SlotId), List<BoolVar>>();
        // ... AddAtMostOne per key ...
    }
```

Persistence copies placements then sets `Succeeded` (`ScheduleGenerationService.cs:96-114`). Failed runs never insert sessions (`SchedulePersistenceHelper.SaveFailedRunAsync`). `ScheduleValidationService` is **pre-generate only**. Unique index on `ScheduledSession` is `(ScheduleRunId, SubjectId, GroupIndex, SessionIndex)` — **not** lecturer/room/slot (`ApplicationDbContext.cs:101-103`).

Export and view trust `Succeeded`; the per-program grid cannot show a cross-program lecturer clash.

**Response guidance verdict:** Confirmed. Challenge `Succeeded ⇒ brak kolizji`. Cheapest layer is an **independent oracle** on a list of sessions (group by lecturer/slot and room/slot, count ≤ 1), fed by synthetic fixtures (including a deliberate collision that must fail the oracle). Do not re-implement CP-SAT constraints in the test.

**Cheapest layer:** unit oracle helper + apply it to (a) synthetic rows and (b) optionally solver `Placements` from a tiny in-memory solve. Integration against real `GenerateAsync` belongs with Phase 2 persist tests.

### Risk #4 — Tryb studiów w zapisanym szablonie

`GetDaysForMode` returns FullTime → Mon–Fri, PartTime → Sat–Sun (`ScheduleGridData.cs:13-23`). Call sites: week calculator, staffing evaluator, **view grid columns**. **Not** generator, solver, export, or DB.

Generator duplicate (drift surface):

```201:203:plan-zajec-uczelnia/Services/Scheduling/ScheduleGenerationService.cs
            var allowedDays = program.StudyMode == StudyMode.FullTime
                ? new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }
                : new[] { DayOfWeek.Saturday, DayOfWeek.Sunday };
```

Solver has no `StudyMode` field; it only picks from `AllowedAssignments`. Demo seeds are FullTime only — PartTime never exercised in S-05.

**Response guidance verdict:** Confirmed. Helper-only tests are insufficient. Cheapest extra assertion: for each session (solver placement or persisted row), `DayOfWeek ∈ GetDaysForMode(program.StudyMode)`. A tiny solver fixture with a PartTime-shaped domain (Sat/Sun only in `AllowedAssignments`) plus a negative fixture that would place Monday if the domain leaked is enough without DB. Wiring the generator to call `GetDaysForMode` is a product fix, not a Phase 1 test requirement — but tests should fail if the two lists diverge.

### Test infrastructure

- Main tree: **no** test csproj, **no** `[Fact]`/`[Theory]`, **no** `InternalsVisibleTo` (`AGENTS.md:30`, `plan-zajec-uczelnia.csproj`).
- Roadmap F-03 (`planned`): smoke xUnit for `ScheduleWeekCalculator` + `GetDaysForMode` only; no WebApplicationFactory, no Postgres, no solver.
- Phase 1 is a **superset** of F-03 for #4, plus #1 and #2.
- S-05/S-06 plans: explicit “do not add test project in this slice”; collisions checked **manually** (API run documented in impl-review).
- Public APIs for units without InternalsVisibleTo: `SubjectStaffingEvaluator`, `ScheduleGridData.GetDaysForMode`, `ScheduleWeekCalculator`, `ScheduleCpSatSolver.Solve` (public), `PlacementTask`.
- Lessons.md: Blazor UI stays simplest — **do not** spend Phase 1 budget on Razor/MudBlazor tests.

## Code References

- `plan-zajec-uczelnia/Services/SubjectStaffingEvaluator.cs:13-88` — slot-capacity staffing; no `assigned >= groups`
- `plan-zajec-uczelnia/Models/InstructionType.cs:16-20` — `EstimateGroupCount`
- `plan-zajec-uczelnia/Services/Scheduling/ScheduleGenerationService.cs:192-226` — tasks per group × weekly slot; shared lecturer domain
- `plan-zajec-uczelnia/Services/Scheduling/ScheduleGenerationService.cs:201-203` — inline mode→days (duplicate of helper)
- `plan-zajec-uczelnia/Services/Scheduling/ScheduleGenerationService.cs:96-114` — persist placements then `Succeeded`
- `plan-zajec-uczelnia/Services/Scheduling/ScheduleCpSatSolver.cs:6-55` — public `Solve`; Optimal/Feasible only
- `plan-zajec-uczelnia/Services/Scheduling/ScheduleCpSatSolver.cs:107-138` — global lecturer/room `AddAtMostOne`
- `plan-zajec-uczelnia/Services/Scheduling/ScheduleGridData.cs:5-23` — cell key + `GetDaysForMode`
- `plan-zajec-uczelnia/Services/Scheduling/ScheduleValidationService.cs:78-116` — pre-generate staffing gate
- `plan-zajec-uczelnia/Services/Scheduling/ScheduleInfeasibilityDiagnostics.cs:99-119` — lecturer overload = sessions vs distinct slots
- `plan-zajec-uczelnia/Data/ApplicationDbContext.cs:101-103` — unique index is not a collision constraint
- `plan-zajec-uczelnia/Models/ScheduledSession.cs` — persisted weekly-template row

GitHub (commit `39b4dfa`): [SubjectStaffingEvaluator.cs](https://github.com/GNunezW/10xDevProject/blob/39b4dfaafc3dfbfdb168098c20a2da1151640f0b/plan-zajec-uczelnia/Services/SubjectStaffingEvaluator.cs), [ScheduleCpSatSolver.cs](https://github.com/GNunezW/10xDevProject/blob/39b4dfaafc3dfbfdb168098c20a2da1151640f0b/plan-zajec-uczelnia/Services/Scheduling/ScheduleCpSatSolver.cs), [ScheduleGenerationService.cs](https://github.com/GNunezW/10xDevProject/blob/39b4dfaafc3dfbfdb168098c20a2da1151640f0b/plan-zajec-uczelnia/Services/Scheduling/ScheduleGenerationService.cs)

## Architecture Insights

- Staffing pre-check and solver share the **same time-slot model**, not a headcount model. Tests should lock that contract.
- Collision-freedom and mode-days are **domain-construction + solver** invariants, then **trusted** at persistence. Tests should assert the **output shape** (sessions), not CP-SAT internals.
- `GetDaysForMode` vs generator inline arrays is a classic drift pair: view/validation vs generate/export can disagree.
- Phase 1 should bootstrap xUnit **without** `.sln` if F-02/F-03 still want to avoid solution-file collisions (roadmap F-03 risk note). Prefer `plan-zajec-uczelnia.Tests/` sibling + `ProjectReference`.

## Historical Context (from prior changes)

- `context/changes/generate-and-view-schedule/plan.md` — no test project in S-05; collisions and study mode were **manual** success criteria.
- `context/changes/generate-and-view-schedule/research.md` — solver global uniqueness; study-mode filter at task-build; **no PartTime seed** (open Q).
- `context/changes/generate-and-view-schedule/reviews/impl-review.md` — API generate: 0 lecturer/room collisions on full-time demo.
- `context/changes/export-schedule/plan.md` — still no test project.
- `context/foundation/roadmap.md` F-03 — smoke only: `ScheduleWeekCalculator` + `GetDaysForMode`; excludes solver and DB.
- `context/archive/2026-06-01-rooms-model-and-preferences/` — rooms as type + number; not Phase 1 staffing.

## Related Research

- `context/changes/generate-and-view-schedule/research.md` — S-05 solver/validation grounding (2026-08).

## Open Questions

- Whether Phase 1 includes F-03 week-calculator tests (`Risk #7` is Phase 2 in the test plan) or only #1/#2/#4. Recommendation: include `GetDaysForMode` unit **plus** output assertion; leave `ScheduleWeekCalculator` teaching-week math to Phase 2 unless cheap to add in the same project bootstrap.
- xUnit v2 vs v3: test-plan §4 says v3 (`dotnet new xunit3`); sibling worktree `10x-project-tests` used xUnit 2.9.3. Plan should pick one and pin it.
- Product follow-up (out of Phase 1 test scope): replace inline `allowedDays` with `GetDaysForMode` to kill drift.

## Corrections vs test-plan §2 (for optional backport)

- **#1 response:** “likely cheapest layer = unit if pure staffing function” is **confirmed** (`SubjectStaffingEvaluator`). Do **not** describe current code as still requiring N lecturers.
- **#2 response:** confirmed; add that the independent oracle is on `(LecturerId|RoomNumber, DayOfWeek, TimeSlotId)` counts.
- **#4 source:** hot-spot dir `Services/Scheduling` is still valid; additional evidence is **duplicate day lists** (helper vs generator), not only the helper.
- No speculative risks to drop.
