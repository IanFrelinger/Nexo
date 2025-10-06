using Nexo.Core.UI.Primitives.DesignTokens;
using Nexo.Core.UI.Primitives.Extensions;

namespace Nexo.Core.UI.Primitives.Components;

/// <summary>
/// Represents a framework-agnostic button primitive.
/// </summary>
public class ButtonPrimitive
{
    public string Text { get; set; } = string.Empty;
    public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;
    public ButtonSize Size { get; set; } = ButtonSize.Medium;
    public bool IsEnabled { get; set; } = true;
    public bool IsLoading { get; set; } = false;
    public string? Icon { get; set; }
    public ButtonIconPosition IconPosition { get; set; } = ButtonIconPosition.Left;
    public string? Tooltip { get; set; }
    public string? AccessibleLabel { get; set; }

    // Typography
    public string FontFamily { get; set; } = TypographyTokens.FontFamilySystem;
    public double FontSize { get; set; } = TypographyTokens.FontSizeBase;
    public string FontWeight { get; set; } = TypographyTokens.FontWeightMedium;
    public double LineHeight { get; set; } = TypographyTokens.LineHeightNormal;

    // Spacing
    public double PaddingHorizontal { get; set; } = SpacingTokens.Lg;
    public double PaddingVertical { get; set; } = SpacingTokens.Md;
    public double MinWidth { get; set; } = 0;
    public double MinHeight { get; set; } = 48; // Touch target minimum

    // Border
    public double BorderRadius { get; set; } = SpacingTokens.BorderRadius.Md;
    public double BorderWidth { get; set; } = 1;

    // Colors
    public string BackgroundColor { get; set; } = ColorTokens.PrimaryBlue.ToHex();
    public string TextColor { get; set; } = ColorTokens.BackgroundPrimary.ToHex();
    public string BorderColor { get; set; } = ColorTokens.PrimaryBlue.ToHex();

    public string HoverBackgroundColor { get; set; } = ColorTokens.PrimaryBlueDark.ToHex();
    public string HoverTextColor { get; set; } = ColorTokens.BackgroundPrimary.ToHex();
    public string HoverBorderColor { get; set; } = ColorTokens.PrimaryBlueDark.ToHex();

    public string PressedBackgroundColor { get; set; } = ColorTokens.PrimaryBlueDark.ToHex();
    public string PressedTextColor { get; set; } = ColorTokens.BackgroundPrimary.ToHex();
    public string PressedBorderColor { get; set; } = ColorTokens.PrimaryBlueDark.ToHex();

    public string DisabledBackgroundColor { get; set; } = ColorTokens.BackgroundDisabled.ToHex();
    public string DisabledTextColor { get; set; } = ColorTokens.TextDisabled.ToHex();
    public string DisabledBorderColor { get; set; } = ColorTokens.BorderMuted.ToHex();

    // Transitions
    public double TransitionDuration { get; set; } = 0.15; // seconds
    public string TransitionTimingFunction { get; set; } = "ease-in-out";

