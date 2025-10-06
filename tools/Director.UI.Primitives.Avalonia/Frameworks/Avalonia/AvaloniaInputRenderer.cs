using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Director.UI.Primitives.Components;
using Director.UI.Primitives.DesignTokens;

namespace Director.UI.Primitives.Frameworks.Avalonia;

/// <summary>
/// Renders InputPrimitive as Avalonia TextBox
/// </summary>
public static class AvaloniaInputRenderer
{
    public static TextBox Render(InputPrimitive primitive)
    {
        primitive.ApplySize();
        
        var textBox = new TextBox
        {
            Text = primitive.Value,
            Watermark = primitive.Placeholder,
            IsEnabled = !primitive.IsDisabled,
            IsReadOnly = primitive.IsReadOnly,
            FontFamily = new FontFamily(primitive.FontFamily),
            FontSize = primitive.FontSize,
            FontWeight = GetFontWeight(primitive.FontWeight),
            Padding = new Thickness(primitive.PaddingHorizontal, primitive.PaddingVertical),
            MinHeight = primitive.MinHeight,
            CornerRadius = new CornerRadius(primitive.BorderRadius),
            BorderThickness = new Thickness(primitive.BorderWidth),
            Background = new SolidColorBrush(Color.Parse(primitive.BackgroundColor)),
            Foreground = new SolidColorBrush(Color.Parse(primitive.TextColor)),
            BorderBrush = new SolidColorBrush(Color.Parse(primitive.BorderColor)),
            Tag = primitive // Store primitive for event handling
        };
        
        // Set accessible properties
        if (!string.IsNullOrEmpty(primitive.AccessibleLabel))
        {
            textBox.SetValue(AutomationProperties.NameProperty, primitive.AccessibleLabel);
        }
        
        if (!string.IsNullOrEmpty(primitive.AccessibleDescription))
        {
            textBox.SetValue(AutomationProperties.DescriptionProperty, primitive.AccessibleDescription);
        }
        
        // Apply styles
        ApplyInputStyles(textBox, primitive);
        
        return textBox;
    }
    
    public static StackPanel RenderWithLabel(InputPrimitive primitive)
    {
        var container = new StackPanel
        {
            Spacing = SpacingTokens.Component.GapSm
        };
        
        // Label
        if (!string.IsNullOrEmpty(primitive.Label))
        {
            var label = new TextBlock
            {
                Text = primitive.Label + (primitive.IsRequired ? " *" : ""),
                FontFamily = new FontFamily(primitive.FontFamily),
                FontSize = TypographyTokens.Scale.Label,
                Foreground = new SolidColorBrush(Color.Parse(primitive.LabelColor)),
                FontWeight = FontWeight.Medium
            };
            container.Children.Add(label);
        }
        
        // Input
        var input = Render(primitive);
        container.Children.Add(input);
        
        // Helper text or error text
        if (primitive.HasError && !string.IsNullOrEmpty(primitive.ErrorText))
        {
            var errorText = new TextBlock
            {
                Text = primitive.ErrorText,
                FontFamily = new FontFamily(primitive.FontFamily),
                FontSize = TypographyTokens.Scale.Helper,
                Foreground = new SolidColorBrush(Color.Parse(primitive.ErrorTextColor)),
                FontWeight = FontWeight.Normal
            };
            container.Children.Add(errorText);
        }
        else if (!string.IsNullOrEmpty(primitive.HelperText))
        {
            var helperText = new TextBlock
            {
                Text = primitive.HelperText,
                FontFamily = new FontFamily(primitive.FontFamily),
                FontSize = TypographyTokens.Scale.Helper,
                Foreground = new SolidColorBrush(Color.Parse(primitive.HelperTextColor)),
                FontWeight = FontWeight.Normal
            };
            container.Children.Add(helperText);
        }
        
        return container;
    }
    
    private static FontWeight GetFontWeight(string weight)
    {
        return weight switch
        {
            TypographyTokens.FontWeightLight => FontWeight.Light,
            TypographyTokens.FontWeightNormal => FontWeight.Normal,
            TypographyTokens.FontWeightMedium => FontWeight.Medium,
            TypographyTokens.FontWeightSemiBold => FontWeight.SemiBold,
            TypographyTokens.FontWeightBold => FontWeight.Bold,
            TypographyTokens.FontWeightExtraBold => FontWeight.ExtraBold,
            _ => FontWeight.Normal
        };
    }
    
    private static void ApplyInputStyles(TextBox textBox, InputPrimitive primitive)
    {
        var style = new Style(typeof(TextBox));
        
        // Base style
        style.Setters.Add(new Setter(TextBox.BackgroundProperty, new SolidColorBrush(Color.Parse(primitive.BackgroundColor))));
        style.Setters.Add(new Setter(TextBox.ForegroundProperty, new SolidColorBrush(Color.Parse(primitive.TextColor))));
        style.Setters.Add(new Setter(TextBox.BorderBrushProperty, new SolidColorBrush(Color.Parse(primitive.BorderColor))));
        
        // Focus state
        var focusTrigger = new StyleTrigger();
        focusTrigger.Property = TextBox.IsFocusedProperty;
        focusTrigger.Value = true;
        focusTrigger.Setters.Add(new Setter(TextBox.BorderBrushProperty, new SolidColorBrush(Color.Parse(primitive.FocusBorderColor))));
        style.Triggers.Add(focusTrigger);
        
        // Error state
        if (primitive.HasError)
        {
            var errorTrigger = new StyleTrigger();
            errorTrigger.Property = TextBox.TagProperty;
            errorTrigger.Value = "error";
            errorTrigger.Setters.Add(new Setter(TextBox.BorderBrushProperty, new SolidColorBrush(Color.Parse(primitive.ErrorBorderColor))));
            style.Triggers.Add(errorTrigger);
        }
        
        // Disabled state
        var disabledTrigger = new StyleTrigger();
        disabledTrigger.Property = TextBox.IsEnabledProperty;
        disabledTrigger.Value = false;
        disabledTrigger.Setters.Add(new Setter(TextBox.BackgroundProperty, new SolidColorBrush(Color.Parse(primitive.DisabledBackgroundColor))));
        disabledTrigger.Setters.Add(new Setter(TextBox.ForegroundProperty, new SolidColorBrush(Color.Parse(primitive.DisabledTextColor))));
        disabledTrigger.Setters.Add(new Setter(TextBox.BorderBrushProperty, new SolidColorBrush(Color.Parse(primitive.DisabledBorderColor))));
        style.Triggers.Add(disabledTrigger);
        
        // Apply transitions
        var backgroundTransition = new BrushTransition
        {
            Property = TextBox.BackgroundProperty,
            Duration = TimeSpan.FromMilliseconds(primitive.TransitionDuration)
        };
        var borderTransition = new BrushTransition
        {
            Property = TextBox.BorderBrushProperty,
            Duration = TimeSpan.FromMilliseconds(primitive.TransitionDuration)
        };
        
        textBox.Transitions.Add(backgroundTransition);
        textBox.Transitions.Add(borderTransition);
        
        textBox.Styles.Add(style);
    }
}
