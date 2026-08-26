using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class SoloSearchCartoonVisualsPlayModeTests
{
    [UnityTest]
    public IEnumerator SearchScreenUsesApprovedRadarAndSingleCancelableAiLifecycle()
    {
        Screen.SetResolution(1080, 1920, false);
        InvokeStatic("SoloSearchVisuals", "Bootstrap");
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

        Component matchmaking = null;
        Component visuals = null;
        for (int frame = 0; frame < 120; frame++)
        {
            matchmaking = FindInScene(RuntimeType("FakeMatchmaking"));
            visuals = FindInScene(RuntimeType("SoloSearchVisuals"));
            if (matchmaking != null && visuals != null)
                break;
            yield return null;
        }

        Assert.That(matchmaking, Is.Not.Null);
        Assert.That(visuals, Is.Not.Null);
        Assert.That(CountInScene(RuntimeType("SoloSearchVisuals")), Is.EqualTo(1));

        GameObject searchPanel = GetField<GameObject>(matchmaking, "searchingPanel");
        GameObject gamePanel = GetField<GameObject>(matchmaking, "panelGame");
        var layout = gamePanel.GetComponentInChildren(
            RuntimeType("HolDuelBoardLayout"), true) as Behaviour;
        Assert.That(layout, Is.Not.Null,
            "The Search lifecycle must wait on the real Solo board owner.");

        // Hold the real board's Start lifecycle so the modal can be inspected
        // without a fake timer or a test-only production delay field.
        layout.enabled = false;
        Invoke(matchmaking, "StartSearch");
        for (int frame = 0; frame < 120 &&
             !GetProperty<bool>(visuals, "IsReady"); frame++)
            yield return null;

        Assert.That(GetProperty<bool>(visuals, "IsReady"), Is.True,
            "The inactive modal owner must become ready after StartSearch activates it.");
        Assert.That(searchPanel.activeInHierarchy, Is.True);
        Assert.That(gamePanel.activeSelf, Is.True,
            "The real board must initialize behind the blocking modal.");
        Assert.That(GetProperty<bool>(matchmaking, "IsPreparing"), Is.True);

        Transform root = Find(searchPanel.transform, "SoloSearchVisualRoot");
        Assert.That(root, Is.Not.Null);

        foreach (string name in new[]
        {
            "SearchBackground",
            "SearchStars",
            "SearchConfetti",
            "SearchOuterFrame",
            "SoloSearchSafeRoot",
            "SearchBackButton",
            "SearchBackIcon",
            "SearchPlayerChip",
            "SearchPlayerAvatar",
            "SearchStreakIcon",
            "SearchLogo",
            "SearchTitleRibbon",
            "SearchTitle",
            "SearchCard",
            "SearchPlayer",
            "SearchModeBadge",
            "SearchRadarRoot",
            "SearchRadarBase",
            "SearchRadarSweep",
            "SearchStatus",
            "CancelButton",
            "SearchMascotSix",
            "SearchMascotSeven",
        })
        {
            Assert.That(Find(root, name), Is.Not.Null,
                "Missing approved Search object: " + name);
        }

        AssertRect(root, "SearchLogo",
            new Vector2(0f, 690f), new Vector2(585f, 310f));
        AssertRect(root, "SearchTitleRibbon",
            new Vector2(0f, 495f), new Vector2(900f, 150f));
        AssertRect(root, "SearchCard",
            new Vector2(0f, 70f), new Vector2(940f, 650f));
        AssertRect(root, "SearchRadarRoot",
            new Vector2(35f, 25f), new Vector2(340f, 340f));
        AssertRect(root, "CancelButton",
            new Vector2(0f, -500f), new Vector2(520f, 112f));
        AssertRect(root, "SearchMascotSix",
            new Vector2(-410f, -790f), new Vector2(265f, 300f));
        AssertRect(root, "SearchMascotSeven",
            new Vector2(410f, -790f), new Vector2(265f, 300f));

        AssertSprite(root, "SearchRadarBase", "cartoon/cartoon_radar_base");
        AssertSprite(root, "SearchRadarSweep", "cartoon/cartoon_radar_sweep");
        Assert.That(Find(root, "SearchRadarSweep").GetComponent(
            RuntimeType("CartoonRadarSweep")), Is.Not.Null,
            "Radar animation must rotate an Image sprite, not draw a procedural Graphic.");

        TMP_Text status = Find(root, "SearchStatus").GetComponent<TMP_Text>();
        Assert.That(status.text, Does.StartWith(Localized("solo_ai_preparing")));
        Assert.That(status.enableAutoSizing, Is.True);
        Assert.That(status.fontSizeMin, Is.GreaterThanOrEqualTo(27f));
        Assert.That(status.GetComponent(RuntimeType("AnimatedEllipsis")), Is.Not.Null);

        Image blocker = Find(root, "SearchBackground").GetComponent<Image>();
        Assert.That(blocker.raycastTarget, Is.True,
            "Search modal must block Home input while preparation is active.");

        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            Assert.That(IsAllowedProductionGraphic(graphic), Is.True,
                "Procedural Graphic found in Search: " + graphic.GetType().Name);
            if (graphic is Image image && image.sprite != null)
                Assert.That(image.color.a, Is.GreaterThanOrEqualTo(0.99f),
                    image.name + " hides approved production art.");
        }

        Button cancel = Find(root, "CancelButton").GetComponent<Button>();
        Button back = Find(root, "SearchBackButton").GetComponent<Button>();
        Assert.That(cancel, Is.Not.Null);
        Assert.That(back, Is.Not.Null);

        cancel.onClick.Invoke();
        yield return null;
        Assert.That(searchPanel.activeSelf, Is.False);
        Assert.That(gamePanel.activeSelf, Is.False);
        Assert.That(GetProperty<bool>(matchmaking, "IsPreparing"), Is.False);

        Invoke(matchmaking, "StartSearch");
        yield return null;
        back.onClick.Invoke();
        yield return null;
        Assert.That(searchPanel.activeSelf, Is.False);
        Assert.That(gamePanel.activeSelf, Is.False);

        // Re-enable the actual board owner. Completion now depends only on its
        // real keypad/submit construction and not on a fixed waiting period.
        layout.enabled = true;
        Invoke(matchmaking, "StartSearch");
        for (int frame = 0; frame < 120 &&
             GetProperty<bool>(matchmaking, "IsPreparing"); frame++)
            yield return null;
        yield return new WaitForEndOfFrame();

        Assert.That(searchPanel.activeSelf, Is.False);
        Assert.That(gamePanel.activeSelf, Is.True);
        Assert.That(GetProperty<bool>(matchmaking, "IsPreparing"), Is.False);
    }

    static bool IsAllowedProductionGraphic(Graphic graphic)
    {
        if (graphic is Image || graphic is TMP_Text)
            return true;

        var subMesh = graphic as TMP_SubMeshUI;
        return subMesh != null &&
               subMesh.transform.parent != null &&
               subMesh.transform.parent.GetComponent<TMP_Text>() != null;
    }

    static string Localized(string key)
    {
        MethodInfo get = RuntimeType("L10n").GetMethod(
            "Get", BindingFlags.Public | BindingFlags.Static);
        Assert.That(get, Is.Not.Null);
        return (string)get.Invoke(null, new object[] { key, new object[0] });
    }

    static void AssertSprite(Transform root, string name, string resource)
    {
        Image image = Find(root, name).GetComponent<Image>();
        Sprite sprite = Resources.Load<Sprite>(resource);
        Assert.That(sprite, Is.Not.Null, resource);
        Assert.That(image.sprite, Is.SameAs(sprite), name);
        Assert.That(image.type, Is.EqualTo(Image.Type.Simple), name);
        Assert.That(image.color.a, Is.EqualTo(1f).Within(0.001f), name);
        Assert.That(image.raycastTarget, Is.False, name);
    }

    static void AssertRect(
        Transform root,
        string name,
        Vector2 position,
        Vector2 size)
    {
        RectTransform rect = Find(root, name) as RectTransform;
        Assert.That(rect, Is.Not.Null, name);
        Assert.That(Vector2.Distance(rect.anchoredPosition, position),
            Is.LessThan(1f), name + " position drifted.");
        Assert.That(Vector2.Distance(rect.sizeDelta, size),
            Is.LessThan(1f), name + " size drifted.");
    }

    static void InvokeStatic(string typeName, string methodName)
    {
        MethodInfo method = RuntimeType(typeName).GetMethod(
            methodName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, typeName + "." + methodName);
        method.Invoke(null, null);
    }

    static object Invoke(Component target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName, BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, methodName);
        return method.Invoke(target, null);
    }

    static T GetField<T>(Component component, string name) where T : class
    {
        FieldInfo field = component.GetType().GetField(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return field.GetValue(component) as T;
    }

    static T GetProperty<T>(Component component, string name)
    {
        PropertyInfo property = component.GetType().GetProperty(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, name);
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

    static int CountInScene(Type type)
    {
        int count = 0;
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            count += root.GetComponentsInChildren(type, true).Length;
        return count;
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
        Assert.That(type, Is.Not.Null, name);
        return type;
    }
}
