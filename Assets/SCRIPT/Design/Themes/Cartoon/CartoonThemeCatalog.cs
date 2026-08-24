using System;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "CartoonThemeCatalog",
    menuName = "HOL/Themes/Cartoon Theme Catalog")]
public sealed class CartoonThemeCatalog : ScriptableObject
{
    [Serializable]
    public sealed class TypographySet
    {
        public TMP_FontAsset montserratExtraBold;
        public TMP_FontAsset montserratBold;
        public TMP_FontAsset plusJakartaSemiBold;
        public TMP_FontAsset plusJakartaMedium;
        public TMP_FontAsset plusJakartaRegular;
        public TMP_FontAsset notoSansExtraBold;
        public TMP_FontAsset notoSansBold;
        public TMP_FontAsset notoSansSemiBold;
        public TMP_FontAsset notoSansMedium;
        public TMP_FontAsset notoSansRegular;

        public bool IsComplete =>
            montserratExtraBold != null && montserratBold != null &&
            plusJakartaSemiBold != null && plusJakartaMedium != null &&
            plusJakartaRegular != null && notoSansExtraBold != null &&
            notoSansBold != null && notoSansSemiBold != null &&
            notoSansMedium != null && notoSansRegular != null;
    }

    [Serializable]
    public sealed class SharedArtSet
    {
        public Sprite logo;
        public Sprite mascotSix;
        public Sprite mascotSeven;
        public Sprite mascotThree;
        public Sprite heroBoy;
        public Sprite heroGirl;
        public Sprite playerPortrait;
        public Sprite opponent;
        public Sprite primaryButton;
        public Sprite secondaryBlueButton;
        public Sprite secondaryMagentaButton;
        public Sprite neutralPanel;
        public Sprite playerChip;
        public Sprite chevron;
        public Sprite backButton;

        public bool IsComplete =>
            logo != null && mascotSix != null && mascotSeven != null &&
            heroBoy != null && heroGirl != null && playerPortrait != null &&
            primaryButton != null &&
            secondaryBlueButton != null && secondaryMagentaButton != null &&
            neutralPanel != null && playerChip != null && chevron != null;
    }

    [Serializable]
    public sealed class SplashArtSet
    {
        public Sprite background;
        public Sprite heroBoy;
        public Sprite heroGirl;
        public Sprite stars;
        public Sprite lightning;
        public Sprite confetti;
        public Sprite numbers;
        public Sprite logoGlow;
        public Sprite loadingTrack;

        public bool IsComplete => background != null && heroBoy != null &&
                                  heroGirl != null && loadingTrack != null;
    }

    [Serializable]
    public sealed class HomeArtSet
    {
        public Sprite background;
        public Sprite heroBoy;
        public Sprite heroGirl;
        public Sprite settingsGear;
        public Sprite soloIcon;
        public Sprite privateRoomIcon;
        public Sprite dailyHuntIcon;
        public Sprite streakIcon;
        public Sprite tipIcon;

        public bool IsComplete => background != null && heroBoy != null &&
                                  heroGirl != null && settingsGear != null &&
                                  soloIcon != null && privateRoomIcon != null &&
                                  dailyHuntIcon != null && tipIcon != null;
    }

    [Serializable]
    public sealed class SettingsArtSet
    {
        public Sprite background;
        public Sprite playerIcon;
        public Sprite languageIcon;
        public Sprite musicIcon;
        public Sprite difficultyIcon;
        public Sprite privacyIcon;
        public Sprite blueButton;
        public Sprite goldButton;
        public Sprite neutralButton;
        public Sprite playerChip;
        public Sprite chevron;

        public bool IsComplete => background != null && playerIcon != null &&
                                  languageIcon != null && musicIcon != null &&
                                  difficultyIcon != null && privacyIcon != null &&
                                  blueButton != null && goldButton != null &&
                                  neutralButton != null && playerChip != null &&
                                  chevron != null;
    }

    [Header("Typography")]
    public TypographySet typography = new TypographySet();

    [Header("Approved production artwork")]
    public SharedArtSet shared = new SharedArtSet();
    public SplashArtSet splash = new SplashArtSet();
    public HomeArtSet home = new HomeArtSet();
    public SettingsArtSet settings = new SettingsArtSet();

    [Header("Layout tokens (1080 x 1920 reference)")]
    public Vector2 referenceResolution = new Vector2(1080f, 1920f);
    public float safeMargin = 48f;
    public float compactGap = 16f;
    public float standardGap = 24f;
    public float sectionGap = 40f;
    public float minimumTouchTarget = 88f;

    public bool IsComplete => typography != null && typography.IsComplete &&
                              shared != null && shared.IsComplete &&
                              splash != null && splash.IsComplete &&
                              home != null && home.IsComplete &&
                              settings != null && settings.IsComplete;
}
