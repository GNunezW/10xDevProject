# Secure API Routes — Plan Brief

> Full plan: `context/changes/secure-api-routes/plan.md`

## What & Why

Ustawiamy `FallbackPolicy = RequireAuthenticatedUser` w `AddAuthorization`, tak by każdy przyszły endpoint API wymagał ważnego JWT bez konieczności jawnego dodawania `.RequireAuthorization()`. To zamyka lukę: middleware auth jest już wdrożony, ale żadna trasa nie jest faktycznie chroniona.

## Starting Point

`Program.cs` ma wdrożone `UseAuthentication()` + `UseAuthorization()` oraz `AddAuthorization()` bez polityk. Wszystkie trasy są w tej chwili publiczne.

## Desired End State

Żądanie do chronionej trasy bez tokenu JWT zwraca `401 Unauthorized`. Trasy publiczne (`/auth/login`, `/weatherforecast`, OpenAPI dev) działają bez zmian. Każdy nowy endpoint automatycznie jest chroniony — nie trzeba o tym pamiętać.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) |
| --- | --- | --- |
| Strategia ochrony | FallbackPolicy globalna | Bezpieczniejsza domyślna — agenty implementujące przyszłe slices nie muszą pamiętać o `.RequireAuthorization()`. |
| Publiczne trasy | AllowAnonymous na 3 istniejące trasy | `/auth/login` musi działać bez tokenu; `/weatherforecast` i OpenAPI to demo/dev. |
| Role / claims | Poza zakresem | PRD nie definiuje ról w MVP; wystarczy `RequireAuthenticatedUser`. |

## Scope

**In scope:**
- `FallbackPolicy` w `AddAuthorization`
- `.AllowAnonymous()` na `/auth/login`, `/weatherforecast`, `MapOpenApi()`

**Out of scope:**
- Role-based policies
- Usunięcie `/weatherforecast`
- Testy automatyczne

## Architecture / Approach

Zmiana konfiguracyjna w dwóch plikach: `Program.cs` (3 linijki) i `AuthEndpoints.cs` (1 linijka). Nie dotyka modelu danych ani migracji.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. FallbackPolicy + AllowAnonymous | Wszystkie trasy chronione, publiczne działają | Brak — zmiana trywialna |

**Prerequisites:** FR-001 (JWT auth) wdrożone ✓  
**Estimated effort:** ~1 sesja, 1 faza

## Open Risks & Assumptions

- Weryfikacja skuteczności FallbackPolicy nastąpi dopiero przy pierwszym rzeczywistym chronionym endpointcie (S-01).

## Success Criteria (Summary)

- `dotnet build` przechodzi bez błędów
- `/auth/login` z poprawnymi danymi zwraca token (AllowAnonymous działa)
- `/weatherforecast` bez tokenu zwraca 200 (AllowAnonymous działa)
