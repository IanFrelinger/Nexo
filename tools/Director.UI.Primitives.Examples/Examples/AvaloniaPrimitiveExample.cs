using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Director.UI.Primitives.Components;
using Director.UI.Primitives.Frameworks.Avalonia;

namespace Director.UI.Primitives.Examples.Examples;

/// <summary>
/// Example showing how to use UI primitives in Avalonia
/// </summary>
public partial class AvaloniaPrimitiveExample : UserControl
{
    public AvaloniaPrimitiveExample()
    {
        InitializeComponent();
        CreatePrimitiveExamples();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private void CreatePrimitiveExamples()
    {
        var container = this.FindControl<StackPanel>("PrimitiveContainer");
        if (container == null) return;
        
        // Button Examples
        CreateButtonExamples(container);
        
        // Input Examples
        CreateInputExamples(container);
        
        // Card Examples
        CreateCardExamples(container);
    }
    
    private void CreateButtonExamples(StackPanel container)
    {
        var buttonSection = new StackPanel
        {
            Spacing = 16,
            Margin = new Thickness(0, 0, 0, 32)
        };
        
        var title = new TextBlock
        {
            Text = "Button Primitives",
            FontSize = 24,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 16)
        };
        buttonSection.Children.Add(title);
        
        var buttonGrid = new Grid();
        buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        // Primary Button
        var primaryButton = new ButtonPrimitive
        {
            Text = "Primary Button",
            Variant = ButtonVariant.Primary,
            Size = ButtonSize.Medium
        };
        var primaryAvaloniaButton = AvaloniaButtonRenderer.Render(primaryButton);
        Grid.SetColumn(primaryAvaloniaButton, 0);
        buttonGrid.Children.Add(primaryAvaloniaButton);
        
        // Secondary Button
        var secondaryButton = new ButtonPrimitive
        {
            Text = "Secondary Button",
            Variant = ButtonVariant.Secondary,
            Size = ButtonSize.Medium
        };
        var secondaryAvaloniaButton = AvaloniaButtonRenderer.Render(secondaryButton);
        Grid.SetColumn(secondaryAvaloniaButton, 1);
        buttonGrid.Children.Add(secondaryAvaloniaButton);
        
        // Success Button
        var successButton = new ButtonPrimitive
        {
            Text = "Success Button",
            Variant = ButtonVariant.Success,
            Size = ButtonSize.Medium
        };
        var successAvaloniaButton = AvaloniaButtonRenderer.Render(successButton);
        Grid.SetColumn(successAvaloniaButton, 2);
        buttonGrid.Children.Add(successAvaloniaButton);
        
        // Danger Button
        var dangerButton = new ButtonPrimitive
        {
            Text = "Danger Button",
            Variant = ButtonVariant.Danger,
            Size = ButtonSize.Medium
        };
        var dangerAvaloniaButton = AvaloniaButtonRenderer.Render(dangerButton);
        Grid.SetColumn(dangerAvaloniaButton, 3);
        buttonGrid.Children.Add(dangerAvaloniaButton);
        
        buttonSection.Children.Add(buttonGrid);
        container.Children.Add(buttonSection);
    }
    
    private void CreateInputExamples(StackPanel container)
    {
        var inputSection = new StackPanel
        {
            Spacing = 16,
            Margin = new Thickness(0, 0, 0, 32)
        };
        
        var title = new TextBlock
        {
            Text = "Input Primitives",
            FontSize = 24,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 16)
        };
        inputSection.Children.Add(title);
        
        // Text Input
        var textInput = new InputPrimitive
        {
            Label = "Text Input",
            Placeholder = "Enter some text...",
            HelperText = "This is a helper text",
            Size = InputSize.Medium
        };
        var textInputControl = AvaloniaInputRenderer.RenderWithLabel(textInput);
        inputSection.Children.Add(textInputControl);
        
        // Email Input
        var emailInput = new InputPrimitive
        {
            Label = "Email Address",
            Placeholder = "Enter your email...",
            Type = InputType.Email,
            IsRequired = true,
            Size = InputSize.Medium
        };
        var emailInputControl = AvaloniaInputRenderer.RenderWithLabel(emailInput);
        inputSection.Children.Add(emailInputControl);
        
        // Error Input
        var errorInput = new InputPrimitive
        {
            Label = "Input with Error",
            Placeholder = "This input has an error...",
            ErrorText = "This field is required",
            Size = InputSize.Medium
        };
        var errorInputControl = AvaloniaInputRenderer.RenderWithLabel(errorInput);
        inputSection.Children.Add(errorInputControl);
        
        container.Children.Add(inputSection);
    }
    
    private void CreateCardExamples(StackPanel container)
    {
        var cardSection = new StackPanel
        {
            Spacing = 16,
            Margin = new Thickness(0, 0, 0, 32)
        };
        
        var title = new TextBlock
        {
            Text = "Card Primitives",
            FontSize = 24,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 16)
        };
        cardSection.Children.Add(title);
        
        var cardGrid = new Grid();
        cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        
        // Default Card
        var defaultCard = new CardPrimitive
        {
            Title = "Default Card",
            Content = "This is a default card with some content. It demonstrates the basic card styling.",
            Variant = CardVariant.Default,
            Size = CardSize.Medium
        };
        var defaultCardControl = AvaloniaCardRenderer.Render(defaultCard);
        Grid.SetColumn(defaultCardControl, 0);
        cardGrid.Children.Add(defaultCardControl);
        
        // Elevated Card
        var elevatedCard = new CardPrimitive
        {
            Title = "Elevated Card",
            Content = "This card has elevation with a shadow effect. It's perfect for important content.",
            Variant = CardVariant.Elevated,
            Size = CardSize.Medium,
            IsInteractive = true
        };
        var elevatedCardControl = AvaloniaCardRenderer.Render(elevatedCard);
        Grid.SetColumn(elevatedCardControl, 1);
        cardGrid.Children.Add(elevatedCardControl);
        
        cardSection.Children.Add(cardGrid);
        container.Children.Add(cardSection);
    }
}
