using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ResponsiveUIFoundationTests
{
    const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    struct Region
    {
        public readonly string Name;
        public readonly Vector2 Position;
        public readonly Vector2 Size;

        public Region(string name, float x, float y, float width, float height)
        {
            Name = name;
            Position = new Vector2(x, y);
            Size = new Vector2(width, height);
        }
    }

    sealed class ScreenLayout
    {
        public readonly string Name;
        public readonly Region[] Regions;
        public readonly string[][] NonOverlap;

        public ScreenLayout(string name, Region[] regions, params string[][] nonOverlap)
        {
            Name = name;
            Regions = regions;
            NonOverlap = nonOverlap;
        }
    }

    [Test]
    public void RequiredViewportLanguageAndInsetMatrixContainsCriticalRegions()
    {
        Vector2[] viewports =
        {
            new Vector2(720f, 1280f),
            new Vector2(1080f, 1920f),
            new Vector2(1080f, 2400f),
            new Vector2(1440f, 3200f)
        };
        string[] languages = { "English", "Greek" };

        foreach (Vector2 viewport in viewports)
        {
            Rect[] safeAreas =
            {
                new Rect(0f, 0f, viewport.x, viewport.y),
                new Rect(0f, 0f, viewport.x, viewport.y * 0.92f),
                new Rect(0f, viewport.y * 0.05f, viewport.x, viewport.y * 0.87f)
            };
            Vector2 canvasSize = CanvasSize(viewport);

            foreach (Rect safe in safeAreas)
            foreach (string language in languages)
            foreach (ScreenLayout screen in CriticalScreens())
            {
                object geometry = Geometry(safe, viewport, canvasSize);
                Rect safeRect = Field<Rect>(geometry, "SafeRect");
                var bounds = new Dictionary<string, Rect>();
                foreach (Region region in screen.Regions)
                {
                    Rect box = Bounds(geometry, region);
                    bounds.Add(region.Name, box);
                    AssertContained(safeRect, box,
                        viewport + " / " + language + " / " + screen.Name +
                        " / " + region.Name);
                }

                foreach (string[] pair in screen.NonOverlap)
                    Assert.That(bounds[pair[0]].Overlaps(bounds[pair[1]]), Is.False,
                        viewport + " / " + language + " / " + screen.Name +
                        ": " + pair[0] + " overlaps " + pair[1]);
            }
        }
    }

    [Test]
    public void PageOwnerRegistersOnceAndRecalculationIsIdempotent()
    {
        var canvasObject = new GameObject("Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var pageObject = new GameObject("Page", typeof(RectTransform));
        var targetObject = new GameObject("CTA", typeof(RectTransform));
        try
        {
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var page = (RectTransform)pageObject.transform;
            page.SetParent(canvasObject.transform, false);
            Stretch(page);
            var target = (RectTransform)targetObject.transform;
            target.SetParent(page, false);
            Center(target, new Vector2(860f, 120f), new Vector2(0f, -820f));

            Type layoutType = RuntimeType("ResponsivePageLayout");
            MethodInfo register = layoutType.GetMethod("Register", StaticFlags);
            Assert.That(register, Is.Not.Null);
            Assert.That(register.Invoke(null, new object[]
            {
                target, new Vector2(860f, 120f), new Vector2(0f, -820f)
            }), Is.EqualTo(true));
            register.Invoke(null, new object[]
            {
                target, new Vector2(860f, 120f), new Vector2(0f, -820f)
            });

            var owner = pageObject.GetComponent(layoutType);
            Assert.That(owner, Is.Not.Null);
            Assert.That(Property<int>(owner, "RegisteredCount"), Is.EqualTo(1));

            Vector2 viewport = new Vector2(1080f, 2400f);
            Rect safe = new Rect(0f, 120f, 1080f, 2100f);
            Vector2 canvasSize = CanvasSize(viewport);
            MethodInfo apply = layoutType.GetMethod("ApplyViewport", InstanceFlags);
            apply.Invoke(owner, new object[]
            {
                new Rect(Vector2.zero, viewport), safe, canvasSize
            });
            Vector2 firstPosition = target.anchoredPosition;
            Vector3 firstScale = target.localScale;
            int firstCount = Property<int>(owner, "RecalculationCount");

            apply.Invoke(owner, new object[]
            {
                new Rect(Vector2.zero, viewport), safe, canvasSize
            });
            Assert.That(target.anchoredPosition, Is.EqualTo(firstPosition));
            Assert.That(target.localScale, Is.EqualTo(firstScale));
            Assert.That(Property<int>(owner, "RegisteredCount"), Is.EqualTo(1));
            Assert.That(Property<int>(owner, "RecalculationCount"),
                Is.EqualTo(firstCount + 1));
            AssertContained(Property<Rect>(owner, "LastSafeRect"),
                RectFor(target), "registered CTA");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void SafeRootRejectsNestedOwnershipAndReappliesWithoutDrift()
    {
        var canvasObject = new GameObject("Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler));
        var rootObject = new GameObject("SafeRoot", typeof(RectTransform));
        var nestedObject = new GameObject("Nested", typeof(RectTransform));
        try
        {
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasRect = (RectTransform)canvasObject.transform;
            var root = (RectTransform)rootObject.transform;
            root.SetParent(canvasRect, false);
            Stretch(root);
            var nested = (RectTransform)nestedObject.transform;
            nested.SetParent(root, false);
            Stretch(nested);

            Type ownerType = RuntimeType("ResponsiveSafeAreaRoot");
            MethodInfo attach = ownerType.GetMethod("Attach", StaticFlags);
            var owner = (Component)attach.Invoke(null, new object[]
            {
                root, canvasRect, new Vector2(1080f, 1920f)
            });
            Assert.That(owner, Is.Not.Null);
            Assert.That(attach.Invoke(null, new object[]
            {
                nested, canvasRect, new Vector2(1080f, 1920f)
            }), Is.Null, "Nested safe-area ownership must be rejected.");

            MethodInfo apply = ownerType.GetMethod("ApplyViewport", InstanceFlags);
            var viewport = new Vector2(1080f, 2400f);
            var safe = new Rect(0f, 120f, 1080f, 2160f);
            var canvasSize = CanvasSize(viewport);
            apply.Invoke(owner, new object[]
            {
                new Rect(Vector2.zero, viewport), safe, canvasSize
            });
            Vector2 firstMin = root.anchorMin;
            Vector2 firstMax = root.anchorMax;
            Vector3 firstScale = root.localScale;
            apply.Invoke(owner, new object[]
            {
                new Rect(Vector2.zero, viewport), safe, canvasSize
            });
            Assert.That(root.anchorMin, Is.EqualTo(firstMin));
            Assert.That(root.anchorMax, Is.EqualTo(firstMax));
            Assert.That(root.localScale, Is.EqualTo(firstScale));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void TextPolicyIsBoundedForEnglishAndGreekAndPreventsVerticalOverflow()
    {
        var go = new GameObject("ActionLabel", typeof(RectTransform),
            typeof(TextMeshProUGUI));
        try
        {
            var text = go.GetComponent<TMP_Text>();
            Type policy = RuntimeType("ResponsiveTextPolicy");
            Type role = RuntimeType("ResponsiveTextRole");
            MethodInfo configure = policy.GetMethod("Configure", StaticFlags);
            object action = Enum.Parse(role, "Action");

            foreach (string value in new[] { "Private Room", "Ιδιωτικό δωμάτιο" })
            {
                text.text = value;
                configure.Invoke(null, new[] { (object)text, action, 30f });
                Assert.That(text.enableAutoSizing, Is.True);
                Assert.That(text.fontSizeMax, Is.EqualTo(30f));
                Assert.That(text.fontSizeMin, Is.GreaterThanOrEqualTo(18f));
                Assert.That(text.fontSizeMin, Is.LessThanOrEqualTo(text.fontSizeMax));
                Assert.That(text.enableWordWrapping, Is.True);
                Assert.That(text.overflowMode, Is.EqualTo(TextOverflowModes.Ellipsis));
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    static IEnumerable<ScreenLayout> CriticalScreens()
    {
        yield return new ScreenLayout("Home", new[]
        {
            new Region("Settings", -455f, 840f, 88f, 88f),
            new Region("PlayerChip", 320f, 820f, 400f, 92f),
            new Region("Solo", 0f, -265f, 900f, 180f),
            new Region("Private", -225f, -475f, 420f, 132f),
            new Region("Daily", 225f, -475f, 420f, 132f),
            new Region("Tip", 0f, -730f, 900f, 190f)
        }, Pair("Solo", "Private"), Pair("Solo", "Daily"),
            Pair("Private", "Daily"), Pair("Private", "Tip"), Pair("Daily", "Tip"));

        yield return new ScreenLayout("Play", new[]
        {
            new Region("Find", 0f, 40f, 860f, 150f),
            new Region("Back", 0f, -140f, 860f, 128f),
            new Region("Disclosure", 0f, -520f, 920f, 200f)
        }, Pair("Find", "Back"), Pair("Back", "Disclosure"));

        yield return new ScreenLayout("Settings", new[]
        {
            new Region("Title", 0f, 455f, 420f, 78f),
            new Region("NameInput", 100f, 340f, 300f, 82f),
            new Region("Save", 350f, 340f, 160f, 74f),
            new Region("Languages", 145f, 110f, 440f, 70f),
            new Region("Music", 250f, -120f, 150f, 70f),
            new Region("Difficulty", 185f, -350f, 470f, 65f),
            new Region("Ads", 305f, -580f, 200f, 72f),
            new Region("Back", -455f, 820f, 84f, 84f)
        }, Pair("NameInput", "Languages"), Pair("Save", "Languages"),
            Pair("Languages", "Music"), Pair("Music", "Difficulty"),
            Pair("Difficulty", "Ads"));

        yield return new ScreenLayout("Solo board", new[]
        {
            new Region("Back", -438f, 790f, 118f, 92f),
            new Region("Player", -265f, 565f, 470f, 205f),
            new Region("Opponent", 265f, 565f, 470f, 205f),
            new Region("Prompt", 0f, 300f, 850f, 90f),
            new Region("Range", 0f, 20f, 820f, 48f),
            new Region("Keypad", -240f, -320f, 620f, 620f),
            new Region("History", 330f, -260f, 330f, 360f),
            new Region("Answer", 0f, -705f, 860f, 100f),
            new Region("Submit", -180f, -850f, 660f, 112f)
        }, Pair("Player", "Opponent"), Pair("Player", "Prompt"),
            Pair("Opponent", "Prompt"), Pair("Range", "Keypad"),
            Pair("Range", "History"), Pair("Keypad", "History"),
            Pair("Keypad", "Answer"), Pair("History", "Answer"),
            Pair("Answer", "Submit"));

        yield return new ScreenLayout("Private room", new[]
        {
            new Region("PageTitle", 0f, 800f, 720f, 70f),
            new Region("Create", 0f, 40f, 860f, 330f),
            new Region("Join", 0f, -315f, 860f, 330f),
            new Region("Tip", 0f, -720f, 860f, 190f),
            new Region("Back", 0f, -860f, 260f, 70f)
        }, Pair("Create", "Join"), Pair("Join", "Tip"), Pair("Tip", "Back"));

        yield return new ScreenLayout("PvP board", new[]
        {
            new Region("Player", -262f, 790f, 480f, 200f),
            new Region("Opponent", 262f, 790f, 480f, 200f),
            new Region("Prompt", 0f, 555f, 900f, 200f),
            new Region("Guess", -255f, -130f, 530f, 900f),
            new Region("Signal", 285f, 245f, 470f, 170f),
            new Region("History", 285f, -35f, 470f, 320f),
            new Region("Range", 285f, -390f, 470f, 320f),
            new Region("Signals", 0f, -678f, 1030f, 140f),
            new Region("Exit", 0f, -830f, 300f, 72f)
        }, Pair("Player", "Opponent"), Pair("Player", "Prompt"),
            Pair("Opponent", "Prompt"), Pair("Prompt", "Guess"),
            Pair("Prompt", "Signal"), Pair("Guess", "Signal"),
            Pair("Guess", "History"), Pair("Guess", "Range"),
            Pair("Signal", "History"), Pair("History", "Range"),
            Pair("Range", "Signals"), Pair("Signals", "Exit"));

        yield return new ScreenLayout("PvP result", new[]
        {
            new Region("Result", 0f, 260f, 1000f, 900f),
            new Region("Rematch", 0f, -365f, 850f, 230f),
            new Region("Status", 0f, -505f, 780f, 42f),
            new Region("Reactions", 0f, -690f, 760f, 310f)
        }, Pair("Result", "Rematch"), Pair("Rematch", "Status"),
            Pair("Status", "Reactions"));

        yield return new ScreenLayout("PvP terminal", new[]
        {
            new Region("Terminal", 0f, 0f, 840f, 540f),
            new Region("Exit", 0f, -155f, 420f, 86f)
        });

        yield return new ScreenLayout("Daily Hunt", new[]
        {
            new Region("Title", 0f, 515f, 850f, 110f),
            new Region("Status", 0f, 380f, 780f, 130f),
            new Region("Trail", 0f, 170f, 800f, 80f),
            new Region("Input", 0f, 15f, 430f, 100f),
            new Region("Submit", 0f, -120f, 480f, 96f),
            new Region("Secondary", 0f, -265f, 640f, 92f),
            new Region("Streak", 0f, -410f, 650f, 50f),
            new Region("Exit", 0f, -550f, 280f, 78f)
        }, Pair("Title", "Status"), Pair("Status", "Trail"),
            Pair("Trail", "Input"), Pair("Input", "Submit"),
            Pair("Submit", "Secondary"), Pair("Secondary", "Streak"),
            Pair("Streak", "Exit"));
    }

    static string[] Pair(string first, string second)
    {
        return new[] { first, second };
    }

    static object Geometry(Rect safe, Vector2 viewport, Vector2 canvasSize)
    {
        Type type = RuntimeType("ResponsiveViewportGeometry");
        return type.GetMethod("Calculate", StaticFlags).Invoke(null, new object[]
        {
            safe, viewport, canvasSize, new Vector2(1080f, 1920f)
        });
    }

    static Rect Bounds(object geometry, Region region)
    {
        return (Rect)geometry.GetType().GetMethod("Bounds", InstanceFlags)
            .Invoke(geometry, new object[] { region.Position, region.Size, 16f });
    }

    static Vector2 CanvasSize(Vector2 viewport)
    {
        return (Vector2)RuntimeType("ResponsiveViewportGeometry")
            .GetMethod("CanvasSizeForViewport", StaticFlags)
            .Invoke(null, new object[] { viewport, new Vector2(1080f, 1920f), 0.5f });
    }

    static void AssertContained(Rect safe, Rect bounds, string context)
    {
        const float tolerance = 0.02f;
        Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(safe.xMin - tolerance), context);
        Assert.That(bounds.xMax, Is.LessThanOrEqualTo(safe.xMax + tolerance), context);
        Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(safe.yMin - tolerance), context);
        Assert.That(bounds.yMax, Is.LessThanOrEqualTo(safe.yMax + tolerance), context);
    }

    static Rect RectFor(RectTransform rect)
    {
        Vector2 size = Vector2.Scale(rect.sizeDelta,
            new Vector2(Mathf.Abs(rect.localScale.x), Mathf.Abs(rect.localScale.y)));
        return new Rect(rect.anchoredPosition - size * 0.5f, size);
    }

    static T Field<T>(object target, string name)
    {
        return (T)target.GetType().GetField(name, InstanceFlags).GetValue(target);
    }

    static T Property<T>(object target, string name)
    {
        return (T)target.GetType().GetProperty(name, InstanceFlags).GetValue(target, null);
    }

    static Type RuntimeType(string name)
    {
        var type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime type " + name);
        return type;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void Center(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }
}
