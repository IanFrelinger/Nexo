using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Director.Avalonia.Services;

/// <summary>
/// Service for discovering Unity installations and projects
/// </summary>
public class UnityDiscoveryService
{
    private readonly ILogger<UnityDiscoveryService> _logger;
    private readonly List<UnityInstallation> _installations = new();
    private readonly List<UnityProject> _projects = new();

    public UnityDiscoveryService(ILogger<UnityDiscoveryService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Discovers all Unity installations on the system
    /// </summary>
    public async Task<List<UnityInstallation>> DiscoverUnityInstallationsAsync()
    {
        _installations.Clear();

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await DiscoverWindowsUnityInstallationsAsync();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                await DiscoverMacOSUnityInstallationsAsync();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                await DiscoverLinuxUnityInstallationsAsync();
            }

            _logger.LogInformation($"Discovered {_installations.Count} Unity installations");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover Unity installations");
        }

        return _installations.ToList();
    }

    /// <summary>
    /// Discovers Unity projects in common locations
    /// </summary>
    public async Task<List<UnityProject>> DiscoverUnityProjectsAsync()
    {
        _projects.Clear();

        try
        {
            var searchPaths = GetCommonProjectSearchPaths();
            
            foreach (var path in searchPaths)
            {
                if (Directory.Exists(path))
                {
                    await SearchForProjectsInDirectoryAsync(path);
                }
            }

            _logger.LogInformation($"Discovered {_projects.Count} Unity projects");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover Unity projects");
        }

        return _projects.ToList();
    }

    /// <summary>
    /// Finds the best Unity installation for a given project
    /// </summary>
    public UnityInstallation? FindBestUnityInstallation(UnityProject project)
    {
        if (!_installations.Any())
        {
            return null;
        }

        // Try to find exact version match first
        var exactMatch = _installations.FirstOrDefault(i => i.Version == project.UnityVersion);
        if (exactMatch != null)
        {
            return exactMatch;
        }

        // Find compatible version
        var compatible = _installations
            .Where(i => IsCompatibleVersion(i.Version, project.UnityVersion))
            .OrderByDescending(i => i.Version)
            .FirstOrDefault();

        return compatible ?? _installations.OrderByDescending(i => i.Version).First();
    }

    /// <summary>
    /// Gets the recommended Unity installation
    /// </summary>
    public UnityInstallation? GetRecommendedInstallation()
    {
        if (!_installations.Any())
        {
            return null;
        }

        // Prefer LTS versions, then latest
        var ltsVersions = _installations.Where(i => i.IsLTS).ToList();
        if (ltsVersions.Any())
        {
            return ltsVersions.OrderByDescending(i => i.Version).First();
        }

        return _installations.OrderByDescending(i => i.Version).First();
    }

    private async Task DiscoverWindowsUnityInstallationsAsync()
    {
        var commonPaths = new[]
        {
            @"C:\Program Files\Unity\Hub\Editor",
            @"C:\Program Files (x86)\Unity\Hub\Editor",
            @"C:\Program Files\Unity\Editor",
            @"C:\Program Files (x86)\Unity\Editor"
        };

        foreach (var path in commonPaths)
        {
            if (Directory.Exists(path))
            {
                await SearchForUnityInDirectoryAsync(path);
            }
        }

        // Also check registry for Unity installations
        await CheckWindowsRegistryAsync();
    }

    private async Task DiscoverMacOSUnityInstallationsAsync()
    {
        var commonPaths = new[]
        {
            "/Applications/Unity/Hub/Editor",
            "/Applications/Unity/Editor",
            "/Applications/Unity"
        };

        foreach (var path in commonPaths)
        {
            if (Directory.Exists(path))
            {
                await SearchForUnityInDirectoryAsync(path);
            }
        }

        // Check for Unity Hub installations
        var hubPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
            "Library", "Application Support", "Unity Hub", "editors");
        if (Directory.Exists(hubPath))
        {
            await SearchForUnityInDirectoryAsync(hubPath);
        }
    }

    private async Task DiscoverLinuxUnityInstallationsAsync()
    {
        var commonPaths = new[]
        {
            "/opt/Unity/Editor",
            "/usr/local/Unity/Editor",
            "/home/" + Environment.UserName + "/Unity/Editor",
            "/snap/unity/current/Editor"
        };

        foreach (var path in commonPaths)
        {
            if (Directory.Exists(path))
            {
                await SearchForUnityInDirectoryAsync(path);
            }
        }

        // Check for Unity Hub installations
        var hubPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
            ".config", "Unity Hub", "editors");
        if (Directory.Exists(hubPath))
        {
            await SearchForUnityInDirectoryAsync(hubPath);
        }
    }

    private async Task SearchForUnityInDirectoryAsync(string directory)
    {
        try
        {
            var subdirs = Directory.GetDirectories(directory);
            
            foreach (var subdir in subdirs)
            {
                var unityExecutable = GetUnityExecutablePath(subdir);
                if (File.Exists(unityExecutable))
                {
                    var version = await GetUnityVersionAsync(unityExecutable);
                    if (!string.IsNullOrEmpty(version))
                    {
                        var installation = new UnityInstallation
                        {
                            Path = subdir,
                            ExecutablePath = unityExecutable,
                            Version = version,
                            IsLTS = IsLTSVersion(version),
                            LastModified = Directory.GetLastWriteTime(subdir)
                        };

                        if (!_installations.Any(i => i.Path == installation.Path))
                        {
                            _installations.Add(installation);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to search for Unity in directory: {directory}");
        }
    }

    private async Task CheckWindowsRegistryAsync()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        try
        {
            // Check Unity Hub registry entries
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Unity Technologies\Unity Hub 2.0");
            if (key != null)
            {
                var subKeys = key.GetSubKeyNames();
                foreach (var subKeyName in subKeys)
                {
                    var subKey = key.OpenSubKey(subKeyName);
                    if (subKey?.GetValue("Location") is string location && Directory.Exists(location))
                    {
                        var unityExecutable = GetUnityExecutablePath(location);
                        if (File.Exists(unityExecutable))
                        {
                            var version = await GetUnityVersionAsync(unityExecutable);
                            if (!string.IsNullOrEmpty(version))
                            {
                                var installation = new UnityInstallation
                                {
                                    Path = location,
                                    ExecutablePath = unityExecutable,
                                    Version = version,
                                    IsLTS = IsLTSVersion(version),
                                    LastModified = Directory.GetLastWriteTime(location)
                                };

                                if (!_installations.Any(i => i.Path == installation.Path))
                                {
                                    _installations.Add(installation);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check Windows registry for Unity installations");
        }
    }

    private async Task SearchForProjectsInDirectoryAsync(string directory)
    {
        try
        {
            var subdirs = Directory.GetDirectories(directory);
            
            foreach (var subdir in subdirs)
            {
                var projectFile = Path.Combine(subdir, "ProjectSettings", "ProjectVersion.txt");
                if (File.Exists(projectFile))
                {
                    var version = await File.ReadAllTextAsync(projectFile);
                    var unityVersion = ExtractUnityVersion(version);
                    
                    if (!string.IsNullOrEmpty(unityVersion))
                    {
                        var project = new UnityProject
                        {
                            Path = subdir,
                            Name = Path.GetFileName(subdir),
                            UnityVersion = unityVersion,
                            LastModified = Directory.GetLastWriteTime(subdir)
                        };

                        if (!_projects.Any(p => p.Path == project.Path))
                        {
                            _projects.Add(project);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to search for projects in directory: {directory}");
        }
    }

    private string GetUnityExecutablePath(string unityPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(unityPath, "Unity.exe");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Path.Combine(unityPath, "Unity.app", "Contents", "MacOS", "Unity");
        }
        else
        {
            return Path.Combine(unityPath, "Unity");
        }
    }

    private async Task<string> GetUnityVersionAsync(string unityExecutable)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = unityExecutable,
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return ExtractUnityVersion(output);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to get Unity version from: {unityExecutable}");
            return string.Empty;
        }
    }

    private string ExtractUnityVersion(string versionText)
    {
        // Extract version from Unity output (e.g., "Unity 2022.3.15f1")
        var match = System.Text.RegularExpressions.Regex.Match(versionText, @"Unity\s+(\d+\.\d+\.\d+[a-z]\d+)");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private bool IsLTSVersion(string version)
    {
        // Check if version is LTS (Long Term Support)
        // LTS versions typically end with LTS or are specific versions
        return version.Contains("LTS") || 
               version.StartsWith("2022.3") || 
               version.StartsWith("2021.3") || 
               version.StartsWith("2020.3");
    }

    private bool IsCompatibleVersion(string installationVersion, string projectVersion)
    {
        // Check if Unity installation version is compatible with project version
        // This is a simplified check - in reality, compatibility is more complex
        var installMajor = GetMajorVersion(installationVersion);
        var projectMajor = GetMajorVersion(projectVersion);
        
        return installMajor == projectMajor;
    }

    private int GetMajorVersion(string version)
    {
        if (int.TryParse(version.Split('.')[0], out var major))
        {
            return major;
        }
        return 0;
    }

    private List<string> GetCommonProjectSearchPaths()
    {
        var paths = new List<string>();

        // User's Documents folder
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        paths.Add(Path.Combine(documentsPath, "Unity Projects"));
        paths.Add(Path.Combine(documentsPath, "Unity"));

        // Desktop
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        paths.Add(Path.Combine(desktopPath, "Unity Projects"));

        // Common project locations
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            paths.Add(@"C:\Users\" + Environment.UserName + @"\Documents\Unity Projects");
            paths.Add(@"C:\Users\" + Environment.UserName + @"\Desktop\Unity Projects");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            paths.Add(Path.Combine(homePath, "Documents", "Unity Projects"));
            paths.Add(Path.Combine(homePath, "Desktop", "Unity Projects"));
        }
        else
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            paths.Add(Path.Combine(homePath, "Documents", "Unity Projects"));
            paths.Add(Path.Combine(homePath, "Desktop", "Unity Projects"));
        }

        return paths;
    }
}

public class UnityInstallation
{
    public string Path { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsLTS { get; set; }
    public DateTime LastModified { get; set; }
}

public class UnityProject
{
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UnityVersion { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
}
