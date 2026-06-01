# Lessons Learned

> Append-only register of recurring rules and patterns. Re-read at start by /10x-frame, /10x-research, /10x-plan, /10x-plan-review, /10x-implement, /10x-impl-review.

## Blazor UI — zawsze najprostszy

- **Context**: Blazor Server + MudBlazor dodany do projektu 2026-06-01 jako warstwa UI dla koordynatora.
- **Rule**: UI jest zawsze najprostszy jak to możliwe — MudBlazor defaults, żadnych własnych komponentów, żadnego własnego CSS. Jeśli coś UI zajmuje więcej niż godzinę, robimy za dużo.
- **Applies to**: implement, plan

## Feature flags must have a kill date

- **Context**: Flagi funkcji w appsettings (nowe wpisy w appsettings / konfiguracja środowiskowa).
- **Problem**: Bez daty wyłączenia flagi zostają w konfiguracji na stałe — brak porządku, martwe przełączniki i trudniejsze utrzymanie.
- **Rule**: Every feature flag must have a kill date (when it will be removed or defaulted on).
- **Applies to**: implement
