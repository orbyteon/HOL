# HOL New Design

Unity-ready design foundation for the consumer-first HOL UI.

## Direction

Friendly social game with a mature, polished presentation:

- deep indigo/plum depth
- cyan-blue secondary actions
- warm gold primary actions
- restrained magenta opponent state
- age-neutral illustrated avatars
- witty microcopy
- no ads during active gameplay
- rewarded ads always disclose the reward first

## Integration targets

- `Assets/SCRIPT/RuntimeUI`
- `Assets/SCRIPT/Design`
- `Assets/SCRIPT/UIJuice`
- `Assets/SCRIPT/Localization`
- `Assets/SCRIPT/AdsManager.cs`
- `Assets/Scenes/MainMenu.unity`

## Asset set

- `design-tokens.json`: palette, spacing, typography, states and monetization rules
- SVG surfaces: background, panel, primary/secondary buttons
- SVG icons: lock, trophy, reaction, rewarded ad

These are source assets for the runtime-built UI. Keep all user-facing copy localized through `L10n.Get` and preserve the existing Converging Light palette contract.
