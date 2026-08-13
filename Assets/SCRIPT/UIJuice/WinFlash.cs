using UnityEngine;
using UnityEngine.UI;

// One warm full-screen light pulse on victory, synced with the confetti —
// the frame of celebration a bare text swap never delivers. Listens to
// GameEvents.OnMatchEnded, so it needs no per-screen wiring; the `gate`
// object scopes it to its own screen (solo game panel / PvP match panel) so
// a solo win never flashes the PvP overlay and vice versa.
public class WinFlash : MonoBehaviour
{
    public GameObject gate;

    static readonly Color FlashColor = new Color(1f, 0.86f, 0.55f);
    const float Duration = 0.55f;

    Image image;
    float t = float.MaxValue;

    // Builds the flash layer as the LAST child of `parent` (topmost) and
    // returns it for gate assignment.
    public static WinFlash Attach(Transform parent)
    {
        var go = RuntimeUI.CreateObject("WinFlash", parent);
        RuntimeUI.Stretch(go);
        var img = go.AddComponent<Image>();
        img.color = ConvergingLight.WithAlpha(FlashColor, 0f);
        img.raycastTarget = false;

        var flash = go.AddComponent<WinFlash>();
        flash.image = img;
        return flash;
    }

    void OnEnable()
    {
        GameEvents.OnMatchEnded += OnMatchEnded;
    }

    void OnDisable()
    {
        GameEvents.OnMatchEnded -= OnMatchEnded;
        if (image != null) image.color = ConvergingLight.WithAlpha(FlashColor, 0f);
        t = float.MaxValue;
    }

    void OnMatchEnded(bool playerWon, int guesses)
    {
        if (!playerWon) return;
        if (gate != null && !gate.activeInHierarchy) return;
        t = 0f;
    }

    void Update()
    {
        if (t >= Duration || image == null) return;

        t += Time.unscaledDeltaTime;
        float fade = Mathf.Clamp01(1f - t / Duration);
        image.color = ConvergingLight.WithAlpha(FlashColor, 0.5f * fade * fade);
    }
}
