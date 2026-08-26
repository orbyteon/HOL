# HOL Cartoon UI v1 — production component and ownership audit

## Runtime ownership

| Screen | Behavior/state owner | Sole presentation owner | Approved reference |
|---|---|---|---|
| Home | `MenuManager`, `PvpGameController`, `DailyHunt` entry wiring | `MainMenuHomeVisuals` | `01-home-approved.png` |
| Private Room | `PvpGameController`, `PvpRuntimeUI` room controls | `PrivateRoomVisuals` | `02-private-room-approved.png` |
| Solo opponent preparation | `FakeMatchmaking` | `SoloSearchVisuals` | `03-opponent-search-approved.png` |
| Solo duel board | `GameManager`, `NumberManager`, `DuelRules` | `HolDuelBoardLayout` | `04-duel-gameplay-approved.png` |
| Solo result | `GameManager`, `SoloBoardPresentationState` | `HolDuelBoardLayout` result state | `05-results-approved.png` |
| Daily Hunt | `DailyHunt` | `DailyHuntVisuals` | `06-daily-hunt-approved.png` |

`CartoonUiKit` is a resource/color contract only. It creates no hierarchy, owns no screen, performs no `Update`/`LateUpdate` writes and is not another fidelity pass.

## Shared production kit

| Component | Production source | Status |
|---|---|---|
| Neon arena background | `cartoonui/v1/home/hol_home_background_v1` | Approved raster production asset; reused by the five current owners |
| HOL logo | `reference/hol_logo_exact` | Reused |
| Player, friend and opponent characters | `reference/player_cyan_exact`, `reference/char_girl_exact`, `reference/opponent_purple_exact` plus approved screen-card sprites | Reused |
| Number mascots | `reference/mascot_3_exact`, `reference/mascot_6_exact`, `reference/mascot_7_exact` | Reused |
| Player chip | `dailyhunt/production/daily_player_chip_shell_v3` | Reused |
| Gold/cyan/magenta CTA surfaces | `phase2a/hol_cta_*_r2_9s` and screen-specific `dailyhunt/v1` action sprites | Reused |
| Outer screen border | `dailyhunt/v1/daily_outer_frame_v1` | Approved raster production asset; consumed through `CartoonUiKit.ScreenFrame` |
| Curved title ribbon | `cartoonui/v1/shared/hol_title_ribbon_v1_raster` | Faithful raster production asset; consumed through `CartoonUiKit.TitleRibbon` |
| Daily reward chest | `cartoonui/v1/shared/hol_reward_chest_v1_raster` | Faithful raster production asset; consumed through `CartoonUiKit.RewardChest` |
| Radar | `cartoonui/v1/raster/hol_radar_base_v1`, `cartoonui/v1/raster/hol_radar_sweep_v1` | Reused |
| VS and trophy symbols | `cartoonui/v1/raster/hol_vs_burst_v1`, `cartoonui/v1/raster/hol_trophy_v1` | Reused |
| Display/body typography | `phase2a/fonts/HOL Menu Display SDF`, `phase2a/fonts/HOL Menu Body SDF` | Reused; live TMP |

## Deliberate semantic differences from concept screenshots

- Opponent preparation remains truthful local Solo-vs-AI preparation. It must not claim real network matchmaking.
- Room codes, names, scores, streaks, mission progress, timers and result details remain live data; screenshot values are not copied.
- Daily Hunt keeps its real date-seeded number challenge and existing persistence/callbacks. The reference guides composition, not invented mission/reward rules.
- Solo results report the real win/loss/draw detail available from `SoloBoardPresentationState`; the concept's illustrative scores and trophy increments are not fabricated.

## Verification gate

The current local handoff was imported and exercised with Unity 2022.3.62f3.
The five-screen runtime matrix contains 30 exact-resolution EN/EL captures with
no runtime exceptions. `SoloBoardPresenterPlayModeTests` passes 5/5. The full
local EditMode run is 177/177 after updating two stale Main Menu assertions to
the approved current Home background/copy contract. The screenshot-based
Private Room PlayMode test is not batch-safe because it waits on
`WaitForEndOfFrame`; the standalone runtime capture is the evidence for that
screen's rendered output. A lifecycle-only readiness patch was then applied to
`MainMenuHomeVisuals`; its focused PlayMode rerun is pending because the local
Unity batch license channel currently aborts before tests start (return code
199), not because of a test assertion. Existing exact visual geometry assertions
remain provisional and have not been rebaselined against the five-screen matrix.
