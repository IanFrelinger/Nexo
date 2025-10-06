using Nexo.Core.UI.Primitives.Components;
using Nexo.Core.UI.Primitives.DesignTokens;
using UnityEditor;
using UnityEngine;

namespace Nexo.Core.UI.Unity.Frameworks.Unity;

public static class UnityButtonRenderer
{
    public static bool Render(ButtonPrimitive primitive, params GUILayoutOption[] options)
    {
        GUIStyle style = GetButtonStyle(primitive);
        GUIContent content = new GUIContent(primitive.Text, primitive.Tooltip);

        bool wasEnabled = GUI.enabled;
        GUI.enabled = primitive.IsEnabled;
        bool clicked = GUILayout.Button(content, style, options);
        GUI.enabled = wasEnabled;
        return clicked;
    }

    private static GUIStyle GetButtonStyle(ButtonPrimitive primitive)
    {
        GUIStyle style = new GUIStyle(GUI.skin.button);

        // Typography
        style.fontSize = Mathf.RoundToInt((float)primitive.FontSize);
        style.fontStyle = ConvertFontWeight(primitive.FontWeight);

        // Padding
        style.padding = new RectOffset(
            Mathf.RoundToInt((float)primitive.PaddingHorizontal),
            Mathf.RoundToInt((float)primitive.PaddingHorizontal),
            Mathf.RoundToInt((float)primitive.PaddingVertical),
            Mathf.RoundToInt((float)primitive.PaddingVertical)
        );

        // Min sizes
        if (primitive.MinWidth > 0) style.fixedWidth = (float)primitive.MinWidth;
        if (primitive.MinHeight > 0) style.fixedHeight = (float)primitive.MinHeight;

        // Colors and backgrounds
        style.normal.textColor = HexToColor(primitive.TextColor);
        style.hover.textColor = HexToColor(primitive.HoverTextColor);
        style.active.textColor = HexToColor(primitive.PressedTextColor);
        style.onNormal.textColor = style.normal.textColor;
        style.onHover.textColor = style.hover.textColor;
        style.onActive.textColor = style.active.textColor;

        style.normal.background = MakeTex(HexToColor(primitive.BackgroundColor));
        style.hover.background = MakeTex(HexToColor(primitive.HoverBackgroundColor));
        style.active.background = MakeTex(HexToColor(primitive.PressedBackgroundColor));
        style.onNormal.background = style.normal.background;
        style.onHover.background = style.hover.background;
        style.onActive.background = style.active.background;

        return style;
    }

    private static Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    private static FontStyle ConvertFontWeight(string weight)
    {
        if (weight == TypographyTokens.FontWeightBold || weight == TypographyTokens.FontWeightSemiBold)
        {
            return FontStyle.Bold;
        }
        return FontStyle.Normal;
    }

    private static Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
        return Color.white;
    }
}


