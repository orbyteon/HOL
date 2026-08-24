using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public enum HolTextRole
{
    Hero,
    PrimaryCta,
    SectionHeading,
    SecondaryCta,
    Emphasis,
    Body,
    Small,
    LiveNumber
}

public static class CartoonTypography
{
    public static TMP_FontAsset Resolve(HolTextRole role)
    {
        var catalog = HolTheme.Current;
        if (catalog == null || catalog.typography == null) return null;

        bool greek = L10n.Current == L10n.Language.Greek;
        if (greek)
        {
            switch (role)
            {
                case HolTextRole.Hero:
                case HolTextRole.PrimaryCta:
                    return catalog.typography.notoSansExtraBold;
                case HolTextRole.SectionHeading:
                case HolTextRole.SecondaryCta:
                    return catalog.typography.notoSansBold;
                case HolTextRole.Emphasis:
                    return catalog.typography.notoSansSemiBold;
                case HolTextRole.Body:
                    return catalog.typography.notoSansMedium;
                case HolTextRole.Small:
                    return catalog.typography.notoSansRegular;
                case HolTextRole.LiveNumber:
                    return catalog.typography.montserratExtraBold;
            }
        }

        switch (role)
        {
            case HolTextRole.Hero:
            case HolTextRole.PrimaryCta:
            case HolTextRole.LiveNumber:
                return catalog.typography.montserratExtraBold;
            case HolTextRole.SectionHeading:
            case HolTextRole.SecondaryCta:
                return catalog.typography.montserratBold;
            case HolTextRole.Emphasis:
                return catalog.typography.plusJakartaSemiBold;
            case HolTextRole.Body:
                return catalog.typography.plusJakartaMedium;
            default:
                return catalog.typography.plusJakartaRegular;
        }
    }

    public static void Apply(TMP_Text text, HolTextRole role)
    {
        if (text == null) return;
        var font = Resolve(role);
        if (font != null) text.font = font;
    }

    public static CartoonTypographyBinding Bind(TMP_Text text,
        HolTextRole role)
    {
        if (text == null) return null;
        var binding = text.GetComponent<CartoonTypographyBinding>();
        if (binding == null)
            binding = text.gameObject.AddComponent<CartoonTypographyBinding>();
        binding.Role = role;
        binding.Refresh();
        return binding;
    }
}

// One canonical set for deterministic TMP baking and verification. It is built
// from the real localization table plus the approved live UI symbol vocabulary.
public static class CartoonCharacterSet
{
    const string ApprovedSymbols =
        "☻ϟ•×←→▲▼●–—…!?%+−=:/#()[]{}'\".,;_@&°";

    public static string Build()
    {
        var seen = new HashSet<char>();
        var ordered = new StringBuilder();
        AddRange(ordered, seen,
            " 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
        foreach (var value in L10n.AllProductionStrings)
            AddRange(ordered, seen, value);
        AddRange(ordered, seen, ApprovedSymbols);
        return ordered.ToString();
    }

    public static string BuildEnglish()
    {
        var seen = new HashSet<char>();
        var ordered = new StringBuilder();
        AddRange(ordered, seen,
            " 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
        foreach (var value in L10n.ProductionStrings(L10n.Language.English))
            AddRange(ordered, seen, value);
        AddRange(ordered, seen, ApprovedSymbols);
        return ordered.ToString();
    }

    static void AddRange(StringBuilder output, HashSet<char> seen,
        string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        foreach (char character in value)
            if (!char.IsControl(character) && seen.Add(character))
                output.Append(character);
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class CartoonTypographyBinding : MonoBehaviour
{
    [SerializeField] HolTextRole role = HolTextRole.Body;

    TMP_Text target;

    public HolTextRole Role
    {
        get => role;
        set => role = value;
    }

    void OnEnable()
    {
        L10n.OnLanguageChanged -= Refresh;
        L10n.OnLanguageChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        if (target == null) target = GetComponent<TMP_Text>();
        CartoonTypography.Apply(target, role);
    }
}
