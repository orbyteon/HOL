# HOL — Higher or Lower
## Canonical Merged Product Scope

**Status:** Product baseline for implementation
**Purpose:** Merge the strongest mechanics from the current Unity HOL repository and the Kimi deployment scope into one native-product direction.
**Primary platform:** Unity native mobile app
**Initial markets:** Cyprus, Greece, Poland, United Kingdom
**Launch languages:** Greek and English

## 1. Product Vision
HOL is a fast, social, replayable number game built around four distinct motivations:
- **SOLO = instinct, risk and reward**
- **DUEL = strategy and competition**
- **DAILY HUNT = habit, challenge and sharing**
- **FRIEND = social play**

Each major mode must have a clearly different emotional role and gameplay loop.

## 2. Product Principles
1. Instant understanding.
2. Short sessions, generally 30 seconds to 5 minutes.
3. Meaningful decisions around risk, banking, locking and guessing.
4. No pay-to-win.
5. Fair competitive rules.
6. Constantly visible progression.
7. Cosmetic identity without balance advantages.
8. Safe younger-player mode.
9. Localization first.
10. Keep the sound Unity, PlayFab, LevelPlay, telemetry, haptics and CI foundations.

## 3. Core Game Modes

### 3.1 SOLO — Higher or Lower Risk Run
The primary HOL namesake mode.

**Core loop**
1. Show current number.
2. Player chooses HIGHER or LOWER.
3. Reveal next number with a strong slot/shuffle animation.
4. Correct answer increases run, pot and XP.
5. Player chooses to continue or BANK.
6. Wrong answer loses the unbanked pot unless protection applies.

**Number ranges by rank**
- Rookie: 1–50
- Player: 1–75
- Pro: 1–100
- Master: 1–150
- Legend: 1–200

**Rewards**
Base correct reward target: `10 coins × rank multiplier`, plus XP. Values must be remotely configurable.

**Run milestones**
- 3 correct: bonus burst
- 5 correct: MEGA milestone
- 7 correct: CHEST milestone
- Beyond 7: prestige, records and escalating reward potential

**BANK**
Bank safely transfers the current pot to the wallet. Banking is available after the first correct answer. Recommended minimum qualifying run for meaningful win statistics is 3. Exiting while a pot exists should auto-bank by default.

**SOLO Lock protection**
Unlocked at Level 7. One use per run, cost scales by rank. A wrong Higher/Lower choice consumes the protection and preserves the pot. Exact continuation/reset behavior should be balance tested.

**Save the Pot**
For meaningful runs, recommended 3+, offer a one-time recovery choice: coins, rewarded ad where allowed, or decline. Pricing scales with run value.

**Feedback**
Slot/reel reveal, run pips, increasing flame intensity, animated pot, milestone character reactions, chest/confetti, and contextual haptics.

**Records**
Best run, biggest bank, lifetime correct guesses, highest milestone, most coins in one run.

### 3.2 DUEL — Strategic Hidden Number Battle
Two players each hide a number from 1–100. Coin flip determines opener. Players alternate guesses and receive Higher, Lower or Correct feedback.

**Fairness**
The responder always receives the answering turn in the same round if the opener finds the number.

**Lock**
Account unlock at Level 7. One per match. Correct locked hit beats an unlocked hit in the same round. Missed Lock forfeits the next turn. UX may suggest Lock only when strategically meaningful.

**Same-round double-hit resolution**
1. Locked correct beats unlocked correct.
2. If equal, smaller remaining candidate range wins.
3. If still tied, enter Sudden Death.
4. Sudden Death continues until one player wins outright.
5. Use a configurable hard cap.
6. At cap, fewest total guesses wins.
7. If still equal, draw.

This merges the strategic current Unity tiebreak with the drama and clarity of Kimi-style Sudden Death.

**Opponents**
Real PvP through PlayFab, plus clearly labelled AI bots. Never fabricate bot social proof.

