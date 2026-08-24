using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Non-visual integrity companion for <see cref="HolDuelBoardLayout"/>.
///
/// HolDuelBoardLayout remains the sole presentation owner. This component does
/// not create artwork or duplicate gameplay controls; it seats controller-owned
/// scene objects inside that owner's safe root, suppresses retired legacy labels,
/// preserves phase-authoritative button visibility, and enforces the approved
/// non-overlapping current-number geometry.
/// </summary>
[DefaultExecutionOrder(3250)]
[DisallowMultipleComponent]
public sealed class SoloDuelVisualIntegrityGuard : MonoBehaviour
{
    const string GoldFrameResource = "mainmenu/mainmenu_cta_gold_9s";
    const string BlueFrameResource = "mainmenu/mainmenu_cta_blue_9s";
    const string PurpleFrameResource = "mainmenu/mainmenu_tip_frame_9s";

    static readonly Color NearWhite = new Color(0.985f, 0.975f, 1f, 1f);
    static readonly Color Ink = new Color(0.09f, 0.05f, 0.16f, 1f);

    HolDuelBoardLayout layout;
    NumberManager numberManager;
    GameManager gameManager;
    RectTransform visualRoot;
    RectTransform safeRoot;
    Button lockButton;
    Button saveStreakButton;
    bool resultControlSeated;
    float nextDynamicControlProbe;

