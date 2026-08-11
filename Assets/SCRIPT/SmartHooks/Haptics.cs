using UnityEngine;

// Minimal haptic feedback. Handheld.Vibrate is the only built-in cross-
// platform trigger (Android long-vibrate); for richer patterns add a
// haptics plugin later — call sites won't change.
public static class Haptics
{
    public static void Light()
    {
        // Reserved for a future haptics plugin; intentionally a no-op so
        // call sites can already be placed where feedback belongs.
    }

    public static void Success()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

    public static void Error()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }
}
