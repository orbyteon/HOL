using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class PvpProductionPresentationPlayModeTests
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly string[] StatKeys = { "StatWins", "StatLosses", "StatStreak", "StatBestStreak",
        "StatBestGuesses", "StatDraws", "StatMatches", "StatRecentBits", "StatRecentCount",
        "Language", "LockIntroShown", "LockEverUsed", "HOL.Onboarding.Avatar", "HOL.Onboarding.Version" };
    readonly Dictionary<string, int?> savedStats = new Dictionary<string, int?>();
    object savedLanguage;
    string savedPlayerName;
    GameObject root;
    Component controller, backend;

    [SetUp]
    public void PreservePlayerState()
    {
        savedPlayerName = PlayerPrefs.HasKey("PlayerName") ? PlayerPrefs.GetString("PlayerName") : null;
        foreach (var key in StatKeys)
            savedStats[key] = PlayerPrefs.HasKey(key) ? (int?)PlayerPrefs.GetInt(key) : null;
        savedLanguage = T("L10n").GetProperty("Current", BindingFlags.Public | BindingFlags.Static).GetValue(null);
    }

    [TearDown]
    public void RestorePlayerState()
    {
        if (root != null) UnityEngine.Object.DestroyImmediate(root);
        if (savedLanguage != null) T("L10n").GetMethod("SetLanguage").Invoke(null, new[] { savedLanguage });
        foreach (var item in savedStats)
            if (item.Value.HasValue) PlayerPrefs.SetInt(item.Key, item.Value.Value);
            else PlayerPrefs.DeleteKey(item.Key);
        if (savedPlayerName == null) PlayerPrefs.DeleteKey("PlayerName");
        else PlayerPrefs.SetString("PlayerName", savedPlayerName);
        PlayerPrefs.Save();
    }

    IEnumerator Build()
    {
        root = new GameObject("PvpPresentationTest", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        // Test scenes do not contain MainMenu's camera. A real display camera
        // is required for native end-of-frame rendering of the overlay Canvas.
        var cameraObject = new GameObject("PvpFixtureCamera", typeof(Camera));
        cameraObject.transform.SetParent(root.transform, false);
        var camera = cameraObject.GetComponent<Camera>();
        camera.cullingMask = 0;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        var ui = root.AddComponent(T("PvpRuntimeUI"));
        ((Behaviour)ui).enabled = false;
        backend = root.AddComponent(T("PvpPresentationFixtureBackend"));
        Invoke(backend, "Configure", false);
        controller = root.AddComponent(T("PvpGameController"));
        Set(controller, "client", backend);
        Invoke(ui, "BuildPanels", controller);
        Assert.That(root.GetComponents(T("PrivateRoomVisuals")).Length, Is.EqualTo(1),
            "The real runtime constructs the sole pre-match owner directly.");
        yield return null;
        yield return null;
        Assert.That((bool)root.GetComponent(T("PvpDuelCartoonVisuals")).GetType().GetProperty("IsReady")
            .GetValue(root.GetComponent(T("PvpDuelCartoonVisuals"))), Is.True);
        Invoke(controller, "OpenPvpMenu");
        yield return null;
    }

    [UnityTest]
    public IEnumerator ExplicitRoomAvatarIdsUseCanonicalCatalogAndNeverTheLocalSelection()
    {
        yield return Build();
        PlayerPrefs.SetInt("HOL.Onboarding.Version", 1);
        PlayerPrefs.SetInt("HOL.Onboarding.Avatar", 6);
        var resolver = T("PlayerProfileAvatarResolver").GetMethod("ResolveId");
        var catalog = T("OnboardingAvatarCatalog");
        int count = (int)catalog.GetProperty("Count").GetValue(null);
        var fallback = Resources.Load<Sprite>("reference/player_cyan_exact");
        Assert.That(fallback, Is.Not.Null);
        for (int id = 0; id < count; id++)
        {
            var entry = catalog.GetMethod("Get").Invoke(null, new object[] { id });
            string resource = (string)entry.GetType().GetProperty("ResourcePath").GetValue(entry);
            bool valid = (bool)catalog.GetMethod("CanEverSelect").Invoke(null, new object[] { id });
            Sprite expected = valid ? Resources.Load<Sprite>(resource) : fallback;
            Assert.That(expected, Is.Not.Null, resource);
            Assert.That(resolver.Invoke(null, new object[] { id.ToString(System.Globalization.CultureInfo.InvariantCulture) }),
                Is.SameAs(expected), "canonical id " + id);
        }
        foreach (string invalid in new[] { null, "", "-1", "11", "12", "01", "0 ", " 0", "1\n", "1.0", "1e0",
            "2147483648", "reference/player_cyan_exact" })
            Assert.That(resolver.Invoke(null, new object[] { invalid }), Is.SameAs(fallback), "invalid " + invalid);
        Assert.That(resolver.Invoke(null, new object[] { "0" }),
            Is.SameAs(Resources.Load<Sprite>("onboarding/avatars/avatar_01_teal_boy")));
        Assert.That(PlayerPrefs.GetInt("HOL.Onboarding.Avatar"), Is.EqualTo(6));
    }

    [UnityTest]
    public IEnumerator RoomIdentitiesStayAuthoritativeAcrossSeatsRefreshRematchAndExit()
    {
        yield return Build();
        var violations = new List<string>();
        foreach (bool host in new[] { true, false })
        {
            Invoke(controller, "OnLeaveMatchPressed");
            PlayerPrefs.SetString("PlayerName", host ? "Marinos" : "Ελένη");
            PlayerPrefs.SetInt("HOL.Onboarding.Version", 1);
            PlayerPrefs.SetInt("HOL.Onboarding.Avatar", host ? 1 : 6);
            if (host)
            {
                Find(root.transform, "CreateButton").GetComponent<Button>().onClick.Invoke();
                ((TMP_InputField)Get(controller, "createSecretInput")).text = "42";
                ((GameObject)Get(controller, "createConfirmButton")).GetComponent<Button>().onClick.Invoke();
            }
            else StartJoined();
            Assert.That((string)Get(backend, "LastName"), Is.EqualTo(host ? "Marinos" : "Ελένη"));
            Assert.That((string)Get(backend, "LastAvatarId"), Is.EqualTo(host ? "1" : "6"));
            var stalePoll = (Delegate)Get(backend, "observer");
            var staleClosed = (Action)Get(backend, "OnRoomClosed");
            var staleLost = (Action)Get(backend, "OnConnectionLost");
            var waiting = State("waiting");
            S(waiting, "hostName", "Marinos"); S(waiting, "guestName", host ? "" : "Ελένη");
            S(waiting, "hostAvatarId", "1"); S(waiting, "guestAvatarId", host ? "" : "6");
            Emit(waiting);
            yield return null;
            var form = ((GameObject)Get(controller, host ? "createPanel" : "joinPanel")).transform;
            Assert.That(Find(form, "WaitingPlayerName").GetComponent<TMP_Text>().text, Is.EqualTo(host ? "Marinos" : "Ελένη"));
            Assert.That(Find(form, "WaitingOpponentAvatar").gameObject.activeSelf, Is.EqualTo(!host),
                "No fabricated opponent while the room has no guest.");
            var live = State("play");
            S(live, "hostName", "Marinos"); S(live, "guestName", "Ελένη");
            Emit(live);
            // Preferences can change while a room exists; its accepted identity
            // must remain the snapshot, including on periodic/language repaint.
            PlayerPrefs.SetString("PlayerName", "Unrelated");
            PlayerPrefs.SetInt("HOL.Onboarding.Avatar", 0);
            string[] requestCounters = { "CreateCalls", "JoinCalls", "GuessCalls", "RematchCalls", "LeaveCalls", "PollCalls", "SignalCalls" };
            int[] beforeRefresh = requestCounters.Select(key => (int)Get(backend, key)).ToArray();
            foreach (string language in new[] { "en", "el" })
            {
                SetLanguage(language);
                Set(root.GetComponent(T("PvpDuelCartoonVisuals")), "nextIdentityRefresh", -1f);
                Set(root.GetComponent(T("PrivateRoomVisuals")), "nextIdentityRefresh", -1f);
                yield return null;
                AssertRoomIdentity(host, "1", "6", "Marinos", "Ελένη");
                AuditGlyphs(language + " identity", violations);
            }
            CollectionAssert.AreEqual(beforeRefresh, requestCounters.Select(key => (int)Get(backend, key)).ToArray(),
                "Periodic and language identity repaint must not send a request or restart polling.");
            var done = State("done");
            S(done, "hostName", "Marinos"); S(done, "guestName", "Ελένη");
            S(done, "winner", "draw");
            Emit(done);
            yield return null;
            AssertRoomIdentity(host, "1", "6", "Marinos", "Ελένη");
            ((TMP_InputField)Get(controller, "rematchSecretInput")).text = "32";
            ((GameObject)Get(controller, "rematchButton")).GetComponent<Button>().onClick.Invoke();
            S(live, "matchIndex", 1);
            Emit(live);
            yield return null;
            AssertRoomIdentity(host, "1", "6", "Marinos", "Ελένη");
            Assert.That(PlayerPrefs.GetString("PlayerName"), Is.EqualTo("Unrelated"));
            Assert.That(PlayerPrefs.GetInt("HOL.Onboarding.Avatar"), Is.Zero);
            Invoke(controller, "OnLeaveMatchPressed");
            StartJoined();
            // Reused room code is deliberately not sufficient: the previous
            // generation's retained callback must not restore the old room.
            stalePoll.DynamicInvoke(live);
            staleClosed();
            staleLost();
            Assert.That(controller.GetType().GetProperty("PresentationState").GetValue(controller), Is.Null);
            var replacement = State("play");
            S(replacement, "hostName", "Zero"); S(replacement, "guestName", "Legacy");
            S(replacement, "hostAvatarId", "0"); S(replacement, "guestAvatarId", null);
            Emit(replacement);
            yield return null;
            AssertRoomIdentity(false, "0", null, "Zero", "Legacy");
        }
        Assert.That(violations, Is.Empty, string.Join("\n", violations));
    }

    void AssertRoomIdentity(bool host, string hostId, string guestId, string hostName, string guestName)
    {
        Sprite mine = ResolveId(host ? hostId : guestId), theirs = ResolveId(host ? guestId : hostId);
        foreach (string name in new[] { "PvpPlayerCharacter", "PvpResultHero", "PvpMatchPlayerChipAvatar", "PvpResultPlayerChipAvatar",
            "PrivateRoomPlayerAvatar", "PvPCreatePanelPlayerAvatar", "PvPJoinPanelPlayerAvatar" })
        {
            var image = Find(root.transform, name).GetComponent<Image>();
            Assert.That(image.sprite, Is.SameAs(mine), name);
            Assert.That(image.raycastTarget, Is.False, name);
            Assert.That(image.preserveAspect, Is.True, name);
        }
        foreach (string name in new[] { "PvpOpponentCharacter", "PvpResultOpponentCharacter", "PvpSignalOpponentAvatar" })
            Assert.That(Find(root.transform, name).GetComponent<Image>().sprite, Is.SameAs(theirs), name);
        foreach (string name in new[] { "PvpPlayerName", "PvpResultPlayerName", "PrivateRoomPlayerName" })
            Assert.That(Find(root.transform, name).GetComponent<TMP_Text>().text, Is.EqualTo(host ? hostName : guestName), name);
        foreach (string name in new[] { "PvpOpponentName", "PvpResultOpponentName" })
            Assert.That(Find(root.transform, name).GetComponent<TMP_Text>().text, Is.EqualTo(host ? guestName : hostName), name);
    }

    static Sprite ResolveId(string id) => (Sprite)T("PlayerProfileAvatarResolver").GetMethod("ResolveId").Invoke(null, new object[] { id });

    [UnityTest]
    public IEnumerator EveryRoomPortraitFitsWaitingMatchAndResultAcrossEnElViewports()
    {
        yield return Build();
        var errors = new List<string>();
        foreach (var viewport in new[] { new Vector2(720, 1280), new Vector2(1080, 1920),
            new Vector2(1080, 2400), new Vector2(1179, 2556) })
        foreach (string language in new[] { "en", "el" })
        foreach (string phase in new[] { "waiting", "play", "done" })
        {
            SetLanguage(language);
            Invoke(controller, "OnLeaveMatchPressed");
            StartJoined();
            var state = State(phase);
            S(state, "hostName", "Κωνσταντίνος"); S(state, "guestName", "Αλεξάνδρα");
            S(state, "winner", phase == "done" ? "draw" : "");
            Emit(state);
            yield return null;
            foreach (var safe in root.GetComponentsInChildren(T("ResponsiveSafeAreaRoot"), true))
                Invoke(safe, "ApplyViewport", new Rect(Vector2.zero, viewport),
                    new Rect(0, 44, viewport.x, viewport.y - 88), new Vector2(1080, 1920));
            Invoke(root.GetComponent(T("PvpDuelCartoonVisuals")), "ApplyResponsiveLayoutForViewport", viewport.x, viewport.y);
            // Exercise every catalog identity in both seats, plus canonical
            // fallback. Measurements below are independent test-owned alpha
            // footprints, not the production framing calculation.
            for (int id = 0; id <= 11; id++)
            {
                S(state, "hostAvatarId", id.ToString());
                S(state, "guestAvatarId", ((id + 6) % 12).ToString());
                Emit(state);
                Canvas.ForceUpdateCanvases();
                string context = language + " " + phase + " " + viewport + " id=" + id;
                foreach (var portrait in root.GetComponentsInChildren<Image>(false))
                {
                    if (!portrait.name.EndsWith("Avatar", StringComparison.Ordinal) &&
                        portrait.name != "PvpPlayerCharacter" && portrait.name != "PvpOpponentCharacter" &&
                        portrait.name != "PvpResultHero" && portrait.name != "PvpResultOpponentCharacter") continue;
                    var aperture = portrait.transform.parent as RectTransform;
                    Assert.That(aperture.GetComponent<Mask>() != null || aperture.GetComponent<RectMask2D>() != null,
                        Is.True, context + " " + portrait.name + " must retain a real mask");
                    PlayerProfileAvatarFramingTestAssertions.AssertLayout(portrait, aperture, context + " " + portrait.name);
                    Assert.That(portrait.raycastTarget, Is.False, context);
                }
                AuditGlyphs(context, errors);
            }
        }
        Assert.That(errors, Is.Empty, string.Join("\n", errors));
    }

    [UnityTest]
    public IEnumerator DirectConstructionHasOneOwnerSharedProfileAndTouchRematch()
    {
        yield return Build();
        Assert.That(root.GetComponents(T("PvpDuelCartoonVisuals")).Length, Is.EqualTo(1));
        Assert.That(Find(root.transform, "PvpDuelCartoonRoot"), Is.Not.Null);
        Assert.That(Find(root.transform, "PvpResultCartoonRoot"), Is.Not.Null);
        Assert.That(Find(root.transform, "PlayerCard"), Is.Null, "Do not build the retired match under the final UI.");
        Assert.That(((GameObject)Get(controller, "keypadRoot")).GetComponentsInChildren<Button>(true).Length, Is.EqualTo(12));
        var input = (TMP_InputField)Get(controller, "rematchSecretInput");
        // TMP 3.0.7 deliberately returns true for both properties on desktop,
        // even when configured false. Only its mobile branch exposes the
        // keyboard flags. Keep that real platform contract, not a fake Editor
        // keyboard override; the source contract also checks keypadOnly=false.
        bool mobile = Application.platform == RuntimePlatform.Android ||
            Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS;
        Assert.That(input.shouldHideSoftKeyboard, Is.EqualTo(!mobile), "Rematch native keyboard platform contract");
        Assert.That(input.shouldHideMobileInput, Is.EqualTo(!mobile), "Rematch native input platform contract");
        Assert.That(input.readOnly, Is.False);
        Assert.That(input.keyboardType, Is.EqualTo(TouchScreenKeyboardType.NumberPad));
        Assert.That(((TMP_InputField)Get(controller, "guessInput")).shouldHideSoftKeyboard, Is.True);
        var selected = (Sprite)T("PlayerProfileAvatarResolver").GetMethod("Resolve").Invoke(null, null);
        foreach (var name in new[] { "PrivateRoomPlayerAvatar", "PvPCreatePanelPlayerAvatar",
            "PvPJoinPanelPlayerAvatar", "PvpMatchPlayerChipAvatar", "PvpResultPlayerChipAvatar" })
        {
            var portrait = Find(root.transform, name).GetComponent<Image>();
            Assert.That(portrait.sprite, Is.SameAs(selected), name);
            Assert.That(portrait.raycastTarget, Is.False);
            Assert.That(portrait.transform.parent.GetComponent<Mask>(), Is.Not.Null);
        }
        string savedName = PlayerPrefs.GetString("PlayerName", "");
        string expectedName = string.IsNullOrWhiteSpace(savedName) ? L("player_default") : savedName;
        foreach (string name in new[] { "PrivateRoomPlayerName", "PvPCreatePanelPlayerName", "PvPJoinPanelPlayerName" })
            Assert.That(Find(root.transform, name).GetComponent<TMP_Text>().text, Is.EqualTo(expectedName),
                "Every pre-match header reads the same saved production name.");
        foreach (string panelField in new[] { "createPanel", "joinPanel" })
        {
            var panel = ((GameObject)Get(controller, panelField)).transform;
            var board = Find(panel, "PrebattleBoard");
            var cancel = Find(panel, "CancelButton");
            Assert.That(board.parent, Is.SameAs(cancel.parent));
            Assert.That(board.GetSiblingIndex(), Is.LessThan(cancel.GetSiblingIndex()),
                "The opaque board must never visually cover Cancel even though it is raycast-transparent.");
        }
    }

    [UnityTest]
    public IEnumerator ApprovedSpritesKeepTheirMeshCornersAndOpaqueNormalFace()
    {
        yield return Build();
        // Both match and result use the approved production Solo artwork.
        var burst = Find(root.transform, "PvpVsBurst").GetComponent<Image>();
        Assert.That(burst.sprite, Is.SameAs(Resources.Load<Sprite>("solo/production/solo_vs_burst_v2")));
        Assert.That(burst.type, Is.EqualTo(Image.Type.Simple));
        Assert.That(burst.color, Is.EqualTo(Color.white));
        foreach (var name in new[] { "PvpResultTrophy" })
        {
            var node = Find(root.transform, name);
            Assert.That(node, Is.Not.Null, name + " must be constructed by its owner");
            var art = node.GetComponent<Image>();
            Assert.That(art, Is.Not.Null, name);
            var sprite = art.sprite;
            Assert.That(sprite, Is.Not.Null, name);
            Assert.That(sprite, Is.SameAs(Resources.Load<Sprite>("solo/production/solo_trophy_v1")), name);
            Assert.That(sprite.vertices.Length, Is.GreaterThan(3), name);
            Assert.That(art.preserveAspect, Is.True, name);
            Assert.That(art.raycastTarget, Is.False, name);
            Assert.That(art.color, Is.EqualTo(Color.white), name);
        }
        foreach (string name in new[] { "PrivateRoomCreateCard", "PrivateRoomJoinCard" })
        {
            var image = Find(root.transform, name).GetComponent<Image>();
            Assert.That(image.sprite, Is.SameAs(Resources.Load<Sprite>(name == "PrivateRoomCreateCard"
                ? "reference/hol_private_create_card_v1" : "reference/hol_private_join_card_v1")), name);
            Assert.That(image.type, Is.EqualTo(Image.Type.Simple), name);
            Assert.That(image.preserveAspect, Is.True, name);
            Assert.That(image.color, Is.EqualTo(Color.white), name);
            Assert.That(image.raycastTarget, Is.False, name);
        }
        Assert.That(Find(root.transform, "PrivateRoomJoinDoor"), Is.Null,
            "An outline-door overlay must not replace or cover the illustrated panel.");
        Assert.That(typeof(MonoBehaviour).IsAssignableFrom(T("PrivateRoomPortraitArtEnvelope")), Is.False,
            "The aspect helper must not install a second scene-wide presentation owner or timer.");
        foreach (string name in new[] { "PrivateRoomBackground", "PvPCreatePanelVisualsBackground", "PvPJoinPanelVisualsBackground" })
        {
            var background = Find(root.transform, name);
            Assert.That(background, Is.Not.Null, name);
            Assert.That(background.GetComponents<AspectRatioFitter>().Length, Is.EqualTo(1), name);
            var fitter = background.GetComponent<AspectRatioFitter>();
            Assert.That(fitter.aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent), name);
            Assert.That(fitter.aspectRatio, Is.EqualTo(1080f / 1920f).Within(.0001f), name);
            Assert.That(background.GetComponent<Image>().sprite,
                Is.SameAs(Resources.Load<Sprite>("solo/production/solo_background_v1")), name);
        }
        Assert.That(Find(root.transform, "PrivateRoomStars"), Is.Null);
        Assert.That(Find(root.transform, "PrivateRoomConfetti"), Is.Null);
        Assert.That(Find(root.transform, "PvpSignalBubbleArtwork").GetComponent<Image>().sprite,
            Is.SameAs(Resources.Load<Sprite>("solo/production/solo_opponent_speech_bubble_v2")));
        foreach (var name in new[] { "LockButton" })
        {
            var image = Find(root.transform, name).GetComponent<Image>();
            Assert.That(image.sprite, Is.Not.Null, name);
            Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), name);
            Assert.That(image.color.a, Is.EqualTo(1f), name);
            var border = image.sprite.border;
            float units = image.pixelsPerUnit * image.pixelsPerUnitMultiplier;
            Assert.That((border.x + border.z) / units,
                Is.LessThan(image.rectTransform.rect.width - 16), name + " horizontal face remains open");
            Assert.That((border.y + border.w) / units,
                Is.LessThan(image.rectTransform.rect.height - 16), name + " vertical face remains open");
        }
    }

    [UnityTest]
    public IEnumerator MatchUsesMeasuredSoloGeometryAssetsAndRealData()
    {
        yield return Build();
        ShowCase("PlayerTurn");
        yield return null;
        var owner = root.GetComponent(T("PvpDuelCartoonVisuals"));
        Invoke(owner, "ApplyResponsiveLayoutForViewport", 1080f, 1920f);
        Invoke(owner, "RefreshMatchPresentation");
        // Independent transcription of the approved Solo production measurements.
        var expected = new[] {
            new { Name="PvpPlayerCard", Art="solo_player_card_shell_v1", X=-276f, Y=472f, W=514f, H=620f },
            new { Name="PvpOpponentCard", Art="solo_opponent_card_shell_v1", X=282f, Y=470f, W=514f, H=620f },
            new { Name="PvpPromptRibbon", Art="solo_prompt_ribbon_v1", X=-18f, Y=86f, W=636f, H=181f },
            new { Name="PvpInteractionCard", Art="solo_interaction_board_v2", X=-189f, Y=-488f, W=760f, H=1004f },
            new { Name="PvpTipCard", Art="solo_tip_board_v1", X=0f, Y=-330f, W=390f, H=260f },
            new { Name="GuessInput", Art="solo_input_field_v1", X=-13f, Y=285f, W=520f, H=140f },
            new { Name="Key1", Art="solo_keypad_key_v1", X=-203f, Y=165f, W=186f, H=108f },
            new { Name="SubmitGuessButton", Art="solo_primary_cta_v1", X=-150f, Y=-385f, W=276f, H=94f },
            new { Name="PvpMatchPlayerChip", Art="solo_player_chip_v1", X=339f, Y=860f, W=370f, H=150f },
        };
        foreach (var item in expected)
        {
            var rect = (RectTransform)Find(root.transform, item.Name);
            Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(item.X, item.Y)), item.Name);
            Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(item.W, item.H)), item.Name);
            var image = rect.GetComponent<Image>();
            Assert.That(image.sprite, Is.SameAs(Resources.Load<Sprite>("solo/production/" + item.Art)), item.Name);
            Assert.That(image.type, Is.EqualTo(Image.Type.Simple), item.Name);
            Assert.That(image.color.a, Is.EqualTo(1), item.Name);
        }
        // Only the history container uses 20px less height, providing the
        // extra PvP Signals control a non-overlapping gap above its title.
        var historyRect = (RectTransform)Find(root.transform, "PvpHistoryCard");
        Assert.That(historyRect.anchoredPosition, Is.EqualTo(Vector2.zero));
        Assert.That(historyRect.sizeDelta, Is.EqualTo(new Vector2(374, 400)));
        Assert.That(historyRect.GetComponent<Image>().sprite,
            Is.SameAs(Resources.Load<Sprite>("solo/production/solo_history_board_v1")));
        Assert.That(Find(root.transform, "PvpDuelCartoonRootStars"), Is.Null);
        Assert.That(Find(root.transform, "PvpDuelCartoonRootOuterFrame"), Is.Null);
        Assert.That(Find(root.transform, "PvpDuelCartoonRootBackground").GetComponent<Image>().sprite,
            Is.SameAs(Resources.Load<Sprite>("solo/production/solo_background_v1")));
        Assert.That(Find(root.transform, "PvpMatchLogo").GetComponent<Image>().preserveAspect, Is.False,
            "Match the approved Solo logo's exact authored face, not a smaller aspect-fit box.");
        Assert.That(Find(root.transform, "Key1").GetComponentInChildren<TMP_Text>().fontSizeMax, Is.EqualTo(76));
        Assert.That(Find(root.transform, "PvpCurrentRange").GetComponent<TMP_Text>().text, Does.Contain("1–49"));
        Assert.That(Find(root.transform, "PvpOpponentSecretPrivate").GetComponent<TMP_Text>().text, Is.EqualTo(L("pvp_secret_private")));
        var history = (Component)Get(controller, "historyRail");
        int count = (int)history.GetType().GetProperty("EventCount").GetValue(history);
        string[] identities = Enumerable.Range(0, count).Select(i => (string)Invoke(history, "IdentityAt", i)).ToArray();
        SetLanguage("el");
        Invoke(owner, "RefreshMatchPresentation");
        CollectionAssert.AreEqual(identities, Enumerable.Range(0, count).Select(i => (string)Invoke(history, "IdentityAt", i)).ToArray(),
            "A language repaint must not record a second event.");
        Set(controller, "myMin", 48);
        Set(controller, "myMax", 50);
        Invoke(controller, "RefreshLockButton");
        Assert.That(((TMP_Text)Get(controller, "lockButtonLabel")).text, Is.EqualTo(L("lock")));
        var suggestion = Find(root.transform, "PvpLockSuggestion").GetComponent<TMP_Text>();
        Assert.That(suggestion.text, Is.EqualTo(L("lock_suggest", 3)));
        Assert.That(suggestion.isActiveAndEnabled, Is.True, "The full suggestion is not lost to fit a small button.");
        Invoke(controller, "OnLeaveMatchPressed");
    }

    [UnityTest]
    public IEnumerator SignalsDrawerPreservesIndexedCallbacksAndCancelsOnExit()
    {
        yield return Build();
        ShowCase("PlayerTurn");
        yield return null;
        var toggle = Find(root.transform, "OpenSignals").GetComponent<Button>();
        toggle.onClick.Invoke();
        var overlay = Find(root.transform, "PvpSignalsOverlay").gameObject;
        Assert.That(overlay.activeInHierarchy, Is.True);
        Assert.That(overlay.GetComponent<Image>().raycastTarget, Is.True);
        Assert.That(Find(overlay.transform, "MatchSignalChoices").GetComponentsInChildren<Button>().Length, Is.EqualTo(6));
        Find(overlay.transform, "Signal0").GetComponent<Button>().onClick.Invoke();
        Assert.That(overlay.activeSelf, Is.False);
        Assert.That(((TMP_Text)Get(controller, "signalFeedText")).text, Does.Contain(L("signal_luck")));
        toggle.onClick.Invoke();
        Invoke(controller, "OnLeaveMatchPressed");
        yield return null;
        Assert.That(overlay.activeSelf, Is.False);
        Assert.That(overlay.activeInHierarchy, Is.False);
    }

    [UnityTest]
    public IEnumerator KeypadSubmitAndLockReflectAuthoritativeTurnAndInFlightGuards()
    {
        yield return Build();
        StartJoined();
        var state = State("play");
        S(state, "turn", "host");
        Emit(state);
        var input = (TMP_InputField)Get(controller, "guessInput");
        var submit = ((GameObject)Get(controller, "guessButton")).GetComponent<Button>();
        var keypad = (GameObject)Get(controller, "keypadRoot");
        Assert.That(input.interactable, Is.False);
        Assert.That(submit.interactable, Is.False);
        Assert.That(keypad.GetComponentsInChildren<Button>().All(button => !button.interactable), Is.True);
        Find(keypad.transform, "Key5").GetComponent<Button>().onClick.Invoke();
        Assert.That(input.text, Is.Empty, "An opponent-turn keypad callback cannot change the draft.");

        S(state, "turn", "guest");
        S(state, "guestGuessCount", 1);
        Emit(state);
        Assert.That(input.interactable && submit.interactable, Is.True);
        input.text = "101";
        submit.onClick.Invoke();
        Assert.That((int)Get(backend, "GuessCalls"), Is.Zero);
        input.text = "";
        Find(keypad.transform, "Key5").GetComponent<Button>().onClick.Invoke();
        Find(keypad.transform, "Key0").GetComponent<Button>().onClick.Invoke();
        Assert.That(input.text, Is.EqualTo("50"));
        var lockButton = ((GameObject)Get(controller, "lockButton")).GetComponent<Button>();
        lockButton.onClick.Invoke();
        Set(backend, "HoldGuesses", true);
        submit.onClick.Invoke();
        submit.onClick.Invoke();
        Assert.That((int)Get(backend, "GuessCalls"), Is.EqualTo(1));
        Assert.That((int)Get(backend, "LastGuess"), Is.EqualTo(50));
        Assert.That((bool)Get(backend, "LastLock"), Is.True);
        Assert.That(input.interactable || submit.interactable || lockButton.interactable, Is.False);
        ((Action<bool>)Get(backend, "PendingGuess"))(false);
        Assert.That(input.text, Is.EqualTo("50"), "A failed request retains the deliberate draft.");
        Assert.That(input.interactable && submit.interactable, Is.True);
        Invoke(controller, "OnLeaveMatchPressed");
    }

    [UnityTest]
    public IEnumerator RealCreateJoinValidationWaitingCancelAndLateCallbackRemainWired()
    {
        yield return Build();
        Find(root.transform, "CreateButton").GetComponent<Button>().onClick.Invoke();
        var secret = (TMP_InputField)Get(controller, "createSecretInput");
        secret.text = "101";
        ((GameObject)Get(controller, "createConfirmButton")).GetComponent<Button>().onClick.Invoke();
        Assert.That((int)Get(backend, "CreateCalls"), Is.Zero);
        secret.text = "80";
        ((GameObject)Get(controller, "createConfirmButton")).GetComponent<Button>().onClick.Invoke();
        Assert.That((int)Get(backend, "CreateCalls"), Is.EqualTo(1));
        Assert.That((int)Get(backend, "LastSecret"), Is.EqualTo(80));
        Assert.That(((GameObject)Get(controller, "createWaitingRoot")).activeSelf, Is.True);
        Assert.That(((TMP_Text)Get(controller, "roomCodeText")).text, Is.EqualTo("MTW8H"));
        Find(((GameObject)Get(controller, "createPanel")).transform, "CancelButton").GetComponent<Button>().onClick.Invoke();
        Assert.That(((GameObject)Get(controller, "pvpMenuPanel")).activeSelf, Is.True);
        Assert.That((int)Get(backend, "LeaveCalls"), Is.EqualTo(1));
        Set(backend, "HoldRequests", true);
        Find(root.transform, "JoinButton").GetComponent<Button>().onClick.Invoke();
        ((TMP_InputField)Get(controller, "joinCodeInput")).text = "MTW8H";
        ((TMP_InputField)Get(controller, "joinSecretInput")).text = "32";
        ((GameObject)Get(controller, "joinConfirmButton")).GetComponent<Button>().onClick.Invoke();
        Assert.That((int)Get(backend, "JoinCalls"), Is.EqualTo(1));
        Invoke(controller, "ClosePvpMenu");
        ((Action<bool, string>)Get(backend, "PendingRoomRequest"))(true, "");
        Assert.That(((GameObject)Get(controller, "joinPanel")).activeSelf, Is.False);
        Assert.That(((GameObject)Get(controller, "matchPanel")).activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator ResultCaptionsRepaintOpponentArrivesAndRealRematchResets()
    {
        yield return Build();
        StartJoined();
        Emit(State("play"));
        var done = State("done");
        S(done, "winner", "guest");
        S(done, "revealedSecret", 73);
        S(done, "hostGuessCount", 4);
        S(done, "guestGuessCount", 3);
        S(done, "resultReason", "range");
        S(done, "resultHostCandidates", 49);
        S(done, "resultGuestCandidates", 30);
        int matches = PlayerPrefs.GetInt("StatMatches");
        Emit(done);
        Emit(done);
        Assert.That(PlayerPrefs.GetInt("StatMatches"), Is.EqualTo(matches + 1), "One authoritative result records once.");
        var result = (Component)Get(controller, "resultPresentation");
        Assert.That(result.gameObject.activeSelf, Is.True);
        var reason = (TMP_Text)Get(result, "explanationText");
        SetLanguage("en");
        yield return null;
        Assert.That(reason.text, Is.EqualTo(L("pvp_reason_range", "Player", 30, 49, "")));
        var role = Find(root.transform, "PvpResultOpponentLabel").GetComponent<TMP_Text>();
        string english = role.text;
        SetLanguage("el");
        yield return null;
        Assert.That(reason.text, Is.EqualTo(L("pvp_reason_range", "Player", 30, 49, "")));
        Assert.That(role.text, Is.Not.EqualTo(english));
        Assert.That(role.text, Is.EqualTo(L("prebattle_opponent")));
        Assert.That(Find(root.transform, "OpponentAttemptsRowCaption").GetComponent<TMP_Text>().text,
            Is.EqualTo(L("guesses")), "The count stays clearly labeled inside the opponent's card.");
        S(done, "hostName", "Κωνσταντίνος");
        Emit(done);
        Assert.That(Find(root.transform, "PvpResultOpponentName").GetComponent<TMP_Text>().text,
            Is.EqualTo("Κωνσταντίνος"), "Finished-room snapshots must update opponent identity.");
        ((TMP_InputField)Get(controller, "rematchSecretInput")).text = "64";
        ((GameObject)Get(controller, "rematchButton")).GetComponent<Button>().onClick.Invoke();
        Assert.That((int)Get(backend, "RematchCalls"), Is.EqualTo(1));
        Assert.That((int)Get(backend, "LastSecret"), Is.EqualTo(64));
        var again = State("play");
        S(again, "matchIndex", 1);
        Emit(again);
        Assert.That(result.gameObject.activeSelf, Is.False);
        Assert.That(reason.text, Is.Empty, "A rematch cannot inherit the previous explanation.");
        Assert.That(((GameObject)Get(controller, "rematchButton")).activeSelf, Is.False);
        Assert.That(((GameObject)Get(controller, "keypadRoot")).activeSelf, Is.True);
        Assert.That(((TMP_InputField)Get(controller, "guessInput")).text, Is.Empty);
        Assert.That((int)Get(controller, "myMin"), Is.EqualTo(1));
        Assert.That((int)Get(controller, "myMax"), Is.EqualTo(100));
        Invoke(controller, "HandleConnectionLost");
        Assert.That(((GameObject)Get(controller, "resultExitButton")).activeSelf, Is.False);
        StartJoined();
        Emit(State("play"));
        Emit(done);
        var exit = ((GameObject)Get(controller, "resultExitButton")).GetComponent<Button>();
        Assert.That(exit.gameObject.activeInHierarchy && exit.IsInteractable(), Is.True,
            "A prior disconnected room cannot remove Exit from a later completed match.");
        exit.onClick.Invoke();
        Assert.That(((GameObject)Get(controller, "pvpMenuPanel")).activeSelf, Is.True);
        Assert.That(((GameObject)Get(controller, "matchPanel")).activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator InviteSharingUsesCurrentCodeAndResumeRefreshesWithoutSendingOrResetting()
    {
        yield return Build();
        string clipboard = GUIUtility.systemCopyBuffer;
        var eventField = T("GameEvents").GetField("OnRoomShared");
        var prior = (Action)eventField.GetValue(null);
        int shared = 0, opened = 0;
        Action observer = () => shared++;
        eventField.SetValue(null, observer);
        try
        {
            Set(controller, "OpenShareChooser", new Func<string, string, bool>((text, title) => {
                opened++;
                Assert.That(text, Is.EqualTo(L("pvp_invite_text", "MTW8H")));
                Assert.That(title, Is.EqualTo(L("pvp_share_chooser")));
                Assert.That(text, Does.Not.Contain("80"));
                Assert.That(text, Does.Not.Contain("11CB9E"));
                return true;
            }));
            Invoke(controller, "OnShareInvitePressed");
            Assert.That(opened, Is.Zero, "No invitation before room creation.");
            Find(root.transform, "CreateButton").GetComponent<Button>().onClick.Invoke();
            ((TMP_InputField)Get(controller, "createSecretInput")).text = "80";
            ((GameObject)Get(controller, "createConfirmButton")).GetComponent<Button>().onClick.Invoke();
            Emit(State("waiting"));
            yield return null;
            foreach (string language in new[] { "en", "el" })
            {
                SetLanguage(language);
                string before = ((TMP_Text)Get(controller, "createStatusText")).text;
                ((GameObject)Get(controller, "createShareButton")).GetComponent<Button>().onClick.Invoke();
                Assert.That(((TMP_Text)Get(controller, "createStatusText")).text, Is.EqualTo(before));
            }
            Assert.That(opened, Is.EqualTo(2));
            Assert.That(shared, Is.Zero, "Opening/cancelling a chooser is not a sent invitation.");
            Assert.That(GUIUtility.systemCopyBuffer, Is.EqualTo(clipboard));
            Set(controller, "OpenShareChooser", new Func<string, string, bool>((text, title) => false));
            Invoke(controller, "OnShareInvitePressed");
            Assert.That(((TMP_Text)Get(controller, "createStatusText")).text, Is.EqualTo(L("pvp_share_unavailable")));
            Assert.That(shared, Is.Zero, "An unavailable chooser is not a sent invitation.");
            Assert.That(GUIUtility.systemCopyBuffer, Is.EqualTo(clipboard));
            ((GameObject)Get(controller, "createCopyButton")).GetComponent<Button>().onClick.Invoke();
            Assert.That(GUIUtility.systemCopyBuffer, Is.EqualTo(L("pvp_invite_text", "MTW8H")));
            Assert.That(shared, Is.EqualTo(1), "Preserve the existing deliberate copy event.");
            int polls = (int)Get(backend, "PollCalls");
            var staleObserver = (Delegate)Get(backend, "observer");
            Invoke(controller, "OnApplicationFocus", false);
            Invoke(controller, "OnApplicationPause", true);
            Invoke(controller, "OnApplicationFocus", true);
            Assert.That((int)Get(backend, "PollCalls"), Is.EqualTo(polls));
            Invoke(controller, "OnApplicationPause", false);
            Invoke(controller, "OnApplicationFocus", true);
            Assert.That((int)Get(backend, "PollCalls"), Is.EqualTo(polls + 1));
            Emit(State("play")); // friend joined while WhatsApp was open
            Assert.That(((GameObject)Get(controller, "matchPanel")).activeSelf, Is.True);
            staleObserver.DynamicInvoke(State("waiting"));
            Assert.That(Get(controller, "lastState").GetType().GetField("phase").GetValue(Get(controller, "lastState")), Is.EqualTo("play"));
            Invoke(controller, "OnShareInvitePressed");
            Assert.That(opened, Is.EqualTo(2), "No stale room invitation from the match.");
            Set(controller, "myMin", 26); Set(controller, "myMax", 49);
            Invoke(controller, "OnApplicationPause", true);
            Invoke(controller, "OnApplicationPause", false);
            Assert.That((int)Get(controller, "myMin"), Is.EqualTo(26));
            Assert.That((int)Get(controller, "myMax"), Is.EqualTo(49));
            Invoke(controller, "OnLeaveMatchPressed");
            Invoke(controller, "OnShareInvitePressed");
            Assert.That(opened, Is.EqualTo(2));
        }
        finally
        {
            GUIUtility.systemCopyBuffer = clipboard;
            eventField.SetValue(null, prior);
        }
    }

    [UnityTest]
    public IEnumerator ReturnedSnapshotsPropagateResultFactsWithoutChangingMatchOrIdentity()
    {
        yield return Build();
        var client = root.AddComponent(T("PlayFabPvpClient"));
        ((Behaviour)client).enabled = false;
        var current = State("play");
        var final = State("done");
        S(final, "winner", "host"); S(final, "resultReason", "only_correct");
        S(final, "resultHostCandidates", 30); S(final, "resultForfeitedSide", "guest");
        string wire = JsonUtility.ToJson(final);
        // The CloudScript wrapper's state is itself a JSON string.
        string response = "{\"ok\":true,\"state\":\"" + wire.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}";
        Invoke(client, "ApplyReturnedState", current, response);
        foreach (var field in new[] { "phase", "winner", "resultReason", "resultHostCandidates", "resultGuestCandidates",
            "resultForfeitedSide", "hostName", "guestName", "hostAvatarId", "guestAvatarId" })
            Assert.That(current.GetType().GetField(field).GetValue(current), Is.EqualTo(final.GetType().GetField(field).GetValue(final)), field);
        S(current, "matchIndex", 1); S(current, "phase", "play"); S(current, "resultReason", "");
        Invoke(client, "ApplyReturnedState", current, response);
        Assert.That(current.GetType().GetField("resultReason").GetValue(current), Is.EqualTo(""));
        UnityEngine.Object.DestroyImmediate(client);
    }

    [UnityTest]
    public IEnumerator InvitationAndFinalReasonsFitEnElPortraitRegions()
    {
        yield return ShareResultLayout(false);
    }

    [UnityTest]
    public IEnumerator ResultCompositionUsesSoloCardsAndFacesAcrossPortraits()
    {
        yield return ResultComposition(false);
    }

    [UnityTest, Explicit("Bounded native result-only visual review; no network or build.")]
    public IEnumerator CaptureNativeResultComposition()
    {
        yield return ResultComposition(true);
    }

    IEnumerator ResultComposition(bool native)
    {
        yield return Build();
        var errors = new List<string>();
        string output = null;
        if (native)
        {
            string parent = (string)T("PvpPresentationReviewTools").GetProperty("OutputDirectory").GetValue(null);
            Assert.That(Directory.Exists(parent), Is.True);
            output = Path.Combine(parent, "ResultComposition-" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));
            Directory.CreateDirectory(output);
        }
        foreach (var viewport in new[] { new Vector2Int(720, 1280), new Vector2Int(1080, 1920),
            new Vector2Int(1080, 2400), new Vector2Int(1179, 2556) })
        foreach (string language in new[] { "en", "el" })
        {
            if (native && (viewport.x != 1080 || (viewport.y == 2400 && language != "el"))) continue;
            SetLanguage(language);
            if (native)
            {
                T("OnboardingGameViewCapture").GetMethod("SetResolution").Invoke(null, new object[] { viewport.x, viewport.y });
                Screen.SetResolution(viewport.x, viewport.y, false);
                yield return (IEnumerator)T("PvpPresentationReviewTools").GetMethod("WaitForStableNativeViewport")
                    .Invoke(null, new object[] { root.transform, viewport.x, viewport.y });
            }
            foreach (string state in new[] { "ResultWin", "ResultLoss", "ResultDraw", "RematchWaiting" })
            {
                if (native && viewport.y == 2400 && state != "ResultDraw" && state != "RematchWaiting") continue;
                ShowCase("PlayerTurn");
                var done = State("done");
                S(done, "hostName", "Κωνσταντίνος"); S(done, "guestName", "Αλεξάνδρα");
                S(done, "winner", state == "ResultDraw" ? "draw" : state == "ResultLoss" ? "host" : "guest");
                S(done, "hostGuessCount", 4); S(done, "guestGuessCount", 4); S(done, "revealedSecret", 77);
                S(done, "resultReason", state == "ResultDraw" ? "draw" : "range");
                S(done, "resultHostCandidates", state == "ResultLoss" || state == "ResultDraw" ? 30 : 49);
                S(done, "resultGuestCandidates", state == "ResultLoss" ? 49 : 30);
                Emit(done);
                if (state == "RematchWaiting")
                {
                    ((TMP_InputField)Get(controller, "rematchSecretInput")).text = "64";
                    ((GameObject)Get(controller, "rematchButton")).GetComponent<Button>().onClick.Invoke();
                }
                yield return null;
                yield return null;
                if (!native)
                    foreach (var safe in root.GetComponentsInChildren(T("ResponsiveSafeAreaRoot"), true))
                        Invoke(safe, "ApplyViewport", new Rect(0, 0, viewport.x, viewport.y),
                            new Rect(0, 44, viewport.x, viewport.y - 88), ResultCanvasSize(viewport));
                var owner = root.GetComponent(T("PvpDuelCartoonVisuals"));
                Invoke(owner, "LayoutResult"); Invoke(owner, "CenterButtonFaces"); Invoke(owner, "CenterResultText");
                Canvas.ForceUpdateCanvases();
                string context = language + " " + state + " " + viewport;
                float extra = Mathf.Max(0, 1080f * (viewport.y - (native ? 0 : 88)) / viewport.x - 1920);
                foreach (bool opponent in new[] { false, true })
                {
                    string prefix = opponent ? "PvpResultOpponent" : "PvpResultPlayer";
                    var card = (RectTransform)Find(root.transform, prefix + "Card");
                    Assert.That(card.sizeDelta.x, Is.EqualTo(510), context);
                    Assert.That(card.sizeDelta.y, Is.EqualTo(640 + extra * .5f).Within(.01f), context);
                    Assert.That(card.localScale, Is.EqualTo(Vector3.one), context);
                    Assert.That(card.GetComponent<Image>().sprite, Is.SameAs(Resources.Load<Sprite>(opponent
                        ? "solo/production/solo_opponent_card_shell_v1" : "solo/production/solo_player_card_shell_v1")), context);
                    AuditFace(Find(root.transform, prefix + "Label").GetComponent<TMP_Text>(), card,
                        new Rect(-130, (640 + extra * .5f) * .382f - 27, 260, 54), context, errors);
                    AuditFace(Find(root.transform, prefix + "Name").GetComponent<TMP_Text>(), card,
                        new Rect(-210, -196 - extra * .25f, 420, 62), context, errors);
                    var count = (RectTransform)Find(root.transform, opponent ? "OpponentAttemptsRow" : "PlayerAttemptsRow");
                    AuditFace(count.Find(count.name + "Caption").GetComponent<TMP_Text>(), count,
                        new Rect(-179, -22, 270, 44), context, errors);
                    AuditFace(count.Find(count.name + "Value").GetComponent<TMP_Text>(), count,
                        new Rect(103, -27, 78, 54), context, errors);
                    Assert.That(count.anchoredPosition.y - 27,
                        Is.GreaterThan(-card.rect.height * .5f + 58), context + " count clears the lower inset rim");
                    var portrait = (RectTransform)Find(root.transform,
                        opponent ? "PvpResultOpponentCharacterAperture" : "PvpResultHeroAperture");
                    Assert.That(portrait.rect.width, Is.InRange(400f, 430f), context);
                    Assert.That(portrait.rect.width, Is.EqualTo(portrait.rect.height), context);
                    Assert.That(portrait.GetComponent<Mask>().showMaskGraphic, Is.False, context);
                }
                foreach (string name in new[] { "ResultConfirmRematchButton", "ResultExitButton" })
                {
                    var button = (RectTransform)Find(root.transform, name);
                    var image = button.GetComponent<Image>();
                    Assert.That(image.sprite, Is.SameAs(Resources.Load<Sprite>("solo/production/solo_primary_cta_v1")), context);
                    Assert.That(image.type, Is.EqualTo(Image.Type.Simple), context);
                    Assert.That(image.raycastTarget, Is.True, context);
                    Assert.That(button.sizeDelta.x, Is.EqualTo(432), context);
                    Assert.That(button.sizeDelta.y, Is.EqualTo(120 + extra * .1f).Within(.01f), context);
                    if (!button.gameObject.activeInHierarchy) continue;
                    var label = button.GetComponentInChildren<TMP_Text>();
                    Assert.That(label.fontSize, Is.EqualTo(40), context);
                    float h = 120 + extra * .1f;
                    AuditFace(label, button, new Rect(-138.24f, -.29f * h, 276.48f, .65f * h), context, errors);
                }
                var input = (TMP_InputField)Get(controller, "rematchSecretInput");
                // TMP 3.0.7 forces both setter and getter to true on desktop.
                // Retain its mobile branch, not a fake Editor keyboard override.
                bool mobile = Application.platform == RuntimePlatform.Android ||
                    Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS;
                Assert.That(input.shouldHideSoftKeyboard, Is.EqualTo(!mobile), context);
                Assert.That(input.keyboardType, Is.EqualTo(TouchScreenKeyboardType.NumberPad), context);
                Assert.That(((GameObject)Get(controller, "resultExitButton")).activeInHierarchy, Is.True, context);
                var stats = (RectTransform)Find(root.transform, "PvpResultStatsCard");
                var actions = (RectTransform)Find(root.transform, "PvpResultActions");
                Assert.That(stats.anchoredPosition.y - stats.rect.height / 2,
                    Is.GreaterThan(actions.anchoredPosition.y + actions.rect.height / 2), context);
                var reason = Find(root.transform, "PvpResultExplanation").GetComponent<TMP_Text>();
                Assert.That(reason.fontSize, Is.EqualTo(32), context);
                AuditFace(reason, stats, new Rect(-430, -103 - extra * .075f, 860, 150 + extra * .15f), context, errors);
                if (state == "RematchWaiting")
                {
                    var status = (TMP_Text)Get(controller, "rematchStatusText");
                    Assert.That(status.transform.parent, Is.SameAs(actions.parent), context + " separate feedback lane");
                    AuditFace(status, (RectTransform)status.transform.parent,
                        new Rect(-430, -720 - extra * .3f, 860, 32), context, errors);
                }
                AuditGlyphs(context, errors);
                if (!native) continue;
                yield return new WaitForSecondsRealtime(1.9f);
                yield return new WaitForEndOfFrame();
                string path = Path.Combine(output, language + "-" + viewport.x + "x" + viewport.y + "-" + state + ".png");
                ScreenCapture.CaptureScreenshot(path);
                for (int i = 0; i < 120 && !File.Exists(path); i++) yield return null;
                Assert.That(File.Exists(path), Is.True);
                var capture = new Texture2D(2, 2);
                Assert.That(capture.LoadImage(File.ReadAllBytes(path)), Is.True);
                Assert.That(capture.width, Is.EqualTo(viewport.x)); Assert.That(capture.height, Is.EqualTo(viewport.y));
                UnityEngine.Object.Destroy(capture);
            }
        }
        if (native) Debug.Log("HOL_PVP_RESULT_COMPOSITION_CAPTURE " + output);
        Assert.That(errors, Is.Empty, string.Join("\n", errors));
    }

    static Vector2 ResultCanvasSize(Vector2Int viewport)
    {
        // Match the real fixture CanvasScaler (.5 width/height), rather than
        // injecting a 16:9 canvas into a tall-portrait geometry test.
        float scale = Mathf.Sqrt(viewport.x / 1080f * viewport.y / 1920f);
        return new Vector2(viewport.x / scale, viewport.y / scale);
    }

    [UnityTest, Explicit("Bounded sharing/result evidence through the existing native Game View workflow.")]
    public IEnumerator CaptureNativeInvitationAndFinalReasons()
    {
        yield return ShareResultLayout(true);
    }

    IEnumerator ShareResultLayout(bool native)
    {
        yield return Build();
        var errors = new List<string>();
        string output = null;
        if (native)
        {
            string parent = (string)T("PvpPresentationReviewTools").GetProperty("OutputDirectory").GetValue(null);
            Assert.That(Directory.Exists(parent), Is.True, "Select the external evidence folder.");
            output = Path.Combine(parent, "ShareResult-" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));
            Directory.CreateDirectory(output);
        }
        foreach (var viewport in new[] { new Vector2Int(720, 1280), new Vector2Int(1080, 1920),
            new Vector2Int(1080, 2400), new Vector2Int(1179, 2556) })
        foreach (string language in new[] { "en", "el" })
        {
            SetLanguage(language);
            if (native)
            {
                T("OnboardingGameViewCapture").GetMethod("SetResolution").Invoke(null, new object[] { viewport.x, viewport.y });
                Screen.SetResolution(viewport.x, viewport.y, false);
                yield return (IEnumerator)T("PvpPresentationReviewTools").GetMethod("WaitForStableNativeViewport")
                    .Invoke(null, new object[] { root.transform, viewport.x, viewport.y });
            }
            foreach (string kind in new[] { "waiting", "only_correct", "lock", "range", "draw", "forfeit", "legacy" })
            {
                ShowCase("Waiting");
                if (kind != "waiting")
                {
                    var done = State("done");
                    S(done, "hostName", "Κωνσταντίνος"); S(done, "guestName", "Αλεξάνδρα");
                    S(done, "winner", kind == "draw" ? "draw" : "host");
                    S(done, "revealedSecret", 77);
                    S(done, "resultReason", kind == "forfeit" ? "only_correct" : kind == "legacy" ? "" : kind);
                    S(done, "resultHostCandidates", kind == "lock" || kind == "draw" ? 100 : 30);
                    S(done, "resultGuestCandidates", kind == "range" ? 49 : kind == "lock" || kind == "draw" ? 100 : 0);
                    S(done, "resultForfeitedSide", kind == "forfeit" ? "guest" : "");
                    Emit(done);
                }
                yield return null;
                yield return null;
                if (!native)
                    foreach (var safe in root.GetComponentsInChildren(T("ResponsiveSafeAreaRoot"), true))
                        Invoke(safe, "ApplyViewport", new Rect(0, 0, viewport.x, viewport.y),
                            new Rect(0, 44, viewport.x, viewport.y - 88), ResultCanvasSize(viewport));
                var visuals = root.GetComponent(T("PvpDuelCartoonVisuals"));
                Invoke(visuals, "LayoutResult");
                Invoke(visuals, "CenterButtonFaces");
                Invoke(visuals, "CenterResultText");
                Canvas.ForceUpdateCanvases();
                string context = language + " " + viewport + " " + kind;
                if (kind == "waiting")
                {
                    foreach (var spec in new[] { new { Name = "NativeShareButton", Face = new Rect(-131.2f, -43.79f, 262.4f, 98.15f), Font = 36f },
                        new { Name = "CopyInviteButton", Face = new Rect(-116, -28, 232, 67.2f), Font = 30f } })
                    {
                        var button = (RectTransform)Find(root.transform, spec.Name);
                        var label = button.GetComponentInChildren<TMP_Text>();
                        Assert.That(button.GetComponent<Button>().interactable, Is.True, context);
                        Assert.That(label.fontSize, Is.EqualTo(spec.Font), context);
                        AuditFace(label, button, spec.Face, context, errors);
                        Assert.That(label.isTextOverflowing || label.isTextTruncated, Is.False, context);
                    }
                }
                else
                {
                    var reason = Find(root.transform, "PvpResultExplanation").GetComponent<TMP_Text>();
                    float extra = Mathf.Max(0, 1080f * (viewport.y - (native ? 0 : 88)) / viewport.x - 1920);
                    Assert.That(reason.fontSize, Is.EqualTo(32), context);
                    Assert.That(reason.isTextTruncated || reason.isTextOverflowing, Is.False, context + " " + reason.text);
                    AuditFace(reason, (RectTransform)reason.transform.parent,
                        new Rect(-430, -103 - extra * .075f, 860, 150 + extra * .15f), context, errors);
                    var streak = (RectTransform)Find(root.transform, "PvpResultStreak");
                    Assert.That(streak.anchoredPosition.y - streak.rect.height / 2, Is.GreaterThan(-55),
                        context + " streak cannot overlap the explanation's padded region");
                    // The explanation has its own padded region inside the stats
                    // face; neither it nor the new stats edge can cover rematch.
                    var stats = (RectTransform)reason.transform.parent;
                    var actions = (RectTransform)Find(root.transform, "PvpResultActions");
                    Assert.That(stats.anchoredPosition.y - stats.rect.height / 2,
                        Is.GreaterThan(actions.anchoredPosition.y + actions.rect.height / 2), context);
                }
                AuditGlyphs(context, errors);
                // Only representative NEW surfaces, not the full capture matrix.
                if (native && ((viewport.y == 1920 && (kind == "waiting" || kind == "range" || kind == "forfeit")) ||
                    (viewport.y == 2400 && language == "el" && (kind == "waiting" || kind == "draw"))))
                {
                    // Capture the resting presentation after the real celebration,
                    // without disabling or replacing any production effect.
                    if (kind != "waiting") yield return new WaitForSecondsRealtime(1.9f);
                    yield return new WaitForEndOfFrame();
                    string path = Path.Combine(output, language + "-" + viewport.x + "x" + viewport.y + "-" + kind + ".png");
                    ScreenCapture.CaptureScreenshot(path);
                    for (int i = 0; i < 120 && !File.Exists(path); i++) yield return null;
                    Assert.That(File.Exists(path), Is.True);
                    var image = new Texture2D(2, 2);
                    Assert.That(image.LoadImage(File.ReadAllBytes(path)), Is.True);
                    Assert.That(image.width, Is.EqualTo(viewport.x));
                    Assert.That(image.height, Is.EqualTo(viewport.y));
                    UnityEngine.Object.Destroy(image);
                }
            }
        }
        Assert.That(errors, Is.Empty, string.Join("\n", errors));
    }

    static readonly string[] Cases = { "PrivateRoom", "CreateEntry", "JoinEntry", "Waiting",
        "PlayerTurn", "OpponentTurn", "LockMiss", "LockIntro", "LockArmed", "LockSuggested", "Signal", "IncomingSignal", "SignalsOpen",
        "ResultWin", "ResultLoss", "ResultDraw", "RematchWaiting", "ConnectionLost" };

    static readonly string[] PrematchCases = { "PrivateRoom", "CreateSecret", "JoinSecret",
        "Creating", "Waiting", "Joining", "JoinError" };

    [UnityTest]
    public IEnumerator PrematchButtonsUseSoloFacesAndCenteredNativeGlyphsInEnEl()
    {
        yield return Build();
        var errors = new List<string>();
        foreach (var viewport in new[] { new Vector2(1080, 1920), new Vector2(1080, 2400), new Vector2(1179, 2556) })
        foreach (string language in new[] { "en", "el" })
        foreach (string state in PrematchCases)
        {
            SetLanguage(language);
            ShowPrematchCase(state);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            foreach (var safe in root.GetComponentsInChildren(T("ResponsiveSafeAreaRoot"), true))
                Invoke(safe, "ApplyViewport", new Rect(Vector2.zero, viewport),
                    new Rect(0, 44, viewport.x, viewport.y - 88), new Vector2(1080, 1920));
            // The menu owner now reflows the forms after safe-area changes.
            // This fixture deliberately retains its 1080x1920 authoring canvas;
            // native aspect-ratio growth is covered by the native capture lane.
            Invoke(root.GetComponentInChildren(T("PrivateRoomVisuals"), true), "ApplyResponsiveLayout");
            Canvas.ForceUpdateCanvases();
            string context = language + " " + state + " " + viewport;
            AuditGlyphs(context, errors);
            if (state == "Creating" || state == "Waiting" || state == "Joining")
            {
                var panel = ((GameObject)Get(controller,
                    state == "Joining" ? "joinPanel" : "createPanel")).transform;
                var board = (RectTransform)Find(panel, "PrebattleBoard");
                Assert.That(board.sizeDelta, Is.EqualTo(new Vector2(1000, 1155)), context);
                Assert.That(board.anchoredPosition, Is.EqualTo(new Vector2(0, -252.5f)), context);
                // Conservative inner face measured from the unchanged board
                // artwork. Its transparent outer rect is not usable space.
                var innerFace = new Rect(-390f * 1000f / 960f, -525f * 1155f / 1280f,
                    780f * 1000f / 960f, 1040f * 1155f / 1280f);
                foreach (string cardName in new[] { "YouCard", "OpponentCard" })
                {
                    var card = (RectTransform)Find(panel, cardName);
                    Assert.That(card.gameObject.activeInHierarchy, Is.True, context);
                    Assert.That(card.sizeDelta, Is.EqualTo(new Vector2(340, 252)), context);
                    Assert.That(card.anchoredPosition, Is.EqualTo(new Vector2(
                        cardName == "YouCard" ? -200 : 200,
                        -252.5f + .4f * 1155f - .17f * (.8f * 1155f))), context);
                    Assert.That(card.localScale, Is.EqualTo(Vector3.one), context);
                    var image = card.GetComponent<Image>();
                    Assert.That(image.sprite, Is.SameAs(Resources.Load<Sprite>(
                        "phase2a/hol_tip_frame_r2_9s")), context);
                    Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), context);
                    Assert.That(image.raycastTarget, Is.False, context);
                    var corners = new Vector3[4];
                    card.GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        Vector3 point = board.InverseTransformPoint(corner);
                        if (point.x < innerFace.xMin || point.x > innerFace.xMax ||
                            point.y < innerFace.yMin || point.y > innerFace.yMax)
                            errors.Add(context + " " + cardName + " crosses visible board face: " + point);
                    }
                    foreach (var text in card.GetComponentsInChildren<TMP_Text>(false))
                    {
                        bool caption = text.name.EndsWith("Caption", StringComparison.Ordinal);
                        bool identity = text.name == "WaitingPlayerName" || text.name == "WaitingOpponentName";
                        Assert.That(text.fontSize, Is.EqualTo(caption ? 30 : identity ? 26 : 31), context + " " + text.name);
                        // The approved card shell is unchanged. Identity now
                        // owns the middle row beside its 80px portrait; ready
                        // status owns the separate lower row, never the name.
                        AuditFace(text, card, caption ? new Rect(-142, 62, 284, 52)
                            : identity ? new Rect(-44, -16, 186, 68)
                            : new Rect(-144, -94, 288, 76), context, errors);
                    }
                }
            }
            if (state == "PrivateRoom")
            {
                var heading = Find(root.transform, "PrivateRoomJoinHeading").GetComponent<TMP_Text>();
                Assert.That(heading.text, Is.EqualTo(language == "el"
                    ? "ΣΥΜΜΕΤΟΧΗ ΣΕ ΔΩΜΑΤΙΟ" : "JOIN A ROOM"), context);
                Assert.That(heading.fontStyle & FontStyles.UpperCase, Is.EqualTo((FontStyles)0),
                    "Use intentional localized casing, not invariant uppercase with Greek tonos.");
                Assert.That(L("private_room_join_title"), Is.EqualTo(language == "el"
                    ? "Συμμετοχή σε δωμάτιο" : "Join a room"), "The form title remains unchanged.");
                var safe = (RectTransform)Find(root.transform, "PrivateRoomSafeRoot");
                var tip = (RectTransform)Find(root.transform, "PrivateRoomTipCard");
                foreach (string name in new[] { "PrivateRoomMascotSix", "PrivateRoomMascotSeven" })
                {
                    var mascot = (RectTransform)Find(root.transform, name);
                    Assert.That(mascot.sizeDelta, Is.EqualTo(new Vector2(220, 260)), context);
                    Assert.That(mascot.GetComponent<Image>().preserveAspect, Is.True);
                    Assert.That(mascot.GetComponent<Image>().raycastTarget, Is.False);
                    // Authoring-space apertures share this one safe root. Full
                    // sprite rectangles must fit and not intrude on the tip.
                    var bounds = new Rect(mascot.anchoredPosition - mascot.sizeDelta * .5f, mascot.sizeDelta);
                    var tipBounds = new Rect(tip.anchoredPosition - tip.sizeDelta * .5f, tip.sizeDelta);
                    Assert.That(mascot.parent, Is.SameAs(safe));
                    Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(-540));
                    Assert.That(bounds.xMax, Is.LessThanOrEqualTo(540));
                    Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(-960));
                    Assert.That(bounds.yMax, Is.LessThanOrEqualTo(960));
                    Assert.That(bounds.Overlaps(tipBounds), Is.False, context + " " + name);
                }
            }
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(false))
            {
                if (text.name == "PrivateRoomCreateHeading")
                    AuditFace(text, (RectTransform)text.transform.parent,
                        new Rect(69.0625f, 44.625f, 403.75f, 127.5f), context, errors);
                if (text.name == "PrivateRoomJoinHeading")
                    AuditFace(text, (RectTransform)text.transform.parent,
                        new Rect(-5.3125f, 83.9375f, 446.25f, 116.875f), context, errors);
                if (text.name == "PrivateRoomCreateHint")
                    AuditFace(text, (RectTransform)text.transform.parent,
                        new Rect(76.5f, -56.3125f, 388.875f, 89.25f), context, errors);
                if (text.name == "PrivateRoomCodeCaption")
                    AuditFace(text, (RectTransform)text.transform.parent,
                        new Rect(5.3125f, 38.25f, 425f, 38.25f), context, errors);
                if (text.name == "PrivateRoomTip")
                    AuditFace(text, (RectTransform)text.transform.parent,
                        new Rect(-270, -65, 540, 130), context, errors);
                if (text.name == "PrivateRoomPlayerName" || text.name == "PvPCreatePanelPlayerName" ||
                    text.name == "PvPJoinPanelPlayerName")
                    AuditFace(text, (RectTransform)text.transform.parent,
                        new Rect(-180, 1, 240, 50), context, errors);
            }
            foreach (string prefix in new[] { "PrivateRoom", "PvPCreatePanel", "PvPJoinPanel" })
            {
                var aperture = (RectTransform)Find(root.transform, prefix + "PlayerAvatarAperture");
                Assert.That(aperture.anchoredPosition, Is.EqualTo(new Vector2(138, 0)),
                    "The actual Solo chip portrait ring is on the right, not the name area.");
                Assert.That(aperture.sizeDelta, Is.EqualTo(new Vector2(126, 126)));
                Assert.That(aperture.GetComponent<Mask>().showMaskGraphic, Is.False);
            }
            foreach (var button in root.GetComponentsInChildren<Button>(false))
            {
                var label = button.GetComponentsInChildren<TMP_Text>(false)
                    .FirstOrDefault(t => t.name == "PrivateRoomActionLabel");
                if (label == null) continue; // Back is an icon, never a hidden third mode.
                var image = button.GetComponent<Image>();
                bool copyInvite = button.name == "CopyInviteButton";
                bool nativeShare = button.name == "NativeShareButton";
                bool secondary = button.name == "CancelButton" || copyInvite;
                bool illustratedCreate = button.name == "CreateButton";
                Assert.That(image.sprite, Is.SameAs(Resources.Load<Sprite>(illustratedCreate
                    ? "phase2a/hol_cta_blue_r2_9s" : secondary
                        ? "phase2a/hol_tip_frame_r2_9s" : "solo/production/solo_primary_cta_v1")), context);
                if (illustratedCreate || secondary)
                    Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), context);
                Assert.That(label.font, Is.SameAs(Resources.Load<TMP_FontAsset>("phase2a/fonts/HOL Menu Display SDF")));
                Assert.That(label.fontStyle & FontStyles.Bold, Is.EqualTo(FontStyles.Bold));
                // The invitation region now has a primary SHARE and a separate
                // compact COPY fallback. Keep the original >=34px contract on
                // every existing CTA; pin both new controls independently.
                if (copyInvite || nativeShare)
                {
                    Assert.That(((RectTransform)button.transform).sizeDelta,
                        Is.EqualTo(copyInvite ? new Vector2(290, 112) : new Vector2(410, 151)), context);
                    Assert.That(label.fontSize, Is.EqualTo(copyInvite ? 30f : 36f), context);
                }
                else
                    Assert.That(label.fontSize, Is.GreaterThanOrEqualTo(34), context);
                Assert.That(label.fontSizeMin, Is.EqualTo(label.fontSizeMax), "Never shrink a CTA into tiny text.");
                // Independent measured faces for the actual production
                // button sizes, not the runtime centering helper's SafeRect.
                Rect face = copyInvite ? new Rect(-116, -28, 232, 67.2f)
                    : nativeShare ? new Rect(-131.2f, -43.79f, 262.4f, 98.15f)
                    : secondary ? new Rect(-144, -27, 288, 64.8f)
                    : illustratedCreate ? new Rect(-152, -24.2f, 304, 55)
                    : button.name == "JoinButton"
                        ? new Rect(-137.6f, -31.9f, 275.2f, 71.5f)
                        : new Rect(-233.6f, -43.79f, 467.2f, 98.15f);
                AuditFace(label, (RectTransform)button.transform, face, context, errors);
            }
        }
        Assert.That(errors, Is.Empty, string.Join("\n", errors));
    }

    [UnityTest]
    public IEnumerator PrematchValidationKeyboardLanguageAndCancelRemainTruthful()
    {
        yield return Build();
        SetLanguage("en");
        Find(root.transform, "CreateButton").GetComponent<Button>().onClick.Invoke();
        var secret = (TMP_InputField)Get(controller, "createSecretInput");
        var confirm = ((GameObject)Get(controller, "createConfirmButton")).GetComponent<Button>();
        foreach (string invalid in new[] { "", "0", "101" })
        {
            secret.text = invalid;
            yield return null;
            Assert.That(confirm.interactable, Is.False, invalid);
        }
        foreach (string valid in new[] { "1", "100" })
        {
            secret.text = valid;
            yield return null;
            Assert.That(confirm.interactable, Is.True, valid);
        }
        Assert.That(secret.keyboardType, Is.EqualTo(TouchScreenKeyboardType.NumberPad));
        Assert.That(secret.readOnly, Is.False);
        Set(backend, "HoldRequests", true);
        confirm.onClick.Invoke();
        confirm.onClick.Invoke();
        yield return null;
        Assert.That((int)Get(backend, "CreateCalls"), Is.EqualTo(1));
        Assert.That(((GameObject)Get(controller, "createCopyButton")).GetComponent<Button>().interactable, Is.False);
        Assert.That(secret.gameObject.activeInHierarchy, Is.False, "Waiting never displays the secret.");
        SetLanguage("el");
        yield return null;
        Assert.That(((TMP_Text)Get(controller, "createStatusText")).text, Does.StartWith(L("pvp_creating")));
        var completion = (Action<bool, string>)Get(backend, "PendingRoomRequest");
        Find(((GameObject)Get(controller, "createPanel")).transform, "PvPCreatePanelTopBack")
            .GetComponent<Button>().onClick.Invoke();
        completion(true, "MTW8H");
        Assert.That(((GameObject)Get(controller, "pvpMenuPanel")).activeSelf, Is.True);
        Assert.That(((GameObject)Get(controller, "createPanel")).activeSelf, Is.False);
        ShowPrematchCase("JoinError");
        Assert.That(((TMP_Text)Get(controller, "joinEntryStatusText")).text, Is.EqualTo(L("pvp_room_not_found")));
        SetLanguage("en");
        yield return null;
        Assert.That(((TMP_Text)Get(controller, "joinEntryStatusText")).text, Is.EqualTo(L("pvp_room_not_found")));
        var code = (TMP_InputField)Get(controller, "joinCodeInput");
        Assert.That(code.characterLimit, Is.EqualTo(5));
        Assert.That(code.keyboardType, Is.EqualTo(TouchScreenKeyboardType.ASCIICapable));
        Assert.That(code.onValidateInput("", 0, 'a'), Is.EqualTo('A'));
        Assert.That(code.onValidateInput("", 0, 'Ω'), Is.EqualTo('\0'));
        code.text = "AB12";
        yield return null;
        Assert.That(((GameObject)Get(controller, "joinConfirmButton")).GetComponent<Button>().interactable, Is.False);
    }

    static void AuditFace(TMP_Text text, RectTransform owner, Rect face, string context, List<string> errors)
    {
        text.ForceMeshUpdate();
        var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        int visible = 0;
        foreach (var glyph in text.textInfo.characterInfo.Take(text.textInfo.characterCount))
        {
            if (!glyph.isVisible) continue;
            Vector2 bottom = owner.InverseTransformPoint(text.transform.TransformPoint(glyph.bottomLeft));
            Vector2 top = owner.InverseTransformPoint(text.transform.TransformPoint(glyph.topRight));
            minimum = Vector2.Min(minimum, Vector2.Min(bottom, top));
            maximum = Vector2.Max(maximum, Vector2.Max(bottom, top));
            visible++;
        }
        if (visible == 0) { errors.Add(context + " " + owner.name + " has no visible CTA glyphs"); return; }
        if (minimum.x < face.xMin || maximum.x > face.xMax || minimum.y < face.yMin || maximum.y > face.yMax)
            errors.Add(context + " " + owner.name + " glyphs leave usable face: " + minimum + " to " + maximum + " in " + face);
        Vector2 expected = RectTransformUtility.WorldToScreenPoint(null, owner.TransformPoint(face.center));
        Vector2 actual = RectTransformUtility.WorldToScreenPoint(null, owner.TransformPoint((minimum + maximum) * .5f));
        if (Mathf.Abs(actual.x - expected.x) > 4 || Mathf.Abs(actual.y - expected.y) > 4)
            errors.Add(context + " " + owner.name + " glyph center offset in pixels " + (actual - expected));
    }

    void ShowPrematchCase(string state)
    {
        Invoke(controller, "OnLeaveMatchPressed");
        Set(backend, "HoldRequests", false);
        if (state == "PrivateRoom") return;
        bool create = state == "CreateSecret" || state == "Creating" || state == "Waiting";
        Find(root.transform, create ? "CreateButton" : "JoinButton").GetComponent<Button>().onClick.Invoke();
        ((TMP_InputField)Get(controller, create ? "createSecretInput" : "joinSecretInput")).text = "80";
        if (!create) ((TMP_InputField)Get(controller, "joinCodeInput")).text = "MTW8H";
        if (state == "CreateSecret" || state == "JoinSecret") return;
        Set(backend, "HoldRequests", state != "Waiting");
        ((GameObject)Get(controller, create ? "createConfirmButton" : "joinConfirmButton")).GetComponent<Button>().onClick.Invoke();
        if (state == "JoinError")
            ((Action<bool, string>)Get(backend, "PendingRoomRequest"))(false, L("pvp_room_not_found"));
        if (state == "Waiting") Emit(State("waiting"));
    }

    [UnityTest]
    public IEnumerator EntireEnElPortraitFlowKeepsRenderedGlyphsInsideOwnedRegions()
    {
        yield return Build();
        var violations = new List<string>();
        foreach (var viewport in new[] { new Vector2(720, 1280), new Vector2(1080, 1920),
            new Vector2(1080, 2400), new Vector2(1179, 2556) })
        foreach (string language in new[] { "en", "el" })
        foreach (string state in Cases)
        {
            SetLanguage(language);
            ShowCase(state);
            yield return null;
            if (state.StartsWith("Result", StringComparison.Ordinal) || state == "RematchWaiting")
                Assert.That(((GameObject)Get(controller, "resultExitButton")).activeInHierarchy, Is.True,
                    language + " " + state + " must retain a working Exit after earlier terminal states");
            Canvas.ForceUpdateCanvases();
            foreach (var safe in root.GetComponentsInChildren(T("ResponsiveSafeAreaRoot"), true))
                Invoke(safe, "ApplyViewport", new Rect(Vector2.zero, viewport),
                    new Rect(0, 44, viewport.x, viewport.y - 88), new Vector2(1080, 1920));
            // Audit synchronously after injection: a subsequent LateUpdate
            // would replace the requested safe viewport with the Editor view.
            var owner = root.GetComponent(T("PvpDuelCartoonVisuals"));
            Invoke(owner, "ApplyResponsiveLayoutForViewport", viewport.x, viewport.y);
            Invoke(owner, "RefreshMatchPresentation");
            Invoke(owner, "CenterButtonFaces");
            Canvas.ForceUpdateCanvases();
            AuditGlyphs(language + " " + state + " " + viewport, violations);
            if (state == "IncomingSignal")
            {
                // Every server-supported signal, with a long real-name shape,
                // must fit the cream artwork face, not only its TMP rectangle.
                for (int signal = 0; signal < 6; signal++)
                {
                    Invoke(controller, "ShowSignalLine", "Κωνσταντίνος", signal);
                    AuditGlyphs(language + " incoming signal " + signal + " " + viewport, violations);
                }
            }
            if (state == "SignalsOpen")
            {
                var drawer = (RectTransform)Find(root.transform, "PvpSignalsDrawer");
                // Measured inner artwork face, excluding its transparent
                // corners and purple rim. Check controls, not just glyphs.
                var inner = new Rect(-335, -246, 670, 490);
                foreach (var button in drawer.GetComponentsInChildren<Button>(false))
                {
                    var corners = new Vector3[4];
                    ((RectTransform)button.transform).GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        Vector2 point = drawer.InverseTransformPoint(corner);
                        if (!inner.Contains(point))
                            violations.Add(language + " " + state + " " + viewport + " " +
                                button.name + " leaves the drawer's usable artwork face: " + point);
                    }
                }
            }
            if (((GameObject)Get(controller, "matchPanel")).activeInHierarchy)
            {
                var historyPanel = (RectTransform)Find(root.transform, "PvpHistoryCard");
                var signalButton = (RectTransform)Find(root.transform, "OpenSignals");
                var corners = new Vector3[4];
                signalButton.GetWorldCorners(corners);
                float bottom = historyPanel.parent.InverseTransformPoint(corners[0]).y;
                float top = historyPanel.anchoredPosition.y + historyPanel.rect.height / 2;
                if (bottom < top + 4)
                    violations.Add(language + " " + state + " " + viewport + " Signals covers the history frame/title");
            }
            if (((GameObject)Get(controller, "matchPanel")).activeInHierarchy)
            foreach (var button in ((GameObject)Get(controller, "matchPanel")).GetComponentsInChildren<Button>(false))
            {
                var label = button.GetComponentsInChildren<TMP_Text>(false).FirstOrDefault(t => t.name == "Label");
                if (label == null || string.IsNullOrEmpty(label.text)) continue;
                var size = ((RectTransform)button.transform).rect.size;
                bool primary = button.GetComponent<Image>().sprite == Resources.Load<Sprite>("solo/production/solo_primary_cta_v1");
                float w = size.x * (primary ? .86f : .84f), h = size.y * (primary ? .65f : .70f);
                float cy = size.y * (primary ? .035f : .02f);
                AuditFace(label, (RectTransform)button.transform, new Rect(-w / 2, cy - h / 2, w, h),
                    language + " " + state + " " + viewport, violations);
            }
        }
        Assert.That(violations, Is.Empty, string.Join("\n", violations));
    }

    [UnityTest, Explicit("Native presentation fixtures; choose a new external evidence folder in HOL/PvP first.")]
    public IEnumerator CaptureNativeEnElFlowAndTallViewports()
    {
        yield return CaptureNative(false);
    }

    [UnityTest, Explicit("Bounded native pre-match evidence; choose a new external folder in HOL/PvP.")]
    public IEnumerator CaptureNativePrematchEnElAndRepresentativeTall()
    {
        yield return CaptureNative(true);
    }

    [UnityTest, Explicit("Bounded room-identity evidence; choose a new external folder in HOL/PvP.")]
    public IEnumerator CaptureNativeRoomIdentitiesEnElAndTall()
    {
        T("PvpPresentationReviewTools").GetMethod("FocusNativeGameView").Invoke(null, null);
        yield return CaptureNative(false, true);
    }

    IEnumerator CaptureNative(bool prematchOnly, bool identityOnly = false)
    {
        var tool = T("PvpPresentationReviewTools");
        string output = (string)tool.GetProperty("OutputDirectory").GetValue(null);
        Assert.That(Directory.Exists(output), Is.True, "Choose the new external capture folder via HOL/PvP.");
        output = Path.Combine(output, "Native-" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));
        Assert.That(Directory.Exists(output), Is.False, "Never overwrite existing evidence.");
        Directory.CreateDirectory(output);
        yield return Build();
        foreach (var viewport in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2400), new Vector2Int(1179, 2556) })
        foreach (string language in new[] { "en", "el" })
        foreach (string state in identityOnly ? new[] { "Waiting", "GuestWaiting", "PlayerTurn", "ResultWin" } :
            prematchOnly ? PrematchCases : Cases)
        {
            if (identityOnly && viewport.y != 1920 &&
                (viewport.y != 2400 || language != "el" || (state != "PlayerTurn" && state != "ResultWin"))) continue;
            if (viewport.y != 1920 && state != "PrivateRoom" && state != "Waiting" &&
                (prematchOnly || (state != "PlayerTurn" && state != "ResultWin"))) continue;
            T("OnboardingGameViewCapture").GetMethod("SetResolution").Invoke(null, new object[] { viewport.x, viewport.y });
            // A previous scene test may restore Screen's runtime size while
            // Game View already has this selectedSizeIndex. Re-selecting that
            // same index does not request a resize; set the real runtime size
            // too, then retain the exact dimensional gate below.
            Screen.SetResolution(viewport.x, viewport.y, false);
            SetLanguage(language);
            if (identityOnly && state == "GuestWaiting")
            {
                Invoke(controller, "OnLeaveMatchPressed");
                StartJoined();
                var waiting = State("waiting");
                S(waiting, "guestName", "Player"); S(waiting, "guestAvatarId", "6");
                Emit(waiting);
            }
            else if (prematchOnly) ShowPrematchCase(state); else ShowCase(state);
            yield return null;
            yield return null;
            yield return (IEnumerator)tool.GetMethod("WaitForStableNativeViewport")
                .Invoke(null, new object[] { root.transform, viewport.x, viewport.y });
            Canvas.ForceUpdateCanvases();
            Assert.That(Screen.width, Is.EqualTo(viewport.x));
            Assert.That(Screen.height, Is.EqualTo(viewport.y));
            string path = Path.Combine(output, "fixture-" + state + "-" + language + "-" + viewport.x + "x" + viewport.y + ".png");
            Assert.That(File.Exists(path), Is.False);
            tool.GetMethod("WriteViewportMetrics").Invoke(null, new object[] { path + ".geometry.json", root.transform,
                new[] { "PvpDuelCartoonRootSafeRoot", "PvpMatchLogo", "PvpPlayerCard", "PvpInteractionCard", "PvpHistoryCard" } });
            // Unity queues the next fully rendered Game View frame. The
            // bounded file/dimension checks remain independent of Editor EOF
            // coroutine scheduling, which can stall in a Simulator layout.
            ScreenCapture.CaptureScreenshot(path);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!File.Exists(path) || new FileInfo(path).Length == 0) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(File.Exists(path), Is.True, "Native screenshot deadline: " + path);
            var image = new Texture2D(2, 2);
            Assert.That(image.LoadImage(File.ReadAllBytes(path)), Is.True);
            Assert.That(image.width, Is.EqualTo(viewport.x));
            Assert.That(image.height, Is.EqualTo(viewport.y));
            UnityEngine.Object.Destroy(image);
        }
        File.WriteAllText(Path.Combine(output, "EVIDENCE.txt"),
            "Editor-only deterministic transport fixtures. Real production visuals/controller callbacks; NOT a two-client PlayFab test. No player name/avatar preference was changed.");
        Debug.Log("HOL_PVP_FIXTURE_CAPTURE_COMPLETE " + output);
    }

    void AuditGlyphs(string context, List<string> errors)
    {
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(false))
        {
            if (!text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text)) continue;
            var input = text.GetComponentInParent<TMP_InputField>();
            if (input != null && text == input.placeholder && !string.IsNullOrEmpty(input.text)) continue;
            text.ForceMeshUpdate();
            if (text.textInfo == null || text.textInfo.characterCount == 0) continue;
            var rect = text.rectTransform.rect;
            bool hasGlyph = false;
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            for (int i = 0; i < text.textInfo.characterCount; i++)
            {
                var ch = text.textInfo.characterInfo[i];
                if (!ch.isVisible) continue;
                hasGlyph = true;
                minX = Mathf.Min(minX, ch.bottomLeft.x);
                maxX = Mathf.Max(maxX, ch.topRight.x);
                minY = Mathf.Min(minY, ch.bottomLeft.y);
                maxY = Mathf.Max(maxY, ch.topRight.y);
            }
            if (hasGlyph && (minX < rect.xMin - 1 || maxX > rect.xMax + 1 ||
                minY < rect.yMin - 1 || maxY > rect.yMax + 1 || text.isTextOverflowing || text.isTextTruncated))
                errors.Add(context + " " + text.name + " font=" + text.fontSize +
                    " rect=" + rect + " glyph=[" + minX + "," + minY + "," + maxX + "," + maxY + "] text=" + text.text);
            if (hasGlyph && (text.name == "RevealedNumber" || text.name == "PvpResultStreak"))
            {
                // The 9-slice has a substantial lower rim: a label can fit its
                // own rect yet touch the artwork. Check its actual glyphs in
                // the independently measured inner face of the stats panel.
                var owner = (RectTransform)text.transform.parent;
                var bottom = owner.InverseTransformPoint(text.transform.TransformPoint(new Vector3(minX, minY)));
                var top = owner.InverseTransformPoint(text.transform.TransformPoint(new Vector3(maxX, maxY)));
                if (bottom.x < -430 || top.x > 430 || bottom.y < 20 || top.y > owner.rect.height / 2 - 24)
                    errors.Add(context + " " + text.name + " touches stats frame: " + bottom + " to " + top);
            }
            if (hasGlyph && (text.transform.parent.name == "YouCard" || text.transform.parent.name == "OpponentCard"))
            {
                var panel = (RectTransform)text.transform.parent;
                float bottom = panel.InverseTransformPoint(text.transform.TransformPoint(new Vector3(minX, minY))).y;
                if (bottom < -115)
                    errors.Add(context + " " + text.name + " touches prebattle card lower rim: " + bottom);
                if (bottom < -95)
                    errors.Add(context + " " + text.name + " leaves the inset waiting text face: " + bottom);
            }
            if (hasGlyph)
            {
                var parent = text.transform.parent as RectTransform;
                Rect? inner = null;
                if (parent.name == "PvpPromptRibbon" && text.name != "Result")
                    inner = new Rect(-298, -54, 596, 129);
                if (text.name == "PvpCurrentNumberHeading")
                    inner = new Rect(-304, -450, 590, parent.rect.height / 2 + 426);
                if (text.name == "PvpPlayerWins" || text.name == "PvpOpponentAttempts")
                    inner = new Rect(-225, -270, 450, 50);
                if (parent.name == "PvpTipCard")
                    inner = new Rect(-parent.rect.width / 2 + 18, -parent.rect.height / 2 + 20,
                        parent.rect.width - 36, parent.rect.height - 44);
                if (parent.name == "PvpSignalBubble")
                    inner = new Rect(-116, -53, 232, 122);
                if (inner.HasValue)
                {
                    Vector3 bottom = parent.InverseTransformPoint(text.transform.TransformPoint(new Vector3(minX, minY)));
                    Vector3 top = parent.InverseTransformPoint(text.transform.TransformPoint(new Vector3(maxX, maxY)));
                    var face = inner.Value;
                    if (bottom.x < face.xMin - 1 || top.x > face.xMax + 1 ||
                        bottom.y < face.yMin - 1 || top.y > face.yMax + 1)
                        errors.Add(context + " " + text.name + " leaves artwork inner face: " + bottom + " to " + top + " in " + face);
                }
            }
        }
    }

    void StartJoined()
    {
        Invoke(controller, "OnLeaveMatchPressed");
        Find(root.transform, "JoinButton").GetComponent<Button>().onClick.Invoke();
        ((TMP_InputField)Get(controller, "joinCodeInput")).text = "MTW8H";
        ((TMP_InputField)Get(controller, "joinSecretInput")).text = "80";
        ((GameObject)Get(controller, "joinConfirmButton")).GetComponent<Button>().onClick.Invoke();
    }

    void ShowCase(string state)
    {
        Invoke(controller, "OnLeaveMatchPressed");
        if (state == "PrivateRoom") return;
        if (state == "CreateEntry" || state == "Waiting")
        {
            Find(root.transform, "CreateButton").GetComponent<Button>().onClick.Invoke();
            if (state == "Waiting")
            {
                ((TMP_InputField)Get(controller, "createSecretInput")).text = "80";
                ((GameObject)Get(controller, "createConfirmButton")).GetComponent<Button>().onClick.Invoke();
                Emit(State("waiting"));
            }
            return;
        }
        if (state == "JoinEntry") { Find(root.transform, "JoinButton").GetComponent<Button>().onClick.Invoke(); return; }
        StartJoined();
        PlayerPrefs.SetInt("LockIntroShown", state == "LockIntro" ? 0 : 1);
        PlayerPrefs.SetInt("LockEverUsed", state == "LockIntro" ? 0 : 1);
        var live = State("play");
        Emit(live);
        if (state == "ConnectionLost") { Invoke(controller, "HandleConnectionLost"); return; }
        S(live, "hostGuessCount", 1);
        S(live, "guestGuessCount", 1);
        S(live, "roundIndex", 2);
        S(live, "lastBy", "guest");
        S(live, "lastGuess", 50);
        S(live, "lastHint", "lower");
        S(live, "lastLocked", state == "LockMiss");
        S(live, "turn", state == "OpponentTurn" ? "host" : "guest");
        Emit(live);
        if (state == "PlayerTurn")
        {
            // Retain both accepted events for a useful real-owner comparison
            // with Solo: our 50 was lower, then the opponent's 50 was higher.
            S(live, "hostGuessCount", 2);
            S(live, "lastBy", "host");
            S(live, "lastGuess", 50);
            S(live, "lastHint", "higher");
            S(live, "lastLocked", false);
            Emit(live);
        }
        if (state == "LockArmed") Invoke(controller, "OnLockTogglePressed");
        if (state == "LockSuggested")
        {
            Set(controller, "myMin", 48);
            Set(controller, "myMax", 50);
            Invoke(controller, "UpdateRangeText");
            Invoke(controller, "RefreshLockButton");
        }
        if (state == "Signal") Invoke(controller, "OnSignalPressed", 5);
        if (state == "IncomingSignal")
        {
            S(live, "signalSeq", 1);
            S(live, "signalBy", "host");
            S(live, "signalId", 5);
            Emit(live);
        }
        if (state == "SignalsOpen")
        {
            Invoke(root.GetComponent(T("PvpDuelCartoonVisuals")), "RefreshMatchPresentation");
            Find(root.transform, "OpenSignals").GetComponent<Button>().onClick.Invoke();
        }
        if (!state.StartsWith("Result", StringComparison.Ordinal) && state != "RematchWaiting") return;
        var done = State("done");
        S(done, "winner", state == "ResultDraw" ? "draw" : state == "ResultLoss" ? "host" : "guest");
        S(done, "revealedSecret", 73);
        S(done, "hostGuessCount", 4);
        S(done, "guestGuessCount", 3);
        Emit(done);
        if (state == "RematchWaiting")
        {
            ((TMP_InputField)Get(controller, "rematchSecretInput")).text = "64";
            ((GameObject)Get(controller, "rematchButton")).GetComponent<Button>().onClick.Invoke();
        }
    }

    object State(string phase)
    {
        object state = Activator.CreateInstance(T("PvpBackend").GetNestedType("RoomState"));
        S(state, "phase", phase); S(state, "turn", "guest"); S(state, "matchIndex", 0);
        S(state, "hostName", "Κωνσταντίνος"); S(state, "guestName", phase == "waiting" ? "" : "Player"); S(state, "opener", "host");
        S(state, "hostAvatarId", "1"); S(state, "guestAvatarId", phase == "waiting" ? "" : "6");
        return state;
    }
    void Emit(object state) { Invoke(backend, "Emit", state); }
    static void SetLanguage(string language)
    {
        var method = T("L10n").GetMethod("SetLanguage");
        var type = method.GetParameters()[0].ParameterType;
        method.Invoke(null, new[] { Enum.Parse(type, language == "el" ? "Greek" : "English") });
    }
    static string L(string key, params object[] args) => (string)T("L10n").GetMethod("Get", new[] { typeof(string), typeof(object[]) }).Invoke(null, new object[] { key, args });
    static void S(object target, string name, object value) { target.GetType().GetField(name).SetValue(target, value); }
    static void Set(Component c, string name, object value) { c.GetType().GetField(name, Flags).SetValue(c, value); }
    static object Get(Component c, string name) => c.GetType().GetField(name, Flags).GetValue(c);
    static object Invoke(Component c, string name, params object[] args) =>
        c.GetType().GetMethods(Flags).First(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(c, args);
    static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    static Transform Find(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent) { var found = Find(child, name); if (found != null) return found; }
        return null;
    }
}
