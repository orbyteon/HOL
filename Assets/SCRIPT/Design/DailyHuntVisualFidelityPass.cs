using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Layout/readability pass for the single DailyHuntVisuals presentation owner.
/// It creates no duplicate art or controls; it only normalizes the modular
/// owner's live TMP/control bounds after DailyHunt has constructed them.
/// </summary>
[DefaultExecutionOrder(3300)]
[DisallowMultipleComponent]
public sealed class DailyHuntVisualFidelityPass : MonoBehaviour
{
    const string CleanPanelResource = "phase2a/hol_player_chip_r2_9s";
    const string GoldFrameResource = "phase2a/hol_cta_gold_r2_9s";
    const string BlueFrameResource = "phase2a/hol_cta_blue_r2_9s";
    const string AvatarResource = "reference/char_boy_exact";
    const string ProductionFontResource = "Fonts & Materials/LiberationSans SDF";

    static readonly Color NearWhite = new Color(0.985f, 0.975f, 1f, 1f);
    static readonly Color Ink = new Color(0.08f, 0.04f, 0.17f, 1f);
    static readonly Color Gold = new Color(1f, 0.80f, 0.20f, 1f);
    static readonly Color Cyan = new Color(0.16f, 0.90f, 1f, 1f);

    DailyHuntVisuals owner;
    RectTransform visualRoot;
    RectTransform safeRoot;
    TMP_FontAsset productionFont;

    public bool IsSettled { get; private set; }

    void Awake()
    {
        owner = GetComponent<DailyHuntVisuals>();
        productionFont = Resources.Load<TMP_FontAsset>(ProductionFontResource);
    }

    IEnumerator Start()
    {
        for (int frame = 0; frame < 300; frame++)
        {
            if (TryApply())
                yield break;
            yield return null;
        }

        Debug.LogError(
            "[DailyHuntVisualFidelityPass] Daily Hunt owner did not settle within 300 frames.");
    }

    void LateUpdate()
    {
        if (!IsSettled)
            TryApply();
    }

    bool TryApply()
    {
        if (IsSettled)
            return true;
        if (owner == null)
            owner = GetComponent<DailyHuntVisuals>();
        if (owner == null || !owner.IsReady || productionFont == null)
            return false;

        visualRoot = Find(transform, DailyHuntVisuals.VisualRootName) as RectTransform;
        safeRoot = visualRoot == null
            ? null
            : Find(visualRoot, DailyHuntVisuals.SafeRootName) as RectTransform;
        if (visualRoot == null || safeRoot == null)
            return false;

        ApplyTopBar();
        ApplyChallengeCard();
        ApplyRewardCard();
        ValidateComposition();
        IsSettled = true;
        return true;
    }

    void ApplyTopBar()
    {
        RectTransform playerChip = FindRect("DailyPlayerChip");
        Place(playerChip, new Vector2(350f, 842f), new Vector2(330f, 110f));

        Image avatar = FindImage("DailyPlayerAvatar");
        Sprite avatarSprite = Resources.Load<Sprite>(AvatarResource);
        if (avatar != null && avatarSprite != null)
        {
            avatar.sprite = avatarSprite;
            avatar.type = Image.Type.Simple;
            avatar.preserveAspect = true;
            avatar.color = Color.white;
            avatar.raycastTarget = false;
            Place(
                avatar.rectTransform,
                new Vector2(-118f, 0f),
                new Vector2(76f, 76f));
        }

        Place(
            FindRect("DailyTrophyIcon"),
            new Vector2(-18f, -25f),
            new Vector2(36f, 36f));

        ConfigureText(
            FindText("DailyPlayerName"),
            new Vector2(42f, 20f),
            new Vector2(178f, 34f),
            NearWhite,
            21f,
            28f,
            TextAlignmentOptions.Center,
            false);
        ConfigureText(
            FindText("DailyPlayerWins"),
            new Vector2(52f, -25f),
            new Vector2(92f, 32f),
            Gold,
            21f,
            28f,
            TextAlignmentOptions.Center,
            false);
    }

