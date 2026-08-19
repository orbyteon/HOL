using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Final reference-board polish for the presentation-only reskin. This layer
// never creates a Button or changes a controller callback. It only replaces
// temporary glyphs with dedicated artwork and makes the board reskin the sole
// visual owner once MainMenu has finished building.
[DefaultExecutionOrder(1200)]
public sealed class AttachmentReskinPolish : MonoBehaviour
{
    const string VsBurstResource = "reference/board_vs_burst_exact";
    const string TrophyResource = "reference/board_trophy_exact";
    const string RocketResource = "reference/board_rocket_exact";
    const string FriendResource = "reference/board_friend_exact";
    const string LightningResource = "reference/board_lightning_exact";
    const string PlusResource = "reference/board_plus_exact";
    const string JoinResource = "reference/board_join_exact";

    static readonly Color Panel = Hex(0x14, 0x0A, 0x43);
    static readonly Color Purple = Hex(0x72, 0x27, 0xD8);
    static readonly Color White = Hex(0xFA, 0xF7, 0xFF);

    Sprite vsBurst;
    Sprite trophy;
    Sprite rocket;
    Sprite friendIcon;
    Sprite lightningIcon;
    Sprite plusIcon;
    Sprite joinIcon;
    float nextPass;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;

        Canvas canvas = null;
        var menu = FindInScene<MenuManager>(scene);
        if (menu != null && menu.mainMenuPanel != null)
            canvas = menu.mainMenuPanel.GetComponentInParent<Canvas>();

