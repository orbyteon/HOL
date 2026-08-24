using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Runtime hardening companion for HolDuelBoardLayout.
//
// HolDuelBoardLayout remains the sole presentation owner. This helper only
// enforces three invariants after that owner has seated the real scene controls:
// 1) the production root renders above retired scene-authored presentation,
// 2) answer controls stay hidden outside the authoritative answer phase, and
// 3) the current-number heading, value and input keep non-overlapping bounds.
[DisallowMultipleComponent]
[DefaultExecutionOrder(3100)]
public sealed class SoloDuelPresentationHardener : MonoBehaviour
{
    const string VisualRootName = "SoloDuelVisualRoot";
    const string SafeRootName = "SoloDuelSafeRoot";
    const string InteractionCardName = "SoloInteractionCard";
    const string GoldFrameResource = "mainmenu/mainmenu_cta_gold_9s";
    const string BodyFontResource = "phase2a/fonts/HOL Menu Body SDF";

    static readonly Vector2 CurrentValuePosition = new Vector2(0f, 355f);
    static readonly Vector2 CurrentValueSize = new Vector2(500f, 36f);
    static readonly Vector2 InputPosition = new Vector2(0f, 270f);
    static readonly Vector2 InputSize = new Vector2(500f, 110f);
    static readonly Vector2 MessagePosition = new Vector2(0f, 208f);
    static readonly Vector2 MessageSize = new Vector2(500f, 36f);
    static readonly Vector2 ResultButtonPosition = new Vector2(0f, -835f);
    static readonly Vector2 ResultButtonSize = new Vector2(360f, 96f);

    HolDuelBoardLayout layout;
    NumberManager numberManager;
    GameManager gameManager;
    RectTransform visualRoot;
    RectTransform safeRoot;
    RectTransform interactionCard;
    TMP_FontAsset bodyFont;
    bool seatedFunctionalLegacy;

    public bool IsApplied { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= InstallForScene;
        SceneManager.sceneLoaded += InstallForScene;
    }

    static void InstallForScene(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (HolDuelBoardLayout owner in
                     root.GetComponentsInChildren<HolDuelBoardLayout>(true))
            {
                if (owner.GetComponent<SoloDuelPresentationHardener>() == null)
                    owner.gameObject.AddComponent<SoloDuelPresentationHardener>();
            }
        }

