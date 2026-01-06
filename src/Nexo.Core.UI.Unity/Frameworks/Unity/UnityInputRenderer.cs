using Nexo.Core.UI.Primitives.Components;
using Nexo.Core.UI.Primitives.DesignTokens;
using UnityEditor;
using UnityEngine;

namespace Nexo.Core.UI.Unity.Frameworks.Unity;

/// <summary>
/// Renders an <see cref="InputPrimitive"/> into a Unity IMGUI input field.
/// 
/// This renderer translates framework-agnostic input primitives into
/// Unity's immediate mode GUI system, supporting various input types
/// (text, password, email, etc.) with validation states and helper text.
/// </summary>
public static class UnityInputRenderer
{
    /// <summary>
    /// Renders an input primitive as a Unity IMGUI input field.
    /// </summary>
    /// <param name="primitive">The input primitive to render.</param>
    /// <param name="options">Optional Unity layout options.</param>
    /// <returns>The current value of the input field.</returns>
    public static string Render(InputPrimitive primitive, params GUILayoutOption[] options)
    {
        GUIStyle fieldStyle = GetFieldStyle(primitive);
        GUIStyle labelStyle = GetLabelStyle();
        GUIStyle helperStyle = GetHelperStyle();
        GUIStyle errorStyle = GetErrorStyle();

        if (!string.IsNullOrEmpty(primitive.Label))
        {
            EditorGUILayout.LabelField(primitive.Label, labelStyle);
        }

        EditorGUI.BeginDisabledGroup(!primitive.IsEnabled);
        string value;
        if (primitive.IsMultiline)
        {
            value = EditorGUILayout.TextArea(primitive.Value, fieldStyle, options);
        }
        else
        {
            value = primitive.Type == InputType.Password
                ? EditorGUILayout.PasswordField(primitive.Value, fieldStyle, options)
                : EditorGUILayout.TextField(primitive.Value, fieldStyle, options);
        }
        EditorGUI.EndDisabledGroup();

        if (!string.IsNullOrEmpty(primitive.ErrorText))
        {
            EditorGUILayout.LabelField(primitive.ErrorText, errorStyle);
        }
        else if (!string.IsNullOrEmpty(primitive.HelperText))
        {
            EditorGUILayout.LabelField(primitive.HelperText, helperStyle);
        }

        return value;
    }

    private static GUIStyle GetFieldStyle(InputPrimitive primitive)
    {
        var style = new GUIStyle(EditorStyles.textField)
        {
            fontSize = Mathf.RoundToInt((float)primitive.FontSize),
            fontStyle = FontStyle.Normal,
            padding = new RectOffset(
                Mathf.RoundToInt((float)primitive.PaddingHorizontal),
                Mathf.RoundToInt((float)primitive.PaddingHorizontal),
                Mathf.RoundToInt((float)primitive.PaddingVertical),
                Mathf.RoundToInt((float)primitive.PaddingVertical)
            )
        };
        style.normal.textColor = HexToColor(primitive.TextColor);
        style.normal.background = MakeTex(HexToColor(primitive.BackgroundColor));
        return style;
    }

    private static GUIStyle GetLabelStyle()
    {
        var style = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
        return style;
    }

    private static GUIStyle GetHelperStyle()
    {
        var style = new GUIStyle(EditorStyles.miniLabel);
        style.normal.textColor = HexToColor("#6C757D");
        return style;
    }

    private static GUIStyle GetErrorStyle()
    {
        var style = new GUIStyle(EditorStyles.miniLabel);
        style.normal.textColor = HexToColor("#DC3545");
        return style;
    }

    private static Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    private static Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
        return Color.white;
    }
}


