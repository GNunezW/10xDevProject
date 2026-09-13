---
change_id: testing-critical-path-coverage
title: Rollout Phase 1 — Critical-path coverage (obsada grup, kolizje, tryb studiów)
status: archived
created: 2026-09-03
updated: 2026-09-13
archived_at: 2026-09-13T08:42:00Z
---

## Notes

Open a change folder for rollout Phase 1 of context/foundation/test-plan.md: "Critical-path coverage".
Risks covered: #1 (obsada grup — fałszywe „N grup ⇒ N prowadzących"), #2 (kolizja prowadzący/sala), #4 (tryb studiów: stac vs niestac).
Test types planned: unit (+ mała integracja solvera jeśli #1 nie jest czystą funkcją).
Risk response intent:
- #1: prove 1 wolny prowadzący + 4 grupy + wystarczające sloty → generowanie możliwe; challenge fałszywe „4 grupy ⇒ 4 prowadzących"; avoid test utrwalający „za mało prowadzących" jako expected.
- #2: prove po sukcesie brak podwójnej rezerwacji prowadzącego/sali w tym samym slocie; challenge Succeeded ⇒ brak kolizji; avoid mirror solvera bez asercji kolizji.
- #4: prove stac tylko Pn–Pt, niestac tylko Sb–Nd w zapisanym szablonie; challenge helper wystarczy bez sprawdzenia wyniku; avoid pokrycie GetDaysForMode bez generate.
After creating the folder, follow the downstream continuation rule.