    public bool IsSettled { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            foreach (HolDuelBoardLayout owner in
                     sceneRoot.GetComponentsInChildren<HolDuelBoardLayout>(true))
            {
                if (owner.GetComponent<SoloDuelVisualIntegrityGuard>() == null)
                    owner.gameObject.AddComponent<SoloDuelVisualIntegrityGuard>();
            }
        }
    }

    void Awake()
    {
        layout = GetComponent<HolDuelBoardLayout>();
        numberManager = GetComponent<NumberManager>();
        gameManager = FindObjectOfType<GameManager>(true);
    }

    IEnumerator Start()
    {
        for (int frame = 0; frame < 300 && !IsSettled; frame++)
        {
            SettleIfReady();
            if (!IsSettled)
                yield return null;
        }

        if (!IsSettled)
        {
            Debug.LogError(
                "[SoloDuelVisualIntegrityGuard] Solo duel owner did not settle within 300 frames.");
        }
    }

    void LateUpdate()
    {
        // HolDuelBoardLayout currently builds from an Invoke(0) callback. The
        // LateUpdate check catches that same frame, before Canvas rendering, so
        // no legacy hierarchy can flash above the approved presentation.
        if (!IsSettled)
        {
            SettleIfReady();
            if (!IsSettled)
                return;
        }

        if (Time.unscaledTime >= nextDynamicControlProbe)
        {
            nextDynamicControlProbe = Time.unscaledTime + 0.25f;
            SeatControllerOwnedControls();
        }

        SuppressLegacyGuessPanels();
        EnforcePhaseVisibility();
        KeepProductionOwnerOnTop();
    }

    void SettleIfReady()
    {
        if (!TryResolveOwnerRoots())
            return;

        ApplyApprovedGeometry();
        SeatControllerOwnedControls();
        SuppressLegacyGuessPanels();
        EnforcePhaseVisibility();
        KeepProductionOwnerOnTop();
        IsSettled = true;
    }

    bool TryResolveOwnerRoots()
    {
        if (layout == null)
            layout = GetComponent<HolDuelBoardLayout>();
        if (numberManager == null)
            numberManager = GetComponent<NumberManager>();
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>(true);

        if (layout == null || !layout.IsReady)
            return false;

        visualRoot = DeepFind(
            transform, HolDuelBoardLayout.VisualRootName) as RectTransform;
        safeRoot = visualRoot == null
            ? null
            : DeepFind(
                visualRoot, HolDuelBoardLayout.SafeRootName) as RectTransform;
        return visualRoot != null && safeRoot != null;
    }

    void ApplyApprovedGeometry()
    {
        RectTransform heading = DeepFind(
            safeRoot, "CurrentNumberHeading") as RectTransform;
        if (heading != null)
            Place(heading, new Vector2(0f, 430f), new Vector2(520f, 44f));

        if (numberManager != null && numberManager.playerNumberText != null)
        {
            Place(
                numberManager.playerNumberText.rectTransform,
                new Vector2(0f, 374f), new Vector2(500f, 40f));
        }

        if (numberManager != null && numberManager.numberInput != null)
        {
            Place(
                numberManager.numberInput.transform as RectTransform,
                new Vector2(0f, 286f), new Vector2(500f, 124f));
        }

        ValidateNoVerticalOverlap(
            heading,
            numberManager == null || numberManager.playerNumberText == null
                ? null
                : numberManager.playerNumberText.rectTransform,
            "CurrentNumberHeading / PlayerNumberValue");
        ValidateNoVerticalOverlap(
            numberManager == null || numberManager.playerNumberText == null
                ? null
                : numberManager.playerNumberText.rectTransform,
            numberManager == null || numberManager.numberInput == null
                ? null
                : numberManager.numberInput.transform as RectTransform,
            "PlayerNumberValue / GuessInput");
    }

    void SeatControllerOwnedControls()
    {
        if (safeRoot == null)
            return;

        if (!resultControlSeated &&
            gameManager != null &&
            gameManager.stopGameButton != null)
        {
            SeatButton(
                gameManager.stopGameButton,
                new Vector2(0f, -824f),
                new Vector2(500f, 96f),
                GoldFrameResource,
                Ink);
            resultControlSeated = true;
        }

        if (lockButton == null)
            lockButton = FindNamedButton("LockButton");
        if (lockButton != null && lockButton.transform.parent != safeRoot)
        {
            SeatButton(
                lockButton.gameObject,
                new Vector2(0f, -716f),
                new Vector2(360f, 82f),
                BlueFrameResource,
                Ink);
        }

        if (saveStreakButton == null)
            saveStreakButton = FindNamedButton("SaveStreakButton");
        if (saveStreakButton != null && saveStreakButton.transform.parent != safeRoot)
        {
            SeatButton(
                saveStreakButton.gameObject,
                new Vector2(0f, -710f),
                new Vector2(560f, 90f),
                PurpleFrameResource,
                NearWhite);
        }
    }

    void SeatButton(
        GameObject control,
        Vector2 position,
        Vector2 size,
        string spriteResource,
        Color labelColor)
    {
        if (control == null || safeRoot == null)
            return;

        bool wasActive = control.activeSelf;
        Transform controlTransform = control.transform;
        if (controlTransform.parent != safeRoot)
            controlTransform.SetParent(safeRoot, false);

        Place(controlTransform as RectTransform, position, size);
        controlTransform.SetAsLastSibling();

        Button button = control.GetComponent<Button>();
        Image image = control.GetComponent<Image>();
        if (image == null)
            image = control.AddComponent<Image>();
        RuntimeUI.ApplyProductionSprite(
            image, spriteResource, Image.Type.Sliced, false, 2f);
        image.raycastTarget = true;

        if (button != null)
        {
            button.targetGraphic = image;
            RuntimeUI.AttachJuice(button);
        }

        TMP_Text label = control.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = labelColor;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }

        control.SetActive(wasActive);
    }

    void SuppressLegacyGuessPanels()
    {
        if (numberManager == null || visualRoot == null)
            return;

        SuppressLegacyPanel(numberManager.playerGuessesPanel);
        SuppressLegacyPanel(numberManager.aiGuessesPanel);
    }

    void SuppressLegacyPanel(GameObject legacy)
    {
        if (legacy == null)
            return;

        Transform legacyTransform = legacy.transform;
        if (legacyTransform == visualRoot ||
            legacyTransform.IsChildOf(visualRoot) ||
            visualRoot.IsChildOf(legacyTransform))
            return;

        if (legacy.activeSelf)
            legacy.SetActive(false);
    }

    void EnforcePhaseVisibility()
    {
        if (layout == null || gameManager == null)
            return;

        // GameManager is authoritative during AnswerOpponent and activates only
        // the truthful hint button after hiding all three. Every other phase has
        // no valid answer action, so stale scene/runtime visibility is rejected.
        if (layout.CurrentState.Phase == SoloBoardPhase.AnswerOpponent)
            return;

        SetInactive(gameManager.higherButton);
        SetInactive(gameManager.correctButton);
        SetInactive(gameManager.lowerButton);
    }

    void KeepProductionOwnerOnTop()
    {
        if (visualRoot != null)
            visualRoot.SetAsLastSibling();
    }

    Button FindNamedButton(string objectName)
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
            if (button.name == objectName)
                return button;
        return null;
    }

    static void SetInactive(GameObject value)
    {
        if (value != null && value.activeSelf)
            value.SetActive(false);
    }

    static void ValidateNoVerticalOverlap(
        RectTransform first,
        RectTransform second,
        string pair)
    {
        if (first == null || second == null || first.parent != second.parent)
            return;

        float firstMin = first.anchoredPosition.y - first.rect.height * first.pivot.y;
        float firstMax = firstMin + first.rect.height;
        float secondMin = second.anchoredPosition.y - second.rect.height * second.pivot.y;
        float secondMax = secondMin + second.rect.height;
        bool overlaps = firstMin < secondMax && secondMin < firstMax;
        if (overlaps)
            Debug.LogError("[SoloDuelVisualIntegrityGuard] Overlap: " + pair + ".");
    }

    static void Place(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
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

    static Transform DeepFind(Transform root, string objectName)
    {
        if (root == null)
            return null;
        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = DeepFind(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }
        return null;
    }
}
