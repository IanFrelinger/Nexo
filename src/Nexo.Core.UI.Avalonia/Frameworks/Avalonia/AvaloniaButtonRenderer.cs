using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using Nexo.Core.UI.Primitives.Components;
using Nexo.Core.UI.Primitives.DesignTokens;

namespace Nexo.Core.UI.Avalonia.Frameworks.Avalonia;

/// <summary>
/// Renders a <see cref="ButtonPrimitive"/> into an Avalonia <see cref="Button"/> control.
/// </summary>
public static class AvaloniaButtonRenderer
{
    public static Button Render(ButtonPrimitive primitive)
    {
        var button = new Button
        {
            Content = primitive.Text,
            IsEnabled = primitive.IsEnabled,
            MinWidth = primitive.MinWidth,
            MinHeight = primitive.MinHeight,
            CornerRadius = new CornerRadius(primitive.BorderRadius),
            BorderThickness = new Thickness(primitive.BorderWidth),
            Padding = new Thickness(primitive.PaddingHorizontal, primitive.PaddingVertical),
            Cursor = new Cursor(StandardCursorType.Hand),
        };

        // Apply typography
        button.FontFamily = new FontFamily(primitive.FontFamily);
        button.FontSize = primitive.FontSize;
        button.FontWeight = ConvertFontWeight(primitive.FontWeight);

        // Apply colors based on variant
        ApplyVariantStyles(button, primitive);

        // Set accessible label
        if (!string.IsNullOrEmpty(primitive.AccessibleLabel))
        {
            // AutomationProperties.SetName(button, primitive.AccessibleLabel); // Requires Avalonia.Automation
        }

        // TODO: Add icon support
        // TODO: Add loading state indicator
        // TODO: Implement hover/pressed/disabled states using Avalonia Styles/Triggers for full effect

        return button;
    }

    private static FontWeight ConvertFontWeight(string fontWeight)
    {
        return fontWeight switch
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