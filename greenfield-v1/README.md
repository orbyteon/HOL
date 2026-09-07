# HOL V1 Greenfield Build Pack

This folder is the clean-room blueprint for building the next HOL product from scratch in a new repository and a new Unity project.

## Product definition
HOL is a mobile Higher or Lower game with four distinct reasons to play:

- SOLO for adrenaline and risk
- DUEL for mastery and competition
- DAILY HUNT for habit and sharing
- FRIEND for connection and viral growth

## Source of truth order
1. `01_PRODUCT_CHARTER_AND_PRD.md`
2. `02_GAME_DESIGN_AND_RULES.md`
3. `03_ECONOMY_PROGRESSION_RETENTION.md`
4. `04_UX_VISUAL_AND_CONTENT_SYSTEM.md`
5. `05_TECH_BACKEND_AND_DATA_ARCHITECTURE.md`
6. `06_ANALYTICS_SAFETY_MONETIZATION.md`
7. `07_QA_CI_RELEASE_AND_DOD.md`
8. `08_IMPLEMENTATION_ROADMAP.md`

If documents conflict, the earlier document in the list controls product behavior, while visual references control art and layout only.

## Greenfield rule
Do not copy legacy implementation structure by default. Reuse only validated concepts, approved art, localization copy, tested algorithms, backend contracts, and production assets that still match this specification.

## Initial platforms
- Android first
- iOS architecture ready from day one
- Unity native client

## Initial markets
- Cyprus
- Greece
- Poland
- United Kingdom

## Launch languages
- Greek
- English

The architecture must support Polish without redesigning screens or gameplay logic.

## Non-negotiables
- no pay-to-win
- fair server-authoritative competitive outcomes
- no fabricated human opponents
- safe younger-player treatment
- config-driven economy and live ops
- telemetry from first playable build
- automated tests for game rules and economy-critical logic
- approved HOL visual authority retained
- each mode must feel meaningfully different

## Build philosophy
Ship a narrow but excellent V1. Core loops, progression, economy, sharing, retention, safety, analytics and technical integrity are part of the product, not cleanup work after gameplay is finished.
