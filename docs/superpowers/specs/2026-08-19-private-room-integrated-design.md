# Private Room — Integrated Entry Design

**Date:** 2026-08-19  
**Status:** Approved (user confirmed reference + "go")

## Goal

Match the Private Room reference sheet: create and join controls live on one
portrait screen with inline secret/code fields and a gold join CTA (`JOIN!` /
`ΜΠΕΣ!`). Pre-battle panels remain for waiting only (room code + VS cards).

## Scope

- **In:** `ReplacePrivateRoomPanels` menu layout, controller panel transitions,
  `private_room_join_cta` L10n key, EditMode contract test.
- **Out:** PvP logic, CloudScript, pre-battle art, share wiring beyond existing
  copy button on create waiting.

## Layout (PvPMenuPanel)

1. Page title + HOL logo + magenta title ribbon (`private_room_title`).
2. **CreateCard** (cyan): hero art, create title, hint, secret input, gold
   create button, inline validation status.
3. **JoinCard** (magenta): door art, join title, room-code input, secret input,
   gold `private_room_join_cta` button, inline validation status.
4. Tip card, mascot 6/7, back button.

## Flow

- **Open:** menu visible; inputs editable.
- **Create confirm:** validate on menu → pre-battle create waiting (code + share).
- **Join confirm:** validate on menu → pre-battle join waiting.
- **Play:** unchanged match panel when server leaves `waiting` phase.
- **Errors:** stay on menu with status text; no navigation to empty pre-battle
  entry panels.

## Testing

- EditMode: `PrivateRoomMenuHasIntegratedEntryFields` — cards, inputs, no legacy
  navigation buttons.
- Existing pre-battle panel count test stays green.
- Stub-compile all `Assets/SCRIPT` after edits.