**AI**
Easy, Normal, Hard, Adaptive. Target midpoint/binary-search competence approximately 30–40%, 55–75%, 90–100%, and performance-adjusted respectively. After 3+ consecutive losses, Adaptive may temporarily soften difficulty.

**Forfeit**
A real loss, trophies reduced, streak reset, no win rewards. Back/close/exit during an active match uses the same confirmation flow.

**Rematch**
One-tap rematch. Real PvP requires both players to accept.

**Signals**
Six lightweight localized signals, initially 🍀 🎯 🩹 ⭐ ⏳ ❤️. Per-match mute, persistent mute, rate limiting, no free-text chat in V1.

### 3.3 DAILY HUNT — Shared Daily Number Hunt
Keep the current Unity concept as canonical:
- one shared deterministic daily number
- range 1–100
- 7 guesses
- Higher/Lower clues
- one primary attempt per day
- state saved after every guess
- same challenge for everyone

Success grants coins, XP, Daily Hunt streak progress and a share result. Failure does not punish the normal competitive streak.

**Rewarded revive**
Where allowed, one revive per day grants +2 guesses. Hide under protected younger-player mode.

**7-day Daily Hunt track**
Add a larger chest for completing the 7-day track. Missing-day behavior should be positive rather than punitive.

**Share card**
Day identifier, attempts, directional trail, result, streak and optional rank badge. Never reveal the daily secret before reset.

For competitive Daily Hunt leaderboards, migrate answer/validation server-side.

### 3.4 FRIEND — Private Room PvP
Create Room, Join Room, short room code, clear expiry, self-join prevention, waiting state, reconnect grace period and start when both players are ready.

Recommended pre-match room lifetime: 10 minutes.

Use real PlayFab room/PvP architecture. If no friend joins, expose an explicit Practice with Bot action, never silently replace the friend.

Future: best-of-3, mini-tournaments, spectating and deep-link invites.

## 4. Progression

### Levels
Levels 1–50. Initial XP requirement formula: `60 + (level - 1) × 40`, remotely configurable.

### Ranks
| Rank | Levels | SOLO Range | Base Win Reward Target |
|---|---:|---:|---:|
| Rookie | 1–5 | 1–50 | 50c / 20xp |
| Player | 6–10 | 1–75 | 100c / 40xp |
| Pro | 11–20 | 1–100 | 150c / 60xp |
| Master | 21–35 | 1–150 | 250c / 100xp |
| Legend | 36–50 | 1–200 | 500c / 200xp |

### Unlock schedule
- Level 1: SOLO
- Level 3: DUEL
- Level 4: DAILY HUNT
- Level 6: FRIEND
- Level 7: LOCK

Later progression unlocks cosmetics, frames, titles, effects and themes.

Level-up presentation queues behind match/result/economy resolution.

## 5. Competitive Trophies
Initial target: win +24 to +32, draw 0, loss -10 to -16, floor 0. Later migration path: hidden MMR plus visible seasonal trophies and leagues. Trophies are never purchasable.

## 6. Economy
**Currencies:** Coins for cosmetics/selected protection, XP for progression, trophies for competitive ranking.

**Faucets:** SOLO banking, DUEL, Daily Hunt, login calendar, missions, achievements, milestones, selected rewarded ads.

**Sinks:** avatars, frames, titles, themes, effects, SOLO Lock, Save-the-Pot, Save-the-Streak.

No paid competitive advantage. Economy is configuration driven and protected from one-mode farming.

## 7. Shop and Collection
Categories supported by the data model: Avatars, Frames, Titles, Themes, Number Skins, Win Effects, Trails.

Use the approved canonical HOL character roster. At least two starter avatars are free. Purchases require preview, price, current balance, resulting balance, confirm and cancel. Show collection counters and restrained discovery dots.

## 8. Core Retention
- competitive win streak
- graceful daily streak
- 7-day login calendar
- daily and weekly missions across modes
- Save-the-Streak where appropriate
- first-session contextual FTUE
- concise returning-player re-entry

Example login cadence: 20, 30, 40, 50, 60, 80 coins, then rank-scaled day-7 chest.

