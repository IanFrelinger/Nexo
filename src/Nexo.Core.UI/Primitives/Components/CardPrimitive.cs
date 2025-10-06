using Nexo.Core.UI.Primitives.DesignTokens;
using Nexo.Core.UI.Primitives.Extensions;
using System.Drawing;

namespace Nexo.Core.UI.Primitives.Components;

/// <summary>
/// Represents a framework-agnostic card primitive.
/// </summary>
public class CardPrimitive
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Subtitle { get; set; }
    public string? Footer { get; set; }
    public CardVariant Variant { get; set; } = CardVariant.Default;
    public CardSize Size { get; set; } = CardSize.Medium;
    public bool IsElevated { get; set; } = false;
    public bool IsInteractive { get; set; } = false;
    public bool IsSelected { get; set; } = false;
    public string? ImageUrl { get; set; }
    public string? ImageAlt { get; set; }
    public string? AccessibleLabel { get; set; }

    // Typography
    public string FontFamily { get; set; } = TypographyTokens.FontFamilySystem;

    // Spacing
    public double Padding { get; set; } = SpacingTokens.Lg;
    public double Margin { get; set; } = SpacingTokens.Md;
    public double Gap { get; set; } = SpacingTokens.Md;

    // Border
    public double BorderRadius { get; set; } = SpacingTokens.BorderRadius.Lg;
    public double BorderWidth { get; set; } = 1;

    // Colors
    public string BackgroundColor { get; set; } = ColorTokens.BackgroundPrimary.ToHex();
    public string BorderColor { get; set; } = ColorTokens.BorderDefault.ToHex();
    public string TitleColor { get; set; } = ColorTokens.TextPrimary.ToHex();
    public string SubtitleColor { get; set; } = ColorTokens.TextSecondary.ToHex();
    public string ContentColor { get; set; } = ColorTokens.TextPrimary.ToHex();
    public string FooterColor { get; set; } = ColorTokens.TextSecondary.ToHex();

    public string HoverBackgroundColor { get; set; } = ColorTokens.BackgroundSecondary.ToHex();
    public string HoverBorderColor { get; set; } = ColorTokens.BorderMuted.ToHex();
    public string SelectedBackgroundColor { get; set; } = ColorTokens.PrimaryBlueLight.ToHex();
    public string SelectedBorderColor { get; set; } = ColorTokens.PrimaryBlue.ToHex();

    // Shadow
    public double ShadowOffsetX { get; set; } = SpacingTokens.Shadow.OffsetX;
    public double ShadowOffsetY { get; set; } = SpacingTokens.Shadow.OffsetY;
    public double ShadowBlurRadius { get; set; } = SpacingTokens.Shadow.BlurRadius;
    public double ShadowSpreadRadius { get; set; } = SpacingTokens.Shadow.SpreadRadius;
    public string ShadowColor { get; set; } = Color.FromArgb(25, 0, 0, 0).ToHexWithAlpha(); // Alpha for shadow

    // Transitions
    public double TransitionDuration { get; set; } = 0.15; // seconds
    public string TransitionTimingFunction { get; set; } = "ease-in-out";

    /// <summary>
    /// Applies the styling based on the current card variant.
    /// </summary>
    public void ApplyVariant()
    {
        switch (Variant)
        {
            case CardVariant.Default:
                IsElevated = false;
                BorderWidth = 1;
                BackgroundColor = ColorTokens.BackgroundPrimary.ToHex();
                BorderColor = ColorTokens.BorderDefault.ToHex();
                break;
            case CardVariant.Elevated:
                IsElevated = true;
                BorderWidth = 0; // Elevated cards typically don't have a visible border
                BackgroundColor = ColorTokens.BackgroundPrimary.ToHex();
                BorderColor = ColorTokens.BorderDefault.ToHex(); // Still define for consistency
                break;
            case CardVariant.Outlined:
                IsElevated = false;
                BorderWidth = 2;
                BackgroundColor = ColorTokens.BackgroundPrimary.ToHex();
                BorderColor = ColorTokens.BorderMuted.ToHex();
                break;
            case CardVariant.Filled:
                IsElevated = false;
                BorderWidth = 0;
                BackgroundColor = ColorTokens.BackgroundSecondary.ToHex();
                BorderColor = ColorTokens.BackgroundSecondary.ToHex(); // Match background
                break;
        }
    }

    /// <summary>
    /// Applies the spacing based on the current card size.
    /// </summary>
    public void ApplySize()
    {
        switch (Size)
        {
            case CardSize.Small:
                Padding = SpacingTokens.Sm;
                Gap = SpacingTokens.Sm;
                break;
            case CardSize.Medium:
                Padding = SpacingTokens.Lg;
                Gap = SpacingTokens.Md;
                break;
            case CardSize.Large:
                Padding = SpacingTokens.Xl;
                Gap = SpacingTokens.Lg;
                break;
        }
    }
}

/// <summary>
/// Defines the available card variants.
/// </summary>
public enum CardVariant
{
    Default,
    Elevated,
    Outlined,
    Filled
}

/// <summary>
/// Defines the available card sizes.
/// </summary>
public enum CardSize
{
    Small,
    Medium,
    Large
}