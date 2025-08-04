namespace Nexo.Feature.Platform.Enums
{
    /// <summary>
    /// Enumeration for desktop optimization levels.
    /// </summary>
    public enum DesktopOptimizationLevel
    {
        /// <summary>
        /// No optimization applied.
        /// </summary>
        None,
        
        /// <summary>
        /// Minimal optimization for development.
        /// </summary>
        Minimal,
        
        /// <summary>
        /// Balanced optimization for general use.
        /// </summary>
        Balanced,
        
        /// <summary>
        /// Aggressive optimization for performance.
        /// </summary>
        Aggressive,
        
        /// <summary>
        /// Maximum optimization for critical performance.
        /// </summary>
        Maximum
    }

    /// <summary>
    /// Enumeration for desktop platform types.
    /// </summary>
    public enum DesktopPlatformType
    {
        /// <summary>
        /// Windows desktop platform.
        /// </summary>
        Windows,
        
        /// <summary>
        /// macOS desktop platform.
        /// </summary>
        macOS,
        
        /// <summary>
        /// Linux desktop platform.
        /// </summary>
        Linux,
        
        /// <summary>
        /// Cross-platform desktop.
        /// </summary>
        CrossPlatform
    }

    /// <summary>
    /// Enumeration for desktop application types.
    /// </summary>
    public enum DesktopApplicationType
    {
        /// <summary>
        /// Console application.
        /// </summary>
        Console,
        
        /// <summary>
        /// Windows Forms application.
        /// </summary>
        WinForms,
        
        /// <summary>
        /// Windows Presentation Foundation application.
        /// </summary>
        WPF,
        
        /// <summary>
        /// Windows UI application.
        /// </summary>
        WinUI,
        
        /// <summary>
        /// Avalonia cross-platform UI application.
        /// </summary>
        Avalonia,
        
        /// <summary>
        /// MAUI cross-platform application.
        /// </summary>
        MAUI,
        
        /// <summary>
        /// Windows Service application.
        /// </summary>
        WindowsService,
        
        /// <summary>
        /// Background service application.
        /// </summary>
        BackgroundService,
        
        /// <summary>
        /// Library/Class library.
        /// </summary>
        Library
    }

    /// <summary>
    /// Enumeration for desktop UI frameworks.
    /// </summary>
    public enum DesktopUIFramework
    {
        /// <summary>
        /// Windows Forms.
        /// </summary>
        WinForms,
        
        /// <summary>
        /// Windows Presentation Foundation.
        /// </summary>
        WPF,
        
        /// <summary>
        /// Windows UI.
        /// </summary>
        WinUI,
        
        /// <summary>
        /// Avalonia UI.
        /// </summary>
        Avalonia,
        
        /// <summary>
        /// .NET MAUI.
        /// </summary>
        MAUI,
        
        /// <summary>
        /// GTK#.
        /// </summary>
        GTK,
        
        /// <summary>
        /// Qt for .NET.
        /// </summary>
        Qt,
        
        /// <summary>
        /// No UI framework (console/service).
        /// </summary>
        None
    }

    /// <summary>
    /// Enumeration for desktop deployment types.
    /// </summary>
    public enum DesktopDeploymentType
    {
        /// <summary>
        /// Windows MSI installer.
        /// </summary>
        MSI,
        
        /// <summary>
        /// Windows MSIX package.
        /// </summary>
        MSIX,
        
        /// <summary>
        /// Windows executable.
        /// </summary>
        EXE,
        
        /// <summary>
        /// macOS DMG package.
        /// </summary>
        DMG,
        
        /// <summary>
        /// macOS PKG installer.
        /// </summary>
        PKG,
        
        /// <summary>
        /// Linux AppImage.
        /// </summary>
        AppImage,
        
        /// <summary>
        /// Linux DEB package.
        /// </summary>
        DEB,
        
        /// <summary>
        /// Linux RPM package.
        /// </summary>
        RPM,
        
        /// <summary>
        /// Portable executable.
        /// </summary>
        Portable
    }

    /// <summary>
    /// Enumeration for desktop system integration features.
    /// </summary>
    public enum DesktopSystemIntegration
    {
        /// <summary>
        /// File system integration.
        /// </summary>
        FileSystem,
        
        /// <summary>
        /// Registry integration (Windows).
        /// </summary>
        Registry,
        
        /// <summary>
        /// System tray integration.
        /// </summary>
        SystemTray,
        
        /// <summary>
        /// Start menu integration.
        /// </summary>
        StartMenu,
        
        /// <summary>
        /// Desktop shortcut integration.
        /// </summary>
        DesktopShortcut,
        
        /// <summary>
        /// Auto-start integration.
        /// </summary>
        AutoStart,
        
        /// <summary>
        /// Notification integration.
        /// </summary>
        Notifications,
        
        /// <summary>
        /// Print integration.
        /// </summary>
        Printing,
        
        /// <summary>
        /// Network integration.
        /// </summary>
        Network,
        
        /// <summary>
        /// Database integration.
        /// </summary>
        Database
    }
} 