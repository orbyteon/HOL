# HOL V1 Analytics, Safety and Monetization

## 1. Analytics objectives
Analytics must answer:
- where players drop
- which mode creates retention
- whether SOLO risk is understandable
- whether DUEL is fair
- whether Daily Hunt drives return behavior
- whether FRIEND creates organic acquisition
- whether economy is healthy
- whether ads damage retention

## 2. Required events
### Acquisition and onboarding
- app_open
- first_open
- onboarding_started
- onboarding_step_completed
- onboarding_completed
- welcome_grant_received

### SOLO
- solo_started
- solo_guess
- solo_push
- solo_bank
- solo_run_ended
- solo_milestone
- shield_shown
- shield_used
- save_pot_shown
- save_pot_accepted
- save_pot_declined

Fields should include rank, run length, pot, current number band, decision, result, and recovery type without collecting unnecessary identity data.

### DUEL
- duel_started
- duel_opener
- duel_guess
- lock_used
- lock_hit
- lock_miss
- sudden_death_started
- duel_completed
- duel_forfeit
- duel_disconnect
- rematch_offered
- rematch_accepted

### Daily Hunt
- daily_opened
- daily_started
- daily_guess
- daily_completed
- daily_failed
- daily_revive_shown
- daily_revive_used
- daily_shared

### Friend
- friend_room_created
- friend_invite_shared
- friend_invite_opened
- friend_room_joined
- friend_match_completed

### Economy and shop
- currency_earned
- currency_spent
- shop_opened
- item_previewed
- purchase_confirmed
- item_equipped

### Retention
Use backend cohorts for D1, D3, D7, D14 and D30. Do not rely only on client events for retained-user truth.

## 3. Core dashboards
- onboarding funnel
- first-session funnel
- retention cohorts
- mode adoption and overlap
- SOLO risk funnel
- DUEL fairness and match duration
- Daily Hunt completion distribution
- friend invite conversion
- economy source/sink dashboard
- ad placement performance
- crashes and backend errors

## 4. Experimentation
Feature flags must allow controlled tests for:
- SOLO reward pacing
- BANK prompts
- mission structure
- login rewards
- Daily Hunt revive placement
- friend invite UX

Never experiment with hidden PvP outcome manipulation.

## 5. Safety model
Age categories follow approved onboarding.

Protected mode requirements may include:
- hide restricted ad CTAs
- conservative audio defaults
- no free-text chat
- no unrestricted profile text
- no direct contact exchange features
- limited external social surfaces
- age-appropriate monetization handling

Final policy must be validated against launch-market requirements before release.

## 6. Consent and privacy
- consent before ad SDK behavior requiring consent
- editable privacy choice in Settings
- data minimization
- documented retention periods
- deletion/account reset path where required
- privacy policy kept consistent with actual SDK behavior

## 7. Monetization principles
Allowed:
- rewarded ads for optional recovery or bonus
- cosmetic purchases later
- cosmetic season pass later

Never sell:
- stronger guesses
- extra competitive Locks
- trophy protection
- hidden-number information
- better RNG
- ranked advantage

## 8. Ad experience rules
- no forced ad during active decision state
- no ad between a correct guess and BANK decision
- no stacked recovery monetization
- one recovery decision at a failure moment
- rewarded grant only after verified reward callback
- interstitial frequency remotely controlled and tested against retention

## 9. Bot transparency
AI opponents must be labelled as bots or practice AI. Do not fabricate human trophies, online status, chat behavior, location, or social history.

## 10. KPI targets
Before soft launch, product leadership must set explicit targets for:
- onboarding completion
- D1
- D7
- first BANK
- first DUEL completion
- Daily Hunt participation
- friend invite conversion
- crash-free sessions
- ad ARPDAU guardrail
- payer conversion only if IAP is introduced

Targets must be stored in a launch scorecard and reviewed weekly during soft launch.
