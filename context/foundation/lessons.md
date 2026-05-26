# Lessons Learned

> Append-only register of recurring rules and patterns. Re-read at start by /10x-frame, /10x-research, /10x-plan, /10x-plan-review, /10x-implement, /10x-impl-review.

## Feature flags must have a kill date

- **Context**: Flagi funkcji w appsettings (nowe wpisy w appsettings / konfiguracja środowiskowa).
- **Problem**: Bez daty wyłączenia flagi zostają w konfiguracji na stałe — brak porządku, martwe przełączniki i trudniejsze utrzymanie.
- **Rule**: Every feature flag must have a kill date (when it will be removed or defaulted on).
- **Applies to**: implement
