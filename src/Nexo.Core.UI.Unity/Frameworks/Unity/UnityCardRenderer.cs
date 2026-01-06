using Nexo.Core.UI.Primitives.Components;
using UnityEditor;
using UnityEngine;

namespace Nexo.Core.UI.Unity.Frameworks.Unity;

/// <summary>
/// Renders a <see cref="CardPrimitive"/> into a Unity IMGUI card container.
/// 
/// This renderer translates framework-agnostic card primitives into
/// Unity's immediate mode GUI system, providing consistent card layouts
/// with title, subtitle, content, and footer sections.
/// </summary>
public static class UnityCardRenderer
{
    /// <summary>
    /// Renders a card primitive as a Unity IMGUI container.
    /// </summary>
    /// <param name="primitive">The card primitive to render.</param>
    /// <param name="content">Action that renders the card's content.</param>
    /// <param name="options">Optional Unity layout options.</param>
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