    /// <summary>
    /// Applies the color tokens based on the current button variant.
    /// </summary>
    public void ApplyVariantColors()
    {
        switch (Variant)
        {
            case ButtonVariant.Primary:
                BackgroundColor = ColorTokens.PrimaryBlue.ToHex();
                TextColor = ColorTokens.BackgroundPrimary.ToHex();
                BorderColor = ColorTokens.PrimaryBlue.ToHex();
                HoverBackgroundColor = ColorTokens.PrimaryBlueDark.ToHex();
                HoverTextColor = ColorTokens.BackgroundPrimary.ToHex();
                HoverBorderColor = ColorTokens.PrimaryBlueDark.ToHex();
                PressedBackgroundColor = ColorTokens.PrimaryBlueDark.ToHex();
                PressedTextColor = ColorTokens.BackgroundPrimary.ToHex();
                PressedBorderColor = ColorTokens.PrimaryBlueDark.ToHex();
                break;
            case ButtonVariant.Secondary:
                BackgroundColor = ColorTokens.BackgroundSecondary.ToHex();
                TextColor = ColorTokens.TextPrimary.ToHex();
                BorderColor = ColorTokens.BorderDefault.ToHex();
                HoverBackgroundColor = ColorTokens.BackgroundTertiary.ToHex();
                HoverTextColor = ColorTokens.TextPrimary.ToHex();
                HoverBorderColor = ColorTokens.PrimaryBlue.ToHex();
                PressedBackgroundColor = ColorTokens.BackgroundDisabled.ToHex();
                PressedTextColor = ColorTokens.TextPrimary.ToHex();
                PressedBorderColor = ColorTokens.PrimaryBlue.ToHex();
                break;
            case ButtonVariant.Success:
                BackgroundColor = ColorTokens.SuccessGreen.ToHex();
                TextColor = ColorTokens.BackgroundPrimary.ToHex();
                BorderColor = ColorTokens.SuccessGreen.ToHex();
                HoverBackgroundColor = ColorTokens.SuccessGreenDark.ToHex();
                HoverTextColor = ColorTokens.BackgroundPrimary.ToHex();
                HoverBorderColor = ColorTokens.SuccessGreenDark.ToHex();
                PressedBackgroundColor = ColorTokens.SuccessGreenDark.ToHex();
                PressedTextColor = ColorTokens.BackgroundPrimary.ToHex();
                PressedBorderColor = ColorTokens.SuccessGreenDark.ToHex();
                break;
            case ButtonVariant.Danger:
                BackgroundColor = ColorTokens.ErrorRed.ToHex();
                TextColor = ColorTokens.BackgroundPrimary.ToHex();
                BorderColor = ColorTokens.ErrorRed.ToHex();
                HoverBackgroundColor = ColorTokens.ErrorRedDark.ToHex();
                HoverTextColor = ColorTokens.BackgroundPrimary.ToHex();
                HoverBorderColor = ColorTokens.ErrorRedDark.ToHex();
                PressedBackgroundColor = ColorTokens.ErrorRedDark.ToHex();
                PressedTextColor = ColorTokens.BackgroundPrimary.ToHex();
                PressedBorderColor = ColorTokens.ErrorRedDark.ToHex();
                break;
            case ButtonVariant.Warning:
                BackgroundColor = ColorTokens.WarningOrange.ToHex();
                TextColor = ColorTokens.TextPrimary.ToHex();
                BorderColor = ColorTokens.WarningOrange.ToHex();
                HoverBackgroundColor = ColorTokens.WarningOrangeDark.ToHex();
                HoverTextColor = ColorTokens.TextPrimary.ToHex();
                HoverBorderColor = ColorTokens.WarningOrangeDark.ToHex();
                PressedBackgroundColor = ColorTokens.WarningOrangeDark.ToHex();
                PressedTextColor = ColorTokens.TextPrimary.ToHex();
                PressedBorderColor = ColorTokens.WarningOrangeDark.ToHex();
                break;
            case ButtonVariant.Info:
                BackgroundColor = ColorTokens.InfoBlue.ToHex();
                TextColor = ColorTokens.BackgroundPrimary.ToHex();
                BorderColor = ColorTokens.InfoBlue.ToHex();
                HoverBackgroundColor = ColorTokens.InfoBlueDark.ToHex();
                HoverTextColor = ColorTokens.BackgroundPrimary.ToHex();
                HoverBorderColor = ColorTokens.InfoBlueDark.ToHex();
                PressedBackgroundColor = ColorTokens.InfoBlueDark.ToHex();
                PressedTextColor = ColorTokens.BackgroundPrimary.ToHex();
                PressedBorderColor = ColorTokens.InfoBlueDark.ToHex();
                break;
        }
    }

    /// <summary>
    /// Applies the size tokens based on the current button size.
    /// </summary>
    public void ApplySize()
    {
        switch (Size)
        {
            case ButtonSize.Small:
                FontSize = TypographyTokens.FontSizeSm;
                PaddingHorizontal = SpacingTokens.Sm;
                PaddingVertical = SpacingTokens.Xs;
                MinHeight = 40; // Smaller touch target
                break;
            case ButtonSize.Medium:
                FontSize = TypographyTokens.FontSizeBase;
                PaddingHorizontal = SpacingTokens.Lg;
                PaddingVertical = SpacingTokens.Md;
                MinHeight = 48; // Standard touch target
                break;
            case ButtonSize.Large:
                FontSize = TypographyTokens.FontSizeLg;
                PaddingHorizontal = SpacingTokens.Xl;
                PaddingVertical = SpacingTokens.Lg;
                MinHeight = 56; // Larger touch target
                break;
        }
    }
}

/// <summary>
/// Defines the available button variants.
/// </summary>
public enum ButtonVariant
{
    Primary,
    Secondary,
    Success,
    Danger,
    Warning,
    Info
}

/// <summary>
/// Defines the available button sizes.
/// </summary>
public enum ButtonSize
{
    Small,
    Medium,
    Large
}

/// <summary>
/// Defines the available icon positions for a button.
/// </summary>
public enum ButtonIconPosition
{
    Left,
    Right,
    Top,
    Bottom
}