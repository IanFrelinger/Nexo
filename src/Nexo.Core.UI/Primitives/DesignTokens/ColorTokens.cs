using System.Drawing;

namespace Nexo.Core.UI.Primitives.DesignTokens;

/// <summary>
/// Design tokens for colors that can be used across different UI frameworks
/// </summary>
public static class ColorTokens
{
    // Primary Colors
    public static readonly Color PrimaryBlue = Color.FromArgb(0, 102, 204);
    public static readonly Color PrimaryBlueDark = Color.FromArgb(0, 61, 153);
    public static readonly Color PrimaryBlueLight = Color.FromArgb(51, 153, 255);
    
    // Semantic Colors
    public static readonly Color SuccessGreen = Color.FromArgb(40, 167, 69);
    public static readonly Color SuccessGreenDark = Color.FromArgb(15, 81, 50);
    public static readonly Color SuccessGreenLight = Color.FromArgb(72, 199, 116);
    
    public static readonly Color ErrorRed = Color.FromArgb(220, 53, 69);
    public static readonly Color ErrorRedDark = Color.FromArgb(169, 27, 35);
    public static readonly Color ErrorRedLight = Color.FromArgb(248, 215, 218);
    
    public static readonly Color WarningOrange = Color.FromArgb(255, 193, 7);
    public static readonly Color WarningOrangeDark = Color.FromArgb(153, 111, 0);
    public static readonly Color WarningOrangeLight = Color.FromArgb(255, 235, 59);
    
    public static readonly Color InfoBlue = Color.FromArgb(23, 162, 184);
    public static readonly Color InfoBlueDark = Color.FromArgb(19, 132, 150);
    public static readonly Color InfoBlueLight = Color.FromArgb(79, 209, 197);
    
    // Neutral Colors
    public static readonly Color TextPrimary = Color.FromArgb(26, 26, 26);
    public static readonly Color TextSecondary = Color.FromArgb(102, 102, 102);
    public static readonly Color TextTertiary = Color.FromArgb(153, 153, 153);
    public static readonly Color TextDisabled = Color.FromArgb(204, 204, 204);
    
    // Background Colors
    public static readonly Color BackgroundPrimary = Color.FromArgb(255, 255, 255);
    public static readonly Color BackgroundSecondary = Color.FromArgb(248, 249, 250);
    public static readonly Color BackgroundTertiary = Color.FromArgb(241, 243, 244);
    public static readonly Color BackgroundDisabled = Color.FromArgb(245, 245, 245);
    
    // Border Colors
    public static readonly Color BorderDefault = Color.FromArgb(204, 204, 204);
    public static readonly Color BorderMuted = Color.FromArgb(224, 224, 224);
    public static readonly Color BorderFocus = Color.FromArgb(0, 102, 204);
    
    // Surface Colors
    public static readonly Color SurfaceElevated = Color.FromArgb(255, 255, 255);
    public static readonly Color SurfaceOverlay = Color.FromArgb(248, 249, 250);
    public static readonly Color SurfaceModal = Color.FromArgb(255, 255, 255);
    
    // Status Colors
    public static readonly Color StatusConnected = Color.FromArgb(40, 167, 69);
    public static readonly Color StatusDisconnected = Color.FromArgb(220, 53, 69);
    public static readonly Color StatusPending = Color.FromArgb(255, 193, 7);
    public static readonly Color StatusError = Color.FromArgb(220, 53, 69);
    
    // Interactive Colors
    public static readonly Color InteractiveHover = Color.FromArgb(245, 245, 245);
    public static readonly Color InteractivePressed = Color.FromArgb(230, 230, 230);
    public static readonly Color InteractiveSelected = Color.FromArgb(0, 102, 204);
    public static readonly Color InteractiveDisabled = Color.FromArgb(245, 245, 245);
    
    // Shadow Colors
    public static readonly Color ShadowLight = Color.FromArgb(0, 0, 0, 8);
    public static readonly Color ShadowMedium = Color.FromArgb(0, 0, 0, 16);
    public static readonly Color ShadowHeavy = Color.FromArgb(0, 0, 0, 24);
}
