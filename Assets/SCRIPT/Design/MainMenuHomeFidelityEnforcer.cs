using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Final Main Menu Home fidelity guard. MainMenuHomeVisuals owns composition and
// existing navigation; this component only restores the approved production
// sprites as the visible source of truth after that owner has finished building.
// It never creates a Button, changes a callback, or changes gameplay state.
[DefaultExecutionOrder(2000)]
public sealed class MainMenuHomeFidelityEnforcer : MonoBehaviour
{
    const string GoldCtaResource = "phase2a/hol_cta_gold_r2_9s";
    const string BlueCtaResource = "phase2a/hol_cta_blue_r2_9s";
    const string PlayerChipResource = "phase2a/hol_player_chip_r2_9s";
    const string AvatarResource = "reference/player_cyan_exact";
    const string GearResource = "phase2a/hol_settings_gear_r2";
    const string PrivateIconResource = "phase2a/hol_mode_private_r2";
    const string DailyIconResource = "phase2a/hol_mode_daily_r2";

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

        Canvas canvas = FindOwnedCanvas(scene);
        if (canvas != null && canvas.GetComponent<MainMenuHomeFidelityEnforcer>() == null)
            canvas.gameObject.AddComponent<MainMenuHomeFidelityEnforcer>();
    }

    IEnumerator Start()
    {
        // MainMenuHomeVisuals intentionally builds late. Wait for its authored
        // production hierarchy rather than competing with it in the same frame.
        for (int i = 0; i < 20; i++)
        {
            var owner = GetComponent<MainMenuHomeVisuals>();
            if (owner != null && owner.IsReady)
                break;
            yield return null;
        }

        ApplyToCanvas(GetComponent<Canvas>());
    }

    // Public for focused EditMode contract tests. Safe to call repeatedly.
    public static bool ApplyToCanvas(Canvas canvas)
    {
        if (canvas == null) return false;
        Transform visualRoot = DeepFind(canvas.transform, MainMenuHomeVisuals.VisualRootName);
        if (visualRoot == null) return false;

        bool ok = true;
        ok &= RestoreButton(visualRoot, "ButtonPlay", GoldCtaResource);
        ok &= RestoreButton(visualRoot, "ButtonPvP", BlueCtaResource);
        ok &= RestoreButton(visualRoot, "DailyHuntButton", GoldCtaResource);
        ok &= RestoreChip(visualRoot);
        ok &= RestoreGear(visualRoot);
        ok &= RestoreModeIcon(visualRoot, MainMenuHomeVisuals.PrivateIconName,
            PrivateIconResource);
        ok &= RestoreModeIcon(visualRoot, MainMenuHomeVisuals.DailyIconName,
            DailyIconResource);
        return ok;
    }

    static bool RestoreButton(Transform root, string buttonName, string resource)
    {
        var buttonTransform = DeepFind(root, buttonName);
        if (buttonTransform == null) return false;
        var button = buttonTransform.GetComponent<Button>();
        var image = buttonTransform.GetComponent<Image>();
        var sprite = LoadRequired(resource);
        if (button == null || image == null || sprite == null) return false;

        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.raycastTarget = true;

        // The production sprite is the base artwork. Interaction lighting may
        // only be additive; the legacy procedural replacement surface is not.
        var luminous = buttonTransform.GetComponent<MainMenuCtaLuminousSurface>();
        if (luminous != null) luminous.enabled = false;
        DisableDescendants<MainMenuChamferedCtaGraphic>(buttonTransform);
        return true;
    }

    static bool RestoreChip(Transform root)
    {
        var chip = DeepFind(root, MainMenuHomeVisuals.ChipName);
        var frame = LoadRequired(PlayerChipResource);
        if (chip == null || frame == null) return false;

        var image = chip.GetComponent<Image>();
        if (image == null) image = chip.gameObject.AddComponent<Image>();
        image.sprite = frame;
        image.color = Color.white;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.raycastTarget = false;

        var proceduralSurface = DeepFind(chip, "HomePlayerChipSurface");
        if (proceduralSurface != null)
            DisableDescendants<MainMenuPlayerChipGraphic>(proceduralSurface);

        var avatarObject = DeepFind(chip, "HomePlayerAvatar");
        var avatar = LoadRequired(AvatarResource);
        if (avatarObject != null && avatar != null)
        {
            var avatarImage = avatarObject.GetComponent<Image>();
            if (avatarImage == null) avatarImage = avatarObject.gameObject.AddComponent<Image>();
            avatarImage.sprite = avatar;
            avatarImage.color = Color.white;
            avatarImage.type = Image.Type.Simple;
            avatarImage.preserveAspect = true;
            avatarImage.raycastTarget = false;
        }

        var avatarSymbol = DeepFind(chip, "HomePlayerAvatarSymbol");
        if (avatarSymbol != null)
            DisableDescendants<MainMenuReferenceIconGraphic>(avatarSymbol);
        return true;
    }

    static bool RestoreGear(Transform root)
    {
        var gearButton = DeepFind(root, "Buttonsettings");
        var sprite = LoadRequired(GearResource);
        if (gearButton == null || sprite == null) return false;

        var image = gearButton.GetComponent<Image>();
        if (image == null) return false;
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = true;

        var symbol = DeepFind(gearButton, "HomeSettingsGearSymbol");
        if (symbol != null)
            DisableDescendants<MainMenuReferenceIconGraphic>(symbol);
        return true;
    }

    static bool RestoreModeIcon(Transform root, string name, string resource)
    {
        var iconObject = DeepFind(root, name);
        var sprite = LoadRequired(resource);
        if (iconObject == null || sprite == null) return false;

        DisableDescendants<MainMenuReferenceIconGraphic>(iconObject);
        var image = iconObject.GetComponent<Image>();
        if (image == null) image = iconObject.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return true;
    }

    static void DisableDescendants<T>(Transform root) where T : Behaviour
    {
        if (root == null) return;
        foreach (var component in root.GetComponentsInChildren<T>(true))
            component.enabled = false;
    }

    static Sprite LoadRequired(string resource)
    {
        var sprite = Resources.Load<Sprite>(resource);
        if (sprite == null)
            Debug.LogError("[MainMenuHomeFidelityEnforcer] Missing Resources/" + resource + ".");
        return sprite;
    }

    static Canvas FindOwnedCanvas(Scene scene)
    {
        var menu = FindInScene<MenuManager>(scene);
        if (menu != null && menu.mainMenuPanel != null)
        {
            var owned = menu.mainMenuPanel.GetComponentInParent<Canvas>();
            if (owned != null && owned.isRootCanvas && owned.renderMode != RenderMode.WorldSpace)
                return owned;
        }

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var candidate in root.GetComponentsInChildren<Canvas>(true))
                if (candidate.isRootCanvas && candidate.renderMode != RenderMode.WorldSpace)
                    return candidate;
        }
        return null;
    }

    static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid()) return null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var result = root.GetComponentInChildren<T>(true);
            if (result != null) return result;
        }
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
}