        if (!OwnedCanvas(canvas, scene))
        {
            canvas = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var candidate in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (!OwnedCanvas(candidate, scene)) continue;
                    canvas = candidate;
                    break;
                }
                if (canvas != null) break;
            }
        }

        if (canvas != null && canvas.GetComponent<AttachmentReskinPolish>() == null)
            canvas.gameObject.AddComponent<AttachmentReskinPolish>();
    }

    static bool OwnedCanvas(Canvas canvas, Scene scene)
    {
        return canvas != null && canvas.gameObject.scene == scene &&
               canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace;
    }

    void Awake()
    {
        vsBurst = Resources.Load<Sprite>(VsBurstResource);
        trophy = Resources.Load<Sprite>(TrophyResource);
        rocket = Resources.Load<Sprite>(RocketResource);
        friendIcon = Resources.Load<Sprite>(FriendResource);
        lightningIcon = Resources.Load<Sprite>(LightningResource);
        plusIcon = Resources.Load<Sprite>(PlusResource);
        joinIcon = Resources.Load<Sprite>(JoinResource);
    }

    void OnEnable()
    {
        L10n.OnLanguageChanged += ApplyPolish;
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= ApplyPolish;
    }

    IEnumerator Start()
    {
        for (int i = 0; i < 6; i++)
            yield return null;

        var menu = FindInScene<MenuManager>(gameObject.scene);
        if (menu == null) yield break;

        // ExactReferenceVisuals remains the Splash bootstrap/fallback, but the
        // board reskin must be the only writer of MainMenu layout and colors.
        var baseline = GetComponent<ExactReferenceVisuals>();
        if (baseline != null) baseline.enabled = false;

        ApplyPolish();
    }

    void LateUpdate()
    {
        if (Time.unscaledTime < nextPass) return;
        nextPass = Time.unscaledTime + 0.25f;
        ApplyPolish();
    }

    void ApplyPolish()
    {
        var menu = FindInScene<MenuManager>(gameObject.scene);
        if (menu == null) return;

        StyleRuntimeCards(transform);
        PolishHome(menu);
        PolishSearching(menu);
        PolishSoloResult();
        PolishPvp();
    }

    void PolishHome(MenuManager menu)
    {
        if (menu.mainMenuPanel == null) return;
        var root = menu.mainMenuPanel.transform;

        // Remove duplicate decorations from the earlier exact pass, not any
        // controller-owned object.
        SetActive(DeepFind(root, "ExactHOLLogo"), false);
        SetActive(DeepFind(root, "ExactTagline"), false);
        SetActive(DeepFind(root, "ExactPlayerHero"), false);
        SetActive(DeepFind(root, "ExactOpponentHero"), false);
        SetActive(DeepFind(root, "ExactPlayerChip"), false);
        SetActive(DeepFind(root, "ExactDailyMascot"), false);

        // Keep the hidden baseline chip copy localized as well, because older
        // regression coverage reads it even though the board chip is visible.
        var exactChipText = DeepFind(root, "ExactPlayerChipText");
        if (exactChipText != null)
        {
            var text = exactChipText.GetComponent<TMP_Text>();
            if (text != null)
            {
                string playerName = PlayerPrefs.GetString("PlayerName", L10n.Get("player_default"));
                text.text = playerName.ToUpperInvariant() + "   " +
                            L10n.Get("stats_streak").ToUpperInvariant() + " " + GameStats.CurrentStreak;
            }
        }

        ReplaceGlyphWithSprite(root, "ButtonPvP", "BoardFriendIcon", "BoardFriendVector",
            friendIcon, new Vector2(-155f, 0f), new Vector2(100f, 80f));
        ReplaceGlyphWithSprite(root, "DailyHuntButton", "BoardDailyIcon", "BoardDailyVector",
            lightningIcon, new Vector2(-155f, 0f), new Vector2(82f, 92f));
    }

    void PolishSearching(MenuManager menu)
    {
        if (menu.panelSearching == null || !menu.panelSearching.activeInHierarchy) return;
        var root = menu.panelSearching.transform;
        if (DeepFind(root, "SoloSearchVisualRoot") != null)
        {
            SetActive(DeepFind(root, "BoardSearchRocketVector"), false);
            SetActive(DeepFind(root, "BoardVsBurstVector"), false);
            return;
        }
        AddImage(root, "BoardSearchRocketVector", rocket,
            new Vector2(350f, -500f), new Vector2(245f, 300f), true);
        EnsureVsBurst(root);
    }

    void PolishSoloResult()
    {
        var game = FindInScene<GameManager>(gameObject.scene);
        if (game == null || game.stopGameButton == null) return;
        var root = game.stopGameButton.transform.parent;
        if (root == null) return;

        EnsureVsBurst(root);
        if (!game.IsMatchOver) return;

        var oldTrophy = DeepFind(root, "BoardSoloTrophy");
        SetActive(oldTrophy, false);

        bool won = game.turnText != null && game.turnText.text.StartsWith(L10n.Get("you_win"));
        if (won)
            AddImage(root, "BoardSoloTrophyVector", trophy,
                new Vector2(-85f, 270f), new Vector2(190f, 190f), true);
        else
            SetActive(DeepFind(root, "BoardSoloTrophyVector"), false);

        // The existing solo result button exits to MainMenu. Its reference-board
        // treatment must describe that real action, not pretend to be Rematch.
        var exit = game.stopGameButton.GetComponent<Button>();
        if (exit != null)
        {
            var label = MainButtonLabel(exit);
            if (label != null)
            {
                label.text = L10n.Get("back").ToUpperInvariant();
                label.fontSize = 38f;
                label.fontStyle = FontStyles.Bold;
                label.color = White;
            }
        }
    }

    void PolishPvp()
    {
        var pvp = FindInScene<PvpGameController>(gameObject.scene);
        if (pvp == null) return;

        if (pvp.pvpMenuPanel != null && pvp.pvpMenuPanel.activeInHierarchy)
        {
            var root = pvp.pvpMenuPanel.transform;
            if (DeepFind(root, "TitleRibbon") == null)
            {
                ReplaceGlyphWithSprite(root, "CreateButton", "BoardCreatePlus", "BoardCreatePlusVector",
                    plusIcon, new Vector2(0f, 225f), new Vector2(88f, 88f));
                ReplaceGlyphWithSprite(root, "JoinButton", "BoardJoinDoor", "BoardJoinDoorVector",
                    joinIcon, new Vector2(0f, 225f), new Vector2(90f, 90f));
                SetActive(DeepFind(root, "BoardCreatePlusPlate"), false);
                SetActive(DeepFind(root, "BoardJoinDoorPlate"), false);
            }
        }

        if (pvp.matchPanel == null || !pvp.matchPanel.activeInHierarchy) return;
        var matchRoot = pvp.matchPanel.transform;
        var approvedResult = DeepFind(matchRoot, "ResultVisualRoot");
        if (approvedResult != null && approvedResult.gameObject.activeInHierarchy)
        {
            SetActive(DeepFind(matchRoot, "BoardPvpTrophyVector"), false);
            approvedResult.SetAsLastSibling();
            return;
        }
        EnsureVsBurst(matchRoot);

        bool result = pvp.resultText != null && !string.IsNullOrEmpty(pvp.resultText.text);
        if (!result) return;

        bool won = pvp.resultText.text.StartsWith(L10n.Get("you_win"));
        if (won)
            AddImage(matchRoot, "BoardPvpTrophyVector", trophy,
                new Vector2(-92f, 268f), new Vector2(185f, 185f), true);
        else
            SetActive(DeepFind(matchRoot, "BoardPvpTrophyVector"), false);
    }

    void EnsureVsBurst(Transform root)
    {
        if (root == null || vsBurst == null) return;
        var vs = DeepFind(root, "BoardVsBadge");
        if (vs == null || !vs.gameObject.activeSelf) return;

        SetActive(DeepFind(root, "BoardVsBurst"), false);
        var rect = vs as RectTransform;
        Vector2 position = rect == null ? Vector2.zero : rect.anchoredPosition;
        var burst = AddImage(root, "BoardVsBurstVector", vsBurst,
            position, new Vector2(185f, 185f), true);
        if (burst != null)
        {
            int vsIndex = vs.GetSiblingIndex();
            burst.transform.SetSiblingIndex(Mathf.Max(0, vsIndex));
            vs.SetAsLastSibling();
        }
    }

    void ReplaceGlyphWithSprite(Transform screenRoot, string buttonName, string glyphName,
        string vectorName, Sprite sprite, Vector2 position, Vector2 size)
    {
        if (screenRoot == null || sprite == null) return;
        var buttonTransform = DeepFind(screenRoot, buttonName);
        if (buttonTransform == null) return;

        SetActive(DeepFind(buttonTransform, glyphName), false);
        SetActive(DeepFind(buttonTransform, glyphName + "Plate"), false);
        var vector = AddImage(buttonTransform, vectorName, sprite, position, size, true);
        if (vector != null) vector.transform.SetAsLastSibling();
    }

    static void StyleRuntimeCards(Transform root)
    {
        if (root == null) return;
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            string name = image.transform.name;
            if (name.StartsWith("Board") || name.StartsWith("Exact")) continue;
            if (!Contains(name, "Card") && !Contains(name, "Frame")) continue;
            if (image.GetComponent<Button>() != null) continue;

            image.sprite = RuntimeUI.RoundedRectSprite;
            image.type = Image.Type.Sliced;
            image.color = Panel;
            EnsureOutline(image.gameObject, Purple, 2f);
        }
    }

    static Image AddImage(Transform parent, string name, Sprite sprite,
        Vector2 position, Vector2 size, bool preserveAspect)
    {
        if (parent == null || sprite == null) return null;
        var image = EnsureImage(parent, name);
        image.gameObject.SetActive(true);
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        Place(image.rectTransform, position, size);
        return image;
    }

    static Image EnsureImage(Transform parent, string name)
    {
        var existing = DirectChild(parent, name);
        if (existing != null)
        {
            var image = existing.GetComponent<Image>();
            if (image != null) return image;
        }
        var go = RuntimeUI.CreateObject(name, parent);
        return go.AddComponent<Image>();
    }

    static TMP_Text MainButtonLabel(Button button)
    {
        if (button == null) return null;
        foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name.StartsWith("Board") || text.name.StartsWith("Exact")) continue;
            return text;
        }
        return button.GetComponentInChildren<TMP_Text>(true);
    }

    static Transform DirectChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name) return parent.GetChild(i);
        return null;
    }

    static Transform DeepFind(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = DeepFind(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void EnsureOutline(GameObject go, Color color, float distance)
    {
        if (go == null) return;
        var outline = go.GetComponent<Outline>();
        if (outline == null) outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, distance);
        outline.useGraphicAlpha = true;
    }

    static void SetActive(Transform transform, bool active)
    {
        if (transform != null && transform.gameObject.activeSelf != active)
            transform.gameObject.SetActive(active);
    }

    static bool Contains(string value, string part)
    {
        return value != null && value.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static Color Hex(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }
}
