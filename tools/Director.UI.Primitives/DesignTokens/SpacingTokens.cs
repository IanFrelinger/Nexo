namespace Director.UI.Primitives.DesignTokens;

/// <summary>
/// Design tokens for spacing that can be used across different UI frameworks
/// </summary>
public static class SpacingTokens
{
    // Base spacing unit (4pt grid system)
    public const double BaseUnit = 4.0;
    
  // Spacing Scale
  public const double None = 0;
  public const double Xs = BaseUnit * 1;    // 4pt
  public const double Sm = BaseUnit * 2;    // 8pt
  public const double Md = BaseUnit * 3;    // 12pt
  public const double Lg = BaseUnit * 4;    // 16pt
  public const double Xl = BaseUnit * 6;    // 24pt
  public const double Xxl = BaseUnit * 8;   // 32pt
  public const double Xxxl = BaseUnit * 12; // 48pt
  public const double Xxxxl = BaseUnit * 16; // 64pt
    
    // Component Spacing
    public static class Component
    {
        // Padding
        public const double PaddingXs = Xs;
        public const double PaddingSm = Sm;
        public const double PaddingMd = Md;
        public const double PaddingLg = Lg;
        public const double PaddingXl = Xl;
        
        // Margins
        public const double MarginXs = Xs;
        public const double MarginSm = Sm;
        public const double MarginMd = Md;
        public const double MarginLg = Lg;
        public const double MarginXl = Xl;
        
        // Gaps
        public const double GapXs = Xs;
        public const double GapSm = Sm;
        public const double GapMd = Md;
        public const double GapLg = Lg;
        public const double GapXl = Xl;
    }
    
    // Layout Spacing
    public static class Layout
    {
        // Container Padding
        public const double ContainerPadding = Xl;
        public const double SectionPadding = Lg;
        public const double CardPadding = Lg;
        
        // Element Spacing
        public const double ElementSpacing = Md;
        public const double GroupSpacing = Lg;
        public const double SectionSpacing = Xl;
        
        // Grid Spacing
        public const double GridGap = Md;
        public const double GridRowGap = Lg;
        public const double GridColumnGap = Lg;
    }
    
    // Interactive Spacing
    public static class Interactive
    {
        // Touch Targets
        public const double TouchTargetMin = 44;
        public const double TouchTargetComfortable = 48;
        public const double TouchTargetLarge = 56;
        
        // Button Spacing
        public const double ButtonPaddingHorizontal = Lg;
        public const double ButtonPaddingVertical = Md;
        public const double ButtonGap = Sm;
        
        // Input Spacing
        public const double InputPaddingHorizontal = Md;
        public const double InputPaddingVertical = Sm;
        public const double InputGap = Sm;
        
        // List Spacing
        public const double ListItemSpacing = Sm;
        public const double ListItemPadding = Md;
    }
    
    // Border Radius
    public static class BorderRadius
    {
        public const double None = 0;
        public const double Xs = 2;
        public const double Sm = 4;
        public const double Md = 6;
        public const double Lg = 8;
        public const double Xl = 12;
        public const double Xxl = 16;
        public const double Xxxl = 24;
        public const double Full = 9999;
    }
    
    // Shadows
    public static class Shadow
    {
        public const double OffsetX = 0;
        public const double OffsetY = 2;
        public const double BlurRadius = 8;
        public const double SpreadRadius = 0;
    }
    
    // Z-Index
    public static class ZIndex
    {
        public const int Base = 0;
        public const int Dropdown = 1000;
        public const int Sticky = 1020;
        public const int Fixed = 1030;
        public const int ModalBackdrop = 1040;
        public const int Modal = 1050;
        public const int Popover = 1060;
        public const int Tooltip = 1070;
    }
}
