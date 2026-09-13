---
id: secure-api-routes
title: Zabezpieczenie tras API (F-01)
status: archived
created: 2026-05-31
updated: 2026-09-13
archived_at: 2026-09-13T08:42:00Z
roadmap-ref: F-01
prd-refs: FR-001
---

## Summary

Ustawienie FallbackPolicy (RequireAuthenticatedUser) w AddAuthorization, tak aby wszystkie przyszłe trasy API wymagały ważnego tokenu JWT. Istniejące publiczne trasy (/auth/login, /weatherforecast, OpenAPI dev) oznaczone jako AllowAnonymous.
