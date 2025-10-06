using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media;
using Director.UI.Primitives.Components;
using Director.UI.Primitives.DesignTokens;

namespace Director.UI.Primitives.Frameworks.Avalonia;

/// <summary>
/// Renders CardPrimitive as Avalonia Border
/// </summary>
public static class AvaloniaCardRenderer
{
    public static Border Render(CardPrimitive primitive)
    {
        primitive.ApplyVariant();
        primitive.ApplySize();
        
        var card = new Border
        {
            Padding = new Thickness(primitive.Padding),
            Margin = new Thickness(primitive.Margin),
            CornerRadius = new CornerRadius(primitive.BorderRadius),
            BorderThickness = new Thickness(primitive.BorderWidth),
            Background = new SolidColorBrush(Color.Parse(primitive.BackgroundColor)),
            BorderBrush = new SolidColorBrush(Color.Parse(primitive.BorderColor)),
            Tag = primitive // Store primitive for event handling
        };
        
        // Set accessible properties
        if (!string.IsNullOrEmpty(primitive.AccessibleLabel))
        {
            card.SetValue(AutomationProperties.NameProperty, primitive.AccessibleLabel);
        }
        
        // Create content
        var content = CreateCardContent(primitive);
        card.Child = content;
        
        // Apply styles
        ApplyCardStyles(card, primitive);
        
        return card;
    }
    
    private static Panel CreateCardContent(CardPrimitive primitive)
    {
        var container = new StackPanel
        {
            Spacing = primitive.Gap
        };
        
        // Image (if provided)
        if (!string.IsNullOrEmpty(primitive.ImageUrl))
        {
            var image = new Image
            {
                Source = new Bitmap(primitive.ImageUrl),
                Stretch = Stretch.UniformToFill,
                MaxHeight = 200,
                CornerRadius = new CornerRadius(primitive.BorderRadius)
            };
            container.Children.Add(image);
        }
        
        // Title
        if (!string.IsNullOrEmpty(primitive.Title))
        {
            var title = new TextBlock
            {
                Text = primitive.Title,
                FontFamily = new FontFamily(primitive.FontFamily),
                FontSize = TypographyTokens.Scale.H3,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse(primitive.TitleColor)),
                TextWrapping = TextWrapping.Wrap
            };
            container.Children.Add(title);
        }
        
        // Subtitle
        if (!string.IsNullOrEmpty(primitive.Subtitle))
        {
            var subtitle = new TextBlock
            {
                Text = primitive.Subtitle,
                FontFamily = new FontFamily(primitive.FontFamily),
                FontSize = TypographyTokens.Scale.BodySmall,
                FontWeight = FontWeight.Normal,
                Foreground = new SolidColorBrush(Color.Parse(primitive.SubtitleColor)),
                TextWrapping = TextWrapping.Wrap
            };
            container.Children.Add(subtitle);
        }
        
        // Content
        if (!string.IsNullOrEmpty(primitive.Content))
        {
            var content = new TextBlock
            {
                Text = primitive.Content,
                FontFamily = new FontFamily(primitive.FontFamily),
                FontSize = TypographyTokens.Scale.Body,
                FontWeight = FontWeight.Normal,
                Foreground = new SolidColorBrush(Color.Parse(primitive.ContentColor)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = TypographyTokens.LineHeightRelaxed
            };
            container.Children.Add(content);
        }
        
        // Footer
        if (!string.IsNullOrEmpty(primitive.Footer))
        {
            var footer = new TextBlock
            {
                Text = primitive.Footer,
                FontFamily = new FontFamily(primitive.FontFamily),
                FontSize = TypographyTokens.Scale.Caption,
                FontWeight = FontWeight.Normal,
                Foreground = new SolidColorBrush(Color.Parse(primitive.FooterColor)),
                TextWrapping = TextWrapping.Wrap
            };
            container.Children.Add(footer);
        }
        
        return container;
    }
    
    private static void ApplyCardStyles(Border card, CardPrimitive primitive)
    {
        var style = new Style(typeof(Border));
        
        // Base style
        style.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.Parse(primitive.BackgroundColor))));
        style.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.Parse(primitive.BorderColor))));
        
        // Shadow effect (simplified for now)
        if (primitive.IsElevated)
        {
            // Note: DropShadowEffect may not be available in all Avalonia versions
            // For now, we'll skip the shadow effect
        }
        
        // Interactive states
        if (primitive.IsInteractive)
        {
            // Hover state
            var hoverTrigger = new StyleTrigger();
            hoverTrigger.Property = Border.IsPointerOverProperty;
            hoverTrigger.Value = true;
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.Parse(primitive.HoverBackgroundColor))));
            hoverTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.Parse(primitive.HoverBorderColor))));
            style.Triggers.Add(hoverTrigger);
            
            // Selected state
            if (primitive.IsSelected)
            {
                var selectedTrigger = new StyleTrigger();
                selectedTrigger.Property = Border.TagProperty;
                selectedTrigger.Value = "selected";
                selectedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.Parse(primitive.SelectedBackgroundColor))));
                selectedTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.Parse(primitive.SelectedBorderColor))));
                style.Triggers.Add(selectedTrigger);
            }
        }
        
        // Apply transitions
        var backgroundTransition = new BrushTransition
        {
            Property = Border.BackgroundProperty,
            Duration = TimeSpan.FromMilliseconds(primitive.TransitionDuration)
        };
        var borderTransition = new BrushTransition
        {
            Property = Border.BorderBrushProperty,
            Duration = TimeSpan.FromMilliseconds(primitive.TransitionDuration)
        };
        
        card.Transitions.Add(backgroundTransition);
        card.Transitions.Add(borderTransition);
        
        card.Styles.Add(style);
    }
}
