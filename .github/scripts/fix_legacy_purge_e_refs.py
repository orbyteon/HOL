from pathlib import Path

path = Path('.github/scripts/legacy_purge_e.py')
text = path.read_text(encoding='utf-8')
old = '''        text = read_text_safe(path)\n        if text and guid in text:\n            hits.append(path.as_posix())\n'''
new = '''        text = read_text_safe(path)\n        if not text:\n            continue\n        # Unity asset references are structured YAML fields (for example\n        # `guid: ...` or `m_SourceFontFileGUID: ...`). Do not treat arbitrary\n        # 32-digit sequences inside TMP atlas `_typelessdata` as GUID links.\n        structured = re.compile(\n            r'(?:\\bguid\\s*:\\s*|\\bGUID\\s*:\\s*)' + re.escape(guid) + r'\\b')\n        if any(structured.search(line) for line in text.splitlines()):\n            hits.append(path.as_posix())\n'''
if old not in text:
    raise SystemExit('legacy_purge_e external_guid_hits body changed')
path.write_text(text.replace(old, new, 1), encoding='utf-8')
