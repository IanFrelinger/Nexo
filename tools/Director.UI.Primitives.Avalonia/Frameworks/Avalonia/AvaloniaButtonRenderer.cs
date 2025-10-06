using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Director.UI.Primitives.Components;
using Director.UI.Primitives.DesignTokens;

namespace Director.UI.Primitives.Frameworks.Avalonia;

/// <summary>
/// Renders ButtonPrimitive as Avalonia Button
/// </summary>
public static class AvaloniaButtonRenderer
{
    public static Button Render(ButtonPrimitive primitive)
    {
        // Apply variant colors
        primitive.ApplyVariantColors();
        primitive.ApplySize();
        
        var button = new Button
        {
            Content = primitive.Text,
            IsEnabled = primitive.IsEnabled,
            FontFamily = new FontFamily(primitive.FontFamily),
            FontSize = primitive.FontSize,
            FontWeight = GetFontWeight(primitive.FontWeight),
            Padding = new Thickness(primitive.PaddingHorizontal, primitive.PaddingVertical),
            MinWidth = primitive.MinWidth,
            MinHeight = primitive.MinHeight,
            CornerRadius = new CornerRadius(primitive.BorderRadius),
            BorderThickness = new Thickness(primitive.BorderWidth),
            Background = new SolidColorBrush(Color.Parse(primitive.BackgroundColor)),
            Foreground = new SolidColorBrush(Color.Parse(primitive.TextColor)),
            BorderBrush = new SolidColorBrush(Color.Parse(primitive.BorderColor)),
            Cursor = Avalonia.Input.Cursor.Parse("Hand"),
            ToolTip = primitive.Tooltip,
            Tag = primitive // Store primitive for event handling
        };
        
        // Set accessible properties
        if (!string.IsNullOrEmpty(primitive.AccessibleLabel))
        {
            button.SetValue(AutomationProperties.NameProperty, primitive.AccessibleLabel);
        }
        
        // Apply styles based on variant
        ApplyVariantStyles(button, primitive);
        
        return button;
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
    
    private static void ApplyVariantStyles(Button button, ButtonPrimitive primitive)
    {
        // Simplified styling - just set the base properties
        // Advanced styling with triggers would require more complex Avalonia setup
        button.Background = new SolidColorBrush(Color.Parse(primitive.BackgroundColor));
        button.Foreground = new SolidColorBrush(Color.Parse(primitive.TextColor));
        button.BorderBrush = new SolidColorBrush(Color.Parse(primitive.BorderColor));
    }
}

