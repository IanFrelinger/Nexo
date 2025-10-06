using Director.UI.Primitives.DesignTokens;

namespace Director.UI.Primitives.Components;

/// <summary>
/// Framework-agnostic button primitive with design tokens
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
    
    // Styling
    public string FontFamily { get; set; } = TypographyTokens.FontFamilySystem;
    public double FontSize { get; set; } = TypographyTokens.Scale.Button;
    public string FontWeight { get; set; } = TypographyTokens.FontWeightMedium;
    public double LineHeight { get; set; } = TypographyTokens.LineHeightNormal;
    
    // Spacing
    public double PaddingHorizontal { get; set; } = SpacingTokens.Interactive.ButtonPaddingHorizontal;
    public double PaddingVertical { get; set; } = SpacingTokens.Interactive.ButtonPaddingVertical;
    public double MinWidth { get; set; } = 0;
    public double MinHeight { get; set; } = SpacingTokens.Interactive.TouchTargetMin;
    
    // Border
    public double BorderRadius { get; set; } = SpacingTokens.BorderRadius.Lg;
    public double BorderWidth { get; set; } = 1;
    
    // Colors (will be set based on variant)
    public string BackgroundColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string BorderColor { get; set; } = string.Empty;
    public string HoverBackgroundColor { get; set; } = string.Empty;
    public string HoverTextColor { get; set; } = string.Empty;
    public string HoverBorderColor { get; set; } = string.Empty;
    public string PressedBackgroundColor { get; set; } = string.Empty;
    public string PressedTextColor { get; set; } = string.Empty;
    public string PressedBorderColor { get; set; } = string.Empty;
    public string DisabledBackgroundColor { get; set; } = string.Empty;
    public string DisabledTextColor { get; set; } = string.Empty;
    public string DisabledBorderColor { get; set; } = string.Empty;
    
    // Transitions
    public double TransitionDuration { get; set; } = 150; // milliseconds
    public string TransitionTimingFunction { get; set; } = "ease-in-out";
    
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
                BackgroundColor = ColorTokens.BackgroundPrimary.ToHex();
                TextColor = ColorTokens.TextPrimary.ToHex();
                BorderColor = ColorTokens.BorderDefault.ToHex();
                HoverBackgroundColor = ColorTokens.InteractiveHover.ToHex();
                HoverTextColor = ColorTokens.TextPrimary.ToHex();
                HoverBorderColor = ColorTokens.PrimaryBlue.ToHex();
                PressedBackgroundColor = ColorTokens.InteractivePressed.ToHex();
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
        
        // Disabled state colors
        DisabledBackgroundColor = ColorTokens.BackgroundDisabled.ToHex();
        DisabledTextColor = ColorTokens.TextDisabled.ToHex();
        DisabledBorderColor = ColorTokens.BorderMuted.ToHex();
    }
    
    public void ApplySize()
    {
        switch (Size)
        {
            case ButtonSize.Small:
                FontSize = TypographyTokens.Scale.BodySmall;
                PaddingHorizontal = SpacingTokens.Sm;
                PaddingVertical = SpacingTokens.Xs;
                MinHeight = SpacingTokens.Interactive.TouchTargetMin;
                break;
                
            case ButtonSize.Medium:
                FontSize = TypographyTokens.Scale.Button;
                PaddingHorizontal = SpacingTokens.Lg;
                PaddingVertical = SpacingTokens.Md;
                MinHeight = SpacingTokens.Interactive.TouchTargetComfortable;
                break;
                
            case ButtonSize.Large:
                FontSize = TypographyTokens.Scale.BodyLarge;
                PaddingHorizontal = SpacingTokens.Xl;
                PaddingVertical = SpacingTokens.Lg;
                MinHeight = SpacingTokens.Interactive.TouchTargetLarge;
                break;
        }
    }
}

public enum ButtonVariant
{
    Primary,
    Secondary,
    Success,
    Danger,
    Warning,
    Info
}

public enum ButtonSize
{
    Small,
    Medium,
    Large
}

public enum ButtonIconPosition
{
    Left,
    Right,
    Top,
    Bottom
}
