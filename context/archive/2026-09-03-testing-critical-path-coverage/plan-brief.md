# Critical-path coverage (Phase 1) — Plan Brief

> Full plan: `context/changes/testing-critical-path-coverage/plan.md`
> Research: `context/changes/testing-critical-path-coverage/research.md`

## What & Why

Add the first automated tests so a coordinator cannot silently regress three failures: “4 groups need 4 lecturers,” a saved template that double-books a lecturer or room, and full-time classes landing on a weekend. The oracles come from the interview and PRD, not from copying solver internals.

## Starting Point

The web app has staffing and CP-SAT constraints that already *intend* slot-capacity and global uniqueness, but there is no test project. The generator duplicates `GetDaysForMode` instead of calling it. Collisions were last checked by hand in S-05.

## Desired End State

`dotnet test` on a sibling xUnit v3 project is green. One lecturer covering four groups with enough slots stays feasible. A collision oracle flags double-books. Generate uses the same weekday/weekend list as the helper, and tests assert days on placements.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) | Source |
| -------- | ------ | ---------------- | ------ |
| Test runner | xUnit v3, net10.0 | Matches test-plan §4 / current template | Plan |
| How deep to call the solver | Evaluator units + tiny in-process `Solve()`, no DB | Cheapest layer that still hits solver output | Research + Plan |
| Helper/generator drift | Replace inline days with `GetDaysForMode` | Helper-only tests would miss generate | Plan |
| WeekCalculator / F-03 | Out of this change | Risk #7 is test-plan Phase 2 | Plan |
| Project layout | `plan-zajec-uczelnia.Tests/` sibling, no `.sln` | Avoids F-02/F-03 solution-file clash | Research + Plan |

## Scope

**In scope:** test project bootstrap; staffing facts; collision oracle + small CP-SAT; generator wiring; cookbook §6.1/§6.5; AGENTS.md `dotnet test`.

**Out of scope:** Postgres, WebApplicationFactory, Excel, auth, WeekCalculator, `.sln`, Blazor, production collision indexes.

## Architecture / Approach

Tests reference the web csproj and call public statics / `ScheduleCpSatSolver.Solve` with hand-built models. Collision checks live in the **test** project (group-by counts), not a new production service.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| ----- | ---------------- | -------- |
| 1. Bootstrap xUnit v3 | Restoring test csproj, `dotnet test` runs | Template pins a pre-release package |
| 2. Staffing oracle | 1 lecturer / 4 groups fixture | Fixture dates make week-slot math accidental |
| 3. Collision + CP-SAT | Oracle + tiny Solve | Consecutive-slot constraints make fixture infeasible |
| 4. Mode + cookbook | Generator uses helper; §6 filled | Cookbook paths drift from real files |

**Prerequisites:** .NET 10 SDK; `dotnet new xunit3` template available (install if missing).
**Estimated effort:** ~1 session across 4 short phases.

## Open Risks & Assumptions

- xUnit v3 template may ship a preview package — pin what `dotnet new` gives, do not silently fall back to xUnit 2.
- Tiny Solve fixtures must leave enough slot gaps to satisfy max-consecutive / adjacency rules.

## Success Criteria (Summary)

- `dotnet test plan-zajec-uczelnia.Tests/plan-zajec-uczelnia.Tests.csproj` passes without a database
- Staffing test would fail if someone adds `assigned < groups`
- Collision oracle would fail a double-booked list even if a fake status said success
- FullTime/PartTime days stay consistent between helper and generate
