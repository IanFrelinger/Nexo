using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;

namespace Nexo.UI.Demo.Avalonia;

/// <summary>
/// Entry point for the Avalonia UI demo application.
/// 
/// This program demonstrates the framework-agnostic design system
/// primitives rendered using Avalonia UI framework.
/// </summary>
class Program
{
    /// <summary>
    /// Main entry point for the Avalonia demo application.
    /// 
    /// Initialization code. Don't use any Avalonia, third-party APIs or any
    /// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    /// yet and stuff might break.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Builds and configures the Avalonia application.
    /// 
    /// Avalonia configuration, don't remove; also used by visual designer.
    /// </summary>
    /// <returns>Configured AppBuilder instance.</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
