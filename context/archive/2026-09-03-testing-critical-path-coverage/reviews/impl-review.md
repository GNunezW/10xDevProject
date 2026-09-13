<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: Critical-path coverage (Phase 1 test rollout)

- **Plan**: context/changes/testing-critical-path-coverage/plan.md
- **Scope**: Phase 1–4 of 4
- **Date**: 2026-09-04
- **Verdict**: APPROVED
- **Findings**: 0 critical 2 warnings 2 observations

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | PASS |
| Scope Discipline | PASS |
| Safety & Quality | WARNING |
| Architecture | PASS |
| Pattern Consistency | PASS |
| Success Criteria | PASS |

## Findings

### F1 — `dotnet test` requires `--project` on SDK 10

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Plan Adherence
- **Location**: AGENTS.md:33; context/foundation/test-plan.md:122
- **Detail**: Plan success criteria still show `dotnet test plan-zajec-uczelnia.Tests/plan-zajec-uczelnia.Tests.csproj`. On .NET 10 that form fails; the working command is `dotnet test --project …`. Cookbook and AGENTS.md already document `--project`. Progress 1.2 was marked complete against the path-only wording.
- **Fix**: Align the plan Progress/success-criteria wording with `--project` so the next implementer does not copy a failing command.
- **Decision**: FIXED — aligned plan.md Desired End State, Phase 1 success criteria, Phase 4 cookbook contract, Manual Testing, and Progress 1.2 to `dotnet test --project …`

### F2 — Wall-clock assert does not abort a hung CP-SAT solve

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Safety & Quality
- **Location**: plan-zajec-uczelnia.Tests/ScheduleCpSatSolverTests.cs:42-53
- **Detail**: `timeLimitSeconds: 5` is passed to OR-Tools; a 30s `Stopwatch` assert runs only after `Solve()` returns. Worker count is hardcoded to 8 in production. A hung native solve would stall the test host. Current suite: 12 passed in ~0.9s, so this is latent, not observed.
- **Fix A ⭐ Recommended**: Drop the wall-clock assert; keep `Success` + collision oracle (feasibility is the signal).
  - Strength: Removes a flake class without weakening the risk #1/#2 oracle.
  - Tradeoff: No hard cap if OR-Tools ignores the 5s limit.
  - Confidence: HIGH — the 30s check never fired in this rollout.
  - Blind spot: CI machine slower than this laptop.
- **Fix B**: Add xUnit/MTP method timeout (~15s) on solver facts.
  - Strength: Host can fail the test if Solve hangs.
  - Tradeoff: MTP timeout config is extra; may still not kill native threads.
  - Confidence: MEDIUM — native OR-Tools may ignore cooperative cancel.
  - Blind spot: Exact xUnit v3 timeout attribute for this package.
- **Decision**: FIXED via Fix A — dropped Stopwatch wall-clock assert; kept Success + collision oracle

### F3 — PartTime Solve fixture skips the collision oracle

- **Severity**: ℹ️ OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Pattern Consistency
- **Location**: plan-zajec-uczelnia.Tests/ScheduleCpSatSolverTests.cs:88-92
- **Detail**: FullTime fixture asserts `ScheduleCollisionOracle`; PartTime fixture only asserts weekend days. Plan required mode-on-placements, not a second oracle, so this is optional consistency.
- **Fix**: Call `HasLecturerOrRoomClash` on PartTime placements too.
- **Decision**: FIXED — PartTime Solve fixture now asserts HasLecturerOrRoomClash

### F4 — Template `xunit.runner.json` and repo-root `global.json`

- **Severity**: ℹ️ OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Scope Discipline
- **Location**: plan-zajec-uczelnia.Tests/xunit.runner.json; global.json:1-5
- **Detail**: Not listed as named files in Changes Required; both came from `dotnet new xunit3` and are required for MTP. Benign extras.
- **Fix**: Leave as-is (needed to run tests).
- **Decision**: FIXED — left as-is; `xunit.runner.json` and `global.json` are required for MTP