    void ApplyChallengeCard()
    {
        ConfigureText(
            FindText("DailyChallengeHeading"),
            new Vector2(215f, 278f),
            new Vector2(430f, 52f),
            Ink,
            26f,
            34f,
            TextAlignmentOptions.Center,
            true);

        Image statusFrame = FindImage("DailyStatusFrame");
        ApplyFrame(statusFrame, CleanPanelResource);
        Place(
            statusFrame == null ? null : statusFrame.rectTransform,
            new Vector2(215f, 112f),
            new Vector2(470f, 188f));
        ConfigureText(
            FindText("Status"),
            Vector2.zero,
            new Vector2(420f, 146f),
            NearWhite,
            21f,
            28f,
            TextAlignmentOptions.Center,
            false);

        Image trailFrame = FindImage("DailyTrailFrame");
        ApplyFrame(trailFrame, CleanPanelResource);
        Place(
            trailFrame == null ? null : trailFrame.rectTransform,
            new Vector2(215f, -62f),
            new Vector2(470f, 82f));
        ConfigureText(
            FindText("Trail"),
            Vector2.zero,
            new Vector2(430f, 54f),
            Cyan,
            24f,
            34f,
            TextAlignmentOptions.Center,
            false);

        TMP_InputField input = FindComponent<TMP_InputField>("GuessInput");
        if (input != null)
        {
            Place(
                input.transform as RectTransform,
                new Vector2(215f, -184f),
                new Vector2(340f, 84f));
            ApplyFrame(input.GetComponent<Image>(), CleanPanelResource, true);
            NormalizeInputText(input);
        }

        Button submit = FindComponent<Button>("SubmitGuessButton");
        NormalizeButton(
            submit,
            new Vector2(215f, -292f),
            new Vector2(420f, 88f),
            GoldFrameResource,
            Ink,
            27f,
            35f);
    }

    void ApplyRewardCard()
    {
        Place(
            FindRect("DailyRewardChest"),
            new Vector2(-305f, 0f),
            new Vector2(205f, 205f));

        ConfigureText(
            FindText("DailyRewardHeading"),
            new Vector2(180f, 72f),
            new Vector2(470f, 48f),
            NearWhite,
            25f,
            34f,
            TextAlignmentOptions.Center,
            true);
        ConfigureText(
            FindText("Streak"),
            new Vector2(180f, 17f),
            new Vector2(470f, 42f),
            Gold,
            21f,
            29f,
            TextAlignmentOptions.Center,
            false);

        NormalizeButton(
            FindComponent<Button>("ReviveButton"),
            new Vector2(180f, -64f),
            new Vector2(430f, 72f),
            GoldFrameResource,
            Ink,
            22f,
            29f);
        NormalizeButton(
            FindComponent<Button>("ShareButton"),
            new Vector2(180f, -64f),
            new Vector2(430f, 72f),
            BlueFrameResource,
            Ink,
            23f,
            30f);
    }

    void NormalizeInputText(TMP_InputField input)
    {
        if (input.textViewport != null)
            StretchInset(input.textViewport, 24f, 12f);

        if (input.textComponent != null)
        {
            input.textComponent.font = productionFont;
            input.textComponent.color = NearWhite;
            input.textComponent.fontStyle = FontStyles.Bold;
            input.textComponent.alignment = TextAlignmentOptions.Center;
            input.textComponent.enableAutoSizing = true;
            input.textComponent.fontSizeMin = 25f;
            input.textComponent.fontSizeMax = 34f;
            input.textComponent.enableWordWrapping = false;
            input.textComponent.overflowMode = TextOverflowModes.Overflow;
            input.textComponent.raycastTarget = false;
            StretchInset(input.textComponent.rectTransform, 8f, 4f);
        }

        TMP_Text placeholder = input.placeholder as TMP_Text;
        if (placeholder != null)
        {
            placeholder.font = productionFont;
            placeholder.color = new Color(0.88f, 0.84f, 0.96f, 0.92f);
            placeholder.alignment = TextAlignmentOptions.Center;
            placeholder.enableAutoSizing = true;
            placeholder.fontSizeMin = 22f;
            placeholder.fontSizeMax = 29f;
            placeholder.enableWordWrapping = false;
            placeholder.overflowMode = TextOverflowModes.Overflow;
            placeholder.raycastTarget = false;
            StretchInset(placeholder.rectTransform, 8f, 4f);
        }
    }

    void NormalizeButton(
        Button button,
        Vector2 position,
        Vector2 size,
        string resource,
        Color labelColor,
        float minimum,
        float maximum)
    {
        if (button == null)
            return;

        Place(button.transform as RectTransform, position, size);
        ApplyFrame(button.GetComponent<Image>(), resource, true);
        button.targetGraphic = button.GetComponent<Image>();

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
            return;

        label.gameObject.SetActive(true);
        label.font = productionFont;
        label.color = labelColor;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = minimum;
        label.fontSizeMax = maximum;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        StretchInset(label.rectTransform, 24f, 12f);
    }

