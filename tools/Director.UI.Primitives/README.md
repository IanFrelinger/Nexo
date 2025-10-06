# Director UI Primitives

A framework-agnostic design system for creating consistent, accessible, and beautiful user interfaces across different UI frameworks.

## Overview

Director UI Primitives provides a comprehensive set of design tokens and component primitives that can be rendered across different UI frameworks (Avalonia, WPF, MAUI, etc.) while maintaining visual and behavioral consistency.

## Features

- **Design Tokens**: Centralized color, typography, and spacing tokens
- **Component Primitives**: Framework-agnostic component definitions
- **Framework Renderers**: Specific implementations for different UI frameworks
- **Accessibility**: Built-in accessibility features and ARIA support
- **Responsive**: Adaptive sizing and spacing
- **Theming**: Support for light/dark themes and custom color schemes

## Architecture

```
Director.UI.Primitives/
├── DesignTokens/          # Color, typography, spacing tokens
├── Components/            # Framework-agnostic component primitives
├── Frameworks/           # Framework-specific renderers
│   ├── Avalonia/         # Avalonia UI renderers
│   ├── WPF/              # WPF renderers (future)
│   └── MAUI/             # .NET MAUI renderers (future)
└── Examples/             # Usage examples
```

## Design Tokens

### Color Tokens

```csharp
// Primary colors
ColorTokens.PrimaryBlue
ColorTokens.PrimaryBlueDark
ColorTokens.PrimaryBlueLight

// Semantic colors
ColorTokens.SuccessGreen
ColorTokens.ErrorRed
ColorTokens.WarningOrange
ColorTokens.InfoBlue

// Neutral colors
ColorTokens.TextPrimary
ColorTokens.TextSecondary
ColorTokens.BackgroundPrimary
ColorTokens.BorderDefault
```

### Typography Tokens

```csharp
// Font families
TypographyTokens.FontFamilySystem
TypographyTokens.FontFamilyMono
TypographyTokens.FontFamilySerif

// Font sizes
TypographyTokens.Scale.H1        // 32pt
TypographyTokens.Scale.H2        // 28pt
TypographyTokens.Scale.H3        // 24pt
TypographyTokens.Scale.Body     // 16pt
TypographyTokens.Scale.Caption  // 12pt

// Typography styles
TypographyTokens.Styles.Heading1
TypographyTokens.Styles.Body
TypographyTokens.Styles.Button
```

### Spacing Tokens

```csharp
// Base spacing
SpacingTokens.Xs    // 4pt
SpacingTokens.Sm    // 8pt
SpacingTokens.Md    // 12pt
SpacingTokens.Lg    // 16pt
SpacingTokens.Xl    // 24pt

// Component spacing
SpacingTokens.Component.PaddingMd
SpacingTokens.Component.MarginLg
SpacingTokens.Layout.CardPadding
```

## Component Primitives

### Button Primitive

```csharp
var button = new ButtonPrimitive
{
    Text = "Click Me",
    Variant = ButtonVariant.Primary,
    Size = ButtonSize.Medium,
    IsEnabled = true,
    Tooltip = "This is a button"
};

// Apply variant colors and sizing
button.ApplyVariantColors();
button.ApplySize();
```

### Input Primitive

```csharp
var input = new InputPrimitive
{
    Label = "Email Address",
    Placeholder = "Enter your email...",
    Type = InputType.Email,
    IsRequired = true,
    Size = InputSize.Medium,
    HelperText = "We'll never share your email"
};

// Apply sizing
input.ApplySize();
```

### Card Primitive

```csharp
var card = new CardPrimitive
{
    Title = "Card Title",
    Content = "Card content goes here...",
    Variant = CardVariant.Elevated,
    Size = CardSize.Medium,
    IsInteractive = true
};

// Apply variant and sizing
card.ApplyVariant();
card.ApplySize();
```

## Framework-Specific Usage

### Avalonia

```csharp
using Director.UI.Primitives.Frameworks.Avalonia;

// Create primitive
var buttonPrimitive = new ButtonPrimitive
{
    Text = "Avalonia Button",
    Variant = ButtonVariant.Primary
};

// Render as Avalonia Button
var avaloniaButton = AvaloniaButtonRenderer.Render(buttonPrimitive);

// Add to your UI
myPanel.Children.Add(avaloniaButton);
```

### Input with Label (Avalonia)

```csharp
var inputPrimitive = new InputPrimitive
{
    Label = "Username",
    Placeholder = "Enter username...",
    IsRequired = true
};

var inputControl = AvaloniaInputRenderer.RenderWithLabel(inputPrimitive);
myPanel.Children.Add(inputControl);
```

## Accessibility Features

All primitives include built-in accessibility features:

- **ARIA Labels**: Proper labeling for screen readers
- **Keyboard Navigation**: Full keyboard support
- **Focus Management**: Clear focus indicators
- **Color Contrast**: WCAG 2.1 AA compliant colors
- **Touch Targets**: Minimum 44pt touch targets
- **Semantic HTML**: Proper semantic structure

## Theming

### Custom Color Schemes

```csharp
// Override color tokens for custom themes
public static class CustomColorTokens
{
    public static readonly Color PrimaryBlue = Color.FromArgb(0, 120, 215);
    public static readonly Color SuccessGreen = Color.FromArgb(40, 167, 69);
    // ... other custom colors
}
```

### Dark Theme Support

```csharp
// Dark theme color overrides
public static class DarkThemeTokens
{
    public static readonly Color BackgroundPrimary = Color.FromArgb(26, 26, 26);
    public static readonly Color TextPrimary = Color.FromArgb(255, 255, 255);
    // ... other dark theme colors
}
```

## Best Practices

### 1. Use Design Tokens

Always use design tokens instead of hardcoded values:

```csharp
// ✅ Good
button.FontSize = TypographyTokens.Scale.Button;
button.Padding = new Thickness(SpacingTokens.Lg, SpacingTokens.Md);

// ❌ Bad
button.FontSize = 16;
button.Padding = new Thickness(16, 12);
```

### 2. Apply Variants and Sizes

Always apply variants and sizes to primitives:

```csharp
var button = new ButtonPrimitive { /* ... */ };
button.ApplyVariantColors(); // Apply color scheme
button.ApplySize();          // Apply sizing
```

### 3. Use Framework Renderers

Use the appropriate framework renderer for your UI framework:

```csharp
// For Avalonia
var avaloniaButton = AvaloniaButtonRenderer.Render(buttonPrimitive);

// For WPF (future)
var wpfButton = WpfButtonRenderer.Render(buttonPrimitive);
```

### 4. Accessibility First

Always provide accessible labels and descriptions:

```csharp
var input = new InputPrimitive
{
    Label = "Email Address",
    AccessibleLabel = "Email address input field",
    AccessibleDescription = "Enter your email address for account verification"
};
```

## Extending the System

### Adding New Primitives

1. Create a new primitive class in `Components/`
2. Define the primitive's properties and methods
3. Create framework-specific renderers in `Frameworks/`
4. Add examples in `Examples/`

### Adding New Frameworks

1. Create a new framework folder in `Frameworks/`
2. Implement renderers for each primitive
3. Add framework-specific project file
4. Update examples and documentation

## Contributing

1. Follow the existing code structure
2. Add comprehensive documentation
3. Include examples for new features
4. Ensure accessibility compliance
5. Test across supported frameworks

## License

This project is part of the Director Studio suite and follows the same licensing terms.
