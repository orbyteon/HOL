# HOL V1 Product Charter and PRD

## 1. Product mission
Build the most replayable mobile Higher or Lower game in its category by combining instant understanding, meaningful risk, fair competition, daily ritual, social challenge, cosmetic identity, and lightweight live ops.

## 2. Product promise
A player should understand what to do in seconds, feel a meaningful decision within the first minute, and have a clear reason to return tomorrow.

## 3. Core motivations
- SOLO: risk, instinct, adrenaline
- DUEL: mastery, fairness, rivalry
- DAILY HUNT: habit, comparison, sharing
- FRIEND: connection, invitations, rematches

## 4. Target audience
Primary:
- casual mobile players age 13+
- short-session players
- friends looking for simple competitive play
- players who enjoy streaks, collections, ranks and daily challenges

Secondary:
- younger players in protected mode
- family play
- social audiences reached through challenge links and share cards

## 5. Product principles
1. Learn in seconds.
2. Sessions should usually fit within 30 seconds to 5 minutes.
3. Every mode must have a distinct emotional loop.
4. Competitive outcomes must be fair and auditable.
5. Monetization must not sell competitive advantage.
6. Progression should be visible almost everywhere.
7. Recovery offers must not feel predatory.
8. Retention systems must reward play, not guilt absence.
9. Social systems should help acquisition without requiring open chat.
10. All economy values and live events should be remotely configurable.

## 6. V1 feature set
### P0 gameplay
- onboarding
- SOLO risk run
- BANK
- SOLO Shield protection
- DUEL against AI
- real online DUEL
- DUEL Lock mechanic
- DAILY HUNT shared daily number
- FRIEND private rooms
- rematch
- signals

### P0 meta
- levels 1 to 50
- Rookie, Player, Pro, Master, Legend ranks
- coins
- XP
- competitive trophies
- stats
- basic cosmetic shop
- ownership and equip flow
- settings
- Greek and English localization
- safe younger-player mode

### P0 production systems
- PlayFab backend
- LevelPlay rewarded ads
- analytics
- crash logging
- remote config
- CI
- automated rule tests
- release pipeline

## 7. V1.1 strong retention
- login calendar
- daily missions
- weekly missions
- Save the Streak
- Daily Hunt day 7 chest
- share cards
- friend invite deep links
- collection counters
- first-session contextual FTUE

## 8. Post-launch expansion
- seasons
- achievements
- rivalries
- victory effects
- number skins
- seasonal themes
- weekly journey
- community milestones
- event weekends
- friend challenges

## 9. Critical product decisions
### SOLO
SOLO is not a secret-number duel. It is the namesake Higher or Lower risk mode.

### DUEL
DUEL is the secret-number strategy mode. AI and online use the same canonical rules.

### DAILY HUNT
Daily Hunt is one shared daily secret number, 7 guesses, common challenge, shareable result.

### FRIEND
FRIEND is private real PvP. Invite access must not be blocked by progression if the player enters through a valid friend invite.

### Lock and Shield
- Lock belongs to DUEL.
- Shield belongs to SOLO.
- Do not reuse the same mechanic name for different behavior.

## 10. Success metrics
Primary:
- onboarding completion
- first SOLO completion
- first BANK rate
- D1 retention
- D7 retention
- Daily Hunt participation
- DUEL matches per active user
- rematch rate
- friend invite conversion
- sessions per active day
- percentage of players using 2 or more modes
- coin earn versus spend health
- earned-currency shop conversion
- share rate

Guardrails:
- disconnect rate
- forfeit rate
- first-mover DUEL win rate
- ad opt-out and decline rate
- economy inflation
- excessive recovery-offer frequency
- frustration after Daily Hunt failure
- crash-free sessions

## 11. Product acceptance criteria
V1 is not ready to ship unless:
- a first-time player can reach first meaningful gameplay quickly
- SOLO and DUEL do not feel like duplicate modes
- first-mover advantage is statistically controlled
- economy cannot be trivially farmed
- no competitive currency can be client-authoritatively forged
- all required age and consent paths work
- English and Greek layouts pass visual QA
- analytics can measure the full funnel
- offline or backend failures fail safely