        var host = new GameObject(nameof(SoloDuelPresentationHardener) + "Installer");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<InstallerProbe>();
    }

    sealed class InstallerProbe : MonoBehaviour
    {
        IEnumerator Start()
        {
            for (int frame = 0; frame < 300; frame++)
            {
                bool found = false;
                foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                {
                    foreach (HolDuelBoardLayout owner in
                             root.GetComponentsInChildren<HolDuelBoardLayout>(true))
                    {
                        found = true;
                        if (owner.GetComponent<SoloDuelPresentationHardener>() == null)
                            owner.gameObject.AddComponent<SoloDuelPresentationHardener>();
                    }
                }

                if (found)
                {
                    Destroy(gameObject);
                    yield break;
                }

                yield return null;
            }

            Debug.LogError(
                "[SoloDuelPresentationHardener] Solo board was not found within 300 frames.");
            Destroy(gameObject);
        }
    }

    void Awake()
    {
        layout = GetComponent<HolDuelBoardLayout>();
        numberManager = GetComponent<NumberManager>();
        gameManager = FindObjectOfType<GameManager>(true);
        bodyFont = Resources.Load<TMP_FontAsset>(BodyFontResource);
    }

    void LateUpdate()
    {
        if (layout == null || !layout.IsReady)
            return;

        ResolveRoots();
        if (visualRoot == null || safeRoot == null || interactionCard == null)
            return;

        // All real controls that remain interactive are descendants of this
        // root after SeatFunctionalLegacyControls. Keeping the root last removes
        // accidental overlays from retired scene-authored labels and panels.
        visualRoot.SetAsLastSibling();

        ApplyCurrentNumberSpacing();
        SeatFunctionalLegacyControls();
        SuppressRetiredGuessPanels();
        EnforceAnswerPhaseVisibility();
        IsApplied = true;
    }

    void ResolveRoots()
    {
        if (visualRoot == null)
            visualRoot = Find(transform, VisualRootName) as RectTransform;
        if (safeRoot == null && visualRoot != null)
            safeRoot = Find(visualRoot, SafeRootName) as RectTransform;
        if (interactionCard == null && visualRoot != null)
            interactionCard = Find(visualRoot, InteractionCardName) as RectTransform;
        if (numberManager == null)
            numberManager = GetComponent<NumberManager>();
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>(true);
    }

    void ApplyCurrentNumberSpacing()
    {
        if (numberManager == null)
            return;

        if (numberManager.playerNumberText != null)
        {
            RectTransform value = numberManager.playerNumberText.rectTransform;
            ReparentPreservingState(value, interactionCard);
            Place(value, CurrentValuePosition, CurrentValueSize);
        }

        if (numberManager.numberInput != null)
        {
            RectTransform input = numberManager.numberInput.transform as RectTransform;
            ReparentPreservingState(input, interactionCard);
            Place(input, InputPosition, InputSize);
        }
    }

    void SeatFunctionalLegacyControls()
    {
        if (seatedFunctionalLegacy || numberManager == null || gameManager == null)
            return;

        if (numberManager.messageText != null)
        {
            RectTransform message = numberManager.messageText.rectTransform;
            ReparentPreservingState(message, interactionCard);
            Place(message, MessagePosition, MessageSize);
            if (bodyFont != null)
                numberManager.messageText.font = bodyFont;
            numberManager.messageText.enableAutoSizing = true;
            numberManager.messageText.fontSizeMin = 19f;
            numberManager.messageText.fontSizeMax = 25f;
            numberManager.messageText.alignment = TextAlignmentOptions.Center;
            numberManager.messageText.overflowMode = TextOverflowModes.Overflow;
            numberManager.messageText.raycastTarget = false;
        }

        GameObject result = gameManager.stopGameButton;
        if (result != null)
        {
            RectTransform rect = result.transform as RectTransform;
            ReparentPreservingState(rect, safeRoot);
            Place(rect, ResultButtonPosition, ResultButtonSize);

            Button button = result.GetComponent<Button>();
            Image image = result.GetComponent<Image>();
            if (image == null)
                image = result.AddComponent<Image>();
            RuntimeUI.ApplyProductionSprite(
                image, GoldFrameResource, Image.Type.Sliced, false, 2f);
            image.raycastTarget = button != null;
            if (button != null)
            {
                button.targetGraphic = image;
                RuntimeUI.AttachJuice(button);
            }
        }

        seatedFunctionalLegacy = true;
    }

    void SuppressRetiredGuessPanels()
    {
        if (numberManager == null || visualRoot == null)
            return;

        DisableIfOutsideProductionRoot(numberManager.playerGuessesPanel);
        DisableIfOutsideProductionRoot(numberManager.aiGuessesPanel);
    }

    void DisableIfOutsideProductionRoot(GameObject target)
    {
        if (target == null || target.transform.IsChildOf(visualRoot))
            return;
        if (target.activeSelf)
            target.SetActive(false);
    }

    void EnforceAnswerPhaseVisibility()
    {
        if (gameManager == null)
            return;

        // GameManager remains authoritative over which one answer is valid.
        // The hardener only closes the invalid state introduced by reparenting:
        // outside AnswerOpponent, none of the three may be visible or clickable.
        if (layout.CurrentState.Phase == SoloBoardPhase.AnswerOpponent)
            return;

        SetInactive(gameManager.higherButton);
        SetInactive(gameManager.correctButton);
        SetInactive(gameManager.lowerButton);
    }

    static void SetInactive(GameObject target)
    {
        if (target != null && target.activeSelf)
            target.SetActive(false);
    }

    static void ReparentPreservingState(Transform child, Transform parent)
    {
        if (child == null || parent == null)
            return;

        bool active = child.gameObject.activeSelf;
        if (child.parent != parent)
            child.SetParent(parent, false);
        child.SetAsLastSibling();
        child.gameObject.SetActive(active);
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

    static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = Find(root.GetChild(i), name);
            if (found != null) return found;
        }

        return null;
    }
}
