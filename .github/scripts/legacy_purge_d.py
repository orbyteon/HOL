from pathlib import Path


def require(cond, msg):
    if not cond:
        raise SystemExit(msg)


path = Path("Assets/SCRIPT/RuntimeUI/ExtrasRuntimeWiring.cs")
text = path.read_text(encoding="utf-8")

# Remove retired-theme wording and the hidden duplicate stats-label presentation.
text = text.replace(
    "    // Converging Light palette (design/philosophy.md).\n",
    "    // Functional fallback colors only; current screen owners assign production sprites.\n",
    1,
)
text = text.replace("    TMP_Text statsLabel;\n", "", 1)
text = text.replace("        AddStatsLabel();\n", "", 1)
text = text.replace("        L10n.OnLanguageChanged -= RefreshStats;\n", "", 1)
text = text.replace("        GameEvents.OnMatchEnded -= OnMatchEnded;\n", "", 1)

start = text.find("    // --- 4. Stats label")
end = text.find("    // --- 5. Simulated-opponent disclosure", start)
require(start >= 0 and end > start, "ExtrasRuntimeWiring hidden stats section markers changed")
text = text[:start] + text[end:]

# Every runtime-created control gets current production art immediately, so the
# neutral RuntimeUI fallback can never flash on a live screen before its owner
# performs final seating/layout.
replacements = {
'''        var button = RuntimeUI.CreateButton(menu.settingsPanel.transform,
            "AdsPrivacyButton", L10n.Get("ads_privacy"),
            new Vector2(0f, -680f), new Vector2(360f, 80f), Neutral);
''': '''        var button = RuntimeUI.CreateButton(menu.settingsPanel.transform,
            "AdsPrivacyButton", L10n.Get("ads_privacy"),
            new Vector2(0f, -680f), new Vector2(360f, 80f), Neutral);
        ApplyWiringSprite(button, "mainmenu/mainmenu_cta_blue_9s");
''',
'''            var button = RuntimeUI.CreateButton(menu.settingsPanel.transform,
                "Difficulty" + i, L10n.Get(keys[i]),
                new Vector2(-300f + i * 200f, -860f), new Vector2(180f, 70f), Neutral);
''': '''            var button = RuntimeUI.CreateButton(menu.settingsPanel.transform,
                "Difficulty" + i, L10n.Get(keys[i]),
                new Vector2(-300f + i * 200f, -860f), new Vector2(180f, 70f), Neutral);
            ApplyWiringSprite(button, "mainmenu/mainmenu_tip_frame_9s");
''',
'''        var cancel = RuntimeUI.CreateButton(mm.searchingPanel.transform,
            "CancelButton", L10n.Get("cancel"),
            new Vector2(0f, -420f), new Vector2(300f, 80f), Neutral);
''': '''        var cancel = RuntimeUI.CreateButton(mm.searchingPanel.transform,
            "CancelButton", L10n.Get("cancel"),
            new Vector2(0f, -420f), new Vector2(300f, 80f), Neutral);
        ApplyWiringSprite(cancel, "mainmenu/mainmenu_cta_blue_9s");
''',
'''        englishButton = RuntimeUI.CreateButton(menu.settingsPanel.transform,
            "EnglishButton", L10n.Get("language_english"),
            new Vector2(-130f, -560f), new Vector2(220f, 80f), Neutral);
''': '''        englishButton = RuntimeUI.CreateButton(menu.settingsPanel.transform,
            "EnglishButton", L10n.Get("language_english"),
            new Vector2(-130f, -560f), new Vector2(220f, 80f), Neutral);
        ApplyWiringSprite(englishButton, "mainmenu/mainmenu_tip_frame_9s");
''',
'''        greekButton = RuntimeUI.CreateButton(menu.settingsPanel.transform,
            "GreekButton", L10n.Get("language_greek"),
            new Vector2(130f, -560f), new Vector2(220f, 80f), Neutral);
''': '''        greekButton = RuntimeUI.CreateButton(menu.settingsPanel.transform,
            "GreekButton", L10n.Get("language_greek"),
            new Vector2(130f, -560f), new Vector2(220f, 80f), Neutral);
        ApplyWiringSprite(greekButton, "mainmenu/mainmenu_tip_frame_9s");
''',
}
for old, new in replacements.items():
    require(old in text, "ExtrasRuntimeWiring expected control creation block changed")
    text = text.replace(old, new, 1)

old_tint = '''    static void TintSelectable(Button button, bool selected)
    {
        if (button == null)
            return;

        var image = button.GetComponent<Image>();
        if (image != null)
            image.color = selected ? Gold : Neutral;

        var label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.color = selected ? DarkLabel : new Color(0.91f, 0.93f, 1f);
    }
'''
require(old_tint in text, "ExtrasRuntimeWiring TintSelectable changed")
text = text.replace(old_tint, '''    static void TintSelectable(Button button, bool selected)
    {
        if (button == null) return;
        ApplyWiringSprite(button, selected
            ? "mainmenu/mainmenu_cta_gold_9s"
            : "mainmenu/mainmenu_tip_frame_9s");

        var label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.color = selected ? DarkLabel : new Color(0.91f, 0.93f, 1f);
    }

    static void ApplyWiringSprite(Button button, string resource)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image == null) return;
        RuntimeUI.ApplyProductionSprite(image, resource, Image.Type.Sliced,
            false, 2f);
        image.raycastTarget = true;
        button.targetGraphic = image;
    }
''', 1)

# Gold constant is no longer used to repaint images after the migration.
text = text.replace("    static readonly Color Gold = ConsumerTokens.Gold;\n\n", "", 1)
path.write_text(text, encoding="utf-8")
