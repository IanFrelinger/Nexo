using Nexo.Core.UI.Primitives.DesignTokens;
using Nexo.Core.UI.Primitives.Extensions;
using System.Drawing;

namespace Nexo.Core.UI.Services;

/// <summary>
/// Centralized theme service for managing UI themes across frameworks.
/// </summary>
public class ThemeService
{
    private static ThemeService? _instance;
    private static readonly object _lock = new object();

    public static ThemeService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new ThemeService();
                }
            }
            return _instance;
        }
    }

    private ThemeService()
    {
        CurrentTheme = Theme.Light;
        AccentColor = ColorTokens.PrimaryBlue;
        InitializeTheme();
    }

    public Theme CurrentTheme { get; private set; }
    public Color AccentColor { get; private set; }
    public string FontFamily { get; private set; } = TypographyTokens.FontFamilySystem;
    public double FontSize { get; private set; } = TypographyTokens.FontSizeBase;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <summary>
    /// Changes the current theme and notifies all subscribers.
    /// </summary>
    public void SetTheme(Theme theme)
    {
        if (CurrentTheme != theme)
        {
            CurrentTheme = theme;
            InitializeTheme();
            OnThemeChanged(new ThemeChangedEventArgs(theme));
        }
    }

    /// <summary>
    /// Changes the accent color and notifies all subscribers.
    /// </summary>
    public void SetAccentColor(Color accentColor)
    {
        if (AccentColor != accentColor)
        {
            AccentColor = accentColor;
            OnThemeChanged(new ThemeChangedEventArgs(CurrentTheme, accentColor));
        }
    }

    /// <summary>
    /// Changes the font family and notifies all subscribers.
    /// </summary>
    public void SetFontFamily(string fontFamily)
    {
        if (FontFamily != fontFamily)
        {
            FontFamily = fontFamily;
            OnThemeChanged(new ThemeChangedEventArgs(CurrentTheme, AccentColor, fontFamily));
        }
    }

    /// <summary>
    /// Changes the font size and notifies all subscribers.
    /// </summary>
    public void SetFontSize(double fontSize)
    {
        if (FontSize != fontSize)
        {
            FontSize = fontSize;
            OnThemeChanged(new ThemeChangedEventArgs(CurrentTheme, AccentColor, FontFamily, fontSize));
        }
    }

    /// <summary>
    /// Gets the appropriate color for a given semantic color based on the current theme.
    /// </summary>
    public Color GetSemanticColor(SemanticColor semanticColor)
    {
        return CurrentTheme switch
        {
            Theme.Light => GetLightThemeColor(semanticColor),
            Theme.Dark => GetDarkThemeColor(semanticColor),
            _ => GetLightThemeColor(semanticColor)
        };
    }

    /// <summary>
    /// Gets the appropriate background color based on the current theme.
    /// </summary>
    public Color GetBackgroundColor()
    {
        return CurrentTheme switch
        {
            Theme.Light => ColorTokens.BackgroundPrimary,
            Theme.Dark => ColorTokens.BackgroundTertiary,
            _ => ColorTokens.BackgroundPrimary
        };
    }

    /// <summary>
    /// Gets the appropriate text color based on the current theme.
    /// </summary>
    public Color GetTextColor()
    {
        return CurrentTheme switch
        {
            Theme.Light => ColorTokens.TextPrimary,
            Theme.Dark => ColorTokens.BackgroundPrimary,
            _ => ColorTokens.TextPrimary
        };
    }

    private void InitializeTheme()
    {
        // Initialize theme-specific settings
        switch (CurrentTheme)
        {
            case Theme.Light:
                // Light theme is the default
                break;
            case Theme.Dark:
                // Dark theme adjustments could be made here
                break;
        }
    }

    private Color GetLightThemeColor(SemanticColor semanticColor)
    {
        return semanticColor switch
        {
            SemanticColor.Primary => AccentColor,
            SemanticColor.Success => ColorTokens.SuccessGreen,
            SemanticColor.Warning => ColorTokens.WarningOrange,
            SemanticColor.Danger => ColorTokens.ErrorRed,
            SemanticColor.Info => ColorTokens.InfoBlue,
            SemanticColor.Text => ColorTokens.TextPrimary,
            SemanticColor.TextSecondary => ColorTokens.TextSecondary,
            SemanticColor.Background => ColorTokens.BackgroundPrimary,
            SemanticColor.Border => ColorTokens.BorderDefault,
            _ => AccentColor
        };
    }

    private Color GetDarkThemeColor(SemanticColor semanticColor)
    {
        return semanticColor switch
        {
            SemanticColor.Primary => AccentColor,
            SemanticColor.Success => ColorTokens.SuccessGreenLight,
            SemanticColor.Warning => ColorTokens.WarningOrangeLight,
            SemanticColor.Danger => ColorTokens.ErrorRedLight,
            SemanticColor.Info => ColorTokens.InfoBlueLight,
            SemanticColor.Text => ColorTokens.BackgroundPrimary,
            SemanticColor.TextSecondary => ColorTokens.TextTertiary,
            SemanticColor.Background => ColorTokens.BackgroundTertiary,
            SemanticColor.Border => ColorTokens.BorderMuted,
            _ => AccentColor
        };
    }

    private void OnThemeChanged(ThemeChangedEventArgs e)
    {
        ThemeChanged?.Invoke(this, e);
    }
}

/// <summary>
/// Available themes for the UI system.
/// </summary>
public enum Theme
{
    Light,
    Dark
}

/// <summary>
/// Semantic colors that can be themed.
/// </summary>
public enum SemanticColor
{
    Primary,
    Success,
    Warning,
    Danger,
    Info,
    Text,
    TextSecondary,
    Background,
    Border
}

/// <summary>
/// Event arguments for theme change events.
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    public Theme Theme { get; }
    public Color AccentColor { get; }
    public string FontFamily { get; }
    public double FontSize { get; }

    public ThemeChangedEventArgs(Theme theme, Color? accentColor = null, string? fontFamily = null, double? fontSize = null)
    {
        Theme = theme;
        AccentColor = accentColor ?? ColorTokens.PrimaryBlue;
        FontFamily = fontFamily ?? TypographyTokens.FontFamilySystem;
        FontSize = fontSize ?? TypographyTokens.FontSizeBase;
    }
}