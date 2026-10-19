using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace IslandEscape
{
    // QA helper: switches the Game view to a fixed resolution (adds it to the current platform's size list)
    public static class StoreGameView
    {
        public static void SetResolution(int width, int height)
        {
            Assembly editor = typeof(Editor).Assembly;
            Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
            Type singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object sizes = singleton.GetProperty("instance").GetValue(null);
            object group = sizesType.GetProperty("currentGroup").GetValue(sizes);
            Type groupType = group.GetType();

            string label = $"QA {width}x{height}";
            int index = -1;
            string[] texts = (string[])groupType.GetMethod("GetDisplayTexts").Invoke(group, null);
            for (int i = 0; i < texts.Length; i++)
                if (texts[i].StartsWith(label)) index = i;

            if (index < 0)
            {
                Type sizeType = editor.GetType("UnityEditor.GameViewSize");
                Type sizeKind = editor.GetType("UnityEditor.GameViewSizeType");
                object size = Activator.CreateInstance(sizeType, Enum.Parse(sizeKind, "FixedResolution"), width, height, label);
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                index = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null) - 1;
            }

            Type gameViewType = editor.GetType("UnityEditor.GameView");
            EditorWindow gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
            PropertyInfo selected = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            selected.SetValue(gameView, index);
            gameView.Repaint();
        }
    }
}
