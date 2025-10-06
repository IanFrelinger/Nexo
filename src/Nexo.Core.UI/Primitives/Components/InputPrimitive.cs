using Nexo.Core.UI.Primitives.DesignTokens;
using Nexo.Core.UI.Primitives.Extensions;

namespace Nexo.Core.UI.Primitives.Components;

/// <summary>
/// Represents a framework-agnostic input primitive.
/// </summary>
public class InputPrimitive
{
    public string Value { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string? HelperText { get; set; }
    public string? ErrorText { get; set; }
    public InputType Type { get; set; } = InputType.Text;
    public bool IsEnabled { get; set; } = true;
    public bool IsReadOnly { get; set; } = false;
    public bool IsRequired { get; set; } = false;
    public bool IsMultiline { get; set; } = false;
    public int MinLines { get; set; } = 1;
    public int MaxLength { get; set; } = 0; // 0 for no limit
    public string? AccessibleLabel { get; set; }

    // Typography
    public string FontFamily { get; set; } = TypographyTokens.FontFamilySystem;
    public double FontSize { get; set; } = TypographyTokens.FontSizeBase;
    public string FontWeight { get; set; } = TypographyTokens.FontWeightNormal;
    public double LineHeight { get; set; } = TypographyTokens.LineHeightNormal;

    // Spacing
    public double PaddingHorizontal { get; set; } = SpacingTokens.Lg;
    public double PaddingVertical { get; set; } = SpacingTokens.Md;
    public double MinHeight { get; set; } = 48; // Touch target minimum

    // Border
    public double BorderRadius { get; set; } = SpacingTokens.BorderRadius.Md;
    public double BorderWidth { get; set; } = 2;

    // Colors
    public string BackgroundColor { get; set; } = ColorTokens.BackgroundPrimary.ToHex();
    public string TextColor { get; set; } = ColorTokens.TextPrimary.ToHex();
    public string BorderColor { get; set; } = ColorTokens.BorderDefault.ToHex();
    public string PlaceholderColor { get; set; } = ColorTokens.TextSecondary.ToHex();
    public string LabelColor { get; set; } = ColorTokens.TextPrimary.ToHex();
    public string HelperTextColor { get; set; } = ColorTokens.TextSecondary.ToHex();
    public string ErrorTextColor { get; set; } = ColorTokens.ErrorRed.ToHex();

    public string FocusBorderColor { get; set; } = ColorTokens.BorderFocus.ToHex();
    public string DisabledBackgroundColor { get; set; } = ColorTokens.BackgroundDisabled.ToHex();
    public string DisabledBorderColor { get; set; } = ColorTokens.BorderMuted.ToHex();
    public string DisabledTextColor { get; set; } = ColorTokens.TextDisabled.ToHex();

    // Transitions
    public double TransitionDuration { get; set; } = 0.15; // seconds
    public string TransitionTimingFunction { get; set; } = "ease-in-out";
}

/// <summary>
/// Defines the available input types.
/// </summary>
public enum InputType
{
    Text,
    Email,
    Password,
    Number,
    Url,
    Search,
    Tel,
    Date
}