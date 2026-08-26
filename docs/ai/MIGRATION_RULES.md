# Migration Safety Rules

- Preserve behavior before changing presentation.
- Preserve callbacks before reparenting controls.
- Preserve GUIDs before moving serialized assets.
- Add direct tests before deleting a legacy path.
- Delete the old owner in the same phase that activates the new owner.
- Do not call a migration complete while both paths can execute.
- Validate EN and EL plus the required portrait viewport matrix.
