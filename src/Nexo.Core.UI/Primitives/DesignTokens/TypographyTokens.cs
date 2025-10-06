namespace Nexo.Core.UI.Primitives.DesignTokens;

/// <summary>
/// Design tokens for typography that can be used across different UI frameworks
/// </summary>
public static class TypographyTokens
{
    // Font Families
    public const string FontFamilySystem = "-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Noto Sans', Helvetica, Arial, sans-serif";
    public const string FontFamilyMono = "SFMono-Regular, 'SF Mono', Consolas, 'Liberation Mono', Menlo, monospace";
    public const string FontFamilySerif = "Georgia, 'Times New Roman', Times, serif";
    
    // Font Sizes (in points)
    public const double FontSizeXs = 12;
    public const double FontSizeSm = 14;
    public const double FontSizeBase = 16;
    public const double FontSizeLg = 18;
    public const double FontSizeXl = 20;
    public const double FontSize2Xl = 24;
    public const double FontSize3Xl = 28;
    public const double FontSize4Xl = 32;
    public const double FontSize5Xl = 36;
    public const double FontSize6Xl = 48;
    
    // Font Weights
    public const string FontWeightLight = "Light";
    public const string FontWeightNormal = "Normal";
    public const string FontWeightMedium = "Medium";
    public const string FontWeightSemiBold = "SemiBold";
    public const string FontWeightBold = "Bold";
    public const string FontWeightExtraBold = "ExtraBold";
    
    // Line Heights (as multipliers)
    public const double LineHeightTight = 1.2;
    public const double LineHeightNormal = 1.4;
    public const double LineHeightRelaxed = 1.6;
    public const double LineHeightLoose = 1.8;
    
    // Letter Spacing
    public const double LetterSpacingTight = -0.025;
    public const double LetterSpacingNormal = 0;
    public const double LetterSpacingWide = 0.025;
    public const double LetterSpacingWider = 0.05;
    
    // Typography Scale
    public static class Scale
    {
        // Headings
        public const double H1 = FontSize4Xl;
        public const double H2 = FontSize3Xl;
        public const double H3 = FontSize2Xl;
        public const double H4 = FontSizeXl;
        public const double H5 = FontSizeLg;
        public const double H6 = FontSizeBase;
        
        // Body Text
        public const double BodyLarge = FontSizeLg;
        public const double Body = FontSizeBase;
        public const double BodySmall = FontSizeSm;
        public const double Caption = FontSizeXs;
        
        // UI Elements
        public const double Button = FontSizeBase;
        public const double Input = FontSizeBase;
        public const double Label = FontSizeSm;
        public const double Helper = FontSizeXs;
        
        // Code
        public const double Code = FontSizeSm;
        public const double CodeBlock = FontSizeSm;
    }
    
    // Typography Styles
    public static class Styles
    {
        public static readonly TypographyStyle Heading1 = new()
        {
            FontSize = Scale.H1,
            FontWeight = FontWeightBold,
            LineHeight = LineHeightTight,
            LetterSpacing = LetterSpacingTight
        };
        
        public static readonly TypographyStyle Heading2 = new()
        {
            FontSize = Scale.H2,
            FontWeight = FontWeightSemiBold,
            LineHeight = LineHeightTight,
            LetterSpacing = LetterSpacingTight
        };
        
        public static readonly TypographyStyle Heading3 = new()
        {
            FontSize = Scale.H3,
            FontWeight = FontWeightSemiBold,
            LineHeight = LineHeightNormal,
            LetterSpacing = LetterSpacingNormal
        };
        
        public static readonly TypographyStyle BodyLarge = new()
        {
            FontSize = Scale.BodyLarge,
            FontWeight = FontWeightNormal,
            LineHeight = LineHeightRelaxed,
            LetterSpacing = LetterSpacingNormal
        };
        
        public static readonly TypographyStyle Body = new()
        {
            FontSize = Scale.Body,
            FontWeight = FontWeightNormal,
            LineHeight = LineHeightRelaxed,
            LetterSpacing = LetterSpacingNormal
        };
        
        public static readonly TypographyStyle BodySmall = new()
        {
            FontSize = Scale.BodySmall,
            FontWeight = FontWeightNormal,
            LineHeight = LineHeightRelaxed,
            LetterSpacing = LetterSpacingNormal
        };
        
        public static readonly TypographyStyle Caption = new()
        {
            FontSize = Scale.Caption,
            FontWeight = FontWeightNormal,
            LineHeight = LineHeightNormal,
            LetterSpacing = LetterSpacingNormal
        };
        
        public static readonly TypographyStyle Button = new()
        {
            FontSize = Scale.Button,
            FontWeight = FontWeightMedium,
            LineHeight = LineHeightNormal,
            LetterSpacing = LetterSpacingNormal
        };
        
        public static readonly TypographyStyle Code = new()
        {
            FontSize = Scale.Code,
            FontWeight = FontWeightNormal,
            LineHeight = LineHeightNormal,
            LetterSpacing = LetterSpacingNormal
        };
    }
}

/// <summary>
/// Represents a typography style configuration
/// </summary>
public class TypographyStyle
{
    public double FontSize { get; set; }
    public string FontWeight { get; set; } = TypographyTokens.FontWeightNormal;
    public double LineHeight { get; set; } = TypographyTokens.LineHeightNormal;
    public double LetterSpacing { get; set; } = TypographyTokens.LetterSpacingNormal;
    public string FontFamily { get; set; } = TypographyTokens.FontFamilySystem;
    
    public TypographyStyle()
    {
    }
    
    public TypographyStyle(double fontSize, string fontWeight, double lineHeight, double letterSpacing, string fontFamily)
    {
        FontSize = fontSize;
        FontWeight = fontWeight;
        LineHeight = lineHeight;
        LetterSpacing = letterSpacing;
        FontFamily = fontFamily;
    }
}
