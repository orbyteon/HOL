from pathlib import Path
import os
import re
import shutil

ROOT = Path('.')


def require(cond, msg):
    if not cond:
        raise SystemExit(msg)


def read_text_safe(path: Path):
    try:
        return path.read_text(encoding='utf-8')
    except (UnicodeDecodeError, OSError):
        return None


def all_text_files():
    roots = [Path('Assets'), Path('ProjectSettings'), Path('Packages'), Path('.github'), Path('tools'), Path('docs')]
    out = []
    for base in roots:
        if not base.exists():
            continue
        for path in base.rglob('*'):
            if path.is_file() and read_text_safe(path) is not None:
                out.append(path)
    return out


TEXT_FILES = all_text_files()


def meta_guid(meta: Path):
    text = meta.read_text(encoding='utf-8')
    match = re.search(r'(?m)^guid:\s*([0-9a-f]{32})\s*$', text)
    return match.group(1) if match else None


def external_guid_hits(asset: Path, meta: Path, ignored_roots):
    guid = meta_guid(meta)
    require(guid, f'Missing GUID in {meta}')
    hits = []
    for path in TEXT_FILES:
        if path == meta:
            continue
        if any(path == root or root in path.parents for root in ignored_roots):
            continue
        text = read_text_safe(path)
        if text and guid in text:
            hits.append(path.as_posix())
    return hits


def delete_asset(asset: Path):
    meta = Path(str(asset) + '.meta')
    if asset.exists():
        asset.unlink()
    if meta.exists():
        meta.unlink()


def delete_tree_if_unreferenced(tree: Path):
    if not tree.exists():
        return
    ignored = [tree]
    refs = []
    for meta in tree.rglob('*.meta'):
        asset = Path(str(meta)[:-5])
        # Folder metas are handled after the tree itself is proven disposable.
        if asset.is_dir():
            continue
        hits = external_guid_hits(asset, meta, ignored)
        if hits:
            refs.append((asset.as_posix(), hits))
    require(not refs, 'Legacy source tree still has external GUID references:\n' + '\n'.join(
        f'  {asset} -> {hits}' for asset, hits in refs))
    shutil.rmtree(tree)
    tree_meta = Path(str(tree) + '.meta')
    if tree_meta.exists():
        tree_meta.unlink()


def delete_unreferenced_assets_in(tree: Path, protected_names=()):
    if not tree.exists():
        return
    metas = sorted(tree.rglob('*.meta'), key=lambda p: len(p.parts), reverse=True)
    for meta in metas:
        asset = Path(str(meta)[:-5])
        if asset.name in protected_names:
            continue
        if asset.is_dir():
            continue
        hits = external_guid_hits(asset, meta, [asset.parent if False else Path('__never__')])
        if not hits:
            if asset.exists():
                asset.unlink()
            meta.unlink()

    # Remove empty directories and their folder metas bottom-up.
    dirs = sorted([p for p in tree.rglob('*') if p.is_dir()], key=lambda p: len(p.parts), reverse=True)
    for directory in dirs:
        if not any(directory.iterdir()):
            directory.rmdir()
            meta = Path(str(directory) + '.meta')
            if meta.exists():
                meta.unlink()


# ---------------------------------------------------------------------------
# Delete the obsolete non-Resources "Consumer First" source pack. These files
# are not the current Unity production resource library; each tree is deleted
# only after proving none of its asset GUIDs are referenced outside the tree.
legacy_newdesign_trees = [
    Path('Assets/newdesign/ads'),
    Path('Assets/newdesign/avatars'),
    Path('Assets/newdesign/badges'),
    Path('Assets/newdesign/branding'),
    Path('Assets/newdesign/cosmetics'),
    Path('Assets/newdesign/navigation'),
    Path('Assets/newdesign/number-system'),
    Path('Assets/newdesign/results'),
    Path('Assets/newdesign/signals'),
]
for tree in legacy_newdesign_trees:
    delete_tree_if_unreferenced(tree)

legacy_top_level_assets = [
    Path('Assets/newdesign/design-tokens.json'),
    Path('Assets/newdesign/icon_lock.svg'),
    Path('Assets/newdesign/icon_reaction.svg'),
    Path('Assets/newdesign/icon_rewarded_ad.svg'),
    Path('Assets/newdesign/icon_trophy.svg'),
]
for asset in legacy_top_level_assets:
    meta = Path(str(asset) + '.meta')
    require(asset.exists() and meta.exists(), f'Expected legacy asset missing before purge: {asset}')
    hits = external_guid_hits(asset, meta, [])
    require(not hits, f'Legacy asset still referenced: {asset} -> {hits}')
    delete_asset(asset)

# Stale inventory describes surfaces already deleted in earlier phases.
for stale in [Path('Assets/newdesign/asset-inventory.md')]:
    if stale.exists():
        delete_asset(stale)


# ---------------------------------------------------------------------------
# Old original-design asset folders: remove only files whose GUID has zero
# references anywhere else in the project. Referenced app icons or any surviving
# scene dependencies remain untouched.
for tree in [Path('Assets/UI'), Path('Assets/Photos')]:
    delete_unreferenced_assets_in(tree)


