# HOL V1 Technical, Backend and Data Architecture

## 1. Greenfield stack
- Unity 2022.3 LTS or approved current LTS at project creation
- Android first, iOS-ready architecture
- C# client
- UGUI + TextMesh Pro unless a later technical spike proves UI Toolkit materially better
- PlayFab for identity, authoritative competitive state, economy and remote config where appropriate
- Unity LevelPlay for ads
- GitHub Actions for CI and release automation

## 2. Repository principles
- explicit assembly definitions from day one
- pure domain logic separated from Unity presentation
- dependency direction enforced
- no giant `Assembly-CSharp` production dependency graph
- no gameplay logic inside visual presenters
- no direct backend calls from view components

## 3. Proposed assemblies
- `HOL.Core` - pure rules, value objects, RNG interfaces, economy formulas
- `HOL.Application` - use cases, commands, orchestration
- `HOL.Infrastructure` - persistence, PlayFab, ads, analytics, platform adapters
- `HOL.Presentation` - scene controllers and UI state
- `HOL.Design` - production visual owners and visual adapters
- `HOL.Tests.Core`
- `HOL.Tests.EditMode`
- `HOL.Tests.PlayMode`

`HOL.Core` must have no UnityEngine dependency where practical.

## 4. Scene strategy
Keep scenes intentionally few.

Recommended:
- `Bootstrap`
- `Main`

Bootstrap owns startup, dependency initialization, consent, authentication, config, migration and routing.

Main hosts controller-owned screens/panels with clean lifecycle boundaries.

Additive scenes are allowed later only for measurable load, memory or team-ownership reasons.

## 5. Client state model
Separate:
- session state
- profile state
- gameplay state
- cached backend state
- UI presentation state

Never use PlayerPrefs as authoritative economy or competitive storage.

## 6. Persistence
Local encrypted or ordinary platform persistence may hold:
- settings
- localization choice
- onboarding completion cache
- non-sensitive UI preferences
- temporary offline-safe state

Server authority should hold:
- balances
- ownership
- trophies
- ranked stats
- PvP match result
- event rewards
- room state

All save formats require explicit schema version and migrations.

## 7. Backend contracts
### Identity
- anonymous/device first login where platform-appropriate
- upgrade/link path later
- stable PlayFab player identity

### Remote config
Config groups should include:
- economy
- XP curve
- rank thresholds
- ad placements
- recovery costs
- mission templates
- live events
- Daily Hunt reset contract
- reconnect timeout
- feature flags

### PvP
Server authoritative for:
- room membership
- secret submission validation
- opener
- turn
- hints
- Lock state
- forfeit
- rematch reset
- terminal result

Clients submit intent, not outcome.

### Economy
All material grants and purchases require idempotent transaction identifiers.

## 8. Daily Hunt authority
Soft launch can use deterministic shared daily seed if cheating impact is low.

Before competitive daily leaderboards, move secret generation and result validation server-side.

## 9. Ads integration
Create an `IAdsService` abstraction.

Placements:
- rewarded revive
- Save the Pot
- Save the Streak
- optional non-intrusive interstitial policy after product testing

Gameplay code must request a reward intent and receive a verified completion result. Never grant reward on ad open.

## 10. Analytics abstraction
Create an `IAnalyticsService`. Game domain code emits semantic application events, not vendor-specific SDK calls.

## 11. Error strategy
Classify:
- recoverable network failure
- backend rejection
- config unavailable
- version unsupported
- corrupted local migration
- fatal initialization

Fail closed for purchases, rewards and competitive writes. Fail gracefully for cosmetics and optional services.

## 12. Security rules
- no secrets in repository
- no PlayFab title secret in client
- signed CI release secrets only in protected environment
- keystore never committed
- validate all CloudScript inputs
- rate-limit room and signal mutations
- sanitize player-visible names
- do not trust client timestamps for reward eligibility

## 13. Performance targets
Define measurable budgets before content growth:
- cold boot target
- memory ceiling on low-mid Android
- frame pacing target 60 fps on target devices
- no avoidable GC spikes during input/reveal moments
- asset loading strategy documented

## 14. Project bootstrap deliverables
Before gameplay feature work:
- project compiles Android
- assemblies exist
- dependency injection/composition root exists
- test assemblies run
- CI runs on every PR
- PlayFab sandbox connects
- analytics sandbox receives events
- remote config loads
- localization service works
- safe local migration framework exists
