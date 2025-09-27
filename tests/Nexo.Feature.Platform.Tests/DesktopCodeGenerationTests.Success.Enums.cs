using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Platform.Enums;

namespace Nexo.Feature.Platform.Tests
{
    /// <summary>
    /// Enum tests for desktop code generation functionality
    /// </summary>
    public partial class DesktopCodeGenerationTests
    {
        #region Enum Tests

        [Fact]
        public void DesktopOptimizationLevel_EnumValues_AreDefined()
        {
            // Arrange & Act
            var values = Enum.GetValues<DesktopOptimizationLevel>();

            // Assert
            Assert.Contains(DesktopOptimizationLevel.None, values);
            Assert.Contains(DesktopOptimizationLevel.Minimal, values);
            Assert.Contains(DesktopOptimizationLevel.Balanced, values);
            Assert.Contains(DesktopOptimizationLevel.Aggressive, values);
            Assert.Contains(DesktopOptimizationLevel.Maximum, values);
            Assert.Equal(5, values.Length);
        }

        [Fact]
        public void DesktopPlatformType_EnumValues_AreDefined()
        {
            // Arrange & Act
            var values = Enum.GetValues<DesktopPlatformType>();

            // Assert
            Assert.Contains(DesktopPlatformType.Windows, values);
            Assert.Contains(DesktopPlatformType.macOS, values);
            Assert.Contains(DesktopPlatformType.Linux, values);
            Assert.Contains(DesktopPlatformType.CrossPlatform, values);
            Assert.Equal(4, values.Length);
        }

        [Fact]
        public void DesktopApplicationType_EnumValues_AreDefined()
        {
            // Arrange & Act
            var values = Enum.GetValues<DesktopApplicationType>();

            // Assert
            Assert.Contains(DesktopApplicationType.Console, values);
            Assert.Contains(DesktopApplicationType.WinForms, values);
            Assert.Contains(DesktopApplicationType.WPF, values);
            Assert.Contains(DesktopApplicationType.WinUI, values);
            Assert.Contains(DesktopApplicationType.Avalonia, values);
            Assert.Contains(DesktopApplicationType.MAUI, values);
            Assert.Contains(DesktopApplicationType.WindowsService, values);
            Assert.Contains(DesktopApplicationType.BackgroundService, values);
            Assert.Contains(DesktopApplicationType.Library, values);
            Assert.Equal(9, values.Length);
        }

        [Fact]
        public void DesktopUIFramework_EnumValues_AreDefined()
        {
            // Arrange & Act
            var values = Enum.GetValues<DesktopUIFramework>();

            // Assert
            Assert.Contains(DesktopUIFramework.WinForms, values);
            Assert.Contains(DesktopUIFramework.WPF, values);
            Assert.Contains(DesktopUIFramework.WinUI, values);
            Assert.Contains(DesktopUIFramework.Avalonia, values);
            Assert.Contains(DesktopUIFramework.MAUI, values);
            Assert.Contains(DesktopUIFramework.GTK, values);
            Assert.Contains(DesktopUIFramework.Qt, values);
            Assert.Contains(DesktopUIFramework.None, values);
            Assert.Equal(8, values.Length);
        }

        [Fact]
        public void DesktopDeploymentType_EnumValues_AreDefined()
        {
            // Arrange & Act
            var values = Enum.GetValues<DesktopDeploymentType>();

            // Assert
            Assert.Contains(DesktopDeploymentType.MSI, values);
            Assert.Contains(DesktopDeploymentType.MSIX, values);
            Assert.Contains(DesktopDeploymentType.EXE, values);
            Assert.Contains(DesktopDeploymentType.DMG, values);
            Assert.Contains(DesktopDeploymentType.PKG, values);
            Assert.Contains(DesktopDeploymentType.AppImage, values);
            Assert.Contains(DesktopDeploymentType.DEB, values);
            Assert.Contains(DesktopDeploymentType.RPM, values);
            Assert.Contains(DesktopDeploymentType.Portable, values);
            Assert.Equal(9, values.Length);
        }

        [Fact]
        public void DesktopSystemIntegration_EnumValues_AreDefined()
        {
            // Arrange & Act
            var values = Enum.GetValues<DesktopSystemIntegration>();

            // Assert
            Assert.Contains(DesktopSystemIntegration.FileSystem, values);
            Assert.Contains(DesktopSystemIntegration.Registry, values);
            Assert.Contains(DesktopSystemIntegration.SystemTray, values);
            Assert.Contains(DesktopSystemIntegration.StartMenu, values);
            Assert.Contains(DesktopSystemIntegration.DesktopShortcut, values);
            Assert.Contains(DesktopSystemIntegration.AutoStart, values);
            Assert.Contains(DesktopSystemIntegration.Notifications, values);
            Assert.Contains(DesktopSystemIntegration.Printing, values);
            Assert.Contains(DesktopSystemIntegration.Network, values);
            Assert.Contains(DesktopSystemIntegration.Database, values);
            Assert.Equal(10, values.Length);
        }

        #endregion
    }
}
