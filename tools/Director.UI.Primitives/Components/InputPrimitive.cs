using Director.UI.Primitives.DesignTokens;

namespace Director.UI.Primitives.Components;

/// <summary>
/// Framework-agnostic input primitive with design tokens
/// </summary>
public class InputPrimitive
{
    public string Value { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string HelperText { get; set; } = string.Empty;
    public string ErrorText { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = false;
    public bool IsDisabled { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
    public InputType Type { get; set; } = InputType.Text;
    public InputSize Size { get; set; } = InputSize.Medium;
    public string? Icon { get; set; }
    public InputIconPosition IconPosition { get; set; } = InputIconPosition.Left;
    public string? AccessibleLabel { get; set; }
    public string? AccessibleDescription { get; set; }
    
    // Styling
    public string FontFamily { get; set; } = TypographyTokens.FontFamilySystem;
    public double FontSize { get; set; } = TypographyTokens.Scale.Input;
    public string FontWeight { get; set; } = TypographyTokens.FontWeightNormal;
    public double LineHeight { get; set; } = TypographyTokens.LineHeightNormal;
    
    // Spacing
    public double PaddingHorizontal { get; set; } = SpacingTokens.Interactive.InputPaddingHorizontal;
    public double PaddingVertical { get; set; } = SpacingTokens.Interactive.InputPaddingVertical;
    public double MinHeight { get; set; } = SpacingTokens.Interactive.TouchTargetMin;
    
    // Border
    public double BorderRadius { get; set; } = SpacingTokens.BorderRadius.Lg;
    public double BorderWidth { get; set; } = 2;
    
    // Colors
    public string BackgroundColor { get; set; } = ColorTokens.BackgroundPrimary.ToHex();
    public string TextColor { get; set; } = ColorTokens.TextPrimary.ToHex();
    public string PlaceholderColor { get; set; } = ColorTokens.TextTertiary.ToHex();
    public string BorderColor { get; set; } = ColorTokens.BorderDefault.ToHex();
    public string FocusBorderColor { get; set; } = ColorTokens.BorderFocus.ToHex();
    public string ErrorBorderColor { get; set; } = ColorTokens.ErrorRed.ToHex();
    public string DisabledBackgroundColor { get; set; } = ColorTokens.BackgroundDisabled.ToHex();
    public string DisabledTextColor { get; set; } = ColorTokens.TextDisabled.ToHex();
    public string DisabledBorderColor { get; set; } = ColorTokens.BorderMuted.ToHex();
    
    // Label Colors
    public string LabelColor { get; set; } = ColorTokens.TextPrimary.ToHex();
    public string RequiredLabelColor { get; set; } = ColorTokens.ErrorRed.ToHex();
    public string HelperTextColor { get; set; } = ColorTokens.TextSecondary.ToHex();
    public string ErrorTextColor { get; set; } = ColorTokens.ErrorRed.ToHex();
    
    // Transitions
    public double TransitionDuration { get; set; } = 150; // milliseconds
    public string TransitionTimingFunction { get; set; } = "ease-in-out";
    
    public void ApplySize()
    {
        switch (Size)
        {
            case InputSize.Small:
                FontSize = TypographyTokens.Scale.BodySmall;
                PaddingHorizontal = SpacingTokens.Sm;
                PaddingVertical = SpacingTokens.Xs;
                MinHeight = SpacingTokens.Interactive.TouchTargetMin;
                break;
                
            case InputSize.Medium:
                FontSize = TypographyTokens.Scale.Input;
                PaddingHorizontal = SpacingTokens.Md;
                PaddingVertical = SpacingTokens.Sm;
                MinHeight = SpacingTokens.Interactive.TouchTargetComfortable;
                break;
                
            case InputSize.Large:
                FontSize = TypographyTokens.Scale.BodyLarge;
                PaddingHorizontal = SpacingTokens.Lg;
                PaddingVertical = SpacingTokens.Md;
                MinHeight = SpacingTokens.Interactive.TouchTargetLarge;
                break;
        }
    }
    
    public bool HasError => !string.IsNullOrEmpty(ErrorText);
    public bool IsValid => !HasError && (IsRequired ? !string.IsNullOrEmpty(Value) : true);
}

public enum InputType
{
    Text,
    Email,
    Password,
    Number,
    Tel,
    Url,
    Search,
    TextArea
}

public enum InputSize
{
    Small,
    Medium,
    Large
}

public enum InputIconPosition
{
    Left,
    Right
}
