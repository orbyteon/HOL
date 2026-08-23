from pathlib import Path


def require(cond, msg):
    if not cond:
        raise SystemExit(msg)


path = Path("Assets/SCRIPT/RuntimeUI/HolDuelBoardLayout.cs")
text = path.read_text(encoding="utf-8")

# Current production resource family. These are explicit screen-owned choices,
# not a global theme or runtime palette override.
anchor = '    const string BackspaceCommand = "BACKSPACE";\n'
require(anchor in text, "HolDuel constants anchor changed")
if "SoloPurpleFrameResource" not in text:
    text = text.replace(anchor, anchor + '''    const string SoloPurpleFrameResource = "mainmenu/mainmenu_tip_frame_9s";
    const string SoloBlueFrameResource = "mainmenu/mainmenu_cta_blue_9s";
    const string SoloMagentaFrameResource = "phase2a/hol_cta_magenta_r2_9s";
    const string SoloGoldFrameResource = "mainmenu/mainmenu_cta_gold_9s";

''', 1)

old_decorative = '''    static void MakeDecorative(Image image)
    {
        image.raycastTarget = false;
        image.type = Image.Type.Sliced;
        image.sprite = RuntimeUI.RoundedRectSprite;
    }
'''
require(old_decorative in text, "HolDuel MakeDecorative changed")
text = text.replace(old_decorative, '''    static void MakeDecorative(Image image, string resource)
    {
        if (image == null) return;
        RuntimeUI.ApplyProductionSprite(image, resource, Image.Type.Sliced,
            false, 2f);
        image.raycastTarget = false;
    }

    static string ResolveCardResource(Color color)
    {
        if (ColorDistance(color, CardPink) < 0.35f)
            return SoloMagentaFrameResource;
        if (ColorDistance(color, CardBlue) < 0.35f)
            return SoloBlueFrameResource;
        if (ColorDistance(color, Gold) < 0.35f)
            return SoloGoldFrameResource;
        return SoloPurpleFrameResource;
    }

    static float ColorDistance(Color a, Color b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db);
    }

    static void StyleSoloButton(Button button, string resource, Color labelColor)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        RuntimeUI.ApplyProductionSprite(image, resource, Image.Type.Sliced,
            false, 2f);
        image.raycastTarget = true;
        button.targetGraphic = image;
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = labelColor;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }
    }
''', 1)

old_card = '''        var image = card.AddComponent<Image>();
        image.color = color;
        MakeDecorative(image);
        return card;
'''
require(old_card in text, "HolDuel Card surface changed")
text = text.replace(old_card, '''        var image = card.AddComponent<Image>();
        MakeDecorative(image, ResolveCardResource(color));
        return card;
''', 1)

back_anchor = '''        var back = RuntimeUI.CreateButton(board, "DuelBack", L10n.Get("back"),
            new Vector2(-438f, 790f), new Vector2(118f, 92f), new Color(0.26f, 0.10f, 0.60f, 1f),
            NearWhite);
'''
require(back_anchor in text, "HolDuel back button block changed")
text = text.replace(back_anchor, back_anchor + '''        StyleSoloButton(back, SoloPurpleFrameResource, NearWhite);
''', 1)

old_input = '''            var image = input.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = RuntimeUI.RoundedRectSprite;
                image.type = Image.Type.Sliced;
                image.color = new Color(0.05f, 0.04f, 0.18f, 1f);
            }
'''
require(old_input in text, "HolDuel input surface changed")
text = text.replace(old_input, '''            var image = input.GetComponent<Image>();
            if (image != null)
            {
                RuntimeUI.ApplyProductionSprite(image, SoloPurpleFrameResource,
                    Image.Type.Sliced, false, 2f);
                image.raycastTarget = true;
            }
            if (input.textComponent != null)
                input.textComponent.color = NearWhite;
            var inputPlaceholder = input.placeholder as TMP_Text;
            if (inputPlaceholder != null)
                inputPlaceholder.color = Muted;
''', 1)

old_move = '''    void MoveIfFound(string name, Vector2 position, Vector2 size)
    {
        var child = FindChild(name);
        if (child == null) return;
        CenterRoot(child, size, position);
    }
'''
require(old_move in text, "HolDuel MoveIfFound changed")
text = text.replace(old_move, '''    void MoveIfFound(string name, Vector2 position, Vector2 size)
    {
        var child = FindChild(name);
        if (child == null) return;
        CenterRoot(child, size, position);
        var button = child.GetComponent<Button>();
        if (button == null) return;
        if (name == "ButtonCORRECT")
            StyleSoloButton(button, SoloGoldFrameResource,
                new Color(0.15f, 0.08f, 0.04f, 1f));
        else if (name == "ButtonLOWER")
            StyleSoloButton(button, SoloMagentaFrameResource, NearWhite);
        else
            StyleSoloButton(button, SoloBlueFrameResource, NearWhite);
    }
''', 1)

key_anchor = '''            var button = RuntimeUI.CreateButton(keypadRoot.transform, "Key_" + keys[i], label,
                new Vector2(-205f + column * 205f, 215f - row * 142f),
                new Vector2(178f, 118f), KeyBlue, NearWhite);
'''
require(key_anchor in text, "HolDuel keypad button block changed")
text = text.replace(key_anchor, key_anchor + '''            StyleSoloButton(button, SoloBlueFrameResource, NearWhite);
''', 1)

submit_center = '''        CenterRoot((RectTransform)submitControl.transform, new Vector2(660f, 112f), new Vector2(-180f, -850f));
'''
require(submit_center in text, "HolDuel submit layout changed")
text = text.replace(submit_center, submit_center + '''        StyleSoloButton(submitControl, SoloGoldFrameResource,
            new Color(0.15f, 0.08f, 0.04f, 1f));
''', 1)

path.write_text(text, encoding="utf-8")

# Strengthen current production-asset test coverage without introducing a new
# test file/meta pair.
test_path = Path("Assets/Tests/EditMode/ExactReferenceAssetsTests.cs")
test = test_path.read_text(encoding="utf-8")
if "SoloBoardProductionFramesLoad" not in test:
    marker = '    [Test]\n    public void SoloSearchPresentationUsesProductionArtWithoutRadarProceduralTypes()\n'
    require(marker in test, "Solo board test insertion marker changed")
    added = '''    [Test]
    public void SoloBoardProductionFramesLoad()
    {
        foreach (string resource in new[]
        {
            "mainmenu/mainmenu_tip_frame_9s",
            "mainmenu/mainmenu_cta_blue_9s",
            "phase2a/hol_cta_magenta_r2_9s",
            "mainmenu/mainmenu_cta_gold_9s"
        })
        {
            var sprite = Resources.Load<Sprite>(resource);
            Assert.That(sprite, Is.Not.Null, resource);
            Vector4 border = sprite.border;
            Assert.That(border.x + border.y + border.z + border.w,
                Is.GreaterThan(0f), resource + " must remain a real 9-slice.");
        }
    }

'''
    test = test.replace(marker, added + marker, 1)
test_path.write_text(test, encoding="utf-8")