Mission examples: Bank 3 SOLO runs, reach 5-run streak, win 2 DUELs, successful Lock, complete Daily Hunt, play with a friend.

## 9. Onboarding
Canonical approved flow:
1. Name
2. Gender
3. Avatar
4. Age

A branded splash may precede it but is not an onboarding step.

Gender: Boy, Girl, Prefer not to say, no default.

Age: under 13, 13–17, 18+ using approved localized strings.

Recommended welcome grant: +200 coins.

Tutorials should be contextual, not three forced slides. First SOLO teaches Higher/Lower, first Bank teaches risk/reward, first DUEL teaches hidden numbers/fair turns, first Lock explains Lock.

## 10. Safety
Protected younger-player mode should hide restricted rewarded-ad CTAs where required, avoid free-text chat and unrestricted profile text, suppress risky external social surfaces, and follow applicable consent/monetisation rules per launch market.

## 11. Settings
Name, Greek/English language, sound, music, haptics, AI difficulty, Signals mute, notifications, reduced motion where supported, stats, privacy/legal links, and double-confirmed progress reset.

## 12. Accessibility
Minimum 44px-equivalent targets, clear focus, safe-area handling, scalable layout, no color-only communication, sufficient contrast, readable Greek typography, and gameplay that does not require audio or haptics.

## 13. Localization
Greek and English at launch, centralized keys, no hardcoded gameplay strings, live switching where practical, fallback language, no baked-in artwork text. Architecture must support Polish next without UI rewrites.

## 14. Native Technical Direction
Retain Unity native client, PlayFab PvP/rooms, LevelPlay rewarded ads, telemetry, haptics, CI, integrity checks and Android preview pipelines. Do not port the Kimi PWA architecture into native. Kimi is a product-mechanics reference.

## 15. Persistence and Authority
Local persistence for settings and non-authoritative temporary state. Server authority for PvP outcome, competitive trophies, sensitive economy balances, purchases/ownership, ranked progression, room state and event rewards. All schemas need versioned migrations.

## 16. Telemetry
Track onboarding, first SOLO, every SOLO outcome/bank/Lock/save decision, DUEL opener/guesses/Lock/sudden-death/result/forfeit/rematch, Daily Hunt attempts/revive/share, economy sources/sinks, shop funnel, and D1/D3/D7/D14/D30 retention. Collect no unnecessary personal data.

## 17. Integrity
Authoritative PvP, self-join prevention, idempotent rewards, replay protection, daily clock rollback protection, no client-authoritative trophies or purchased ownership, reconnect grace and defensible timeout/forfeit handling.

## 18. Visual Authority
This document defines behavior, not a new art direction. Existing approved HOL boards remain authoritative for characters, mascots, composition, UI styling, cartoon/neon language, typography, colors, buttons and hierarchy. Production screens remain true 1080×1920 portrait screens following locked references.

# 19. WOW FACTORS — Good to Have

### Heat Number Reveal
Anticipation compression, rising haptic pulse, glowing reel, near-miss slowdown and explosive reveal. Never alter probability after player input.

### Dynamic Character Reactions
Characters react to risky banks, 3/5/7 milestones, narrow escapes, Lock success/failure, comebacks and Daily Hunt wins.

### AI Rival Personalities
Recognizable clearly-labelled bots such as aggressive, analytical, lucky and chaotic rivals.

### Clutch Moment Presentation
When DUEL reaches one or two candidates, darken background, pulse tension and emphasize the decision.

### Sudden Death Transformation
Distinct visual/audio mode shift, stronger music layer and reduced UI clutter.

### Equipable Victory Animations
Lightning, number explosion, confetti cannon, portal, crown drop and future event effects.

### Number Skins
Neon tube, arcade pixel, molten, ice, hologram and seasonal variants.

### Seasonal Theme Layer
Theme packs such as Halloween, Christmas, summer, cyber, Greek mythology and space, without rebuilding gameplay.

