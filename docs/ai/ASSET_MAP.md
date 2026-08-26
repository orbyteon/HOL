# HOL Production Asset Map

Approved runtime artwork lives under `Assets/newdesign/Resources/`, grouped by
screen/family. The exact approved source for an element is determined by its
current screen owner and reference contract, not by visual similarity.

| Family | Intended use |
|---|---|
| `reference/` | exact approved logo, characters, mascots and board symbols |
| `phase2a/` | current menu/arena frames, backgrounds, characters and fonts |
| `mainmenu/` | modular Home/menu frames, icons and decoration |
| `settings/` | approved Settings background and icons |
| `splash/` | approved Splash composition artwork |
| `cartoon/` | approved modular cartoon support art |
| `design/` | fixed PvP Signal symbols loaded by index |

Rules:

- Required artwork is referenced by the screen owner and must remain visible at
  normal alpha.
- `_9s` assets use authored borders and `Image.Type.Sliced`.
- Missing required artwork disables the affected image and fails validation; a
  procedural placeholder must not become the production look.
- Do not add `Old`, `Legacy`, backup or `_to_delete` asset graveyards. Git history
  is the backup.
- Update this map when a new approved production family is introduced.
