# HOL V1 UX, Visual and Content System

## 1. Visual authority
The previously approved HOL cartoon visual system remains the visual baseline. This greenfield project must not invent a new identity unless explicitly approved.

Authority covers:
- canonical characters and mascots
- neon cartoon styling
- typography feel
- button language
- card and panel language
- color treatment
- icon style
- composition patterns
- 1080 x 1920 portrait screen target

## 2. UX principles
- one dominant action per state
- important numbers and choices read instantly
- minimum interaction target equivalent to 44 px
- no duplicated navigation
- no hidden critical state
- no modal stacking
- animations explain state, not just decorate
- Greek and English must both fit naturally

## 3. Canonical screen map
### First launch
1. branded splash
2. Name
3. Gender
4. Avatar
5. Age
6. first SOLO FTUE
7. Home

### Home
- player identity
- level and rank
- coins
- XP progress
- primary mode cards
- missions shortcut
- shop
- profile
- rank/leaderboard
- settings

### SOLO
- current number
- Higher
- Lower
- run length
- pot
- BANK
- Shield state
- milestone state
- result / recovery state

### DUEL
- VS intro
- secret selection
- coin-flip opener
- player and opponent cards
- current range
- guess input
- history rail
- Lock
- signals
- match point
- sudden death
- result
- rematch

### Daily Hunt
- daily intro/dashboard
- current range
- guess input
- attempts remaining
- trail
- streak
- revive state
- result/share

### Friend
- create
- join
- waiting room
- invite/deep link
- connected state
- duel
- rematch

### Meta
- shop
- collection
- profile
- stats
- settings
- missions
- login reward

## 4. Moment design
Treat these as designed moments with unique motion, sound and haptics:
- first BANK
- 3, 5 and 7 SOLO milestones
- Shield save
- Lock arm
- Lock success
- Lock miss
- match point
- sudden death
- rank up
- Daily Hunt success
- new personal record

## 5. WOW priorities
P1 wow features:
- high-tension number reveal
- dynamic character reactions
- match-point visual simplification
- sudden-death transformation
- shareable result cards

P2 wow features:
- equipable victory animations
- number skins
- seasonal theme packs
- cosmetic Hype Meter

## 6. Content voice
Tone:
- energetic
- playful
- concise
- competitive without hostility
- positive around failure

Avoid:
- shame-based streak language
- fake urgency
- fabricated human behavior from bots
- long instructional copy during play

## 7. Localization rules
- all UI text uses centralized keys
- no text baked into production artwork
- Greek is written natively, not literal machine-style translation
- layouts are tested at realistic long-string cases
- numbers and currency surfaces remain language-safe

## 8. Accessibility
- sufficient contrast
- no color-only state communication
- reduced motion option
- audio and haptics optional
- safe area support
- readable text at supported phone sizes
- focus/accessibility labels where platform requires

## 9. Visual QA process
Every production screen requires:
1. approved reference or spec
2. clean 1080 x 1920 capture
3. English QA
4. Greek QA
5. interaction-state QA
6. small-device QA
7. safe-area QA
8. reduced-motion check if animated

No screen is accepted because it is merely similar to a reference. Production acceptance is based on side-by-side fidelity plus functional clarity.
