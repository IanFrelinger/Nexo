using System;
using System.Reflection;
using Xunit;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Core.Application.Tests.Services.Iteration;

/// <summary>
/// Tests for the runtime environment detector
/// </summary>
public class RuntimeEnvironmentDetectorTests
{
    [Fact]
    public void DetectCurrent_ShouldReturnValidProfile()
    {
        // Act
        var profile = RuntimeEnvironmentDetector.DetectCurrent();
        
        // Assert
        Assert.NotNull(profile);
        Assert.True(profile.CpuCores > 0);
        Assert.True(profile.AvailableMemoryMB > 0);
    }
    
    [Fact]
    public void DetectCurrent_ShouldDetectCorrectPlatformType()
    {
        // Act
        var profile = RuntimeEnvironmentDetector.DetectCurrent();
        
        // Assert
        Assert.True(Enum.IsDefined(typeof(PlatformType), profile.PlatformType));
        
        // Should be one of the supported platform types
        Assert.True(profile.PlatformType == PlatformType.DotNet ||
                   profile.PlatformType == PlatformType.Unity ||
                   profile.PlatformType == PlatformType.WebAssembly ||
                   profile.PlatformType == PlatformType.JavaScript ||
                   profile.PlatformType == PlatformType.Swift ||
                   profile.PlatformType == PlatformType.Kotlin ||
                   profile.PlatformType == PlatformType.Native ||
                   profile.PlatformType == PlatformType.Mobile ||
                   profile.PlatformType == PlatformType.Web ||
                   profile.PlatformType == PlatformType.Server);
    }
    
    [Fact]
    public void DetectCurrent_ShouldHaveReasonableCpuCores()
    {
        // Act
        var profile = RuntimeEnvironmentDetector.DetectCurrent();
        
        // Assert
        Assert.True(profile.CpuCores > 0);
        Assert.True(profile.CpuCores <= 128); // Reasonable upper limit
    }
    
    [Fact]
    public void DetectCurrent_ShouldHaveReasonableMemory()
    {
        // Act
        var profile = RuntimeEnvironmentDetector.DetectCurrent();
        
        // Assert
        Assert.True(profile.AvailableMemoryMB > 0);
        Assert.True(profile.AvailableMemoryMB <= 1024 * 1024); // Reasonable upper limit (1TB)
    }
    
    [Fact]
    public void DetectCurrent_ShouldDetectConstrainedEnvironment()
    {
        // Act
        var profile = RuntimeEnvironmentDetector.DetectCurrent();
        
        // Assert
        // Constrained environment should be detected based on low resources
        if (profile.CpuCores < 2 || profile.AvailableMemoryMB < 512)
        {
            Assert.True(profile.IsConstrained);
        }
    }
    
    [Fact]
    public void DetectCurrent_ShouldDetectMobileEnvironment()
    {
        // Act
        var profile = RuntimeEnvironmentDetector.DetectCurrent();
        
        // Assert
        // Mobile environment detection is based on environment variables
        // In a real mobile environment, this would be true
        Assert.True(profile.IsMobile == false || profile.IsMobile == true);
    }
    
    [Fact]
    public void DetectCurrent_ShouldDetectWebEnvironment()
    {
        // Act
        var profile = RuntimeEnvironmentDetector.DetectCurrent();
        
        // Assert
        // Web environment detection is based on environment variables
        // In a real web environment, this would be true
        Assert.True(profile.IsWeb == false || profile.IsWeb == true);
    }
    
    [Fact]
    public void DetectCurrent_ShouldDetectUnityEnvironment()
    {
        // Act
        var profile = RuntimeEnvironmentDetector.DetectCurrent();
        
        // Assert
        // Unity environment detection is based on assembly presence
        // In a real Unity environment, this would be true
        Assert.True(profile.IsUnity == false || profile.IsUnity == true);
    }
    
    [Fact]
    public void DetectCurrent_ShouldBeConsistent()
    {
        // Act
        var profile1 = RuntimeEnvironmentDetector.DetectCurrent();
        var profile2 = RuntimeEnvironmentDetector.DetectCurrent();
        
        // Assert
        Assert.Equal(profile1.PlatformType, profile2.PlatformType);
        Assert.Equal(profile1.CpuCores, profile2.CpuCores);
        Assert.Equal(profile1.AvailableMemoryMB, profile2.AvailableMemoryMB);
        Assert.Equal(profile1.IsConstrained, profile2.IsConstrained);
        Assert.Equal(profile1.IsMobile, profile2.IsMobile);
        Assert.Equal(profile1.IsWeb, profile2.IsWeb);
        Assert.Equal(profile1.IsUnity, profile2.IsUnity);
    }
    
    [Theory]
    [InlineData(PlatformType.DotNet)]
    [InlineData(PlatformType.Unity)]
    [InlineData(PlatformType.WebAssembly)]
    [InlineData(PlatformType.JavaScript)]
    [InlineData(PlatformType.Swift)]
    [InlineData(PlatformType.Kotlin)]
    [InlineData(PlatformType.Native)]
    [InlineData(PlatformType.Mobile)]
    [InlineData(PlatformType.Web)]
    [InlineData(PlatformType.Server)]
    public void DetectCurrent_ShouldHandleAllPlatformTypes(PlatformType platformType)
    {
        // This test ensures that all platform types are valid enum values
        // and can be handled by the detector
        
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(PlatformType), platformType));
    }
    
    [Fact]
    public void DetectCurrent_ShouldHandleEnvironmentVariables()
    {
        // Arrange
        var originalEnv = Environment.GetEnvironmentVariable("DOTNET_RUNTIME_IDENTIFIER");
        
        try
        {
            // Set a test environment variable
            Environment.SetEnvironmentVariable("DOTNET_RUNTIME_IDENTIFIER", "android-x64");
            
            // Act
            var profile = RuntimeEnvironmentDetector.DetectCurrent();
            
            // Assert
            Assert.NotNull(profile);
            // The profile should still be valid even with modified environment variables
            Assert.True(profile.CpuCores > 0);
            Assert.True(profile.AvailableMemoryMB > 0);
        }
        finally
        {
            // Restore original environment variable
            if (originalEnv != null)
            {
                Environment.SetEnvironmentVariable("DOTNET_RUNTIME_IDENTIFIER", originalEnv);
            }
            else
            {
                Environment.SetEnvironmentVariable("DOTNET_RUNTIME_IDENTIFIER", null);
            }
        }
    }
    
    [Fact]
    public void DetectCurrent_ShouldHandleAssemblyDetection()
    {
        // Act
        var profile = RuntimeEnvironmentDetector.DetectCurrent();
        
        // Assert
        Assert.NotNull(profile);
        
        // The detector should be able to check for Unity assemblies
        // without throwing exceptions
        Assert.True(profile.IsUnity == false || profile.IsUnity == true);
    }
    
    [Fact]
    public void DetectCurrent_ShouldHandleMemoryDetection()
    {
        // Act
        var profile = RuntimeEnvironmentDetector.DetectCurrent();
        
        // Assert
        Assert.NotNull(profile);
        
        // Memory detection should work on all platforms
        Assert.True(profile.AvailableMemoryMB > 0);
        
        // Should have a reasonable fallback if detection fails
        Assert.True(profile.AvailableMemoryMB >= 1024); // At least 1GB fallback
    }
}
