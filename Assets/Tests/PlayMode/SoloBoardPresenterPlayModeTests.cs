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
        layout = FindInScene(RuntimeType("HolDuelBoardLayout"));
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
            Is.EqualTo(Localized("enter_your_number")));
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
    public IEnumerator PlayerOpeningPublishesRangeHistoriesAndAnswerLockTruthfully()
    {
        StartWithOpener("Host", 101);
        SetField(game, "aiSecretNumber", 90);

        AssertPhase("PlayerGuess");
        Assert.That(((TMP_Text)Field(game, "turnText")).text, Is.EqualTo(Localized("your_guess")));
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
        AssertPhase("AnswerOpponent");
        Assert.That(Property(State(), "RoundNumber"), Is.EqualTo(1),
            "Acknowledging the closing guess still belongs to the round it was made in.");
        Assert.That(History("AiGuessHistory"), Has.Length.EqualTo(1));
        Assert.That(input.gameObject.activeSelf, Is.False);
        Assert.That(Submit().gameObject.activeSelf, Is.False);

        GameObject[] actions = AnswerActions();
        Assert.That(actions.Count(action => action.activeSelf), Is.EqualTo(1));
        actions.Single(action => action.activeSelf).GetComponent<Button>().onClick.Invoke();

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
    public IEnumerator AiOpeningRequiresTruthfulAcknowledgementBeforeNumericInput()
    {
        StartWithOpener("Guest", 101);
        AssertPhase("OpponentThinking");
        Assert.That(input.gameObject.activeSelf, Is.False);

        Invoke(game, "AIGuess");
        AssertPhase("AnswerOpponent");
        Assert.That(History("AiGuessHistory"), Has.Length.EqualTo(1));
        Assert.That(AnswerActions().Count(action => action.activeSelf), Is.EqualTo(1));
        Assert.That(input.gameObject.activeSelf, Is.False);
        Assert.That(Submit().gameObject.activeSelf, Is.False);

        AnswerActions().Single(action => action.activeSelf).GetComponent<Button>().onClick.Invoke();
        AssertPhase("PlayerGuess");
        Assert.That(Property(State(), "RoundNumber"), Is.EqualTo(1));
        Assert.That(input.gameObject.activeSelf, Is.True);
        Assert.That(Submit().gameObject.activeSelf, Is.True);
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
                Is.EqualTo(Localized("enter_your_number")));
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
        Type side = RuntimeType("DuelRules").GetNestedType("Side");
        MethodInfo start = game.GetType().GetMethod("StartGameWithOpener",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(start, Is.Not.Null);
        start.Invoke(game, new[] { Enum.Parse(side, opener) });
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
