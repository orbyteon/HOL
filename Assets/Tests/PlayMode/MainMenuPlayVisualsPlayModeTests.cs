using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class MainMenuPlayVisualsPlayModeTests
{
    const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    static readonly Vector2Int[] RequiredViewports =
    {
        new Vector2Int(720, 1280),
        new Vector2Int(1080, 1920),
        new Vector2Int(1080, 2400),
        new Vector2Int(1179, 2556),
    };

    [UnityTearDown]
    public IEnumerator RestoreFixtureState()
    {
        SetLanguage(0);
        Scene active = SceneManager.GetActiveScene();
        Scene quiescent = SceneManager.CreateScene(
            "MainMenuPlayVisualsQuiescent");
        SceneManager.SetActiveScene(quiescent);
        if (active.IsValid() && active.isLoaded &&
            active.handle != quiescent.handle)
            yield return SceneManager.UnloadSceneAsync(active);
        yield return null;
#if UNITY_EDITOR
        FirstLaunchSoloEndToEndPlayModeTests
            .RestoreEditorWindowAfterSettlement();
#endif
    }

    [UnityTest]
    public IEnumerator PlayHubExposesOnlyAuthoritativeSoloAndPrivateRoomRoutes()
    {
        yield return LoadReadyMainMenu();

        Component menu = FindInScene(RuntimeType("MenuManager"));
        GameObject panelPlay = GetField<GameObject>(menu, "panelPlay");
        GameObject mainMenu = GetField<GameObject>(menu, "mainMenuPanel");
        GameObject searching = GetField<GameObject>(menu, "panelSearching");
        Component matchmaking = FindInScene(RuntimeType("FakeMatchmaking"));
        GameObject panelGame = GetField<GameObject>(matchmaking, "panelGame");
        Component pvp = FindInScene(RuntimeType("PvpGameController"));
        GameObject pvpMenu = GetField<GameObject>(pvp, "pvpMenuPanel");
        Component playVisuals = FindInScene(RuntimeType("MainMenuPlayVisuals"));
        Canvas canvas = playVisuals.GetComponent<Canvas>();
        Assert.That(canvas, Is.Not.Null);

        Assert.That(mainMenu.activeSelf, Is.True);
        Assert.That(panelPlay.activeSelf, Is.False);
        Assert.That(searching.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.False);
        Assert.That(pvpMenu.activeSelf, Is.False);
        Assert.That(CountInScene(RuntimeType("SoloSearchVisuals")), Is.Zero,
            "The retired Solo search owner must stay absent.");

        Button homePlay = Find(canvas.transform, "ButtonPlay").GetComponent<Button>();
        Assert.That(PersistentMethods(homePlay), Does.Contain("OnPlayPressed"));
        homePlay.onClick.Invoke();
        yield return null;

        Component owner = FindInScene(RuntimeType("MainMenuPlayVisuals"));
        Assert.That(owner, Is.Not.Null);
        Assert.That(GetProperty<bool>(owner, "IsReady"), Is.True);
        Assert.That(GetProperty<bool>(owner, "IsSettled"), Is.True);
        Assert.That(CountInScene(RuntimeType("MainMenuPlayVisuals")), Is.EqualTo(1),
            "PanelPlay must have exactly one presentation owner.");
        Assert.That(mainMenu.activeSelf, Is.False);
        Assert.That(panelPlay.activeSelf, Is.True);
        Assert.That(searching.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.False,
            "Opening the hub cannot begin Solo.");
        Assert.That(pvpMenu.activeSelf, Is.False,
            "Opening the hub cannot open Private Room.");

        Transform root = Find(panelPlay.transform, "PlayVisualRoot");
        Transform safe = Find(root, "PlaySafeAreaRoot");
        Assert.That(root, Is.Not.Null);
        Assert.That(root.gameObject.activeInHierarchy, Is.True);
        Assert.That(safe, Is.Not.Null);
        foreach (string required in new[]
        {
            "PlayBackground", "PlayDecorations", "PlayLogo",
            "PlayTitleRibbon", "PlayHubTitle", "PlayHubSubtitle",
            "ButtonChallenger", "PlaySoloIcon", "PlaySoloTitle",
            "PlaySoloSubtitle", "PlaySoloAction", "ButtonPvP",
            "PlayFriendIcon", "PlayFriendTitle", "PlayFriendSubtitle",
            "PlayFriendAction", "ButtonBack", "PlayMascotSeven",
            "PlayMascotThree",
        })
        {
            Assert.That(Find(root, required), Is.Not.Null,
                "Missing Play Hub object: " + required);
        }

        foreach (string retired in new[]
        {
            "PlayDisclosure", "DisclosureLabel", "PlayFindIcon",
            "ExactPlayLogo",
        })
        {
            Transform found = Find(panelPlay.transform, retired);
            Assert.That(found == null || !found.gameObject.activeInHierarchy, Is.True,
                "Retired selector presentation remains visible: " + retired);
        }

        Button solo = Find(safe, "ButtonChallenger").GetComponent<Button>();
        Button friend = Find(safe, "ButtonPvP").GetComponent<Button>();
        Button back = Find(safe, "ButtonBack").GetComponent<Button>();
        AssertProductionButton(
            solo, "solo/production/solo_player_card_shell_v1");
        AssertProductionButton(
            friend, "solo/production/solo_opponent_card_shell_v1");
        AssertProductionButton(
            back, "solo/production/solo_back_button_v1");
        Assert.That(safe.GetComponentsInChildren<Button>(false), Has.Length.EqualTo(3),
            "Selector must expose exactly VS AI, one Private Room route, and Back.");
        Assert.That(root.GetComponentsInChildren<TMP_Text>(false), Has.Length.EqualTo(8),
            "Selector must expose one heading/helper and three live labels per real mode.");
        Assert.That(CountNamedButtons(canvas.transform, "ButtonPvP"), Is.EqualTo(1),
            "There must be exactly one active Private Room/PvP entry.");
        Assert.That(CountNamedButtons(canvas.transform, "ButtonPrivateRoom"), Is.Zero,
            "The duplicate friend entry must not be recreated.");
        Assert.That(PersistentMethods(solo), Does.Contain("StartSearch"),
            "VS AI must preserve the scene-authored authoritative Solo callback.");
        foreach (Button candidate in safe.GetComponentsInChildren<Button>(true))
        {
            if (candidate == solo) continue;
            Assert.That(PersistentMethods(candidate), Does.Not.Contain("StartSearch"),
                candidate.name + " must not duplicate the Solo route.");
        }

        AssertLocalizedHubCopy(root, 0,
            "CHOOSE A MODE", "What do you want to play?",
            "VS AI", "A number duel against the computer",
            "PLAY WITH A FRIEND", "Create or join a private room", "Play");
        AssertLocalizedHubCopy(root, 1,
            "ΔΙΑΛΕΞΕ ΤΡΟΠΟ", "Τι θέλεις να παίξεις;",
            "ΕΝΑΝΤΙΟΝ AI", "Μονομαχία αριθμών με τον υπολογιστή",
            "ΠΑΙΞΕ ΜΕ ΦΙΛΟ", "Δημιούργησε ή μπες σε ιδιωτικό δωμάτιο", "Παίξε");
        SetLanguage(0);

        string visibleCopy = VisibleCopy(root).ToUpperInvariant();
        foreach (string unavailable in new[]
        {
            "QUICK MATCH", "PUBLIC DUEL", "RANKED", "MATCHMAKING",
        })
        {
            Assert.That(visibleCopy, Does.Not.Contain(unavailable),
                "Play Hub must not promise an unavailable mode.");
        }

        // Back closes only the selector and restores the unchanged Home.
        back.onClick.Invoke();
        yield return null;
        Assert.That(panelPlay.activeSelf, Is.False);
        Assert.That(mainMenu.activeSelf, Is.True);
        Assert.That(searching.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.False);
        Assert.That(pvpMenu.activeSelf, Is.False);
        Assert.That(Find(canvas.transform, "HomeVisualRoot").gameObject.activeSelf,
            Is.True);

        // The one friend entry invokes the existing room controller and leaves
        // Home active behind its modal, so the controller's real Close returns Home.
        homePlay.onClick.Invoke();
        yield return null;
        friend.onClick.Invoke();
        yield return null;
        Assert.That(panelPlay.activeSelf, Is.False);
        Assert.That(mainMenu.activeSelf, Is.True);
        Assert.That(pvpMenu.activeSelf, Is.True,
            "Friend must route through PvpGameController.OpenPvpMenu().");
        Assert.That(Find(pvpMenu.transform, "CreateButton").GetComponent<Button>().interactable,
            Is.True);
        Assert.That(Find(pvpMenu.transform, "JoinButton").GetComponent<Button>().interactable,
            Is.True);
        pvp.SendMessage("ClosePvpMenu", SendMessageOptions.RequireReceiver);
        yield return null;
        Assert.That(pvpMenu.activeSelf, Is.False);
        Assert.That(mainMenu.activeSelf, Is.True);

        // Reopen the hub and verify VS AI reaches the existing real Solo board
        // without ever showing the retired search presentation.
        homePlay.onClick.Invoke();
        yield return null;
        solo.onClick.Invoke();
        yield return null;
        Assert.That(panelPlay.activeSelf, Is.False);
        Assert.That(mainMenu.activeSelf, Is.False);
        Assert.That(searching.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.True,
            "VS AI must route through the authoritative Solo entry.");
        Assert.That(pvpMenu.activeSelf, Is.False);
        Assert.That(IsPreparing(matchmaking), Is.True);
        for (int frame = 0; frame < 120 && IsPreparing(matchmaking); frame++)
            yield return null;
        Assert.That(IsPreparing(matchmaking), Is.False,
            "Solo preparation did not complete after selector entry.");
        Assert.That(panelGame.activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator PlayHubLocalizedGlyphsRemainContainedAtAllRequiredViewports()
    {
        yield return LoadReadyMainMenu();

        Component menu = FindInScene(RuntimeType("MenuManager"));
        Component playVisuals = FindInScene(RuntimeType("MainMenuPlayVisuals"));
        Assert.That(playVisuals, Is.Not.Null, "Missing MainMenuPlayVisuals owner.");
        Canvas canvas = playVisuals.GetComponent<Canvas>();
        Assert.That(canvas, Is.Not.Null, "Play Hub owner must share the UI canvas.");
        Transform homePlayTransform = Find(canvas.transform, "ButtonPlay");
        Assert.That(homePlayTransform, Is.Not.Null, "Missing Home PLAY gateway.");
        Button homePlay = homePlayTransform.GetComponent<Button>();
        Assert.That(homePlay, Is.Not.Null, "Home PLAY gateway lost its Button.");
        homePlay.onClick.Invoke();
        yield return null;

        Component owner = FindInScene(RuntimeType("MainMenuPlayVisuals"));
        Assert.That(owner, Is.Not.Null,
            "MainMenuPlayVisuals owner disappeared after opening the selector.");
        Transform root = Find(owner.transform, "PlayVisualRoot");
        Assert.That(root, Is.Not.Null, "Missing PlayVisualRoot.");
        RectTransform safe = Find(root, "PlaySafeAreaRoot") as RectTransform;
        Assert.That(safe, Is.Not.Null, "Missing PlaySafeAreaRoot.");
        RectTransform solo = Find(safe, "ButtonChallenger") as RectTransform;
        RectTransform friend = Find(safe, "ButtonPvP") as RectTransform;
        RectTransform back = Find(safe, "ButtonBack") as RectTransform;
        Assert.That(solo, Is.Not.Null, "Missing authoritative VS AI button.");
        Assert.That(friend, Is.Not.Null, "Missing authoritative Private Room button.");
        Assert.That(back, Is.Not.Null, "Missing selector Back button.");
        TMP_Text hubTitle = RequireText(root, "PlayHubTitle");
        TMP_Text hubSubtitle = RequireText(root, "PlayHubSubtitle");
        TMP_Text soloTitle = RequireText(root, "PlaySoloTitle");
        TMP_Text soloSubtitle = RequireText(root, "PlaySoloSubtitle");
        TMP_Text soloAction = RequireText(root, "PlaySoloAction");
        TMP_Text friendTitle = RequireText(root, "PlayFriendTitle");
        TMP_Text friendSubtitle = RequireText(root, "PlayFriendSubtitle");
        TMP_Text friendAction = RequireText(root, "PlayFriendAction");
        TMP_Text[] texts =
        {
            hubTitle, hubSubtitle, soloTitle, soloSubtitle,
            soloAction, friendTitle, friendSubtitle, friendAction,
        };
        AssertApprovedTitleApertures(owner, soloTitle, friendTitle, "initial");
        MethodInfo applyViewport = owner.GetType().GetMethod(
            "ApplyResponsiveLayoutForViewport", InstanceFlags);
        Assert.That(applyViewport, Is.Not.Null,
            "Play Hub must expose its deterministic responsive-layout seam.");

        for (int language = 0; language <= 1; language++)
        {
            SetLanguage(language);
            yield return null;
            foreach (Vector2Int viewport in RequiredViewports)
            {
                string lane = (language == 0 ? "EN " : "EL ") +
                              viewport.x + "x" + viewport.y;
                applyViewport.Invoke(owner, new object[]
                {
                    viewport.x, viewport.y, true,
                });
                Canvas.ForceUpdateCanvases();
                foreach (TMP_Text text in texts)
                {
                    text.ForceMeshUpdate(true, true);
                    Assert.That(text.isTextOverflowing, Is.False,
                        lane + " " + text.name);
                    Assert.That(text.textInfo.characterCount, Is.GreaterThan(0),
                        lane + " " + text.name);
                }

                AssertApprovedTitleApertures(owner, soloTitle, friendTitle, lane);
                AssertContained(safe.rect, GlyphBounds(hubTitle, safe), 28f,
                    lane + " hub title");
                AssertContained(safe.rect, GlyphBounds(hubSubtitle, safe), 28f,
                    lane + " hub subtitle");
                AssertContained(solo.rect, GlyphBounds(soloTitle, solo), 20f,
                    lane + " VS AI title");
                AssertContained(solo.rect, GlyphBounds(soloSubtitle, solo), 16f,
                    lane + " VS AI subtitle");
                AssertContained(friend.rect, GlyphBounds(friendTitle, friend), 20f,
                    lane + " friend title");
                AssertContained(friend.rect, GlyphBounds(friendSubtitle, friend), 16f,
                    lane + " friend subtitle");
                AssertContained(solo.rect, GlyphBounds(soloAction, solo), 16f,
                    lane + " VS AI action");
                AssertContained(friend.rect, GlyphBounds(friendAction, friend), 16f,
                    lane + " friend action");

                Assert.That(hubTitle.fontSize, Is.GreaterThanOrEqualTo(42f), lane);
                Assert.That(hubSubtitle.fontSize, Is.GreaterThanOrEqualTo(24f), lane);
                Assert.That(soloTitle.fontSize, Is.GreaterThanOrEqualTo(32f), lane);
                Assert.That(friendTitle.fontSize, Is.GreaterThanOrEqualTo(24f), lane);
                Assert.That(soloSubtitle.fontSize, Is.GreaterThanOrEqualTo(21f), lane);
                Assert.That(friendSubtitle.fontSize, Is.GreaterThanOrEqualTo(21f), lane);
                Assert.That(soloAction.fontSize, Is.GreaterThanOrEqualTo(36f), lane);
                Assert.That(friendAction.fontSize, Is.GreaterThanOrEqualTo(36f), lane);

                AssertRectSize(solo, new Vector2(560f, 920f), lane + " VS AI");
                AssertRectSize(friend, new Vector2(560f, 920f), lane + " friend");
                AssertRectSize(back, new Vector2(118f, 118f), lane + " Back");
                AssertHorizontalSeparation(solo, friend, lane + " mode cards");
                Assert.That(solo.sizeDelta.y, Is.GreaterThanOrEqualTo(48f));
                Assert.That(friend.sizeDelta.y, Is.GreaterThanOrEqualTo(48f));
                Assert.That(back.sizeDelta.y, Is.GreaterThanOrEqualTo(48f));
            }
        }
    }

    static void AssertApprovedTitleApertures(
        Component owner, TMP_Text soloTitle, TMP_Text friendTitle, string lane)
    {
        MainMenuHomeVisualsPlayModeTests.AssertApprovedCenteredTextRegion(
            owner, soloTitle, new Vector2(-16f, 361.6f),
            new Vector2(210f, 86.9f), new Vector2(210f, 118.9f),
            0f, lane + " VS AI title aperture");
        MainMenuHomeVisualsPlayModeTests.AssertApprovedCenteredTextRegion(
            owner, friendTitle, new Vector2(7f, 361.6f),
            new Vector2(210f, 86.9f), new Vector2(210f, 118.9f),
            0f, lane + " friend title aperture");
    }

    static IEnumerator LoadReadyMainMenu()
    {
#if UNITY_EDITOR
        FirstLaunchSoloEndToEndPlayModeTests
            .FocusGameViewForEndOfFrameSettlement();
#endif
        SetLanguage(0);
        InvokeInstaller("MainMenuHomeVisuals");
        InvokeInstaller("MainMenuPlayVisuals");
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        Component homeOwner = null;
        Component playOwner = null;
        for (int frame = 0; frame < 180; frame++)
        {
            homeOwner = FindInScene(RuntimeType("MainMenuHomeVisuals"));
            playOwner = FindInScene(RuntimeType("MainMenuPlayVisuals"));
            if (homeOwner != null && playOwner != null &&
                GetProperty<bool>(homeOwner, "IsReady") &&
                GetProperty<bool>(homeOwner, "IsSettled") &&
                GetProperty<bool>(playOwner, "IsReady") &&
                GetProperty<bool>(playOwner, "IsSettled"))
                break;
            yield return null;
        }

        Assert.That(homeOwner, Is.Not.Null);
        Assert.That(playOwner, Is.Not.Null);
        Assert.That(GetProperty<bool>(homeOwner, "IsReady"), Is.True);
        Assert.That(GetProperty<bool>(homeOwner, "IsSettled"), Is.True);
        Assert.That(GetProperty<bool>(playOwner, "IsReady"), Is.True);
        Assert.That(GetProperty<bool>(playOwner, "IsSettled"), Is.True);
    }

    static Rect GlyphBounds(TMP_Text text, RectTransform container)
    {
        Vector2 minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        bool found = false;
        TMP_TextInfo info = text.textInfo;
        for (int index = 0; index < info.characterCount; index++)
        {
            TMP_CharacterInfo character = info.characterInfo[index];
            if (!character.isVisible) continue;
            Vector3 bottomLeft = container.InverseTransformPoint(
                text.rectTransform.TransformPoint(character.bottomLeft));
            Vector3 topRight = container.InverseTransformPoint(
                text.rectTransform.TransformPoint(character.topRight));
            minimum = Vector2.Min(minimum, bottomLeft);
            maximum = Vector2.Max(maximum, topRight);
            found = true;
        }

        Assert.That(found, Is.True, text.name + " has no visible glyphs.");
        return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
    }

    static TMP_Text RequireText(Transform root, string name)
    {
        Transform found = Find(root, name);
        Assert.That(found, Is.Not.Null, "Missing selector text: " + name);
        TMP_Text text = found.GetComponent<TMP_Text>();
        Assert.That(text, Is.Not.Null, "Missing TMP component: " + name);
        return text;
    }

    static void AssertContained(Rect outer, Rect inner, float padding, string label)
    {
        Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin + padding),
            label + " left");
        Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax - padding),
            label + " right");
        Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin + padding),
            label + " bottom");
        Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax - padding),
            label + " top");
    }

    static void AssertHorizontalSeparation(
        RectTransform left, RectTransform right, string label)
    {
        Vector4 leftPadding = left.GetComponent<Image>().raycastPadding;
        Vector4 rightPadding = right.GetComponent<Image>().raycastPadding;
        Assert.That(leftPadding, Is.EqualTo(new Vector4(40f, 0f, 40f, 0f)), label);
        Assert.That(rightPadding, Is.EqualTo(leftPadding), label);
        float leftEdge = left.anchoredPosition.x + left.sizeDelta.x * 0.5f - leftPadding.z;
        float rightEdge = right.anchoredPosition.x - right.sizeDelta.x * 0.5f + rightPadding.x;
        Assert.That(leftEdge, Is.LessThan(rightEdge), label);
    }

    static void AssertRectSize(RectTransform rect, Vector2 size, string label)
    {
        Assert.That(rect.sizeDelta.x, Is.EqualTo(size.x).Within(0.01f),
            label + " width");
        Assert.That(rect.sizeDelta.y, Is.EqualTo(size.y).Within(0.01f),
            label + " height");
    }

    static void AssertProductionButton(Button button, string resource)
    {
        Image image = button.GetComponent<Image>();
        Sprite sprite = Resources.Load<Sprite>(resource);
        Assert.That(image, Is.Not.Null, button.name);
        Assert.That(sprite, Is.Not.Null, resource);
        Assert.That(image.sprite, Is.SameAs(sprite), button.name);
        Assert.That(image.type, Is.EqualTo(Image.Type.Simple), button.name);
        Assert.That(image.color.a, Is.EqualTo(1f).Within(0.001f), button.name);
        Assert.That(image.raycastTarget, Is.True, button.name);
        Assert.That(button.targetGraphic, Is.SameAs(image), button.name);
        Assert.That(button.interactable, Is.True, button.name);
        Component juice = button.GetComponent("ButtonJuice");
        Assert.That(juice, Is.Not.Null, button.name + " press feedback");
        object pressedScale = juice.GetType().GetField("pressedScale").GetValue(juice);
        Assert.That((float)pressedScale, Is.EqualTo(0.92f).Within(0.001f),
            button.name + " press scale");
    }

    static void AssertLocalizedHubCopy(
        Transform root,
        int language,
        string heading,
        string helper,
        string solo,
        string soloSubtitle,
        string friend,
        string friendSubtitle,
        string action)
    {
        SetLanguage(language);
        Assert.That(Text(root, "PlayHubTitle"), Is.EqualTo(heading));
        Assert.That(Text(root, "PlayHubSubtitle"), Is.EqualTo(helper));
        Assert.That(Text(root, "PlaySoloTitle"), Is.EqualTo(solo));
        Assert.That(Text(root, "PlaySoloSubtitle"), Is.EqualTo(soloSubtitle));
        Assert.That(Text(root, "PlayFriendTitle"), Is.EqualTo(friend));
        Assert.That(Text(root, "PlayFriendSubtitle"), Is.EqualTo(friendSubtitle));
        Assert.That(Text(root, "PlaySoloAction"), Is.EqualTo(action));
        Assert.That(Text(root, "PlayFriendAction"), Is.EqualTo(action));
    }

    static string Text(Transform root, string name)
    {
        Transform found = Find(root, name);
        Assert.That(found, Is.Not.Null, name);
        return found.GetComponent<TMP_Text>().text;
    }

    static string VisibleCopy(Transform root)
    {
        string copy = string.Empty;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(false))
            copy += "\n" + text.text;
        return copy;
    }

    static bool IsPreparing(Component matchmaking)
    {
        PropertyInfo property = matchmaking.GetType().GetProperty("IsPreparing");
        Assert.That(property, Is.Not.Null);
        return (bool)property.GetValue(matchmaking, null);
    }

    static string[] PersistentMethods(Button button)
    {
        int count = button.onClick.GetPersistentEventCount();
        var methods = new string[count];
        for (int index = 0; index < count; index++)
            methods[index] = button.onClick.GetPersistentMethodName(index);
        return methods;
    }

    static void SetLanguage(int value)
    {
        Type l10n = RuntimeType("L10n");
        Type language = l10n.GetNestedType("Language");
        object enumValue = Enum.ToObject(language, value);
        l10n.GetMethod("SetLanguage", BindingFlags.Static | BindingFlags.Public)
            .Invoke(null, new[] { enumValue });
    }

    static void InvokeInstaller(string typeName)
    {
        Type type = RuntimeType(typeName);
        MethodInfo install = type.GetMethod("Install", StaticFlags);
        Assert.That(install, Is.Not.Null);
        install.Invoke(null, null);
    }

    static int CountInScene(Type type)
    {
        int count = 0;
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            count += root.GetComponentsInChildren(type, true).Length;
        return count;
    }

    static int CountNamedButtons(Transform root, string name)
    {
        int count = 0;
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.name == name)
                count++;
        }
        return count;
    }

    static T GetField<T>(Component component, string name) where T : class
    {
        FieldInfo field = component.GetType().GetField(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Missing field " + name);
        return field.GetValue(component) as T;
    }

    static T GetProperty<T>(Component component, string name)
    {
        PropertyInfo property = component.GetType().GetProperty(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, "Missing property " + name);
        return (T)property.GetValue(component);
    }

    static Component FindInScene(Type type)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Component found = root.GetComponentInChildren(type, true) as Component;
            if (found != null) return found;
        }
        return null;
    }

    static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int index = 0; index < root.childCount; index++)
        {
            Transform found = Find(root.GetChild(index), name);
            if (found != null) return found;
        }
        return null;
    }

    static Type RuntimeType(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime component: " + name);
        return type;
    }
}
