# HOL V1 Game Design and Rules

## 1. SOLO
### Goal
Create a fast Higher or Lower risk loop with tension between continuing and banking.

### Flow
1. Generate starting number inside the player's rank range.
2. Player chooses Higher or Lower.
3. Generate next number using the canonical RNG service.
4. Resolve result.
5. On correct, add to run, pot and XP preview.
6. Player chooses Continue or BANK.
7. On wrong, resolve Shield or Save the Pot if eligible, otherwise end run and lose unbanked pot.

### Tie rule
If the next number equals the current number, the default V1 behavior is a push, not a loss. No pot change, no streak increase, roll again. This must be shown clearly to the player.

### Rank ranges
- Rookie: 1 to 50
- Player: 1 to 75
- Pro: 1 to 100
- Master: 1 to 150
- Legend: 1 to 200

### Run milestones
- 3 correct: bonus event
- 5 correct: MEGA event
- 7 correct: CHEST event
- beyond 7: escalating prestige and record tracking

### BANK
- available after first correct guess
- transfers entire unbanked pot to wallet
- ends current run cleanly
- leaving active SOLO auto-banks unless a future high-risk mode explicitly overrides this

### Shield
- account unlock after player understands the base loop
- one per run
- optional to arm or automatically offered before a risky continuation, exact UX to test
- absorbs one wrong outcome
- does not affect underlying RNG

### Save the Pot
- eligible from meaningful run threshold, initial target 3+
- one recovery decision maximum at the loss moment
- possible payment: coins or rewarded ad, based on age and monetization eligibility
- never stack multiple recovery prompts

## 2. DUEL
### Goal
Outguess the opponent's secret number while managing information and one strategic Lock.

### Setup
- each player selects a secret from 1 to 100
- server validates secret range and readiness
- opener is decided fairly
- both players begin with full 1 to 100 search interval

### Round fairness
Each round gives one action slot to each side. If opener finds the number, responder still receives the answering slot for that round.

### Hint resolution
For each valid guess:
- guess below secret: Higher
- guess above secret: Lower
- guess equals secret: Correct

### Lock
- one Lock per player per match after account unlock
- correct locked hit outranks an unlocked correct hit in the same round
- wrong Lock forfeits the player's next action slot
- Lock usage is server authoritative

### Double-hit resolution
If both players find the number in the same round:
1. locked correct beats unlocked correct
2. otherwise smaller remaining candidate range wins
3. otherwise enter Sudden Death

### Sudden Death
- clear visual state change
- continue rounds until an outright winner
- hard cap prevents pathological duration
- at cap, fewest valid guesses wins
- if still equal, draw

### Forfeit
Forfeit is a real loss. Update stats, streak and trophies exactly like a loss, except no opponent-action telemetry is fabricated.

### Disconnect
- short reconnect grace period
- server retains authoritative room state
- after grace period, unresolved disconnect can become forfeit
- exact grace duration is remote config

### Rematch
- AI rematch is immediate
- real PvP rematch requires both players
- same room may be reused if backend contract supports clean state reset

## 3. AI Duel
AI must use the same visible rules as human DUEL.

Difficulty targets:
- Easy: imperfect narrowing and occasional non-optimal guesses
- Normal: competent but beatable
- Hard: near-optimal search
- Adaptive: rank and recent-performance based

After 3+ consecutive losses, Adaptive may soften temporarily. Never claim the AI is a real person.

## 4. DAILY HUNT
### Goal
Find the common daily secret in 7 guesses.

Rules:
- range 1 to 100
- one challenge per UTC day contract
- same daily secret for all players in the same production environment
- 7 base guesses
- every miss narrows visible range
- one rewarded +2 guess revive where allowed
- state persists after each guess
- secret is never shown in share content before reset

### Daily streak
Daily Hunt has its own streak. It must not destroy the competitive DUEL streak.

### Day 7 track
Complete the daily ritual across the track to unlock a larger chest. Miss handling should be forgiving, with any Streak Freeze feature handled by retention rules.

## 5. FRIEND
- create room
- receive short code and deep link where supported
- join room
- reject self-join
- 10-minute pre-match expiry target
- ready state
- reconnect handling
- play canonical DUEL rules

Friend invite deep links can bypass normal progression gating for FRIEND access.

## 6. Signals
Fixed indexed signals only in V1. Localize on recipient client. No free text.

Requirements:
- rate limited
- per-match mute
- persistent mute
- safe for all supported ages

## 7. Determinism and testability
Core game rules must live in pure deterministic domain modules without Unity scene dependencies. RNG calls must be injectable for tests. Every terminal outcome must be reproducible from a test fixture.
