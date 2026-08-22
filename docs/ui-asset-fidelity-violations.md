# HOL UI Asset Fidelity — Deferred Violation Ledger

This ledger records out-of-scope findings under the mandatory repository-wide
UI asset fidelity contract in `AGENTS.md`. Entries here are not authorization to
change another screen. Each item requires its own controlled visual phase and a
side-by-side native-resolution review.

## Current Settings phase

- Settings production buttons and the player chip are repaired in the current
  phase to render their approved sprites directly at normal-state alpha `1`.
- The music toggle and the small streak flame remain procedural because no
  approved, semantically matching production sprite has been identified for
  either element. This is allowed by rule 12 and should be revisited only when
  approved source artwork is supplied.
- The Settings shell, row panels, and title panel remain procedural because no
  separated approved production sprites exist for those exact elements. Their
  replacement is outside the current button-fidelity repair.

## Deferred controlled audits

- `Assets/SCRIPT/Design/MainMenuHomeVisuals.cs` hides approved Main Menu gear,
  CTA frame, player-chip, and avatar sprites at alpha `0.002f` and renders
  procedural replacements. The Main Menu is outside the current Settings phase
  and must receive a dedicated fidelity repair with native-resolution visual
  comparison.
- `Assets/SCRIPT/Design/AttachmentReskinVisuals.cs`,
  `Assets/SCRIPT/Design/AttachmentReskinCanvasBindings.cs`,
  `Assets/SCRIPT/Design/ExactReferenceVisuals.cs`,
  `Assets/SCRIPT/RuntimeUI/HolDuelBoardLayout.cs`, and
  `Assets/SCRIPT/RuntimeUI/PvpRuntimeUI.cs` contain runtime/procedural panel or
  button construction. These are audit candidates, not automatically confirmed
  violations: each future screen phase must first map approved assets to the
  exact production controls before changing them.
