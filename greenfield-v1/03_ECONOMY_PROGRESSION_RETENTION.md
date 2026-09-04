# HOL V1 Economy, Progression and Retention

## 1. Currencies
### Coins
Earned currency for cosmetics and selected recovery mechanics.

### XP
Progression currency only, never spendable.

### Trophies
Competitive rating surface, never purchasable.

## 2. Levels and ranks
Levels 1 to 50.

Initial XP requirement target:
`60 + (level - 1) * 40`

Ranks:
- Rookie: 1 to 5
- Player: 6 to 10
- Pro: 11 to 20
- Master: 21 to 35
- Legend: 36 to 50

All values are remote-configurable.

## 3. Recommended unlock flow
- Level 1: SOLO
- Level 2: AI DUEL
- Level 3: Online DUEL
- Daily Hunt: available after first completed session
- FRIEND: available immediately through valid invite, otherwise surfaced early
- Lock: introduced after player has completed enough DUEL turns to understand basic rules
- Shield: introduced after player has completed enough SOLO runs to understand risk and BANK

Do not block a referred friend from joining a valid challenge because of account level.

## 4. Trophy targets
Initial balancing range:
- win: +24 to +32
- draw: 0
- loss: -10 to -16
- floor: 0

Later migration can use hidden MMR plus visible seasonal trophies.

## 5. Coin faucets
- banked SOLO runs
- DUEL completion and wins
- Daily Hunt
- missions
- login calendar
- level and rank rewards
- achievements later
- selected rewarded ads
- event participation

## 6. Coin sinks
- avatars
- frames
- titles
- number skins
- trails
- victory effects
- themes
- Shield
- Save the Pot
- Save the Streak

## 7. Economy guardrails
- no paid competitive advantage
- server authority for material balances and ownership
- reward idempotency
- daily and event caps where needed
- no mode should dominate total coin generation
- recovery cost should scale with expected saved value
- economy tuning must not require a client release

## 8. Shop
Data model supports:
- avatars
- frames
- titles
- number skins
- trails
- win effects
- themes

Purchase flow:
1. preview
2. price
3. current balance
4. post-purchase balance or deficit
5. confirm
6. success feedback
7. equip option

At least two starter avatars are free.

## 9. Session retention
Drivers:
- current SOLO pot
- next streak milestone
- BANK decision
- DUEL rematch
- rival rematch
- personal record proximity

## 10. Daily retention
- Daily Hunt
- daily mission
- login reward
- daily streak
- one relevant home reminder, not popup spam

## 11. Weekly retention
- weekly missions
- weekly journey after V1
- event weekend
- friend challenge

## 12. Long-term retention
- ranks
- trophies
- collection completion
- achievements
- seasons
- rivalries
- prestige cosmetics

## 13. Login calendar
Initial target:
- Day 1: 20 coins
- Day 2: 30
- Day 3: 40
- Day 4: 50
- Day 5: 60
- Day 6: 80
- Day 7: rank-scaled chest

## 14. Missions
Daily examples:
- Bank 3 SOLO runs
- Reach a 5-run SOLO streak
- Win 2 DUELs
- Use Lock successfully
- Complete Daily Hunt
- Play with a friend

Weekly examples:
- Bank 20 runs
- Complete 5 Daily Hunts
- Win 10 DUELs
- Use 3 different game modes

Prefer choice and variety over forcing one unpopular mode.

## 15. Recovery rules
At most one recovery decision should appear at a meaningful failure moment.

Never chain:
loss -> Save Pot -> Save Streak -> ad -> purchase.

Recovery mechanics must preserve trust.

## 16. Good-to-have retention backlog
- Streak Freeze
- Comeback Chest earned through play
- Achievement Book
- Rivalries
- 8 to 12 week seasons
- promotion series
- daily choice mission
- friend challenges
- personal record cards
- community milestones
- event weekends

## 17. Economy simulation requirement
Before soft launch, run Monte Carlo or deterministic simulation for:
- average coins per active day
- sink consumption by player age
- expected time to cosmetic purchase
- high-skill SOLO inflation
- ad-assisted recovery value
- trophy distribution
- rank progression speed

Economy sign-off requires target ranges, not intuition alone.
