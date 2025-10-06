using System.Drawing;

namespace Director.UI.Primitives.DesignTokens;

/// <summary>
/// Extension methods for Color to support UI primitives
/// </summary>
public static class ColorExtensions
{
    /// <summary>
    /// Converts a Color to a hex string (e.g., "#FF0000")
    /// </summary>
    public static string ToHex(this Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
    
    /// <summary>
    /// Converts a Color to a hex string with alpha (e.g., "#FF0000FF")
    /// </summary>
    public static string ToHexWithAlpha(this Color color)
    {
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }
    
    /// <summary>
    /// Converts a Color to an RGB string (e.g., "rgb(255, 0, 0)")
    /// </summary>
    public static string ToRgb(this Color color)
    {
        return $"rgb({color.R}, {color.G}, {color.B})";
    }
    
    /// <summary>
    /// Converts a Color to an RGBA string (e.g., "rgba(255, 0, 0, 0.5)")
    /// </summary>
    public static string ToRgba(this Color color)
    {
        var alpha = color.A / 255.0;
        return $"rgba({color.R}, {color.G}, {color.B}, {alpha:F2})";
    }
    
    /// <summary>
    /// Creates a lighter version of the color
    /// </summary>
    public static Color Lighten(this Color color, double percentage)
    {
        var factor = 1 + (percentage / 100);
        return Color.FromArgb(
            color.A,
            Math.Min(255, (int)(color.R * factor)),
            Math.Min(255, (int)(color.G * factor)),
            Math.Min(255, (int)(color.B * factor))
        );
    }
    
    /// <summary>
    /// Creates a darker version of the color
    /// </summary>
    public static Color Darken(this Color color, double percentage)
    {
        var factor = 1 - (percentage / 100);
        return Color.FromArgb(
            color.A,
            Math.Max(0, (int)(color.R * factor)),
            Math.Max(0, (int)(color.G * factor)),
            Math.Max(0, (int)(color.B * factor))
        );
    }
    
    /// <summary>
    /// Creates a color with adjusted alpha
    /// </summary>
    public static Color WithAlpha(this Color color, int alpha)
    {
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
    
    /// <summary>
    /// Gets the contrast ratio between two colors
    /// </summary>
    public static double GetContrastRatio(this Color color1, Color color2)
    {
        var luminance1 = GetLuminance(color1);
        var luminance2 = GetLuminance(color2);
        
        var lighter = Math.Max(luminance1, luminance2);
        var darker = Math.Min(luminance1, luminance2);
        
        return (lighter + 0.05) / (darker + 0.05);
    }
    
    /// <summary>
    /// Gets the relative luminance of a color
    /// </summary>
    private static double GetLuminance(Color color)
    {
        var r = GetLinearValue(color.R);
        var g = GetLinearValue(color.G);
        var b = GetLinearValue(color.B);
        
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }
    
    private static double GetLinearValue(byte value)
    {
        var normalized = value / 255.0;
        return normalized <= 0.03928 
            ? normalized / 12.92 
            : Math.Pow((normalized + 0.055) / 1.055, 2.4);
    }
}
