using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Microsoft.Extensions.Logging;

namespace Director.Avalonia.Services;

/// <summary>
/// Service for detecting and applying system settings
/// </summary>
public class SystemSettingsService
{
    private readonly ILogger<SystemSettingsService> _logger;

    public SystemSettingsService(ILogger<SystemSettingsService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the system theme (Light/Dark)
    /// </summary>
    public SystemTheme GetSystemTheme()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsTheme();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacOSTheme();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxTheme();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect system theme, defaulting to Light");
        }

        return SystemTheme.Light;
    }

    /// <summary>
    /// Gets the system accent color
    /// </summary>
    public Color GetSystemAccentColor()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsAccentColor();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacOSAccentColor();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect system accent color, using default");
        }

        return Color.FromRgb(0, 120, 215); // Default blue
    }

    /// <summary>
    /// Gets the system font family
    /// </summary>
    public FontFamily GetSystemFontFamily()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new FontFamily("Segoe UI");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new FontFamily("SF Pro Display");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new FontFamily("Ubuntu");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect system font, using default");
        }

        return FontFamily.Default;
    }

    /// <summary>
    /// Gets the system font size
    /// </summary>
    public double GetSystemFontSize()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsFontSize();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacOSFontSize();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxFontSize();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect system font size, using default");
        }

        return 14.0;
    }

    /// <summary>
    /// Gets the system scaling factor
    /// </summary>
    public double GetSystemScaling()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsScaling();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacOSScaling();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxScaling();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect system scaling, using default");
        }

        return 1.0;
    }

    private SystemTheme GetWindowsTheme()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return SystemTheme.Light;

        try
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int value)
            {
                return value == 0 ? SystemTheme.Dark : SystemTheme.Light;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read Windows theme from registry");
        }

        return SystemTheme.Light;
    }

    private SystemTheme GetMacOSTheme()
    {
        try
        {
            var result = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "defaults",
                Arguments = "read -g AppleInterfaceStyle",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            
            if (result?.WaitForExit(1000) == true && result.ExitCode == 0)
            {
                var output = result.StandardOutput.ReadToEnd().Trim();
                return output == "Dark" ? SystemTheme.Dark : SystemTheme.Light;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read macOS theme from defaults");
        }

        return SystemTheme.Light;
    }

    private SystemTheme GetLinuxTheme()
    {
        try
        {
            var gtkTheme = Environment.GetEnvironmentVariable("GTK_THEME") ?? "";
            var colorScheme = Environment.GetEnvironmentVariable("COLORSCHEME") ?? "";
            
            if (gtkTheme.Contains("dark", StringComparison.OrdinalIgnoreCase) ||
                colorScheme.Contains("dark", StringComparison.OrdinalIgnoreCase))
            {
                return SystemTheme.Dark;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read Linux theme from environment");
        }

        return SystemTheme.Light;
    }

    private Color GetWindowsAccentColor()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Color.FromRgb(0, 120, 215);

        try
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
            if (key?.GetValue("AccentColor") is int value)
            {
                // Convert DWORD to RGB
                var r = (byte)(value & 0xFF);
                var g = (byte)((value >> 8) & 0xFF);
                var b = (byte)((value >> 16) & 0xFF);
                return Color.FromRgb(r, g, b);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read Windows accent color from registry");
        }

        return Color.FromRgb(0, 120, 215);
    }

    private Color GetMacOSAccentColor()
    {
        try
        {
            var result = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "defaults",
                Arguments = "read -g AppleAccentColor",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            
            if (result?.WaitForExit(1000) == true && result.ExitCode == 0)
            {
                var output = result.StandardOutput.ReadToEnd().Trim();
                if (int.TryParse(output, out var colorIndex))
                {
                    // Map macOS accent color index to actual colors
                    return colorIndex switch
                    {
                        0 => Color.FromRgb(0, 122, 255),     // Blue
                        1 => Color.FromRgb(255, 149, 0),     // Orange
                        2 => Color.FromRgb(255, 45, 85),     // Pink
                        3 => Color.FromRgb(255, 204, 0),     // Yellow
                        4 => Color.FromRgb(52, 199, 89),     // Green
                        5 => Color.FromRgb(255, 59, 48),     // Red
                        6 => Color.FromRgb(88, 86, 214),     // Purple
                        _ => Color.FromRgb(0, 122, 255)      // Default Blue
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read macOS accent color from defaults");
        }

        return Color.FromRgb(0, 122, 255);
    }

    private double GetWindowsFontSize()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return 14.0;

        try
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop\WindowMetrics");
            if (key?.GetValue("AppliedDPI") is int dpi)
            {
                return dpi switch
                {
                    96 => 14.0,   // 100% scaling
                    120 => 16.8,  // 125% scaling
                    144 => 20.16, // 150% scaling
                    192 => 26.88, // 200% scaling
                    _ => 14.0 + (dpi - 96) * 0.14 // Linear scaling
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read Windows font size from registry");
        }

        return 14.0;
    }

    private double GetMacOSFontSize()
    {
        try
        {
            var result = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "defaults",
                Arguments = "read -g AppleFontSize",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            
            if (result?.WaitForExit(1000) == true && result.ExitCode == 0)
            {
                var output = result.StandardOutput.ReadToEnd().Trim();
                if (double.TryParse(output, out var fontSize))
                {
                    return fontSize;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read macOS font size from defaults");
        }

        return 14.0;
    }

    private double GetLinuxFontSize()
    {
        try
        {
            var fontSize = Environment.GetEnvironmentVariable("GTK_FONT_SIZE");
            if (!string.IsNullOrEmpty(fontSize) && double.TryParse(fontSize, out var size))
            {
                return size;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read Linux font size from environment");
        }

        return 14.0;
    }

    private double GetWindowsScaling()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return 1.0;

        try
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
            if (key?.GetValue("LogPixels") is int dpi)
            {
                return dpi / 96.0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read Windows scaling from registry");
        }

        return 1.0;
    }

    private double GetMacOSScaling()
    {
        try
        {
            var result = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "system_profiler",
                Arguments = "SPDisplaysDataType -json",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            
            if (result?.WaitForExit(2000) == true && result.ExitCode == 0)
            {
                var output = result.StandardOutput.ReadToEnd();
                // Parse JSON to find scaling factor
                // This is a simplified implementation
                if (output.Contains("\"scale\": 2"))
                {
                    return 2.0;
                }
                else if (output.Contains("\"scale\": 1.5"))
                {
                    return 1.5;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read macOS scaling from system profiler");
        }

        return 1.0;
    }

    private double GetLinuxScaling()
    {
        try
        {
            var scaling = Environment.GetEnvironmentVariable("GDK_SCALE");
            if (!string.IsNullOrEmpty(scaling) && double.TryParse(scaling, out var scale))
            {
                return scale;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read Linux scaling from environment");
        }

        return 1.0;
    }
}

public enum SystemTheme
{
    Light,
    Dark
}
