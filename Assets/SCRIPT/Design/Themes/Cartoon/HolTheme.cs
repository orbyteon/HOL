using UnityEngine;

public enum HolThemeId
{
    Cartoon = 0
}

// Single production theme entry point. There is intentionally no runtime
// theme selector: every production screen resolves the same audited catalog.
public static class HolTheme
{
    public const string CartoonCatalogResource =
        "Themes/Cartoon/CartoonThemeCatalog";

    static CartoonThemeCatalog current;

    public static HolThemeId CurrentId => HolThemeId.Cartoon;

    public static CartoonThemeCatalog Current
    {
        get
        {
            if (current == null)
                current = Resources.Load<CartoonThemeCatalog>(
                    CartoonCatalogResource);
            return current;
        }
    }

    public static bool IsReady => Current != null && Current.IsComplete;

    public static void ResetCache()
    {
        current = null;
    }
}
