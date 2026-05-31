# Secure API Routes (F-01) Implementation Plan

## Overview

Ustawienie `FallbackPolicy` w `AddAuthorization` tak, by wszystkie przyszłe trasy API wymagały ważnego tokenu JWT domyślnie — bez konieczności pamiętania o `.RequireAuthorization()` przy każdym nowym endpointcie. Istniejące publiczne trasy oznaczamy jako `AllowAnonymous`.

## Current State Analysis

- `UseAuthentication()` + `UseAuthorization()` są już wdrożone (`Program.cs:61-62`).
- `AddAuthorization()` wywołane bez żadnych polityk (`Program.cs:49`).
- Żadna trasa nie jest chroniona — brak `.RequireAuthorization()` ani `[Authorize]` w kodzie.
- Istniejące trasy: `POST /auth/login`, `GET /weatherforecast`, `GET /.well-known/openapi.json` (dev).
- Brak projektu testowego.

### Key Discoveries

- `AddAuthorization()` w `Program.cs:49` nie definiuje `FallbackPolicy` — to jest luka do wypełnienia.
- `MapOpenApi()` (`Program.cs:57`) zwraca `IEndpointConventionBuilder`, więc `.AllowAnonymous()` zadziała.
- `app.MapAuthEndpoints()` (`Program.cs:64`) mapuje login w `AuthEndpoints.cs:14` — potrzebuje `.AllowAnonymous()` na `MapPost("/auth/login")`.
- `app.MapGet("/weatherforecast", ...)` (`Program.cs:71`) — demo endpoint per `AGENTS.md`; musi pozostać publiczny.

## Desired End State

Wszystkie trasy API wymagają ważnego JWT Bearer tokenu — chyba że jawnie oznaczone `.AllowAnonymous()`. Żądanie bez tokenu do chronionej trasy zwraca `401 Unauthorized`. Istniejące publiczne trasy (`/auth/login`, `/weatherforecast`, OpenAPI dev) działają bez zmian.

## What We're NOT Doing

- Nie dodajemy ról ani claims-based policies (to osobna decyzja w przyszłości).
- Nie usuwamy `/weatherforecast` (per `AGENTS.md`).
- Nie dodajemy projektu testowego.
- Nie chronimy tras z poprzednich slices — żadne trasy danych jeszcze nie istnieją.

## Implementation Approach

Zamiast dodawać `.RequireAuthorization()` do każdego przyszłego endpointu ręcznie, ustawiamy `FallbackPolicy` równą `RequireAuthenticatedUser`. Oznacza to, że każda trasa bez jawnej polityki automatycznie wymaga uwierzytelnionego użytkownika. Publiczne trasy dostają `.AllowAnonymous()`.

## Phase 1: FallbackPolicy + AllowAnonymous na publiczne trasy

### Overview

Jedna faza: zmiana konfiguracji w dwóch plikach. Nie dotyka modelu danych, nie wymaga migracji.

### Changes Required

#### 1. Polityka autoryzacji

**File**: `plan-zajec-uczelnia/Program.cs`

**Intent**: Zastąp `builder.Services.AddAuthorization()` wersją ustawiającą `FallbackPolicy` na `RequireAuthenticatedUser`, tak by każdy endpoint bez jawnej polityki wymagał JWT.

**Contract**: Linia `49`. Nowa forma:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

#### 2. AllowAnonymous na OpenAPI (dev)

**File**: `plan-zajec-uczelnia/Program.cs`

**Intent**: Dodaj `.AllowAnonymous()` na `MapOpenApi()`, żeby dokumentacja OpenAPI była dostępna bez tokenu w środowisku developerskim.

**Contract**: Linia `57`. Zmień `app.MapOpenApi();` na `app.MapOpenApi().AllowAnonymous();`.

#### 3. AllowAnonymous na /weatherforecast

**File**: `plan-zajec-uczelnia/Program.cs`

**Intent**: Oznacz demo endpoint jako publiczny, żeby FallbackPolicy go nie zablokowała.

**Contract**: Linia `83`. Dołącz `.AllowAnonymous()` do łańcucha budowniczego endpointu (za `.WithName("GetWeatherForecast")`).

#### 4. AllowAnonymous na POST /auth/login

**File**: `plan-zajec-uczelnia/Endpoints/AuthEndpoints.cs`

**Intent**: Oznacz endpoint logowania jako publiczny — login musi być dostępny bez tokenu.

**Contract**: `MapPost("/auth/login", ...)` w `AuthEndpoints.cs:14`. Dołącz `.AllowAnonymous()` do zwróconego buildera.

### Success Criteria

#### Automated Verification

- `dotnet build` — 0 błędów, 0 ostrzeżeń
- `dotnet run` uruchamia się bez błędów; seed koordynatora przechodzi

#### Manual Verification

- `curl -X POST http://localhost:<port>/auth/login` z poprawnymi danymi → `200 OK` z tokenem
- `curl http://localhost:<port>/weatherforecast` bez tokenu → `200 OK`
- `curl http://localhost:<port>/openapi/v1.json` (dev) bez tokenu → `200 OK`
- `curl http://localhost:<port>/auth/login` bez ciała → `400 Bad Request` (nie `401`)
- żądanie do przyszłego chronionego endpointu bez tokenu → `401 Unauthorized` (weryfikacja po S-01)

**Implementation Note**: Po zakończeniu tej fazy i przejściu automated verification zatrzymaj się i potwierdź manual testing przed przejściem dalej.

---

## Testing Strategy

### Manual Testing Steps

1. `dotnet run` w `plan-zajec-uczelnia/`
2. Curl POST `/auth/login` z `{"email":"<seed_email>","password":"<seed_pass>"}` → token
3. Curl GET `/weatherforecast` bez nagłówka → 200
4. Curl GET `/openapi/v1.json` bez nagłówka → 200 (tylko dev)
5. Curl POST `/auth/login` bez body → 400 (nie 401 — weryfikacja że AllowAnonymous działa)

## References

- Roadmap: `context/foundation/roadmap.md` (F-01, linia ~158)
- PRD: `context/foundation/prd.md` FR-001
- Program.cs: `plan-zajec-uczelnia/Program.cs:49,57,71,83`
- AuthEndpoints.cs: `plan-zajec-uczelnia/Endpoints/AuthEndpoints.cs:14`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: FallbackPolicy + AllowAnonymous

#### Automated

- [x] 1.1 `dotnet build` — 0 błędów, 0 ostrzeżeń
- [x] 1.2 `dotnet run` uruchamia się bez błędów

#### Manual

- [x] 1.3 POST /auth/login z poprawnymi danymi → 200 z tokenem
- [x] 1.4 GET /weatherforecast bez tokenu → 200
- [x] 1.5 GET /openapi/v1.json bez tokenu → 200 (dev)
- [x] 1.6 POST /auth/login bez body → 400 (nie 401)
