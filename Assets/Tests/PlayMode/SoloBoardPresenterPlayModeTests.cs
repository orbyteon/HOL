using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class SoloBoardPresenterPlayModeTests
{
    Component game;
    Component numberManager;
    Component layout;
    Component menuManager;
    GameObject panel;
    TMP_InputField input;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        for (int i = 0; i < 8; i++) yield return null;

        game = FindInScene(RuntimeType("GameManager"));
        numberManager = FindInScene(RuntimeType("NumberManager"));
        layout = FindInScene(RuntimeType("SoloDuelVisuals"));
        menuManager = FindInScene(RuntimeType("MenuManager"));
        Assert.That(game, Is.Not.Null);
        Assert.That(numberManager, Is.Not.Null);
        Assert.That(layout, Is.Not.Null);
        Assert.That(menuManager, Is.Not.Null);

        panel = numberManager.gameObject;
        panel.SetActive(true);
        for (int i = 0; i < 4; i++) yield return null;

        input = (TMP_InputField)Field(numberManager, "numberInput");
        Assert.That(input, Is.Not.Null);
        ((MonoBehaviour)game).CancelInvoke();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (game != null) ((MonoBehaviour)game).CancelInvoke();
        if (menuManager != null) ((MonoBehaviour)menuManager).CancelInvoke();
        yield return null;
    }

    [UnityTest]
    public IEnumerator SecretEntryUsesOneSubmitAndSuppressesTheSoftKeyboard()
    {
        AssertPhase("ChooseSecret");
        Assert.That(((TMP_Text)Field(game, "turnText")).text,
            Is.EqualTo(Localized("solo_choose_secret")));
        Assert.That(input.shouldHideMobileInput, Is.True);
        Assert.That(input.shouldHideSoftKeyboard, Is.True);

        Button[] visibleSubmits = panel.GetComponentsInChildren<Button>(true)
            .Where(button => (button.name == "ButtonConfirm" || button.name == "NumberSubmit") &&
                             button.gameObject.activeInHierarchy)
            .ToArray();
        Assert.That(visibleSubmits, Has.Length.EqualTo(1));
        Assert.That(visibleSubmits[0].name, Is.EqualTo("ButtonConfirm"));

        input.text = "42";
        visibleSubmits[0].onClick.Invoke();
        ((MonoBehaviour)game).CancelInvoke("AIGuess");

        Assert.That(Field(numberManager, "playerNumber"), Is.EqualTo(42));
        Assert.That(Property(State(), "RoundNumber"), Is.EqualTo(1));
        Assert.That(Property(State(), "Phase").ToString(),
            Is.EqualTo("PlayerGuess").Or.EqualTo("OpponentThinking"));

        TMP_Text round = Find(panel.transform, "RoundLabel").GetComponent<TMP_Text>();
        Assert.That(round.text, Is.EqualTo(Localized("round_label_open", 1)));
        Assert.That(round.text, Does.Not.Contain("/"));
        yield return null;
    }

    [UnityTest]
    public IEnumerator OnScreenKeypadEditsAndSubmitsTheLiveNumericInput()
    {
        AssertPhase("ChooseSecret");
        Assert.That(input.text, Is.Empty,
            "Instructional copy must be placeholder text, not the input value.");

        Button key4 = Find(panel.transform, "Key_4").GetComponent<Button>();
        Button key2 = Find(panel.transform, "Key_2").GetComponent<Button>();
        Assert.That(key4.interactable, Is.True);
        Assert.That(key2.interactable, Is.True);

        key4.onClick.Invoke();
        yield return null;
        Assert.That(input.text, Is.EqualTo("4"));
        Assert.That(RenderedInputText(), Is.EqualTo("4"));

        key2.onClick.Invoke();
        yield return null;
        Assert.That(input.text, Is.EqualTo("42"));
        Assert.That(RenderedInputText(), Is.EqualTo("42"));

        Submit().onClick.Invoke();
        ((MonoBehaviour)game).CancelInvoke("AIGuess");

        Assert.That(Field(numberManager, "playerNumber"), Is.EqualTo(42));
        Assert.That(Property(State(), "Phase").ToString(),
            Is.EqualTo("PlayerGuess").Or.EqualTo("OpponentThinking"));
    }

    [UnityTest]
    public IEnumerator SecretInputRejectsInvalidAndOutOfRangeValuesAndAcceptsLowerBoundary()
    {
        AssertPhase("ChooseSecret");
        TMP_Text message = (TMP_Text)Field(numberManager, "messageText");

        input.text = string.Empty;
        Submit().onClick.Invoke();
        AssertPhase("ChooseSecret");
        Assert.That(message.gameObject.activeSelf, Is.True);
        Assert.That(message.text, Is.EqualTo(Localized("invalid_number")));

        foreach (string rejected in new[] { "0", "101" })
        {
            input.text = rejected;
            Submit().onClick.Invoke();
            AssertPhase("ChooseSecret");
            Assert.That(message.gameObject.activeSelf, Is.True);
            Assert.That(message.text,
                Is.EqualTo(Localized("number_out_of_range")));
            Assert.That(Field(numberManager, "gameStarted"), Is.EqualTo(false));
        }

        input.text = "1";
        Submit().onClick.Invoke();
        ((MonoBehaviour)game).CancelInvoke("AIGuess");

        Assert.That(Field(numberManager, "playerNumber"), Is.EqualTo(1));
        Assert.That(Field(numberManager, "gameStarted"), Is.EqualTo(true));
        Assert.That(Property(State(), "Phase").ToString(),
            Is.EqualTo("PlayerGuess").Or.EqualTo("OpponentThinking"));
        yield return null;
    }

    [UnityTest]
    public IEnumerator PlayerOpeningPublishesAutomaticAiFeedbackThenReturnsToInput()
    {
        StartWithOpener("Host", 101);
        SetField(game, "aiSecretNumber", 90);

        AssertPhase("PlayerGuess");
        Assert.That(((TMP_Text)Field(game, "turnText")).text,
            Is.EqualTo(Localized("solo_guess_number")));
        Assert.That(input.interactable, Is.True);
        Assert.That(Submit().gameObject.activeSelf, Is.True);

        Assert.That(Invoke(game, "PlayerGuess", 50), Is.EqualTo(true));
        AssertPhase("OpponentThinking");
        Assert.That(History("PlayerGuessHistory"), Is.EqualTo(new[] { 50 }));
        Assert.That(Property(State(), "RangeMin"), Is.EqualTo(51));
        Assert.That(Property(State(), "RangeMax"), Is.EqualTo(100));
        Assert.That(input.interactable, Is.False);
        Assert.That(Submit().gameObject.activeSelf, Is.False);

        ((MonoBehaviour)game).CancelInvoke("AIGuess");
        Invoke(game, "AIGuess");
        ((MonoBehaviour)game).CancelInvoke("ResolveAiAnswerAutomatically");
        AssertPhase("AnswerOpponent");
        Assert.That(Property(State(), "RoundNumber"), Is.EqualTo(1),
            "Acknowledging the closing guess still belongs to the round it was made in.");
        Assert.That(History("AiGuessHistory"), Has.Length.EqualTo(1));
        Assert.That(input.gameObject.activeSelf, Is.False);
        Assert.That(Submit().gameObject.activeSelf, Is.False);

        Assert.That(AnswerActions().Any(action => action.activeSelf), Is.False,
            "Solo already knows the truthful hint; no manual answer controls should appear.");
        Assert.That(Find(panel.transform, "LockButton").gameObject.activeSelf, Is.False,
            "LOCK belongs only to the player's numeric turn.");

        object feedbackState = State();
        int publishedGuess = (int)Field(game, "aiGuess");
        Assert.That(Property(feedbackState, "Prompt").ToString(),
            Is.EqualTo("OpponentGuessedHigher"));
        Assert.That(Property(feedbackState, "DetailValue"), Is.EqualTo(publishedGuess));
        Assert.That(History("AiGuessHistory").Last(), Is.EqualTo(publishedGuess),
            "The final Solo owner must publish the accepted AI guess in its presentation state.");
        TMP_Text visiblePrompt = (TMP_Text)Field(game, "turnText");
        Assert.That(visiblePrompt.gameObject.activeInHierarchy, Is.True);
        Assert.That(visiblePrompt.text, Is.EqualTo(Localized("your_number_is_higher")),
            "The retired SoloPhaseFeedback object is replaced by the owner's live phase prompt.");
        Transform promptRibbon = Find(panel.transform, "SoloPromptRibbon");
        Assert.That(promptRibbon, Is.Not.Null);
        Assert.That(visiblePrompt.transform.parent, Is.SameAs(promptRibbon),
            "The final Solo presentation owner must seat the live prompt in its ribbon.");
        visiblePrompt.ForceMeshUpdate();
        Assert.That(visiblePrompt.isTextOverflowing, Is.False,
            "The semantic replacement for SoloPhaseFeedback must remain readable.");
        Assert.That(visiblePrompt.textBounds.size.x,
            Is.LessThanOrEqualTo(visiblePrompt.rectTransform.rect.width + 0.5f));
        Assert.That(visiblePrompt.textBounds.size.y,
            Is.LessThanOrEqualTo(visiblePrompt.rectTransform.rect.height + 0.5f));
        Assert.That(Find(panel.transform, "SoloPhaseFeedback"), Is.Null,
            "The retired feedback GameObject must not be recreated.");

        Invoke(game, "ResolveAiAnswerAutomatically");

        AssertPhase("PlayerGuess");
        Assert.That(Property(State(), "RoundNumber"), Is.EqualTo(2));
        Assert.That(input.gameObject.activeSelf, Is.True);
        Assert.That(input.interactable, Is.True);
        Assert.That(Submit().gameObject.activeSelf, Is.True);

        Assert.That(Invoke(game, "PlayerGuess", 50), Is.EqualTo(false));
        Assert.That(History("PlayerGuessHistory"), Is.EqualTo(new[] { 50 }),
            "An out-of-range rejection must not become a history event.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator RapidDuplicatePlayerSubmissionCreatesOnlyOneTurnAndHistoryEntry()
    {
        StartWithOpener("Host", 73);
        SetField(game, "aiSecretNumber", 90);

        Assert.That(Invoke(game, "PlayerGuess", 50), Is.EqualTo(true));
        Assert.That(Invoke(game, "PlayerGuess", 60), Is.EqualTo(false),
            "Turn ownership must reject a second submit before AI resolves.");
        Assert.That(History("PlayerGuessHistory"), Is.EqualTo(new[] { 50 }));
        Assert.That(Property(State(), "Phase").ToString(),
            Is.EqualTo("OpponentThinking"));
        ((MonoBehaviour)game).CancelInvoke("AIGuess");
        yield return null;
    }

    [UnityTest]
    public IEnumerator AiOpeningAutoResolvesBeforeNumericInput()
    {
        StartWithOpener("Guest", 101);
        AssertPhase("OpponentThinking");
        Assert.That(input.gameObject.activeSelf, Is.False);

        Invoke(game, "AIGuess");
        ((MonoBehaviour)game).CancelInvoke("ResolveAiAnswerAutomatically");
        AssertPhase("AnswerOpponent");
        Assert.That(History("AiGuessHistory"), Has.Length.EqualTo(1));
        Assert.That(AnswerActions().Any(action => action.activeSelf), Is.False);
        Assert.That(input.gameObject.activeSelf, Is.False);
        Assert.That(Submit().gameObject.activeSelf, Is.False);

        Invoke(game, "ResolveAiAnswerAutomatically");
        AssertPhase("PlayerGuess");
        Assert.That(Property(State(), "RoundNumber"), Is.EqualTo(1));
        Assert.That(input.gameObject.activeSelf, Is.True);
        Assert.That(Submit().gameObject.activeSelf, Is.True);
        yield return null;
    }

    [UnityTest]
    public IEnumerator AiFeedbackAdvancesWithoutAnyPlayerTap()
    {
        var difficulty = new PrefValue("AIDifficulty");
        try
        {
            PlayerPrefs.SetInt("AIDifficulty", 2);
            StartWithOpener("Guest", 80);
            Invoke(game, "AIGuess");

            AssertPhase("AnswerOpponent");
            Assert.That(AnswerActions().Any(action => action.activeSelf), Is.False);
            Assert.That(input.gameObject.activeSelf, Is.False);

            yield return new WaitForSeconds(2.7f);

            AssertPhase("PlayerGuess");
            Assert.That(input.gameObject.activeSelf, Is.True);
            Assert.That(Submit().gameObject.activeSelf, Is.True);
            Assert.That(Field(game, "min"), Is.EqualTo(51));
            Assert.That(Field(game, "max"), Is.EqualTo(100));
        }
        finally
        {
            difficulty.Restore();
        }
    }

    [UnityTest]
    public IEnumerator AutomaticAiFeedbackCannotBeCorruptedByLegacyCallbacks()
    {
        var difficulty = new PrefValue("AIDifficulty");
        try
        {
            // Hard opens at the midpoint. With the player's secret at 80 the
            // only truthful response to 50 is Higher.
            PlayerPrefs.SetInt("AIDifficulty", 2);
            StartWithOpener("Guest", 80);
            Invoke(game, "AIGuess");
            ((MonoBehaviour)game).CancelInvoke("ResolveAiAnswerAutomatically");

            AssertPhase("AnswerOpponent");
            Assert.That(Field(game, "aiGuess"), Is.EqualTo(50));
            Assert.That(AnswerActions().Any(action => action.activeSelf), Is.False);

            // A stale/miswired hidden callback must neither advance the turn
            // nor change the AI bounds.
            Invoke(game, "Lower");
            AssertPhase("AnswerOpponent");
            Assert.That(Field(game, "min"), Is.EqualTo(1));
            Assert.That(Field(game, "max"), Is.EqualTo(100));

            Invoke(game, "ResolveAiAnswerAutomatically");
            AssertPhase("PlayerGuess");
            Assert.That(Field(game, "min"), Is.EqualTo(51));
            Assert.That(Field(game, "max"), Is.EqualTo(100));

            // Duplicate input after acknowledgement is also a no-op.
            Invoke(game, "Lower");
            AssertPhase("PlayerGuess");
            Assert.That(Field(game, "min"), Is.EqualTo(51));
            Assert.That(Field(game, "max"), Is.EqualTo(100));
        }
        finally
        {
            difficulty.Restore();
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator ResultAndRematchResetHistoryAndRepublishOpponentIdentity()
    {
        string[] statKeys =
        {
            "StatWins", "StatLosses", "StatStreak", "StatBestStreak", "StatBestGuesses",
            "StatDraws", "StatMatches", "StatRecentBits", "StatRecentCount",
        };
        var prefs = statKeys.ToDictionary(key => key, key => new PrefValue(key));

        try
        {
            StartWithOpener("Host", 101);
            SetField(game, "aiSecretNumber", 77);
            game.GetType().GetField("adsManager").SetValue(game, null);

            Assert.That(Invoke(game, "PlayerGuess", 77), Is.EqualTo(true));
            ((MonoBehaviour)game).CancelInvoke("AIGuess");
            Invoke(game, "AIGuess");

            AssertPhase("MatchResult");
            Assert.That(((TMP_Text)Field(game, "turnText")).text,
                Does.StartWith(Localized("you_win")));
            Assert.That(input.gameObject.activeSelf, Is.False);
            Assert.That(Submit().gameObject.activeSelf, Is.False);
            Assert.That(History("PlayerGuessHistory"), Is.EqualTo(new[] { 77 }));
            Assert.That(History("AiGuessHistory"), Has.Length.EqualTo(1));

            Invoke(game, "RestartMatch");
            AssertPhase("ChooseSecret");
            Assert.That(History("PlayerGuessHistory"), Is.Empty);
            Assert.That(History("AiGuessHistory"), Is.Empty);
            Assert.That(Property(State(), "OpponentName"),
                Is.EqualTo(Property(game, "CurrentOpponentName")));
            Assert.That(((TMP_Text)Field(game, "turnText")).text,
                Is.EqualTo(Localized("solo_choose_secret")));
            Assert.That(input.gameObject.activeSelf, Is.True);
            Assert.That(Submit().gameObject.activeSelf, Is.True);
        }
        finally
        {
            foreach (PrefValue pref in prefs.Values) pref.Restore();
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator DuelBackUsesTheLiveMatchConfirmationPath()
    {
        string sceneBefore = SceneManager.GetActiveScene().name;
        Transform back = Find(panel.transform, "DuelBack");
        Assert.That(back, Is.Not.Null);

        back.GetComponent<Button>().onClick.Invoke();
        yield return null;

        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneBefore));
        Transform hint = Find(panel.transform, "BackExitHint");
        Assert.That(hint, Is.Not.Null);
        Assert.That(hint.gameObject.activeSelf, Is.True);
        Assert.That(hint.GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized("back_again_to_leave")));
    }

    void StartWithOpener(string opener, int playerSecret)
    {
        Invoke(game, "SetPlayerNumber", playerSecret);
        MethodInfo start = game.GetType().GetMethod("StartGameWithOpener",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(start, Is.Not.Null);
        ParameterInfo[] parameters = start.GetParameters();
        Assert.That(parameters, Has.Length.EqualTo(1),
            "StartGameWithOpener must take exactly one opener enum.");
        Assert.That(parameters[0].ParameterType.IsEnum, Is.True,
            "StartGameWithOpener opener must remain an enum.");
        start.Invoke(game,
            new[] { Enum.Parse(parameters[0].ParameterType, opener) });
        ((MonoBehaviour)game).CancelInvoke("AIGuess");
    }

    void AssertPhase(string expected)
    {
        Assert.That(Property(State(), "Phase").ToString(), Is.EqualTo(expected));
    }

    object State()
    {
        return Property(layout, "CurrentState");
    }

    int[] History(string name)
    {
        return ((IEnumerable)Property(State(), name)).Cast<object>()
            .Select(Convert.ToInt32).ToArray();
    }

    Button Submit()
    {
        return (Button)Property(layout, "SubmitControl");
    }

    string RenderedInputText()
    {
        // TMP appends a zero-width caret marker to its render string while
        // the field is active. It is not part of the submitted value.
        return input.textComponent.text.TrimEnd('\u200B');
    }

    GameObject[] AnswerActions()
    {
        return new[]
        {
            (GameObject)Field(game, "higherButton"),
            (GameObject)Field(game, "lowerButton"),
            (GameObject)Field(game, "correctButton"),
        };
    }

    static object Invoke(Component target, string method, params object[] arguments)
    {
        MethodInfo info = target.GetType().GetMethod(method,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(info, Is.Not.Null, "Missing method " + method);
        return info.Invoke(target, arguments);
    }

    static object Field(Component target, string name)
    {
        FieldInfo field = target.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Missing field " + name);
        return field.GetValue(target);
    }

    static void SetField(Component target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Missing field " + name);
        field.SetValue(target, value);
    }

    static object Property(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, "Missing property " + name);
        return property.GetValue(target);
    }

    static string Localized(string key, params object[] arguments)
    {
        MethodInfo get = RuntimeType("L10n").GetMethod("Get",
            BindingFlags.Public | BindingFlags.Static);
        return (string)get.Invoke(null, new object[] { key, arguments });
    }

    static Component FindInScene(Type type)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Component found = root.GetComponentsInChildren(type, true).FirstOrDefault();
            if (found != null) return found;
        }
        return null;
    }

    static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = Find(root.GetChild(i), name);
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

    sealed class PrefValue
    {
        readonly string key;
        readonly bool existed;
        readonly int value;

        public PrefValue(string key)
        {
            this.key = key;
            existed = PlayerPrefs.HasKey(key);
            value = PlayerPrefs.GetInt(key, 0);
        }

        public void Restore()
        {
            if (existed) PlayerPrefs.SetInt(key, value);
            else PlayerPrefs.DeleteKey(key);
        }
    }
}
