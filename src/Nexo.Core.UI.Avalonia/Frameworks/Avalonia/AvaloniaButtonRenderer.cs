using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Styling;
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
        var content = BuildButtonContent(primitive);
        var button = new Button
        {
            Content = content,
            IsEnabled = primitive.IsEnabled && !primitive.IsLoading,
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

        // Add hover/pressed/disabled state styles
        AddStateStyles(button, primitive);

        // Set accessible label
        if (!string.IsNullOrEmpty(primitive.AccessibleLabel))
        {
            // AutomationProperties.SetName(button, primitive.AccessibleLabel); // Requires Avalonia.Automation
        }

        return button;
    }

    private static object BuildButtonContent(ButtonPrimitive primitive)
    {
        if (primitive.IsLoading)
        {
            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            stack.Children.Add(new ProgressBar { IsIndeterminate = true, Width = 20, Height = 20 });
            stack.Children.Add(new TextBlock { Text = "Loading...", VerticalAlignment = VerticalAlignment.Center });
            return stack;
        }

        if (!string.IsNullOrEmpty(primitive.Icon))
        {
            var icon = ResolveIcon(primitive.Icon);
            var stack = new StackPanel
            {
                Orientation = primitive.IconPosition == ButtonIconPosition.Right ? Orientation.Horizontal : Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            if (primitive.IconPosition == ButtonIconPosition.Right)
            {
                stack.Children.Add(new TextBlock { Text = primitive.Text, VerticalAlignment = VerticalAlignment.Center });
                stack.Children.Add(new TextBlock { Text = icon, FontSize = primitive.FontSize, VerticalAlignment = VerticalAlignment.Center });
            }
            else
            {
                stack.Children.Add(new TextBlock { Text = icon, FontSize = primitive.FontSize, VerticalAlignment = VerticalAlignment.Center });
                stack.Children.Add(new TextBlock { Text = primitive.Text, VerticalAlignment = VerticalAlignment.Center });
            }
            return stack;
        }

        return primitive.Text;
    }

    private static string ResolveIcon(string iconName)
    {
        return iconName.ToLowerInvariant() switch
        {
            "add" or "plus" => "\u002B",
            "delete" or "remove" or "trash" => "\u2715",
            "edit" or "pencil" => "\u270E",
            "save" => "\u2713",
            "close" or "cancel" => "\u2715",
            "search" => "\u2315",
            "settings" or "gear" => "\u2699",
            "refresh" => "\u27F3",
            _ => iconName
        };
    }

    private static void AddStateStyles(Button button, ButtonPrimitive primitive)
    {
        var hoverStyle = new Style(x => x.OfType<Button>().Class(":pointerover"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.Parse(primitive.HoverBackgroundColor))),
                new Setter(Button.ForegroundProperty, new SolidColorBrush(Color.Parse(primitive.HoverTextColor))),
                new Setter(Button.BorderBrushProperty, new SolidColorBrush(Color.Parse(primitive.HoverBorderColor)))
            }
        };
        var pressedStyle = new Style(x => x.OfType<Button>().Class(":pressed"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.Parse(primitive.PressedBackgroundColor))),
                new Setter(Button.ForegroundProperty, new SolidColorBrush(Color.Parse(primitive.PressedTextColor))),
                new Setter(Button.BorderBrushProperty, new SolidColorBrush(Color.Parse(primitive.PressedBorderColor)))
            }
        };
        var disabledStyle = new Style(x => x.OfType<Button>().Class(":disabled"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.Parse(primitive.DisabledBackgroundColor))),
                new Setter(Button.ForegroundProperty, new SolidColorBrush(Color.Parse(primitive.DisabledTextColor))),
                new Setter(Button.BorderBrushProperty, new SolidColorBrush(Color.Parse(primitive.DisabledBorderColor)))
            }
        };
        button.Styles.Add(hoverStyle);
        button.Styles.Add(pressedStyle);
        button.Styles.Add(disabledStyle);
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