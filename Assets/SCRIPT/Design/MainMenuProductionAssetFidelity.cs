using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Main Menu Home contract enforcer.
//
// MainMenuHomeVisuals remains the sole layout/presentation owner. This component
// does not create navigation, move controls, or invent a second layout pass. It
// only corrects the final rendering state of already-built Home objects so the
// approved production sprites remain the visible source of truth required by
// AGENTS.md and design/cartoon-theme.md.
[DefaultExecutionOrder(1800)]
public sealed class MainMenuProductionAssetFidelity : MonoBehaviour
{
    const string GoldCtaResource = "phase2a/hol_cta_gold_r2_9s";
    const string BlueCtaResource = "phase2a/hol_cta_blue_r2_9s";
    const string ChipResource = "phase2a/hol_player_chip_r2_9s";
    const string TipResource = "phase2a/hol_tip_frame_r2_9s";
    const string GearResource = "phase2a/hol_settings_gear_r2";
    const string AvatarResource = "reference/player_cyan_exact";
    const string PrivateIconResource = "phase2a/hol_mode_private_r2";
    const string DailyIconResource = "phase2a/hol_mode_daily_r2";

    public bool IsApplied { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainMenu" || !scene.IsValid() || !scene.isLoaded)
            return;

        Canvas canvas = null;
        var menu = FindInScene<MenuManager>(scene);
        if (menu != null && menu.mainMenuPanel != null)
            canvas = menu.mainMenuPanel.GetComponentInParent<Canvas>();

        if (canvas == null || !canvas.isRootCanvas ||
            canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var candidate in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (!candidate.isRootCanvas ||
                        candidate.renderMode == RenderMode.WorldSpace)
                        continue;
                    canvas = candidate;
                    break;
                }
                if (canvas != null) break;
            }
        }

        if (canvas != null &&
            canvas.GetComponent<MainMenuProductionAssetFidelity>() == null)
            canvas.gameObject.AddComponent<MainMenuProductionAssetFidelity>();
    }

    IEnumerator Start()
    {
        // MainMenuHomeVisuals intentionally builds after other legacy/runtime
        // layers. Wait for that owner to finish BuildHome, then correct only
        // the production rendering state before its Android capture settles.
        MainMenuHomeVisuals home = null;
        for (int i = 0; i < 45; i++)
        {
            home = GetComponent<MainMenuHomeVisuals>();
            if (home != null && home.IsReady)
                break;
            yield return null;
        }

        if (home == null || !home.IsReady)
        {
            Debug.LogError("[MainMenuProductionAssetFidelity] Home owner did not become ready.");
            yield break;
        }

        IsApplied = ApplyToRoot(transform);
        if (!IsApplied)
            Debug.LogError("[MainMenuProductionAssetFidelity] Production fidelity application failed.");
    }

    public static bool ApplyToRoot(Transform canvasRoot)
    {
        if (canvasRoot == null) return false;

        var gold = LoadRequired(GoldCtaResource);
        var blue = LoadRequired(BlueCtaResource);
        var chip = LoadRequired(ChipResource);
        var tip = LoadRequired(TipResource);
        var gear = LoadRequired(GearResource);
        var avatar = LoadRequired(AvatarResource);
        var privateIcon = LoadRequired(PrivateIconResource);
        var dailyIcon = LoadRequired(DailyIconResource);

        if (gold == null || blue == null || chip == null || tip == null ||
            gear == null || avatar == null || privateIcon == null || dailyIcon == null)
            return false;

        bool ok = true;
        ok &= ApplyCta(canvasRoot, "ButtonPlay", gold);
        ok &= ApplyCta(canvasRoot, "ButtonPvP", blue);
        // Keep the current approved Home composition: Daily uses the gold Phase
        // 2A frame in MainMenuHomeVisuals. Theme-role changes are a separate
        // visual-approval decision, not part of this fidelity correction.
        ok &= ApplyCta(canvasRoot, "DailyHuntButton", gold);
        ok &= ApplyGear(canvasRoot, gear);
        ok &= ApplyPlayerChip(canvasRoot, chip, avatar);
        ok &= ApplyTip(canvasRoot, tip);
        ok &= ApplyModeIcon(canvasRoot, "ButtonPvP",
            MainMenuHomeVisuals.PrivateIconName, privateIcon);
        ok &= ApplyModeIcon(canvasRoot, "DailyHuntButton",
            MainMenuHomeVisuals.DailyIconName, dailyIcon);
        return ok;
    }

    static bool ApplyCta(Transform root, string buttonName, Sprite sprite)
    {
        var buttonTransform = DeepFind(root, buttonName);
        if (buttonTransform == null) return false;
        var button = buttonTransform.GetComponent<Button>();
        var image = buttonTransform.GetComponent<Image>();
        if (button == null || image == null) return false;

        image.gameObject.SetActive(true);
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.raycastTarget = true;
        button.targetGraphic = image;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
        colors.disabledColor = new Color(0.58f, 0.58f, 0.58f, 0.72f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.07f;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;

        DisableAndDestroy(button.GetComponent<MainMenuCtaLuminousSurface>());
        RemoveChild(buttonTransform, "HomeCtaInnerLight");
        return true;
    }

    static bool ApplyGear(Transform root, Sprite sprite)
    {
        var buttonTransform = DeepFind(root, "Buttonsettings");
        if (buttonTransform == null) return false;
        var button = buttonTransform.GetComponent<Button>();
        var image = buttonTransform.GetComponent<Image>();
        if (button == null || image == null) return false;

        image.gameObject.SetActive(true);
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = true;
        button.targetGraphic = image;

        RemoveChild(buttonTransform, "HomeSettingsGearSymbol");
        return true;
    }

    static bool ApplyPlayerChip(Transform root, Sprite frame, Sprite avatar)
    {
        var chipTransform = DeepFind(root, MainMenuHomeVisuals.ChipName);
        if (chipTransform == null) return false;
        var image = chipTransform.GetComponent<Image>();
        if (image == null) return false;

        image.gameObject.SetActive(true);
        image.sprite = frame;
        image.color = Color.white;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.raycastTarget = false;
        RemoveChild(chipTransform, "HomePlayerChipSurface");

        var avatarTransform = DeepFind(chipTransform, "HomePlayerAvatar");
        if (avatarTransform == null) return false;
        var avatarImage = avatarTransform.GetComponent<Image>();
        if (avatarImage == null) return false;
        avatarImage.gameObject.SetActive(true);
        avatarImage.sprite = avatar;
        avatarImage.color = Color.white;
        avatarImage.type = Image.Type.Simple;
        avatarImage.preserveAspect = true;
        avatarImage.raycastTarget = false;
        RemoveChild(chipTransform, "HomePlayerAvatarSymbol");
        return true;
    }

    static bool ApplyTip(Transform root, Sprite frame)
    {
        var tipTransform = DeepFind(root, MainMenuHomeVisuals.TipName);
        if (tipTransform == null) return false;
        var image = tipTransform.GetComponent<Image>();
        if (image == null) return false;
        image.gameObject.SetActive(true);
        image.sprite = frame;
        image.color = Color.white;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.raycastTarget = false;
        return true;
    }

    static bool ApplyModeIcon(Transform root, string buttonName,
        string iconName, Sprite sprite)
    {
        var buttonTransform = DeepFind(root, buttonName);
        if (buttonTransform == null) return false;
        var iconTransform = DeepFind(buttonTransform, iconName);
        if (iconTransform == null) return false;

        var procedural = iconTransform.GetComponent<MainMenuReferenceIconGraphic>();
        DisableAndDestroy(procedural);

        var image = iconTransform.GetComponent<Image>();
        if (image == null) image = iconTransform.gameObject.AddComponent<Image>();
        image.gameObject.SetActive(true);
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return true;
    }

    static Sprite LoadRequired(string path)
    {
        var sprite = Resources.Load<Sprite>(path);
        if (sprite == null)
            Debug.LogError("[MainMenuProductionAssetFidelity] Missing Resources/" + path + ".");
        return sprite;
    }

    static void DisableAndDestroy(Component component)
    {
        if (component == null) return;
        if (component is Behaviour behaviour)
            behaviour.enabled = false;
        RuntimeUI.DestroyNow(component);
    }

    static void RemoveChild(Transform parent, string name)
    {
        var child = DirectChild(parent, name);
        if (child == null) return;
        child.gameObject.SetActive(false);
        RuntimeUI.DestroyNow(child.gameObject);
    }

    static Transform DirectChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name)
                return parent.GetChild(i);
        return null;
    }

    static Transform DeepFind(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var found = DeepFind(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid()) return null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }
}