### Shareable Match Cards
High SOLO streak, Daily Hunt, rank-up, DUEL upset and league-promotion cards.

### Hype Meter
Purely cosmetic momentum meter that intensifies presentation, never odds.

# 20. RETENTION GOOD TO HAVES

### Streak Freeze
Earnable weekly/seasonal/rank reward protecting one missed daily streak day.

### Comeback Chest
After meaningful inactivity, complete a match to earn a modest return chest.

### Weekly Journey
Seven-node weekly path progressed by normal play, ending in a chest, no energy system required.

### Achievement Book
Long-term goals with cosmetics, coins or badges.

### Rivalries
For repeated matches with the same real player, show privacy-safe lifetime/recent record and rematch.

### Seasons
8–12 week soft-reset competitive seasons with cosmetic rewards.

### League Promotion Series
Short dramatic promotion challenge at selected trophy thresholds, used carefully.

### Daily Choice Mission
Offer three missions and let the player choose one.

### Cosmetic Shards
Transparent, duplicate-safe progress toward known cosmetics. Avoid opaque gambling mechanics.

### Friend Challenges
Asynchronous challenges such as beat my SOLO streak, Daily Hunt score or best-of-3 invitation.

### Personal Records Feed
Occasional home highlights for new best streak, biggest bank, fastest Daily Hunt and rank progress.

### Community Daily Milestone
Global participation goals with small event/cosmetic rewards for contributors.

### Event Weekends
Examples: Double XP Saturday, DUEL Weekend, Daily Hunt Festival and Lock Master Challenge.

# 21. Monetisation Principles
Prioritize rewarded ads and later cosmetic purchases/optional cosmetic season pass. Never sell stronger PvP guesses, extra PvP Locks, trophy protection, hidden-number information or ranked advantage.

# 22. Live Ops Architecture
Make rewards, XP curve, rank thresholds, mission templates, shop prices, event multipliers, Daily rewards, login calendar, cosmetics and seasonal content remotely/config driven wherever practical.

# 23. Delivery Priority

## P0 — Must Ship
- approved onboarding
- SOLO Higher/Lower risk-run core
- BANK
- DUEL fair-turn gameplay
- PlayFab PvP foundation
- AI Duel
- Lock
- Daily Hunt shared-number mode
- FRIEND private-room core
- Greek/English localization
- XP, levels, ranks
- trophies
- coins
- core stats
- basic shop
- safe-mode rules
- settings
- telemetry
- haptics
- result screens
- stable persistence

## P1 — Strong Launch Retention
- login calendar
- missions
- streak save
- Daily Hunt day-7 chest
- first-session FTUE
- share cards
- collection counters
- AI rival personalities

## P2 — WOW / Post-launch
- seasons
- achievements
- rivalries
- victory animations
- seasonal themes
- league promotion series
- community milestones
- friend challenges
- weekly journey

# 24. Product Success Metrics
Primary: onboarding completion, first SOLO completion, first Bank rate, D1/D7 retention, Daily Hunt participation, DUEL matches per active user, rematch rate, FRIEND completion, sessions/day, mode diversity, economy balance, earned-coin shop conversion and share rate.

Guardrails: forfeit/disconnect rate, Daily Hunt frustration, ad decline rate, save-flow overuse, economy inflation, PvP first-mover win rate and AI win-rate spread.

# 25. Canonical Decision
The strongest combined HOL uses:
- Kimi-style SOLO as the namesake arcade mode
- Unity native PlayFab architecture for DUEL and FRIEND
- Unity fair-turn rules plus Sudden Death only after Lock and candidate-range tiebreaks cannot separate players
- Unity shared Daily Hunt plus stronger reward progression and a 7-day chest
- Kimi-inspired progression, economy and retention layers
- approved HOL onboarding and visual authority
- Unity native infrastructure, not Kimi PWA architecture

The target is four distinct reasons to open HOL:

**SOLO for adrenaline.**

**DUEL for mastery.**

**DAILY HUNT for habit.**

**FRIEND for connection.**
