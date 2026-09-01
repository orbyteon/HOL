using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class OnboardingFlowPlayModeTests
{
    const string PlayerNameKey = "PlayerName";
    const string VersionKey = "HOL.Onboarding.Version";
    const string GenderKey = "HOL.Onboarding.Gender";
    const string AvatarKey = "HOL.Onboarding.Avatar";
    const string AgeKey = "HOL.Onboarding.AgeCategory";

    readonly string[] keys =
    {
        PlayerNameKey, VersionKey, GenderKey, AvatarKey, AgeKey,
    };

    bool[] hadKey;
    string savedName;
    int[] savedInts;

    [SetUp]
    public void SetUp()
    {
        hadKey = new bool[keys.Length];
        savedInts = new int[keys.Length];
        for (int index = 0; index < keys.Length; index++)
        {
            hadKey[index] = PlayerPrefs.HasKey(keys[index]);
            if (index == 0)
                savedName = PlayerPrefs.GetString(keys[index], string.Empty);
            else
                savedInts[index] = PlayerPrefs.GetInt(keys[index], 0);
            PlayerPrefs.DeleteKey(keys[index]);
        }
        PlayerPrefs.Save();
    }

    [TearDown]
    public void TearDown()
    {
        for (int index = 0; index < keys.Length; index++)
        {
            if (!hadKey[index])
            {
                PlayerPrefs.DeleteKey(keys[index]);
                continue;
            }

            if (index == 0)
                PlayerPrefs.SetString(keys[index], savedName);
            else
                PlayerPrefs.SetInt(keys[index], savedInts[index]);
        }
        PlayerPrefs.Save();
    }

    [UnityTest]
    public IEnumerator FreshPlayerGetsOneFunctionalFiveStepFlow()
    {
        yield return SceneManager.LoadSceneAsync(
            "SplashScene", LoadSceneMode.Single);
        yield return null;
        yield return null;

        Scene scene = SceneManager.GetActiveScene();
        Assert.That(scene.name, Is.EqualTo("SplashScene"));
        Assert.That(ComponentsInScene(scene, RuntimeType("SplashDesign")),
            Has.Count.EqualTo(1), "SplashDesign must remain the one presentation owner.");
        Assert.That(ComponentsInScene(scene, RuntimeType("SplashOnboardingController")),
            Has.Count.EqualTo(1), "The flow must have one functional state owner.");

        Transform root = Find(scene, "HOLOnboardingRoot");
        Assert.That(root, Is.Not.Null);
        Assert.That(Find(scene, "SplashVisualRoot"), Is.Null,
            "The returning-player splash presentation must not compete with onboarding.");

        string[] screens =
        {
            "OnboardingWelcomeScreen",
            "OnboardingNameScreen",
            "OnboardingGenderScreen",
            "OnboardingAvatarScreen",
            "OnboardingAgeScreen",
        };
        foreach (string screen in screens)
            Assert.That(Find(root, screen), Is.Not.Null, screen + " is missing.");
        Assert.That(ActiveScreenCount(root, screens), Is.EqualTo(1));

        MonoBehaviour loader = (MonoBehaviour)ComponentsInScene(
            scene, RuntimeType("SplashLoader"))[0];
        Assert.That(loader.IsInvoking(), Is.False,
            "First-run onboarding must not schedule the returning-player timer.");

        Click(root, "WelcomeContinue");
        yield return null;
        Assert.That(Find(root, "OnboardingNameScreen").gameObject.activeSelf, Is.True);

        TMP_InputField nameInput = Find(root, "OnboardingNameInput")
            .GetComponent<TMP_InputField>();
        Button nameContinue = Find(root, "NameContinue").GetComponent<Button>();
        nameInput.text = "ab";
        yield return null;
        Assert.That(nameContinue.interactable, Is.False,
            "The real Continue button must reject an invalid nickname.");
        nameInput.text = "Marinos";
        yield return null;
        Assert.That(nameContinue.interactable, Is.True);
        Assert.That(Find(root, "NameCounter").GetComponent<TMP_Text>().text,
            Is.EqualTo("7 / 12"));

        nameContinue.onClick.Invoke();
        yield return null;
        Assert.That(Find(root, "OnboardingGenderScreen").gameObject.activeSelf, Is.True);
        Click(root, "GenderCard1");
        Assert.That(Find(root, "GenderContinue").GetComponent<Button>().interactable,
            Is.True);
        Click(root, "GenderContinue");
        yield return null;

        Assert.That(Find(root, "OnboardingAvatarScreen").gameObject.activeSelf, Is.True);
        Assert.That(Descendants(root, "AvatarCard"), Has.Count.EqualTo(12));
        Assert.That(Find(root, "AvatarContinue").GetComponent<Button>()
            .interactable, Is.False,
            "Avatar must begin unselected and mandatory.");
        Click(root, "AvatarCard1");
        Click(root, "AvatarContinue");
        yield return null;

        Assert.That(Find(root, "OnboardingAgeScreen").gameObject.activeSelf, Is.True);
        Click(root, "AgeCard2");
        Button finalContinue = Find(root, "AgeContinue").GetComponent<Button>();
        Assert.That(finalContinue.interactable, Is.True);
        finalContinue.onClick.Invoke();

        float timeout = Time.realtimeSinceStartup + 5f;
        while (SceneManager.GetActiveScene().name == "SplashScene" &&
               Time.realtimeSinceStartup < timeout)
            yield return null;

        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        Assert.That(PlayerPrefs.GetString(PlayerNameKey), Is.EqualTo("Marinos"));
        Assert.That(PlayerPrefs.GetInt(GenderKey), Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt(AvatarKey), Is.EqualTo(0));
        Assert.That(PlayerPrefs.GetInt(AgeKey), Is.EqualTo(2));
        Assert.That(PlayerPrefs.GetInt(VersionKey), Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator NavigationProgressAndEligibilityStayBounded()
    {
        yield return LoadFreshOnboarding();
        Scene scene = SceneManager.GetActiveScene();
        Transform root = Find(scene, "HOLOnboardingRoot");
        Component controller = ComponentsInScene(
            scene, RuntimeType("SplashOnboardingController"))[0];

        Assert.That(ReadInt(controller, "CurrentStageNumber"), Is.EqualTo(1));
        Click(root, "WelcomeContinue");
        yield return null;
        Assert.That(ReadInt(controller, "CurrentStageNumber"), Is.EqualTo(2));
        Click(root, "OnboardingBack");
        yield return null;
        Assert.That(ReadInt(controller, "CurrentStageNumber"), Is.EqualTo(1));

        Click(root, "WelcomeContinue");
        TMP_InputField input = Find(root, "OnboardingNameInput")
            .GetComponent<TMP_InputField>();
        input.text = "Marinos";
        yield return null;
        Click(root, "NameContinue");
        yield return null;

        Assert.That(ReadInt(controller, "CurrentStageNumber"), Is.EqualTo(3));
        Assert.That(ReadInt(controller, "SelectedGender"), Is.EqualTo(-1));
        Assert.That(Find(root, "GenderContinue").GetComponent<Button>()
            .interactable, Is.False);
        Assert.That(Descendants(
            Find(root, "OnboardingGenderScreen"),
            "OnboardingProgressNode"), Has.Count.EqualTo(5));

        Click(root, "OnboardingGenderSkip");
        yield return null;
        Assert.That(ReadInt(controller, "SelectedGender"), Is.EqualTo(2));
        Assert.That(ReadInt(controller, "CurrentStageNumber"), Is.EqualTo(4));
        Assert.That(Find(root, "AvatarContinue").GetComponent<Button>()
            .interactable, Is.False);
        Assert.That(Find(root, "AvatarCard4").GetComponent<Button>()
            .interactable, Is.False,
            "Coin eligibility must not be invented without a balance source.");
        Assert.That(Find(root, "AvatarCard12").GetComponent<Button>()
            .interactable, Is.False, "Locked avatar must remain unselectable.");

        Find(root, "AvatarCard12").GetComponent<Button>().onClick.Invoke();
        Assert.That(ReadInt(controller, "SelectedAvatar"), Is.EqualTo(-1));
        Click(root, "AvatarCard1");
        Assert.That(ReadInt(controller, "SelectedAvatar"), Is.EqualTo(0));
        Assert.That(Find(root, "AvatarContinue").GetComponent<Button>()
            .interactable, Is.True);

        Click(root, "OnboardingBack");
        yield return null;
        Assert.That(ReadInt(controller, "CurrentStageNumber"), Is.EqualTo(3));
        Assert.That(ReadInt(controller, "SelectedGender"), Is.EqualTo(2));
        Click(root, "GenderContinue");
        Click(root, "AvatarContinue");
        yield return null;
        Assert.That(ReadInt(controller, "CurrentStageNumber"), Is.EqualTo(5));
        Assert.That(ReadInt(controller, "SelectedAge"), Is.EqualTo(-1));
        Assert.That(Find(root, "AgeContinue").GetComponent<Button>()
            .interactable, Is.False);
        Assert.That(Find(Find(root, "OnboardingAgeScreen"),
            "OnboardingGenderSkip"), Is.Null,
            "Mandatory Age must not expose Skip.");

        TMP_FontAsset display = Resources.Load<TMP_FontAsset>(
            "phase2a/fonts/HOL Menu Display SDF");
        TMP_FontAsset body = Resources.Load<TMP_FontAsset>(
            "phase2a/fonts/HOL Menu Body SDF");
        Assert.That(display.HasCharacter('_'), Is.True);
        Assert.That(body.HasCharacter('_'), Is.True);
    }

    [UnityTest]
    public IEnumerator VisualPolishKeepsMeasuredTargetsAndRedundantSelectionCues()
    {
        yield return LoadFreshOnboarding();
        Scene scene = SceneManager.GetActiveScene();
        Transform root = Find(scene, "HOLOnboardingRoot");

        Transform welcomeScreen = Find(root, "OnboardingWelcomeScreen");
        RectTransform mascotSix = AssertMascotPlacement(
            welcomeScreen, "WelcomeMascotSix", "reference/mascot_6_exact",
            new Vector2(-395f, -405f), new Vector2(265f, 300f));
        RectTransform mascotSeven = AssertMascotPlacement(
            welcomeScreen, "WelcomeMascotSeven", "reference/mascot_7_exact",
            new Vector2(395f, -405f), new Vector2(260f, 300f));
        // Conservative bounds mapped from the approved ensemble sprite into
        // the 1080x1920 content reference space used by both mascots.
        Rect[] humanHeadBounds =
        {
            new Rect(-412.3f, 171.1f, 301.1f, 354.4f),
            new Rect(-153.2f, 133.0f, 297.3f, 323.9f),
            new Rect(90.7f, 152.0f, 285.8f, 343.0f),
            new Rect(-412.3f, -175.7f, 316.3f, 316.3f),
            new Rect(-157.0f, -206.2f, 312.5f, 312.5f),
            new Rect(121.2f, -244.3f, 316.3f, 354.4f),
        };
        AssertNoHeadOverlap(mascotSix, humanHeadBounds);
        AssertNoHeadOverlap(mascotSeven, humanHeadBounds);

        RectTransform welcomeContinue = Find(root, "WelcomeContinue")
            as RectTransform;
        Assert.That(welcomeContinue.rect.size,
            Is.EqualTo(new Vector2(780f, 180f)),
            "A1.2 must not resize the approved Welcome CTA.");
        foreach (string ctaName in new[]
        {
            "NameContinue", "GenderContinue", "AvatarContinue", "AgeContinue",
        })
            AssertPrimaryContinueCta(root, ctaName);

        Click(root, "WelcomeContinue");
        yield return null;
        Button nameContinue = Find(root, "NameContinue").GetComponent<Button>();
        Assert.That(nameContinue.interactable, Is.False,
            "Name must still begin disabled and non-interactable.");
        TMP_Text disabledLabel = Find(nameContinue.transform, "Label")
            .GetComponent<TMP_Text>();
        TMP_Text disabledArrow = Find(nameContinue.transform, "Arrow")
            .GetComponent<TMP_Text>();
        Assert.That(disabledLabel.color.grayscale, Is.GreaterThan(0.70f),
            "Disabled CTA copy must remain readable on the darkened sprite.");
        Assert.That(disabledArrow.color.grayscale, Is.GreaterThan(0.70f),
            "Disabled CTA arrow must remain readable on the darkened sprite.");

        TMP_InputField input = Find(root, "OnboardingNameInput")
            .GetComponent<TMP_InputField>();
        input.text = "Marinos";
        yield return null;
        Click(root, "NameContinue");
        yield return null;

        Transform genderScreen = Find(root, "OnboardingGenderScreen");
        Button genderContinue = Find(genderScreen, "GenderContinue")
            .GetComponent<Button>();
        Assert.That(genderContinue.interactable, Is.False,
            "Gender must still begin unselected.");
        for (int index = 0; index < 3; index++)
            AssertSelectionCue(
                Find(genderScreen, "GenderCard" + index), false);
        Click(genderScreen, "GenderCard0");
        yield return null;
        AssertSelectionCue(Find(genderScreen, "GenderCard0"), true);
        Assert.That(genderContinue.interactable, Is.True);
        Click(genderScreen, "GenderContinue");
        yield return null;

        Transform avatarScreen = Find(root, "OnboardingAvatarScreen");
        for (int index = 0; index < 5; index++)
        {
            RectTransform filter = Find(
                avatarScreen, "AvatarFilter" + index) as RectTransform;
            Assert.That(filter.rect.width, Is.GreaterThanOrEqualTo(170f));
            Assert.That(filter.rect.height, Is.GreaterThanOrEqualTo(82f),
                "Avatar category controls must retain the 720px tap-height floor.");
            TMP_Text label = Find(filter, "Label").GetComponent<TMP_Text>();
            Assert.That(label.fontSizeMax, Is.GreaterThanOrEqualTo(24f));
            Assert.That(label.fontSizeMin, Is.GreaterThanOrEqualTo(20f));
        }

        for (int index = 1; index <= 12; index++)
        {
            Transform card = Find(avatarScreen, "AvatarCard" + index);
            RectTransform rect = card as RectTransform;
            Assert.That(card.gameObject.activeSelf, Is.True,
                "The All filter must keep all 12 avatars visible.");
            Assert.That(rect.rect.width, Is.GreaterThanOrEqualTo(184f));
            Assert.That(rect.rect.height, Is.GreaterThanOrEqualTo(208f));
            TMP_Text availability = Find(card, "Availability")
                .GetComponent<TMP_Text>();
            Assert.That(availability.fontSizeMax, Is.GreaterThanOrEqualTo(24f));
            Assert.That(availability.fontSizeMin, Is.GreaterThanOrEqualTo(20f));
            AssertSelectionCue(card, false);
        }
        Assert.That(Find(avatarScreen, "AvatarContinue").GetComponent<Button>()
            .interactable, Is.False, "Avatar must still begin unselected.");
        Click(avatarScreen, "AvatarCard1");
        yield return null;
        AssertSelectionCue(Find(avatarScreen, "AvatarCard1"), true);
        Assert.That(Find(avatarScreen, "AvatarContinue").GetComponent<Button>()
            .interactable, Is.True);
        Click(avatarScreen, "AvatarContinue");
        yield return null;

        Transform ageScreen = Find(root, "OnboardingAgeScreen");
        Assert.That(Find(ageScreen, "AgeContinue").GetComponent<Button>()
            .interactable, Is.False, "Age must still begin unselected.");
        TMP_Text ageSubtitle = Find(ageScreen, "OnboardingSubtitle")
            .GetComponent<TMP_Text>();
        Assert.That(ageSubtitle.fontSizeMax, Is.GreaterThanOrEqualTo(29f));
        Assert.That(ageSubtitle.fontSizeMin, Is.GreaterThanOrEqualTo(23f));
        RectTransform privacyPanel = Find(ageScreen, "AgePrivacyPanel")
            as RectTransform;
        TMP_Text privacyText = Find(privacyPanel, "AgePrivacyText")
            .GetComponent<TMP_Text>();
        Assert.That(privacyPanel.rect.width, Is.GreaterThanOrEqualTo(900f));
        Assert.That(privacyPanel.rect.height, Is.GreaterThanOrEqualTo(170f));
        Assert.That(privacyText.fontSizeMax, Is.GreaterThanOrEqualTo(28f));
        Assert.That(privacyText.fontSizeMin, Is.GreaterThanOrEqualTo(23f));
        for (int index = 0; index < 3; index++)
            AssertSelectionCue(Find(ageScreen, "AgeCard" + index), false);
        Click(ageScreen, "AgeCard0");
        yield return null;
        AssertSelectionCue(Find(ageScreen, "AgeCard0"), true);
        Assert.That(Find(ageScreen, "AgeContinue").GetComponent<Button>()
            .interactable, Is.True);
    }

    [UnityTest]
    public IEnumerator DeterministicNotchKeepsCriticalControlsInsideSafeRoot()
    {
        yield return LoadFreshOnboarding();
        Scene scene = SceneManager.GetActiveScene();
        Transform root = Find(scene, "HOLOnboardingRoot");
        RectTransform safeRoot = Find(root, "OnboardingSafeAreaRoot")
            .GetComponent<RectTransform>();
        Canvas canvas = safeRoot.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.transform as RectTransform;
        Component safeArea = safeRoot.GetComponent(
            RuntimeType("ResponsiveSafeAreaRoot"));
        MethodInfo apply = safeArea.GetType().GetMethod(
            "ApplyViewport", BindingFlags.Instance | BindingFlags.Public);
        apply.Invoke(safeArea, new object[]
        {
            new Rect(0f, 0f, 1179f, 2556f),
            new Rect(0f, 102f, 1179f, 2352f),
            canvasRect.rect.size,
        });
        Canvas.ForceUpdateCanvases();
        yield return null;
        Rect safeRect = (Rect)safeArea.GetType().GetProperty(
            "LastSafeRect", BindingFlags.Instance | BindingFlags.Public)
            .GetValue(safeArea, null);

        string[] criticalPrefixes =
        {
            "WelcomeContinue", "OnboardingBack", "OnboardingGenderSkip",
            "OnboardingProgressNode", "OnboardingTitle", "NameContinue",
            "GenderContinue", "AvatarContinue", "AgeContinue",
        };
        foreach (string prefix in criticalPrefixes)
        {
            foreach (Transform target in CriticalTargets(root, prefix))
                AssertInside(canvasRect, safeRect, (RectTransform)target,
                    target.name + " escaped the deterministic notch safe area.");
        }
    }

    static IEnumerator LoadFreshOnboarding()
    {
        yield return SceneManager.LoadSceneAsync(
            "SplashScene", LoadSceneMode.Single);
        yield return null;
        yield return null;
    }

    static int ReadInt(Component target, string property)
    {
        return (int)target.GetType().GetProperty(
            property, BindingFlags.Instance | BindingFlags.Public)
            .GetValue(target, null);
    }

    static void AssertInside(
        RectTransform canvasRect,
        Rect safeRect,
        RectTransform target,
        string message)
    {
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
            canvasRect, target);
        const float tolerance = 2f;
        Assert.That(bounds.min.x,
            Is.GreaterThanOrEqualTo(safeRect.xMin - tolerance),
            message);
        Assert.That(bounds.max.x,
            Is.LessThanOrEqualTo(safeRect.xMax + tolerance),
            message);
        Assert.That(bounds.min.y,
            Is.GreaterThanOrEqualTo(safeRect.yMin - tolerance),
            message);
        Assert.That(bounds.max.y,
            Is.LessThanOrEqualTo(safeRect.yMax + tolerance),
            message);
    }

    static void AssertSelectionCue(Transform card, bool selected)
    {
        Assert.That(card, Is.Not.Null);
        Image image = card.GetComponent<Image>();
        Button button = card.GetComponent<Button>();
        Outline outline = card.GetComponent<Outline>();
        Transform badge = Find(card, "SelectedBadge");
        Assert.That(image, Is.Not.Null);
        Assert.That(image.sprite, Is.Not.Null,
            "Selection controls must retain their approved production sprite.");
        Assert.That(image.color.a, Is.EqualTo(1f).Within(0.001f));
        Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
        Assert.That(button, Is.Not.Null);
        Assert.That(button.targetGraphic, Is.SameAs(image));
        Assert.That(outline, Is.Not.Null,
            "Selected state needs an additive outline/glow.");
        Assert.That(outline.enabled, Is.EqualTo(selected));
        Assert.That(badge, Is.Not.Null,
            "Selected state needs a non-color-only check badge.");
        Assert.That(badge.gameObject.activeSelf, Is.EqualTo(selected));
        if (!selected) return;
        Assert.That(outline.effectColor.a, Is.GreaterThanOrEqualTo(0.90f));
        Assert.That(Mathf.Abs(outline.effectDistance.x),
            Is.GreaterThanOrEqualTo(5f));
        Assert.That(Find(badge, "CheckShort").gameObject.activeSelf, Is.True);
        Assert.That(Find(badge, "CheckLong").gameObject.activeSelf, Is.True);
    }

    static RectTransform AssertMascotPlacement(
        Transform root,
        string name,
        string resource,
        Vector2 expectedPosition,
        Vector2 expectedSize)
    {
        RectTransform mascot = Find(root, name) as RectTransform;
        Assert.That(mascot, Is.Not.Null, name + " is missing.");
        Assert.That(mascot.anchoredPosition,
            Is.EqualTo(expectedPosition), name + " position drifted.");
        Assert.That(mascot.rect.size,
            Is.EqualTo(expectedSize), name + " scale must remain approved.");
        Image image = mascot.GetComponent<Image>();
        Assert.That(image, Is.Not.Null);
        Assert.That(image.sprite, Is.SameAs(Resources.Load<Sprite>(resource)),
            name + " must retain its approved production sprite.");
        Assert.That(image.preserveAspect, Is.True);
        Assert.That(image.color.a, Is.EqualTo(1f).Within(0.001f));
        return mascot;
    }

    static void AssertNoHeadOverlap(
        RectTransform mascot, IEnumerable<Rect> humanHeadBounds)
    {
        Rect mascotBounds = RectInParent(mascot);
        int headIndex = 0;
        foreach (Rect headBounds in humanHeadBounds)
        {
            Assert.That(mascotBounds.Overlaps(headBounds), Is.False,
                mascot.name + " overlaps human head " + headIndex + ".");
            headIndex++;
        }
    }

    static void AssertPrimaryContinueCta(Transform root, string name)
    {
        RectTransform rect = Find(root, name) as RectTransform;
        Assert.That(rect, Is.Not.Null, name + " is missing.");
        Assert.That(rect.rect.size, Is.EqualTo(new Vector2(920f, 205f)),
            name + " must use the shared A1.2 primary CTA size.");
        Image image = rect.GetComponent<Image>();
        Button button = rect.GetComponent<Button>();
        Assert.That(image, Is.Not.Null);
        Sprite expectedSprite = Resources.Load<Sprite>(
            "phase2a/hol_cta_gold_r2_9s");
        Assert.That(expectedSprite, Is.Not.Null);
        Assert.That(image.sprite, Is.SameAs(expectedSprite),
            name + " must retain its approved production sprite.");
        Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
        Assert.That(image.color.a, Is.EqualTo(1f).Within(0.001f));
        Assert.That(button, Is.Not.Null);
        Assert.That(button.targetGraphic, Is.SameAs(image));

        TMP_Text label = Find(rect, "Label").GetComponent<TMP_Text>();
        TMP_Text arrow = Find(rect, "Arrow").GetComponent<TMP_Text>();
        Assert.That(label.alignment, Is.EqualTo(TextAlignmentOptions.Center));
        Assert.That(arrow.alignment, Is.EqualTo(TextAlignmentOptions.Center));
        Assert.That(Mathf.Abs(label.rectTransform.anchoredPosition.y),
            Is.LessThanOrEqualTo(4f), name + " label must remain centered.");
        Assert.That(Mathf.Abs(arrow.rectTransform.anchoredPosition.y),
            Is.LessThanOrEqualTo(4f), name + " arrow must remain centered.");
        AssertContained(rect.rect, RectInParent(label.rectTransform),
            name + " label escaped the CTA bounds.");
        AssertContained(rect.rect, RectInParent(arrow.rectTransform),
            name + " arrow escaped the CTA bounds.");
    }

    static void AssertContained(Rect outer, Rect inner, string message)
    {
        Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin), message);
        Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax), message);
        Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin), message);
        Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax), message);
    }

    static Rect RectInParent(RectTransform target)
    {
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Transform parent = target.parent;
        Vector3 bottomLeft = parent.InverseTransformPoint(corners[0]);
        Vector3 topRight = parent.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(
            bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
    }

    static List<Transform> CriticalTargets(
        Transform root, string nameOrProgressPrefix)
    {
        var found = new List<Transform>();
        bool isProgressPrefix =
            nameOrProgressPrefix == "OnboardingProgressNode";
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (isProgressPrefix
                    ? child.name.StartsWith(
                        nameOrProgressPrefix, StringComparison.Ordinal)
                    : child.name == nameOrProgressPrefix)
                found.Add(child);
        return found;
    }

    static void Click(Transform root, string name)
    {
        Transform target = Find(root, name);
        Assert.That(target, Is.Not.Null, name + " is missing.");
        Button button = target.GetComponent<Button>();
        Assert.That(button, Is.Not.Null, name + " is not a real Button.");
        Assert.That(button.interactable, Is.True, name + " is disabled.");
        button.onClick.Invoke();
    }

    static int ActiveScreenCount(Transform root, IEnumerable<string> names)
    {
        int count = 0;
        foreach (string name in names)
        {
            Transform screen = Find(root, name);
            if (screen != null && screen.gameObject.activeSelf) count++;
        }
        return count;
    }

    static List<Transform> Descendants(Transform root, string namePrefix)
    {
        var found = new List<Transform>();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name.StartsWith(namePrefix, StringComparison.Ordinal))
                found.Add(child);
        return found;
    }

    static Transform Find(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = Find(root.transform, name);
            if (found != null) return found;
        }
        return null;
    }

    static Transform Find(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }

    static List<Component> ComponentsInScene(Scene scene, Type type)
    {
        var found = new List<Component>();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Component component in root.GetComponentsInChildren(type, true))
                found.Add(component);
        return found;
    }

    static Type RuntimeType(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, name + " runtime type is missing.");
        return type;
    }
}