# ---------------------------------------------------------------------------
# Rename the last old "Consumer" token class to what it actually is now:
# dynamic live UI state colors. It is static source, not a serialized component;
# preserve the .meta GUID while renaming the file.
old_cs = Path('Assets/SCRIPT/Design/ConsumerTokens.cs')
old_meta = Path('Assets/SCRIPT/Design/ConsumerTokens.cs.meta')
new_cs = Path('Assets/SCRIPT/Design/HolUiStateColors.cs')
new_meta = Path('Assets/SCRIPT/Design/HolUiStateColors.cs.meta')
require(old_cs.exists() and old_meta.exists(), 'ConsumerTokens source/meta missing before rename')
require(not new_cs.exists() and not new_meta.exists(), 'HolUiStateColors already exists unexpectedly')
old_cs.rename(new_cs)
old_meta.rename(new_meta)

# Replace runtime/test/doc references. Do not touch git internals or binary files.
for path in TEXT_FILES:
    if not path.exists():
        continue
    text = read_text_safe(path)
    if text is None or 'ConsumerTokens' not in text:
        continue
    text = text.replace('ConsumerTokens', 'HolUiStateColors')
    path.write_text(text, encoding='utf-8')

state = new_cs.read_text(encoding='utf-8')
state = state.replace(
    '// Compatibility colors for dynamic text/state that cannot be baked into an\n'
    '// approved sprite (status copy, live numbers, player/opponent state, etc.).\n'
    '// These values are NOT a global theme and must never be used to recolor or\n'
    '// replace approved production artwork.\n'
    'public static class HolUiStateColors',
    '// Current live UI state colors for values that cannot be baked into approved\n'
    '// artwork (status copy, numbers, player/opponent state, accessibility text).\n'
    '// This is not a theme selector and must never recolor or replace production art.\n'
    'public static class HolUiStateColors',
    1)
new_cs.write_text(state, encoding='utf-8')


# ---------------------------------------------------------------------------
# Clean current docs so no future agent is instructed by retired doctrine.
readme_path = Path('Assets/newdesign/README.md')
readme = readme_path.read_text(encoding='utf-8')
readme = re.sub(
    r'\nGeneric historical theme surfaces under `design/` are migration-only until all\n'
    r'remaining consumers have been moved to current screen-specific production art;\n'
    r'they are not an approved source for new work\.\n',
    '\n`Resources/design/` contains only the current localized PvP signal icon set.\n'
    'The retired generic background/panel/button theme surfaces have been deleted.\n',
    readme,
    count=1)
readme_path.write_text(readme, encoding='utf-8')

screen_map_path = Path('Assets/newdesign/screen-map.md')
screen_map_path.write_text('''# HOL production screen ownership map

This map documents the current one-screen/one-presentation-owner architecture.
Approved references and screen-specific production sprites remain the visual source of truth.

| Production surface | Presentation owner | Functional/state owner |
|---|---|---|
| Splash / loading | `SplashDesign` | `SplashLoader` |
| Home / mode selection | `MainMenuHomeVisuals` | `MenuManager`, runtime entry wiring |
| Solo entry / PanelPlay | `MainMenuPlayVisuals` | `MenuManager`, `FakeMatchmaking` |
| Private Room landing | `PrivateRoomVisuals` | `PvpGameController`, `PvpRuntimeUI` functional roots |
| Private Room prebattle | `PvpRuntimeUI` screen-local production helpers | `PvpGameController` |
| Solo duel board | `HolDuelBoardLayout` | `NumberManager`, `GameManager`, `DuelRules` |
| PvP duel / result / terminal | `PvpRuntimeUI` screen-local production helpers | `PvpGameController` |
| Settings | `SettingsVisuals` | `MenuManager`, localization/settings controllers |
| Daily Hunt | `DailyHuntVisuals` | `DailyHunt` |
| Solo search fallback | `SoloSearchVisuals` | `FakeMatchmaking` |
| Consent / force update | controller-local production surfaces | `ConsentManager`, `ForceUpdate` |
| Motion / feedback | additive `UIJuice/*` only | existing Button callbacks/controllers |

## Production rules

- One screen has one presentation owner; no late global recolor/reskin passes.
- Approved sprites render visibly at alpha `1`; `_9s` assets use `Image.Type.Sliced`.
- `RuntimeUI` provides neutral construction/localization/safe-area infrastructure only.
- `HolUiStateColors` is limited to dynamic text/state colors; it never selects or recolors artwork.
- All user-facing copy is localization-keyed in `L10n.cs` and validated in EN/EL.
- Final acceptance uses native-resolution captures compared with the approved reference.
''', encoding='utf-8')

# Remove stale old-concept wording in runtime comments.
pvp_path = Path('Assets/SCRIPT/RuntimeUI/PvpRuntimeUI.cs')
pvp = pvp_path.read_text(encoding='utf-8')
pvp = pvp.replace(
    '        // Match — laid out to the "HOL Consumer First" board the design tokens\n'
    '        // and the newdesign asset library describe: a duel-identity header, one\n',
    '        // Match — current production duel board: a duel-identity header, one\n',
    1)
pvp = pvp.replace('        RetireLegacyPanel(controller.pvpMenuPanel);\n', '', 1)
pvp = pvp.replace('        RetireLegacyPanel(controller.createPanel);\n', '', 1)
pvp = pvp.replace('        RetireLegacyPanel(controller.joinPanel);\n\n', '', 1)
pvp = re.sub(
    r'\n    void RetireLegacyPanel\(GameObject panel\)\n    \{\n'
    r'        if \(panel == null\) return;\n'
    r'        panel\.SetActive\(false\);\n'
    r'        panel\.name = "Retired" \+ panel\.name;\n'
    r'        RuntimeUI\.DestroyNow\(panel\);\n'
    r'    \}\n',
    '\n', pvp, count=1)
pvp_path.write_text(pvp, encoding='utf-8')

# Permanent purge guard will be updated in the follow-up user-authored commit.
