using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Core.Domain.Services.Export
{
    public class PackageExporter
    {
        private readonly ILogger _logger;

        public PackageExporter(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ExportResult> ExportPackageAsync(
            string outputPath,
            PackageExportOptions options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Exporting package to {OutputPath}", outputPath);

                var exportPath = Path.Combine(outputPath, "nexo-package");
                Directory.CreateDirectory(exportPath);

                // Create package manifest
                await CreatePackageManifestAsync(exportPath, options, cancellationToken);

                // Create package files
                await CreatePackageFilesAsync(exportPath, options, cancellationToken);

                // Create installation scripts
                await CreateInstallationScriptsAsync(exportPath, options, cancellationToken);

                // Create uninstallation scripts
                await CreateUninstallationScriptsAsync(exportPath, options, cancellationToken);

                // Create package configuration
                await CreatePackageConfigurationAsync(exportPath, options, cancellationToken);

                var result = new ExportResult
                {
                    Success = true,
                    ExportPath = exportPath,
                    ArtifactType = "Package",
                    Files = Directory.GetFiles(exportPath, "*", SearchOption.AllDirectories).ToList()
                };

                _logger.LogInformation("Package export completed successfully. {FileCount} files created.", result.Files.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export package");
                return new ExportResult
                {
                    Success = false,
                    Message = $"Package export failed: {ex.Message}",
                    Files = new List<string>()
                };
            }
        }

        private async Task CreatePackageManifestAsync(string exportPath, PackageExportOptions options, CancellationToken cancellationToken)
        {
            var manifestContent = GeneratePackageManifest(options);
            var manifestPath = Path.Combine(exportPath, "package.json");
            await File.WriteAllTextAsync(manifestPath, manifestContent, cancellationToken);
        }

        private async Task CreatePackageFilesAsync(string exportPath, PackageExportOptions options, CancellationToken cancellationToken)
        {
            var filesPath = Path.Combine(exportPath, "files");
            Directory.CreateDirectory(filesPath);

            // Create core files
            var coreFiles = GetCorePackageFiles(options);
            foreach (var file in coreFiles)
            {
                var filePath = Path.Combine(filesPath, file.Name);
                await File.WriteAllTextAsync(filePath, file.Content, cancellationToken);
            }
        }

        private async Task CreateInstallationScriptsAsync(string exportPath, PackageExportOptions options, CancellationToken cancellationToken)
        {
            var scriptsPath = Path.Combine(exportPath, "scripts");
            Directory.CreateDirectory(scriptsPath);

            // Create installation script
            var installContent = GenerateInstallationScript(options);
            var installPath = Path.Combine(scriptsPath, "install.sh");
            await File.WriteAllTextAsync(installPath, installContent, cancellationToken);

            // Create Windows installation script
            var installBatContent = GenerateWindowsInstallationScript(options);
            var installBatPath = Path.Combine(scriptsPath, "install.bat");
            await File.WriteAllTextAsync(installBatPath, installBatContent, cancellationToken);
        }

        private async Task CreateUninstallationScriptsAsync(string exportPath, PackageExportOptions options, CancellationToken cancellationToken)
        {
            var scriptsPath = Path.Combine(exportPath, "scripts");

            // Create uninstallation script
            var uninstallContent = GenerateUninstallationScript(options);
            var uninstallPath = Path.Combine(scriptsPath, "uninstall.sh");
            await File.WriteAllTextAsync(uninstallPath, uninstallContent, cancellationToken);

            // Create Windows uninstallation script
            var uninstallBatContent = GenerateWindowsUninstallationScript(options);
            var uninstallBatPath = Path.Combine(scriptsPath, "uninstall.bat");
            await File.WriteAllTextAsync(uninstallBatPath, uninstallBatContent, cancellationToken);
        }

        private async Task CreatePackageConfigurationAsync(string exportPath, PackageExportOptions options, CancellationToken cancellationToken)
        {
            var configContent = GeneratePackageConfiguration(options);
            var configPath = Path.Combine(exportPath, "package.config.json");
            await File.WriteAllTextAsync(configPath, configContent, cancellationToken);
        }

        private string GeneratePackageManifest(PackageExportOptions options)
        {
            return $@"{{
  ""name"": ""nexo-package"",
  ""version"": ""{options.Version}"",
  ""description"": ""Nexo platform package"",
  ""author"": ""Nexo Team"",
  ""license"": ""MIT"",
  ""platforms"": [""{options.Platform}""],
  ""dependencies"": {{
    ""dotnet"": ""6.0+"",
    ""node"": ""16+""
  }},
  ""scripts"": {{
    ""install"": ""scripts/install.sh"",
    ""uninstall"": ""scripts/uninstall.sh""
  }},
  ""files"": [
    ""files/"",
    ""scripts/"",
    ""package.config.json""
  ]
}}";
        }

        private List<PackageFileInfo> GetCorePackageFiles(PackageExportOptions options)
        {
            return new List<PackageFileInfo>
            {
                new PackageFileInfo { Name = "nexo.exe", Content = "Nexo executable placeholder" },
                new PackageFileInfo { Name = "nexo.dll", Content = "Nexo library placeholder" },
                new PackageFileInfo { Name = "config.json", Content = "{\"version\":\"" + options.Version + "\"}" },
                new PackageFileInfo { Name = "README.md", Content = "# Nexo Package\n\nInstallation package for Nexo platform." }
            };
        }

        private string GenerateInstallationScript(PackageExportOptions options)
        {
            return $@"#!/bin/bash
set -e

echo ""Installing Nexo package v{options.Version}...""

# Create installation directory
INSTALL_DIR=""$HOME/.nexo""
mkdir -p ""$INSTALL_DIR""

# Copy files
cp -r files/* ""$INSTALL_DIR/""

# Set permissions
chmod +x ""$INSTALL_DIR/nexo.exe""

# Create symlink
ln -sf ""$INSTALL_DIR/nexo.exe"" /usr/local/bin/nexo

echo ""Installation completed successfully!""
echo ""Run 'nexo --help' to get started.""";
        }

        private string GenerateWindowsInstallationScript(PackageExportOptions options)
        {
            return $@"@echo off
echo Installing Nexo package v{options.Version}...

REM Create installation directory
set INSTALL_DIR=%USERPROFILE%\.nexo
if not exist ""%INSTALL_DIR%"" mkdir ""%INSTALL_DIR%""

REM Copy files
xcopy /E /I files ""%INSTALL_DIR%""

REM Add to PATH
setx PATH ""%PATH%;%INSTALL_DIR%""

echo Installation completed successfully!
echo Run 'nexo --help' to get started.
pause";
        }

        private string GenerateUninstallationScript(PackageExportOptions options)
        {
            return @"#!/bin/bash
set -e

echo ""Uninstalling Nexo package...""

# Remove symlink
rm -f /usr/local/bin/nexo

# Remove installation directory
rm -rf ""$HOME/.nexo""

echo ""Uninstallation completed successfully!""";
        }

        private string GenerateWindowsUninstallationScript(PackageExportOptions options)
        {
            return @"@echo off
echo Uninstalling Nexo package...

REM Remove from PATH
setx PATH ""%PATH:;%USERPROFILE%\.nexo=%""

REM Remove installation directory
rmdir /S /Q ""%USERPROFILE%\.nexo""

echo Uninstallation completed successfully!
pause";
        }

        private string GeneratePackageConfiguration(PackageExportOptions options)
        {
            return $@"{{
  ""version"": ""{options.Version}"",
  ""platform"": ""{options.Platform}"",
  ""installPath"": ""$HOME/.nexo"",
  ""features"": {{
    ""autoUpdate"": {options.EnableAutoUpdate.ToString().ToLower()},
    ""telemetry"": {options.EnableTelemetry.ToString().ToLower()},
    ""analytics"": {options.EnableAnalytics.ToString().ToLower()}
  }},
  ""dependencies"": [
    ""dotnet"",
    ""node""
  ]
}}";
        }
    }

    public class PackageFileInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
