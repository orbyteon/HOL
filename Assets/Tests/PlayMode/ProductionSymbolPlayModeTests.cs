using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class ProductionSymbolPlayModeTests
{
    [UnityTest]
    public IEnumerator HomeTipInitializesWithoutUnsupportedStar()
    {
        int previousLanguage = PlayerPrefs.GetInt("Language", 0);
        bool hadLanguage = PlayerPrefs.HasKey("Language");
        PlayerPrefs.SetInt("Language", 0);

        GameObject host = null;
        try
        {
            host = new GameObject("SymbolTestHost", typeof(RectTransform));
            Component reskin = host.AddComponent(RuntimeType("AttachmentReskinVisuals"));
            ((Behaviour)reskin).enabled = false;

            var root = new GameObject("TipRoot", typeof(RectTransform));
            root.transform.SetParent(host.transform, false);
            MethodInfo buildTip = reskin.GetType().GetMethod("BuildTipCard",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(buildTip, Is.Not.Null);
            buildTip.Invoke(reskin, new object[] { root.transform });

            Transform titleTransform = Find(root.transform, "BoardHomeTipTitle");
            Assert.That(titleTransform, Is.Not.Null);
            TMP_Text title = titleTransform.GetComponent<TMP_Text>();
            Assert.That(title, Is.Not.Null);
            Assert.That(title.text, Is.EqualTo("TIP:"));
            Assert.That(title.text, Does.Not.Contain(char.ConvertFromUtf32(0x2605)));
        }
        finally
        {
            if (host != null) UnityEngine.Object.Destroy(host);
            if (hadLanguage) PlayerPrefs.SetInt("Language", previousLanguage);
            else PlayerPrefs.DeleteKey("Language");
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator SoloBackspaceShowsLeftArrowAndDeletesExactlyOneDigit()
    {
        var boardObject = new GameObject("PanelGAME", typeof(RectTransform));
        var inputObject = new GameObject("NumberInput", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        inputObject.transform.SetParent(boardObject.transform, false);

        Component numberManager = boardObject.AddComponent(RuntimeType("NumberManager"));
        var input = inputObject.GetComponent<TMP_InputField>();
        numberManager.GetType().GetField("numberInput").SetValue(numberManager, input);

        Component layout = boardObject.AddComponent(RuntimeType("HolDuelBoardLayout"));
        ((Behaviour)layout).enabled = false;
        MethodInfo build = layout.GetType().GetMethod("Build",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(build, Is.Not.Null);
        build.Invoke(layout, null);

        Transform backspaceTransform = Find(boardObject.transform, "Key_BACKSPACE");
        Assert.That(backspaceTransform, Is.Not.Null,
            "The visible label must not also be the internal command identifier.");
        var label = backspaceTransform.GetComponentInChildren<TMP_Text>();
        Assert.That(label, Is.Not.Null);
        Assert.That(label.text, Is.EqualTo(char.ConvertFromUtf32(0x2190)));

        input.text = "123";
        backspaceTransform.GetComponent<Button>().onClick.Invoke();
        Assert.That(input.text, Is.EqualTo("12"),
            "Backspace must remove exactly the final digit.");

        UnityEngine.Object.Destroy(boardObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator FinalSoloWinUsesApprovedTrophyImageWithoutTmpStar()
    {
        InvokeInstaller(RuntimeType("AttachmentReskinVisuals"));
        InvokeInstaller(RuntimeType("AttachmentReskinPolish"));

        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        for (int i = 0; i < 12; i++) yield return null;

        Component game = FindObject(RuntimeType("GameManager"));
        Component reskin = FindObject(RuntimeType("AttachmentReskinVisuals"));
        Component polish = FindObject(RuntimeType("AttachmentReskinPolish"));
        Assert.That(game, Is.Not.Null);
        Assert.That(reskin, Is.Not.Null);
        Assert.That(polish, Is.Not.Null);

        Type gameType = game.GetType();
        object rules = gameType.GetField("rules", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(game);
        rules.GetType().GetField("<Finished>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic).SetValue(rules, true);
        Type outcomeType = rules.GetType().GetNestedType("Outcome");
        rules.GetType().GetField("<Result>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(rules, Enum.Parse(outcomeType, "HostWins"));
        gameType.GetField("matchSetUp", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(game, true);

        var stopButton = (GameObject)gameType.GetField("stopGameButton").GetValue(game);
        Assert.That(stopButton, Is.Not.Null);
        Transform resultRoot = stopButton.transform.parent;
        resultRoot.gameObject.SetActive(true);
        var turnText = (TMP_Text)gameType.GetField("turnText").GetValue(game);
        turnText.text = Localized("you_win");

        MethodInfo applyResult = reskin.GetType().GetMethod("ApplySoloResult",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo polishResult = polish.GetType().GetMethod("PolishSoloResult",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(applyResult, Is.Not.Null);
        Assert.That(polishResult, Is.Not.Null);
        applyResult.Invoke(reskin, new object[] { game, resultRoot });
        polishResult.Invoke(polish, null);

        Transform legacyText = Find(resultRoot, "BoardSoloTrophy");
        Assert.That(legacyText == null || !legacyText.gameObject.activeSelf, Is.True);
        Transform trophyTransform = Find(resultRoot, "BoardSoloTrophyVector");
        Assert.That(trophyTransform, Is.Not.Null);
        var image = trophyTransform.GetComponent<Image>();
        var expected = Resources.Load<Sprite>("reference/board_trophy_exact");
        Assert.That(image, Is.Not.Null);
        Assert.That(expected, Is.Not.Null);
        Assert.That(image.sprite, Is.SameAs(expected));
    }

    static string Localized(string key)
    {
        Type l10n = RuntimeType("L10n");
        MethodInfo get = l10n.GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
        return (string)get.Invoke(null, new object[] { key, new object[0] });
    }

    static void InvokeInstaller(Type type)
    {
        MethodInfo install = type.GetMethod("Install", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(install, Is.Not.Null, "Missing runtime installer on " + type.Name);
        install.Invoke(null, null);
    }

    static Component FindObject(Type type)
    {
        return UnityEngine.Object.FindObjectOfType(type) as Component;
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
}
