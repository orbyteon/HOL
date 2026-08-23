from pathlib import Path

path = Path('.github/scripts/legacy_purge_e.py')
text = path.read_text(encoding='utf-8')
old = '''        text = read_text_safe(path)\n        if text and guid in text:\n            hits.append(path.as_posix())\n'''
new = '''        text = read_text_safe(path)\n        if not text:\n            continue\n        # Unity asset references are structured YAML fields (for example\n        # `guid: ...` or `m_SourceFontFileGUID: ...`). Do not treat arbitrary\n        # 32-digit sequences inside TMP atlas `_typelessdata` as GUID links.\n        structured = re.compile(\n            r'(?:\\bguid\\s*:\\s*|\\bGUID\\s*:\\s*)' + re.escape(guid) + r'\\b')\n        if any(structured.search(line) for line in text.splitlines()):\n            hits.append(path.as_posix())\n'''
if old not in text:
    raise SystemExit('legacy_purge_e external_guid_hits body changed')
text = text.replace(old, new, 1)

# Branding contains the current Android app icon referenced by ProjectSettings.
# Purge only zero-reference siblings in that folder instead of deleting the tree.
old_branding = '''    Path('Assets/newdesign/badges'),\n    Path('Assets/newdesign/branding'),\n    Path('Assets/newdesign/cosmetics'),\n'''
new_branding = '''    Path('Assets/newdesign/badges'),\n    Path('Assets/newdesign/cosmetics'),\n'''
if old_branding not in text:
    raise SystemExit('legacy_newdesign_trees branding entry changed')
text = text.replace(old_branding, new_branding, 1)
loop = '''for tree in legacy_newdesign_trees:\n    delete_tree_if_unreferenced(tree)\n\n'''
if loop not in text:
    raise SystemExit('legacy_newdesign_trees loop changed')
text = text.replace(loop, loop + '''# Keep only actually referenced branding assets (currently the app icon).\ndelete_unreferenced_assets_in(Path('Assets/newdesign/branding'))\n\n''', 1)

path.write_text(text, encoding='utf-8')
