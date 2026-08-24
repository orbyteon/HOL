using System;
using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SoloDuelPresentationHardenerPlayModeTests
{
    [UnityTest]
    public IEnumerator ProductionRootOwnsVisibleControlsWithoutLegacyOverlap()
    {
        Screen.SetResolution(1080, 1920, false);
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

        NumberManager numberManager = null;
        GameManager gameManager = null;
        SoloDuelPresentationHardener hardener = null;

        for (int frame = 0; frame < 180; frame++)
        {
            numberManager = FindInScene<NumberManager>();
            gameManager = FindInScene<GameManager>();
            hardener = FindInScene<SoloDuelPresentationHardener>();
            if (numberManager != null && gameManager != null && hardener != null)
                break;
            yield return null;
        }

        Assert.That(numberManager, Is.Not.Null);
        Assert.That(gameManager, Is.Not.Null);
        Assert.That(hardener, Is.Not.Null);

        numberManager.gameObject.SetActive(true);
        for (int frame = 0; frame < 120 && !hardener.IsApplied; frame++)
            yield return null;

        Assert.That(hardener.IsApplied, Is.True);
        Assert.That(CountInScene<SoloDuelPresentationHardener>(), Is.EqualTo(1));

        Transform root = Find(numberManager.transform, "SoloDuelVisualRoot");
        Assert.That(root, Is.Not.Null);
        Assert.That(root.GetSiblingIndex(),
            Is.EqualTo(root.parent.childCount - 1),
            "The production root must render above retired scene presentation.");

        Assert.That(numberManager.playerGuessesPanel.activeSelf, Is.False,
            "The retired player-guesses panel must never overlay the cartoon board.");
        Assert.That(numberManager.aiGuessesPanel.activeSelf, Is.False,
            "The retired AI-guesses panel must never overlay the cartoon board.");

        Assert.That(numberManager.messageText.transform.IsChildOf(root), Is.True,
            "Validation feedback must remain visible inside the production root.");
        Assert.That(gameManager.stopGameButton.transform.IsChildOf(root), Is.True,
            "The real result/stop control must remain visible inside the production root.");

        Assert.That(gameManager.higherButton.activeSelf, Is.False);
        Assert.That(gameManager.correctButton.activeSelf, Is.False);
        Assert.That(gameManager.lowerButton.activeSelf, Is.False,
            "Answer controls must stay hidden during secret-number entry.");

        RectTransform heading = Find(root, "CurrentNumberHeading") as RectTransform;
        RectTransform value = numberManager.playerNumberText.rectTransform;
        RectTransform input = numberManager.numberInput.transform as RectTransform;
        RectTransform message = numberManager.messageText.rectTransform;

        AssertRect(value, new Vector2(0f, 355f), new Vector2(500f, 36f));
        AssertRect(input, new Vector2(0f, 270f), new Vector2(500f, 110f));
        AssertRect(message, new Vector2(0f, 208f), new Vector2(500f, 36f));

        AssertNoVerticalOverlap(heading, value, "heading/value");
        AssertNoVerticalOverlap(value, input, "value/input");
        AssertNoVerticalOverlap(input, message, "input/message");
    }

    static void AssertRect(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
    {
        Assert.That(rect, Is.Not.Null);
        Assert.That(Vector2.Distance(rect.anchoredPosition, position),
            Is.LessThan(1f), rect.name + " position drifted.");
        Assert.That(Vector2.Distance(rect.sizeDelta, size),
            Is.LessThan(1f), rect.name + " size drifted.");
    }

    static void AssertNoVerticalOverlap(
        RectTransform upper,
        RectTransform lower,
        string label)
    {
        Assert.That(upper, Is.Not.Null, label + " upper missing");
        Assert.That(lower, Is.Not.Null, label + " lower missing");

        float upperBottom = upper.anchoredPosition.y - upper.rect.height * 0.5f;
        float lowerTop = lower.anchoredPosition.y + lower.rect.height * 0.5f;
        Assert.That(upperBottom, Is.GreaterThanOrEqualTo(lowerTop),
            label + " bounds overlap.");
    }

    static T FindInScene<T>() where T : Component
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    static int CountInScene<T>() where T : Component
    {
        int count = 0;
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            count += root.GetComponentsInChildren<T>(true).Length;
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
}
