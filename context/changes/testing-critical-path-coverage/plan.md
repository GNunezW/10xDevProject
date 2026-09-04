# Critical-path coverage (Phase 1 test rollout) Implementation Plan

## Overview

Bootstrap the first xUnit v3 test project in this repo and lock three critical-path oracles without PostgreSQL or Blazor: staffing is slot-capacity not headcount (risk #1), persisted-shaped session lists have no lecturer/room double-book in the same `(Day, TimeSlotId)` (risk #2), and study-mode days on solver/generator output match `GetDaysForMode` (risk #4).

## Current State Analysis

No test csproj, no `[Fact]`, no `InternalsVisibleTo` (`AGENTS.md`, `plan-zajec-uczelnia.csproj`). S-05/S-06 verified collisions and generate **manually**.

`SubjectStaffingEvaluator.Evaluate` already compares weekly slot capacity to `groups × weeklySlotsPerGroup` — never `assigned >= groups` (`SubjectStaffingEvaluator.cs:55-62`). The lived bug is a **regression/UX** risk (messages mention groups), not current headcount logic.

`ScheduleCpSatSolver` enforces global `AddAtMostOne` on lecturer and room per `(Day, TimeSlotId)` (`ScheduleCpSatSolver.cs:107-138`). `Succeeded` copies placements with **no** post-save collision check (`ScheduleGenerationService.cs:96-114`). DB unique index is `(ScheduleRunId, SubjectId, GroupIndex, SessionIndex)`, not resources (`ApplicationDbContext.cs:101-103`).

`GetDaysForMode` is used by view, week math, and staffing (`ScheduleGridData.cs:13-23`). The generator duplicates Mon–Fri / Sat–Sun inline (`ScheduleGenerationService.cs:201-203`). Solver has no `StudyMode` constraint.

Lessons.md: do not spend budget on MudBlazor/UI tests.

## Desired End State

- `plan-zajec-uczelnia.Tests/` sits **next to** the web project, targets `net10.0`, xUnit v3, `ProjectReference` to the web csproj, **no** `.sln`.
- `dotnet test --project plan-zajec-uczelnia.Tests/plan-zajec-uczelnia.Tests.csproj` is green.
- A fixture of 1 lecturer + 4 groups + enough weekly slots is **sufficient**; a slot shortage (same 1 lecturer) is **not**.
- A collision oracle fails on a synthetic double-book and passes on a tiny in-process `Solve()` of 4 groups / 1 lecturer / distinct slots.
- Generator uses `GetDaysForMode`; FullTime placements are weekdays; PartTime-shaped domains are weekends.
- `context/foundation/test-plan.md` §6.1 and §6.5 name the patterns; `AGENTS.md` documents `dotnet test`.

### Key Discoveries:

- Staffing oracle lives in public static `SubjectStaffingEvaluator` — no InternalsVisibleTo (`SubjectStaffingEvaluator.cs:19-22`).
- Solver is public `ScheduleCpSatSolver.Solve(tasks, slotIndexByTimeSlotId, timeLimitSeconds)` (`ScheduleCpSatSolver.cs:6-10`); OR-Tools comes transitively via the web project.
- `PlacementTask` + `ScheduleAssignment` are public and enough to build an in-memory solve (`PlacementTask.cs:3-24`).
- Groups: `InstructionType.EstimateGroupCount` — 120 students / 30 per group → 4 (`InstructionType.cs:16-20`).
- Roadmap F-03 (`xunit-smoke-tests`) is a **narrower** WeekCalculator smoke; this change does **not** close F-03 / risk #7.

## What We're NOT Doing

- PostgreSQL, seeds, `WebApplicationFactory`, JWT, export Excel (later rollout phases).
- `ScheduleWeekCalculator` teaching-week math (risk #7 / test-plan Phase 2 / F-03).
- `.sln` file (roadmap F-02/F-03 collision note).
- Blazor / MudBlazor / snapshot UI (lessons.md + interview Q5 adjacent).
- Import S-04.
- Production post-persist collision service or DB unique indexes on lecturer/room/slot.
- InternalsVisibleTo.
- Copying `10x-project-tests/` (xUnit 2.9.3 worktree).

## Implementation Approach

Cost × signal: bootstrap runner first, then cheapest public function (#1 evaluator), then an independent session-list oracle (#2) applied to synthetics **and** a tiny CP-SAT result, then kill generator/helper drift (#4) by calling `GetDaysForMode` and asserting days on placements. Last phase fills the cookbook so the next agent can add tests the same way.

Oracle for #1 and #4 comes from the interview/PRD (1 lecturer may cover many groups; stac weekdays / niestac weekends) — **not** from copying current evaluator/solver internals into expected values beyond those behaviors.

## Critical Implementation Details

xUnit v3 templates set `OutputType` to `Exe` and `TestingPlatformDotnetTestSupport` to `true`; retarget `net10.0` after `dotnet new xunit3`. Pin whatever stable (or template) `xunit.v3*` version the template installs — do not downgrade to xUnit 2. Staffing tests must construct a real `SemesterPeriod` whose dates make `ResolveWeeklySlotCount` a **known** integer (pick dates/sessions so `weeklySlotsPerGroup` is 1 or 2) before claiming “enough slots”; otherwise the test becomes an implementation mirror of the week calculator (out of scope). Keep CP-SAT time limit small (1–5 s) and worker count as in production or 1 — flaky timeouts are a fail.

## Phase 1: Bootstrap xUnit v3

### Overview

Create `plan-zajec-uczelnia.Tests/` sibling project so `dotnet test` exists. Delete template sample tests once the project builds, or replace them immediately in Phase 2 — do not leave `UnitTest1` asserting `1 + 1` in the final tree.

### Changes Required:

#### 1. Test project scaffold

**File**: `plan-zajec-uczelnia.Tests/plan-zajec-uczelnia.Tests.csproj` (new)

**Intent**: First test runner in the main repo; xUnit v3 on net10.0, referencing the web project, no solution file.

**Contract**: SDK `Microsoft.NET.Sdk`; `TargetFramework` `net10.0`; nullable + implicit usings; xUnit v3 packages from `dotnet new xunit3`; `ProjectReference` to `../plan-zajec-uczelnia/plan-zajec-uczelnia.csproj`. No InternalsVisibleTo on the web csproj.

#### 2. Scaffold location

**File**: repo root layout (`plan-zajec-uczelnia/` + `plan-zajec-uczelnia.Tests/`)

**Intent**: Match research (“sibling, no .sln”) and F-03 layout intent without claiming F-03 done.

**Contract**: Directory name `plan-zajec-uczelnia.Tests`. Do not put tests inside the web project folder.

### Success Criteria:

#### Automated Verification:

- `dotnet new xunit3` (or equivalent) produces a project that restores
- `dotnet test --project plan-zajec-uczelnia.Tests/plan-zajec-uczelnia.Tests.csproj` exits 0 after scaffold (template test may still be present)

#### Manual Verification:

- Human confirms the test project sits beside `plan-zajec-uczelnia/`, not inside `Components/` or `10x-project-tests/`

**Implementation Note**: After automated verification, pause for human confirmation of location before Phase 2.

---

## Phase 2: Staffing oracle (risk #1)

### Overview

Lock “N groups do not mean N lecturers.” Behavior from interview Q2, not from today’s message strings.

**Behavior asserted:** 1 assigned lecturer, 4 groups, weekly capacity ≥ needed sessions → `IsSufficient == true`. Same lecturer with capacity **below** needed → `IsSufficient == false` (slot shortage).
**Regression caught:** a future `assigned < groups` hard fail.
**Research:** `research.md` Risk #1; `SubjectStaffingEvaluator.cs:55-62`.
**Boundary:** `assigned == 0` remains insufficient; weekend availability must not count toward a FullTime subject (evaluator already filters via `GetDaysForMode`).
**Anti-pattern:** do not expect failure for 1 lecturer + 4 groups + enough slots; do not assert exact Polish message text as the oracle.

### Changes Required:

#### 1. Staffing tests

**File**: `plan-zajec-uczelnia.Tests/` (e.g. `SubjectStaffingEvaluatorTests.cs`)

**Intent**: Hand-built `Subject` + `SubjectStaffingContext` (no DB). 120 students, `MaxStudentsPerGroup = 30` → 4 groups.

**Contract**: Call `SubjectStaffingEvaluator.Evaluate`. Fixture includes `StudyProgram`, `InstructionType`, one `SubjectLecturer`, matching `SemesterPeriod` for `AcademicYear`, and availability hashes. Assert `GroupCount == 4` and `IsSufficient` true/false as above. Optional: FullTime subject ignores Saturday slots in capacity.

### Success Criteria:

#### Automated Verification:

- `dotnet test` — staffing facts pass
- `dotnet build` of web + test projects — 0 errors

#### Manual Verification:

- Skim test names: they describe behavior (feasible / slot shortage), not “covers Evaluate”

---

## Phase 3: Collision oracle + tiny CP-SAT (risks #2 and #1 solver)

### Overview

Independent collision check on a session-shaped list; apply it to synthetics and to `Solve()` output. Do not re-encode `AddAtMostOne`.

**Behavior asserted:** two sessions with the same lecturer (or room) and same `(DayOfWeek, TimeSlotId)` → oracle fail; distinct slots → pass. Tiny solve: 4 tasks (groups 1–4), one lecturer, ≥4 distinct allowed slots, one room type → `Success` and oracle pass (1 lecturer may teach all groups).
**Regression caught:** solver or mapping that double-books; staffing-feasible case that the solver wrongly treats as needing 4 people.
**Research:** `ScheduleCpSatSolver.cs:107-138`; `research.md` Risk #2.
**Boundary:** same lecturer, same day, **different** TimeSlotId is allowed; two programs sharing a lecturer in the same slot is a collision (oracle keys omit program id).
**Anti-pattern:** asserting `Succeeded` without grouping rows; copying CP-SAT constraint code into the test.

### Changes Required:

#### 1. Collision oracle (test-only)

**File**: `plan-zajec-uczelnia.Tests/` helper (not production)

**Intent**: Group by `(LecturerId, Day, TimeSlotId)` and `(RoomNumber, Day, TimeSlotId)`; each group count ≤ 1.

**Contract**: Accept a list of tuples or `ScheduledPlacement` / `ScheduledSession`-like records. Tests: (a) deliberate lecturer clash fails; (b) deliberate room clash fails; (c) clean list passes.

#### 2. In-process Solve fixture

**File**: `plan-zajec-uczelnia.Tests/` (solver tests)

**Intent**: Public `ScheduleCpSatSolver.Solve` with hand-built `PlacementTask`s; short time limit; `slotIndexByTimeSlotId` covering used slot ids.

**Contract**: 4 groups / 1 lecturer / distinct slots → `result.Success`; run collision oracle on `result.Placements`. Adjacent slots may hit max-consecutive / adjacency rules — space allowed assignments so the fixture stays feasible (research: max two consecutive blocks; 15-min break is discrete slots).

### Success Criteria:

#### Automated Verification:

- `dotnet test` — oracle facts + Solve fixture pass
- Solve fixture completes well under 60 s (target: a few seconds)

#### Manual Verification:

- Confirm tests do not start PostgreSQL or `dotnet run`

---

## Phase 4: Study-mode days + cookbook (risk #4)

### Overview

Kill helper/generator drift. Assert days on **output**, not only the helper.

**Behavior asserted:** FullTime → Mon–Fri; PartTime → Sat–Sun via `GetDaysForMode`. After wiring, generator domain uses that helper. Tiny PartTime-shaped `AllowedAssignments` (Sat/Sun only) → all placements on weekend; FullTime-shaped domain → weekdays only.
**Regression caught:** generator list drifting from helper; UI-only helper tests masking bad persisted days.
**Research:** `ScheduleGenerationService.cs:201-203` vs `ScheduleGridData.cs:13-23`.
**Boundary:** helper never returns an empty list; PartTime is two days only.
**Anti-pattern:** testing `GetDaysForMode` without an output/domain assertion.

### Changes Required:

#### 1. Generator uses helper

**File**: `plan-zajec-uczelnia/Services/Scheduling/ScheduleGenerationService.cs`

**Intent**: Single source of mode→days so view, staffing, and generate cannot diverge.

**Contract**: Replace inline `allowedDays` arrays with `ScheduleGridData.GetDaysForMode(program.StudyMode)`. No behavior change if lists already match.

#### 2. Mode tests

**File**: `plan-zajec-uczelnia.Tests/`

**Intent**: Helper units plus assertion on Solve placements’ `Day` ∈ `GetDaysForMode` for the fixture’s mode (encode mode in the allowed domain, then assert).

**Contract**: At least one FullTime-shaped and one PartTime-shaped domain fixture.

#### 3. Cookbook + agent docs

**File**: `context/foundation/test-plan.md` §6.1, §6.5, §6.6; `AGENTS.md` build/test commands

**Intent**: Next agent knows where tests live and which oracle to copy. Allowed: this 10x change owns `context/` cookbook fill-in.

**Contract**: §6.1 location `plan-zajec-uczelnia.Tests/`, naming `*Tests.cs`, run `dotnet test --project plan-zajec-uczelnia.Tests/plan-zajec-uczelnia.Tests.csproj`. §6.5: staffing / collision-oracle / mode-on-placements patterns with pointers to the new tests. AGENTS.md: add `dotnet test --project …` next to `dotnet build`; remove “brak projektu testowego”. Do not rewrite test-plan §1–§2.

### Success Criteria:

#### Automated Verification:

- `dotnet test` — all Phase 2–4 tests pass
- `dotnet build` — web project still 0 errors after generator one-liner

#### Manual Verification:

- Open `test-plan.md` §6: placeholders for 6.1 and 6.5 are gone
- AGENTS.md shows the test command

**Implementation Note**: This is the last phase. After it, the change is ready for `/10x-implement` completion and later `/10x-test-plan` to mark rollout Phase 1 complete.

---

## Testing Strategy

### Unit Tests:

- Staffing: 1 lecturer / 4 groups / enough slots → sufficient; not enough **slots** → insufficient; zero lecturers → insufficient
- Collision oracle: lecturer clash, room clash, clean list
- CP-SAT: 4 groups one lecturer distinct slots → success + oracle
- `GetDaysForMode` FullTime / PartTime
- Placements’ days ⊆ mode days for FullTime- and PartTime-shaped domains

### Integration Tests:

- None in this change (no DB / no host)

### Manual Testing Steps:

1. From repo root: `dotnet test --project plan-zajec-uczelnia.Tests/plan-zajec-uczelnia.Tests.csproj`
2. Confirm no Docker/Postgres required
3. Spot-check cookbook paths match files on disk

## Performance Considerations

Tiny CP-SAT models only (handful of tasks). If a test hits the 60 s product limit, the fixture is wrong — shrink domain, do not raise production `SolverTimeLimitSeconds`.

## Migration Notes

No database migrations. No `.sln` — CI Phase 4 (test-plan) will invoke the test csproj path explicitly.

## References

- Related research: `context/changes/testing-critical-path-coverage/research.md`
- Test plan: `context/foundation/test-plan.md` §2 risks #1, #2, #4; §3 Phase 1
- Staffing: `plan-zajec-uczelnia/Services/SubjectStaffingEvaluator.cs:19-70`
- Solver uniqueness: `plan-zajec-uczelnia/Services/Scheduling/ScheduleCpSatSolver.cs:107-138`
- Generator days: `plan-zajec-uczelnia/Services/Scheduling/ScheduleGenerationService.cs:201-203`
- Helper: `plan-zajec-uczelnia/Services/Scheduling/ScheduleGridData.cs:13-23`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Bootstrap xUnit v3

#### Automated

- [x] 1.1 `dotnet new xunit3` (or equivalent) produces a project that restores — 895fb5e
- [x] 1.2 `dotnet test --project plan-zajec-uczelnia.Tests/plan-zajec-uczelnia.Tests.csproj` exits 0 after scaffold — 895fb5e

#### Manual

- [x] 1.3 Human confirms the test project sits beside `plan-zajec-uczelnia/`, not inside `Components/` or `10x-project-tests/` — 895fb5e

### Phase 2: Staffing oracle (risk #1)

#### Automated

- [x] 2.1 `dotnet test` — staffing facts pass — 1245a65
- [x] 2.2 `dotnet build` of web + test projects — 0 errors — 1245a65

#### Manual

- [x] 2.3 Skim test names: they describe behavior (feasible / slot shortage), not “covers Evaluate” — 1245a65

### Phase 3: Collision oracle + tiny CP-SAT (risks #2 and #1 solver)

#### Automated

- [x] 3.1 `dotnet test` — oracle facts + Solve fixture pass — b0b2ae5
- [x] 3.2 Solve fixture completes well under 60 s (target: a few seconds) — b0b2ae5

#### Manual

- [x] 3.3 Confirm tests do not start PostgreSQL or `dotnet run` — b0b2ae5

### Phase 4: Study-mode days + cookbook (risk #4)

#### Automated

- [x] 4.1 `dotnet test` — all Phase 2–4 tests pass — 742a0ba
- [x] 4.2 `dotnet build` — web project still 0 errors after generator one-liner — 742a0ba

#### Manual

- [x] 4.3 Open `test-plan.md` §6: placeholders for 6.1 and 6.5 are gone — 742a0ba
- [x] 4.4 AGENTS.md shows the test command — 742a0ba
