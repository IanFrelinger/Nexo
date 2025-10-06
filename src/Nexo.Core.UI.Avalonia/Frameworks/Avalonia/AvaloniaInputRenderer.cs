using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Nexo.Core.UI.Primitives.Components;
using Nexo.Core.UI.Primitives.DesignTokens;

namespace Nexo.Core.UI.Avalonia.Frameworks.Avalonia;

/// <summary>
/// Renders an <see cref="InputPrimitive"/> into an Avalonia <see cref="TextBox"/> control.
/// </summary>
public static class AvaloniaInputRenderer
{
    public static Control Render(InputPrimitive primitive)
    {
        var textBox = new TextBox
        {
            Text = primitive.Value,
            Watermark = primitive.Placeholder,
            IsEnabled = primitive.IsEnabled,
            IsReadOnly = primitive.IsReadOnly,
            AcceptsReturn = primitive.IsMultiline,
            TextWrapping = primitive.IsMultiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            MinWidth = SpacingTokens.Xxl * 4, // Example min width
            MinHeight = primitive.MinHeight,
            CornerRadius = new CornerRadius(primitive.BorderRadius),
            BorderThickness = new Thickness(primitive.BorderWidth),
            Padding = new Thickness(primitive.PaddingHorizontal, primitive.PaddingVertical),
            Background = new SolidColorBrush(Color.Parse(primitive.BackgroundColor)),
            BorderBrush = new SolidColorBrush(Color.Parse(primitive.BorderColor)),
            Foreground = new SolidColorBrush(Color.Parse(primitive.TextColor)),
            FontFamily = new FontFamily(primitive.FontFamily),
            FontSize = primitive.FontSize,
            FontWeight = ConvertFontWeight(primitive.FontWeight),
        };

        if (primitive.MaxLength > 0)
        {
            textBox.MaxLength = primitive.MaxLength; // For single line, MaxLength is char limit
        }

        // Set accessible label
        if (!string.IsNullOrEmpty(primitive.AccessibleLabel))
        {
            // AutomationProperties.SetName(textBox, primitive.AccessibleLabel); // Requires Avalonia.Automation
        }

        // Apply placeholder text color (requires custom styling or template modification in Avalonia)
        // For simplicity, we'll just set the Foreground for now, but a proper solution involves templates.
        // textBox.Resources["TextControlForegroundPointerOver"] = new SolidColorBrush(Color.Parse(primitive.PlaceholderColor));

        // Wrap in a StackPanel to include Label and HelperText/ErrorText
        var stackPanel = new StackPanel();

        if (!string.IsNullOrEmpty(primitive.Label))
        {
            stackPanel.Children.Add(new TextBlock
            {
                Text = primitive.Label,
                Margin = new Thickness(0, 0, 0, SpacingTokens.Xs),
                Foreground = new SolidColorBrush(Color.Parse(primitive.LabelColor)),
                FontFamily = new FontFamily(TypographyTokens.FontFamilySystem),
                FontSize = TypographyTokens.FontSizeSm,
                FontWeight = ConvertFontWeight(TypographyTokens.FontWeightMedium)
            });
        }

        stackPanel.Children.Add(textBox);

        if (!string.IsNullOrEmpty(primitive.ErrorText))
        {
            stackPanel.Children.Add(new TextBlock
            {
                Text = primitive.ErrorText,
                Margin = new Thickness(0, SpacingTokens.Xs, 0, 0),
                Foreground = new SolidColorBrush(Color.Parse(primitive.ErrorTextColor)),
                FontFamily = new FontFamily(TypographyTokens.FontFamilySystem),
                FontSize = TypographyTokens.FontSizeXs,
                FontWeight = ConvertFontWeight(TypographyTokens.FontWeightNormal)
            });
        }
        else if (!string.IsNullOrEmpty(primitive.HelperText))
        {
            stackPanel.Children.Add(new TextBlock
            {
                Text = primitive.HelperText,
                Margin = new Thickness(0, SpacingTokens.Xs, 0, 0),
                Foreground = new SolidColorBrush(Color.Parse(primitive.HelperTextColor)),
                FontFamily = new FontFamily(TypographyTokens.FontFamilySystem),
                FontSize = TypographyTokens.FontSizeXs,
                FontWeight = ConvertFontWeight(TypographyTokens.FontWeightNormal)
            });
        }

        return stackPanel;
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
}