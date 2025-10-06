using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using Nexo.Core.UI.Primitives.Components;
using Nexo.Core.UI.Primitives.DesignTokens;

namespace Nexo.Core.UI.Avalonia.Frameworks.Avalonia;

/// <summary>
/// Renders a <see cref="CardPrimitive"/> into an Avalonia <see cref="Border"/> control.
/// </summary>
public static class AvaloniaCardRenderer
{
    public static Border Render(CardPrimitive primitive)
    {
        var cardContent = new StackPanel
        {
            Spacing = primitive.Gap,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        if (!string.IsNullOrEmpty(primitive.ImageUrl))
        {
            // Note: Loading images from URL requires Avalonia.Markup.Xaml.Loader or similar
            // For simplicity, we'll just add a placeholder TextBlock for now
            cardContent.Children.Add(new TextBlock
            {
                Text = $"Image: {primitive.ImageAlt ?? primitive.ImageUrl}",
                Foreground = new SolidColorBrush(Color.Parse("#666666")), // TextSecondary color
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        if (!string.IsNullOrEmpty(primitive.Title))
        {
            cardContent.Children.Add(new TextBlock
            {
                Text = primitive.Title,
                FontFamily = new FontFamily(TypographyTokens.FontFamilySystem),
                FontSize = TypographyTokens.FontSize2Xl,
                FontWeight = ConvertFontWeight(TypographyTokens.FontWeightSemiBold),
                Foreground = new SolidColorBrush(Color.Parse(primitive.TitleColor))
            });
        }

        if (!string.IsNullOrEmpty(primitive.Subtitle))
        {
            cardContent.Children.Add(new TextBlock
            {
                Text = primitive.Subtitle,
                FontFamily = new FontFamily(TypographyTokens.FontFamilySystem),
                FontSize = TypographyTokens.FontSizeSm,
                FontWeight = ConvertFontWeight(TypographyTokens.FontWeightNormal),
                Foreground = new SolidColorBrush(Color.Parse(primitive.SubtitleColor))
            });
        }

        if (!string.IsNullOrEmpty(primitive.Content))
        {
            cardContent.Children.Add(new TextBlock
            {
                Text = primitive.Content,
                FontFamily = new FontFamily(TypographyTokens.FontFamilySystem),
                FontSize = TypographyTokens.FontSizeBase,
                FontWeight = ConvertFontWeight(TypographyTokens.FontWeightNormal),
                Foreground = new SolidColorBrush(Color.Parse(primitive.ContentColor))
            });
        }

        if (!string.IsNullOrEmpty(primitive.Footer))
        {
            cardContent.Children.Add(new TextBlock
            {
                Text = primitive.Footer,
                FontFamily = new FontFamily(TypographyTokens.FontFamilySystem),
                FontSize = TypographyTokens.FontSizeXs,
                FontWeight = ConvertFontWeight(TypographyTokens.FontWeightNormal),
                Foreground = new SolidColorBrush(Color.Parse(primitive.FooterColor)),
                Margin = new Thickness(0, SpacingTokens.Sm, 0, 0) // Add some top margin for separation
            });
        }

        var card = new Border
        {
            Child = cardContent,
            Padding = new Thickness(primitive.Padding),
            Margin = new Thickness(primitive.Margin),
            CornerRadius = new CornerRadius(primitive.BorderRadius),
            BorderThickness = new Thickness(primitive.BorderWidth),
            Background = new SolidColorBrush(Color.Parse(primitive.BackgroundColor)),
            BorderBrush = new SolidColorBrush(Color.Parse(primitive.BorderColor)),
            // AutomationProperties.SetAccessibleName(card, primitive.AccessibleLabel ?? primitive.Title), // Requires Avalonia.Automation
        };

        if (primitive.IsElevated)
        {
            // Avalonia's BoxShadow is a bit different from CSS.
            // For simplicity, we'll use a basic shadow for now.
            card.BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = primitive.ShadowOffsetX,
                OffsetY = primitive.ShadowOffsetY,
                Blur = primitive.ShadowBlurRadius,
                Spread = primitive.ShadowSpreadRadius,
                Color = Color.Parse(primitive.ShadowColor)
            });
        }

        if (primitive.IsInteractive)
        {
            card.Cursor = new Cursor(StandardCursorType.Hand);
            // TODO: Implement hover/selected states using Avalonia Styles/Triggers for full effect
        }

        return card;
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