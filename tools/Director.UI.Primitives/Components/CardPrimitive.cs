using Director.UI.Primitives.DesignTokens;

namespace Director.UI.Primitives.Components;

/// <summary>
/// Framework-agnostic card primitive with design tokens
/// </summary>
public class CardPrimitive
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Footer { get; set; }
    public CardVariant Variant { get; set; } = CardVariant.Default;
    public CardSize Size { get; set; } = CardSize.Medium;
    public bool IsElevated { get; set; } = true;
    public bool IsInteractive { get; set; } = false;
    public bool IsSelected { get; set; } = false;
    public string? ImageUrl { get; set; }
    public string? ImageAlt { get; set; }
    public string? AccessibleLabel { get; set; }
    
    // Styling
    public string FontFamily { get; set; } = TypographyTokens.FontFamilySystem;
    
    // Spacing
    public double Padding { get; set; } = SpacingTokens.Layout.CardPadding;
    public double Margin { get; set; } = SpacingTokens.Component.MarginMd;
    public double Gap { get; set; } = SpacingTokens.Component.GapMd;
    
    // Border
    public double BorderRadius { get; set; } = SpacingTokens.BorderRadius.Xl;
    public double BorderWidth { get; set; } = 1;
    
    // Colors
    public string BackgroundColor { get; set; } = ColorTokens.BackgroundPrimary.ToHex();
    public string BorderColor { get; set; } = ColorTokens.BorderMuted.ToHex();
    public string TitleColor { get; set; } = ColorTokens.TextPrimary.ToHex();
    public string SubtitleColor { get; set; } = ColorTokens.TextSecondary.ToHex();
    public string ContentColor { get; set; } = ColorTokens.TextPrimary.ToHex();
    public string FooterColor { get; set; } = ColorTokens.TextSecondary.ToHex();
    
    // Interactive Colors
    public string HoverBackgroundColor { get; set; } = ColorTokens.InteractiveHover.ToHex();
    public string HoverBorderColor { get; set; } = ColorTokens.BorderDefault.ToHex();
    public string SelectedBackgroundColor { get; set; } = ColorTokens.InteractiveSelected.ToHex();
    public string SelectedBorderColor { get; set; } = ColorTokens.PrimaryBlue.ToHex();
    
    // Shadow
    public double ShadowOffsetX { get; set; } = SpacingTokens.Shadow.OffsetX;
    public double ShadowOffsetY { get; set; } = SpacingTokens.Shadow.OffsetY;
    public double ShadowBlurRadius { get; set; } = SpacingTokens.Shadow.BlurRadius;
    public double ShadowSpreadRadius { get; set; } = SpacingTokens.Shadow.SpreadRadius;
    public string ShadowColor { get; set; } = ColorTokens.ShadowLight.ToHex();
    
    // Transitions
    public double TransitionDuration { get; set; } = 150; // milliseconds
    public string TransitionTimingFunction { get; set; } = "ease-in-out";
    
    public void ApplyVariant()
    {
        switch (Variant)
        {
            case CardVariant.Default:
                BackgroundColor = ColorTokens.BackgroundPrimary.ToHex();
                BorderColor = ColorTokens.BorderMuted.ToHex();
                break;
                
            case CardVariant.Elevated:
                IsElevated = true;
                ShadowColor = ColorTokens.ShadowMedium.ToHex();
                break;
                
            case CardVariant.Outlined:
                BorderWidth = 2;
                BorderColor = ColorTokens.BorderDefault.ToHex();
                break;
                
            case CardVariant.Filled:
                BackgroundColor = ColorTokens.BackgroundSecondary.ToHex();
                BorderColor = ColorTokens.BorderMuted.ToHex();
                break;
        }
    }
    
    public void ApplySize()
    {
        switch (Size)
        {
            case CardSize.Small:
                Padding = SpacingTokens.Component.PaddingSm;
                Gap = SpacingTokens.Component.GapSm;
                break;
                
            case CardSize.Medium:
                Padding = SpacingTokens.Layout.CardPadding;
                Gap = SpacingTokens.Component.GapMd;
                break;
                
            case CardSize.Large:
                Padding = SpacingTokens.Component.PaddingLg;
                Gap = SpacingTokens.Component.GapLg;
                break;
        }
    }
}

public enum CardVariant
{
    Default,
    Elevated,
    Outlined,
    Filled
}

public enum CardSize
{
    Small,
    Medium,
    Large
}