    void ConfigureText(
        TMP_Text text,
        Vector2 position,
        Vector2 size,
        Color color,
        float minimum,
        float maximum,
        TextAlignmentOptions alignment,
        bool shadow)
    {
        if (text == null)
            return;

        Place(text.rectTransform, position, size);
        text.font = productionFont;
        text.color = color;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.enableAutoSizing = true;
        text.fontSizeMin = minimum;
        text.fontSizeMax = maximum;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;

        Shadow existing = text.GetComponent<Shadow>();
        if (shadow)
        {
            if (existing == null)
                existing = text.gameObject.AddComponent<Shadow>();
            existing.effectColor = new Color(0.02f, 0.01f, 0.12f, 0.72f);
            existing.effectDistance = new Vector2(2f, -3f);
            existing.useGraphicAlpha = true;
            existing.enabled = true;
        }
        else if (existing != null)
        {
            existing.enabled = false;
        }
    }

    static void ApplyFrame(
        Image image,
        string resource,
        bool raycast = false)
    {
        if (image == null)
            return;
        RuntimeUI.ApplyProductionSprite(
            image, resource, Image.Type.Sliced, false, 2f);
        image.color = Color.white;
        image.raycastTarget = raycast;
    }

    void ValidateComposition()
    {
        AssertNoOverlap("DailyChallengeHeading", "DailyStatusFrame");
        AssertNoOverlap("DailyStatusFrame", "DailyTrailFrame");
        AssertNoOverlap("DailyTrailFrame", "GuessInput");
        AssertNoOverlap("GuessInput", "SubmitGuessButton");
        AssertNoOverlap("DailyRewardHeading", "Streak");
        AssertNoOverlap("Streak", "ShareButton");
        AssertNoOverlap("Streak", "ReviveButton");
    }

    void AssertNoOverlap(string firstName, string secondName)
    {
        RectTransform first = FindRect(firstName);
        RectTransform second = FindRect(secondName);
        if (first == null || second == null || first.parent != second.parent)
            return;

        Rect a = BoundsInParent(first);
        Rect b = BoundsInParent(second);
        bool overlaps = a.xMin < b.xMax && b.xMin < a.xMax &&
                        a.yMin < b.yMax && b.yMin < a.yMax;
        if (overlaps)
        {
            Debug.LogError(
                "[DailyHuntVisualFidelityPass] Overlap: " +
                firstName + " / " + secondName + ".");
        }
    }

    RectTransform FindRect(string objectName)
    {
        return Find(visualRoot, objectName) as RectTransform;
    }

    Image FindImage(string objectName)
    {
        RectTransform rect = FindRect(objectName);
        return rect == null ? null : rect.GetComponent<Image>();
    }

    TMP_Text FindText(string objectName)
    {
        RectTransform rect = FindRect(objectName);
        return rect == null ? null : rect.GetComponent<TMP_Text>();
    }

    T FindComponent<T>(string objectName) where T : Component
    {
        RectTransform rect = FindRect(objectName);
        return rect == null ? null : rect.GetComponent<T>();
    }

    static Rect BoundsInParent(RectTransform rect)
    {
        Vector2 size = rect.rect.size;
        Vector2 minimum = rect.anchoredPosition - Vector2.Scale(size, rect.pivot);
        return new Rect(minimum, size);
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null)
            return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    static void StretchInset(
        RectTransform rect,
        float horizontal,
        float vertical)
    {
        if (rect == null)
            return;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontal, vertical);
        rect.offsetMax = new Vector2(-horizontal, -vertical);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    static Transform Find(Transform root, string objectName)
    {
        if (root == null)
            return null;
        if (root.name == objectName)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = Find(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }
        return null;
    }
}

/// <summary>
/// Lifecycle-only installer. Daily Hunt is generated after scene load, so this
/// host waits for the real owner and then attaches one non-visual fidelity pass.
/// </summary>
[DefaultExecutionOrder(3290)]
public sealed class DailyHuntVisualFidelityInstaller : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != "MainMenu")
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<DailyHuntVisualFidelityInstaller>(true) != null)
                return;
        }

        GameObject host = new GameObject(nameof(DailyHuntVisualFidelityInstaller));
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<DailyHuntVisualFidelityInstaller>();
    }

    IEnumerator Start()
    {
        for (int frame = 0; frame < 600; frame++)
        {
            DailyHuntVisuals owner = FindObjectOfType<DailyHuntVisuals>(true);
            if (owner != null)
            {
                if (owner.GetComponent<DailyHuntVisualFidelityPass>() == null)
                    owner.gameObject.AddComponent<DailyHuntVisualFidelityPass>();
                Destroy(gameObject);
                yield break;
            }
            yield return null;
        }

        Debug.LogError(
            "[DailyHuntVisualFidelityInstaller] Daily Hunt owner was not created within 600 frames.");
        Destroy(gameObject);
    }
}
