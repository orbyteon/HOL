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
        "Language", "LockIntroShown", "LockEverUsed" };
    readonly Dictionary<string, int?> savedStats = new Dictionary<string, int?>();
    object savedLanguage;
    GameObject root;
    Component controller, backend;

    [SetUp]
    public void PreservePlayerState()
    {
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
        root.AddComponent(T("PrivateRoomVisuals"));
        yield return null;
        yield return null;
        Assert.That((bool)root.GetComponent(T("PvpDuelCartoonVisuals")).GetType().GetProperty("IsReady")
            .GetValue(root.GetComponent(T("PvpDuelCartoonVisuals"))), Is.True);
        Invoke(controller, "OpenPvpMenu");
        yield return null;
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
        foreach (var name in new[] { "PrivateRoomPlayerAvatar", "PvpMatchPlayerChipAvatar", "PvpResultPlayerChipAvatar" })
        {
            var portrait = Find(root.transform, name).GetComponent<Image>();
            Assert.That(portrait.sprite, Is.SameAs(selected), name);
            Assert.That(portrait.raycastTarget, Is.False);
            Assert.That(portrait.transform.parent.GetComponent<Mask>(), Is.Not.Null);
        }
    }

    [UnityTest]
    public IEnumerator ApprovedSpritesKeepTheirMeshCornersAndOpaqueNormalFace()
    {
        yield return Build();
        foreach (var name in new[] { "PvpVsBurst", "PvpResultTrophy", "Rocket" })
        {
            var art = Find(root.transform, name).GetComponent<Graphic>();
            Assert.That(art.GetType().FullName, Is.EqualTo("Unity.VectorGraphics.SVGImage"), name);
            var sprite = (Sprite)art.GetType().GetProperty("sprite").GetValue(art);
            Assert.That(sprite, Is.Not.Null, name);
            Assert.That(sprite.vertices.Length, Is.GreaterThan(3), name);
            Assert.That(art.color, Is.EqualTo(Color.white), name);
        }
        Assert.That(Find(root.transform, "PvpSignalBubble").GetComponent<Image>().sprite,
            Is.SameAs(Resources.Load<Sprite>("cartoon/cartoon_speech_bubble_raster")));
        foreach (var name in new[] { "Key1", "LockButton", "SubmitGuessButton", "ResultConfirmRematchButton", "PvpMatchPlayerChip" })
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
        int matches = PlayerPrefs.GetInt("StatMatches");
        Emit(done);
        Emit(done);
        Assert.That(PlayerPrefs.GetInt("StatMatches"), Is.EqualTo(matches + 1), "One authoritative result records once.");
        var result = (Component)Get(controller, "resultPresentation");
        Assert.That(result.gameObject.activeSelf, Is.True);
        SetLanguage("en");
        yield return null;
        var role = Find(root.transform, "OpponentAttemptsRowCaption").GetComponent<TMP_Text>();
        string english = role.text;
        SetLanguage("el");
        yield return null;
        Assert.That(role.text, Is.Not.EqualTo(english));
        Assert.That(role.text, Is.EqualTo(L("prebattle_opponent")));
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

    static readonly string[] Cases = { "PrivateRoom", "CreateEntry", "JoinEntry", "Waiting",
        "PlayerTurn", "OpponentTurn", "LockMiss", "LockIntro", "LockArmed", "LockSuggested", "Signal",
        "ResultWin", "ResultLoss", "ResultDraw", "RematchWaiting", "ConnectionLost" };

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
            Canvas.ForceUpdateCanvases();
            AuditGlyphs(language + " " + state + " " + viewport, violations);
        }
        Assert.That(violations, Is.Empty, string.Join("\n", violations));
    }

    [UnityTest, Explicit("Native presentation fixtures; choose a new external evidence folder in HOL/PvP first.")]
    public IEnumerator CaptureNativeEnElFlowAndTallViewports()
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
        foreach (string state in Cases)
        {
            if (viewport.y != 1920 && state != "PrivateRoom" && state != "Waiting" && state != "PlayerTurn" && state != "ResultWin") continue;
            T("OnboardingGameViewCapture").GetMethod("SetResolution").Invoke(null, new object[] { viewport.x, viewport.y });
            // A previous scene test may restore Screen's runtime size while
            // Game View already has this selectedSizeIndex. Re-selecting that
            // same index does not request a resize; set the real runtime size
            // too, then retain the exact dimensional gate below.
            Screen.SetResolution(viewport.x, viewport.y, false);
            SetLanguage(language);
            ShowCase(state);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(Screen.width, Is.EqualTo(viewport.x));
            Assert.That(Screen.height, Is.EqualTo(viewport.y));
            string path = Path.Combine(output, "fixture-" + state + "-" + language + "-" + viewport.x + "x" + viewport.y + ".png");
            Assert.That(File.Exists(path), Is.False);
            // Let Unity capture its next rendered frame. Waiting on an EOF
            // enumerator can stall while the Editor has another view focused.
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
                if (bottom.x < -398 || top.x > 398 || bottom.y < -105 || top.y > 160)
                    errors.Add(context + " " + text.name + " touches stats frame: " + bottom + " to " + top);
            }
            if (hasGlyph && (text.transform.parent.name == "YouCard" || text.transform.parent.name == "OpponentCard"))
            {
                var panel = (RectTransform)text.transform.parent;
                float bottom = panel.InverseTransformPoint(text.transform.TransformPoint(new Vector3(minX, minY))).y;
                if (bottom < -115)
                    errors.Add(context + " " + text.name + " touches prebattle card lower rim: " + bottom);
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
        if (state == "LockArmed") Invoke(controller, "OnLockTogglePressed");
        if (state == "LockSuggested")
        {
            Set(controller, "myMin", 48);
            Set(controller, "myMax", 50);
            Invoke(controller, "UpdateRangeText");
            Invoke(controller, "RefreshLockButton");
        }
        if (state == "Signal") Invoke(controller, "OnSignalPressed", 5);
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
        S(state, "hostName", "Κωνσταντίνος"); S(state, "guestName", "Player"); S(state, "opener", "host");
        return state;
    }
    void Emit(object state) { Invoke(backend, "Emit", state); }
    static void SetLanguage(string language)
    {
        var method = T("L10n").GetMethod("SetLanguage");
        var type = method.GetParameters()[0].ParameterType;
        method.Invoke(null, new[] { Enum.Parse(type, language == "el" ? "Greek" : "English") });
    }
    static string L(string key) => (string)T("L10n").GetMethod("Get", new[] { typeof(string), typeof(object[]) }).Invoke(null, new object[] { key, Array.Empty<object>() });
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
