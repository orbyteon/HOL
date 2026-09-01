using System;
using System.Reflection;
using UnityEditor;

// Editor-only fixed-resolution seam used by the explicit visual capture test.
// Reflection is isolated here because Unity keeps GameView sizing APIs internal.
public static class OnboardingGameViewCapture
{
    const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static void SetResolution(int width, int height)
    {
        Assembly editorAssembly = typeof(Editor).Assembly;
        Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
        Type groupTypeEnum =
            editorAssembly.GetType("UnityEditor.GameViewSizeGroupType");
        Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
        Type sizeKindEnum =
            editorAssembly.GetType("UnityEditor.GameViewSizeType");
        Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
        if (sizesType == null || groupTypeEnum == null || sizeType == null ||
            sizeKindEnum == null || gameViewType == null)
            throw new InvalidOperationException(
                "Unity GameView reflection API is unavailable.");

        Type singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        object sizes = singleton.GetProperty("instance", BindingFlags.Static |
            BindingFlags.Public).GetValue(null, null);
        object standalone = Enum.Parse(groupTypeEnum, "Standalone");
        object group = sizesType.GetMethod("GetGroup", InstanceFlags)
            .Invoke(sizes, new[] { standalone });
        Type groupType = group.GetType();

        int builtin = (int)groupType.GetMethod(
            "GetBuiltinCount", InstanceFlags).Invoke(group, null);
        int custom = (int)groupType.GetMethod(
            "GetCustomCount", InstanceFlags).Invoke(group, null);
        MethodInfo getSize = groupType.GetMethod("GetGameViewSize", InstanceFlags);
        int selectedIndex = -1;
        for (int index = 0; index < builtin + custom; index++)
        {
            object size = getSize.Invoke(group, new object[] { index });
            int candidateWidth = (int)sizeType.GetProperty(
                "width", InstanceFlags).GetValue(size, null);
            int candidateHeight = (int)sizeType.GetProperty(
                "height", InstanceFlags).GetValue(size, null);
            if (candidateWidth == width && candidateHeight == height)
            {
                selectedIndex = index;
                break;
            }
        }

        if (selectedIndex < 0)
        {
            object fixedResolution = Enum.Parse(sizeKindEnum, "FixedResolution");
            object newSize = Activator.CreateInstance(
                sizeType,
                fixedResolution,
                width,
                height,
                "HOL Onboarding " + width + "x" + height);
            groupType.GetMethod("AddCustomSize", InstanceFlags)
                .Invoke(group, new[] { newSize });
            selectedIndex = builtin + custom;
        }

        EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
        PropertyInfo selectedSize = gameViewType.GetProperty(
            "selectedSizeIndex", InstanceFlags);
        if (selectedSize == null)
            throw new InvalidOperationException(
                "Unity GameView selectedSizeIndex is unavailable.");
        selectedSize.SetValue(gameView, selectedIndex, null);
        gameView.Repaint();
    }
}
