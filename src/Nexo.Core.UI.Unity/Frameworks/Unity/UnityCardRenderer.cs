using Nexo.Core.UI.Primitives.Components;
using UnityEditor;
using UnityEngine;

namespace Nexo.Core.UI.Unity.Frameworks.Unity;

public static class UnityCardRenderer
{
    public static void Render(CardPrimitive primitive, System.Action content, params GUILayoutOption[] options)
    {
        var boxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(
                Mathf.RoundToInt((float)primitive.Padding),
                Mathf.RoundToInt((float)primitive.Padding),
                Mathf.RoundToInt((float)primitive.Padding),
                Mathf.RoundToInt((float)primitive.Padding)
            )
        };

        EditorGUILayout.BeginVertical(boxStyle, options);
        if (!string.IsNullOrEmpty(primitive.Title))
        {
            EditorGUILayout.LabelField(primitive.Title, EditorStyles.boldLabel);
        }
        if (!string.IsNullOrEmpty(primitive.Subtitle))
        {
            EditorGUILayout.LabelField(primitive.Subtitle, EditorStyles.miniLabel);
        }

        GUILayout.Space((float)primitive.Gap);
        content?.Invoke();
        GUILayout.Space((float)primitive.Gap);

        if (!string.IsNullOrEmpty(primitive.Footer))
        {
            EditorGUILayout.LabelField(primitive.Footer, EditorStyles.miniLabel);
        }
        EditorGUILayout.EndVertical();
    }
}


