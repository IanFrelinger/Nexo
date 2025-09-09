using System;

namespace Nexo.Core.Domain.Entities.Infrastructure;

/// <summary>
/// Platform types supported by Nexo
/// </summary>
public enum PlatformType
{
    /// <summary>
    /// .NET Framework/Core
    /// </summary>
    DotNet = 1,
    
    /// <summary>
    /// Unity game engine
    /// </summary>
    Unity = 2,
    
    /// <summary>
    /// Unity 2022
    /// </summary>
    Unity2022 = 2,
    
    /// <summary>
    /// Unity 2023
    /// </summary>
    Unity2023 = 2,
    
    /// <summary>
    /// WebAssembly
    /// </summary>
    WebAssembly = 4,
    
    /// <summary>
    /// Mobile platforms (iOS/Android)
    /// </summary>
    Mobile = 8,
    
    /// <summary>
    /// Server environments
    /// </summary>
    Server = 16,
    
    /// <summary>
    /// Browser environments
    /// </summary>
    Browser = 32,
    
    /// <summary>
    /// Native desktop applications
    /// </summary>
    Native = 64,
    
    /// <summary>
    /// Windows platform
    /// </summary>
    Windows = 128,
    
    /// <summary>
    /// Linux platform
    /// </summary>
    Linux = 256,
    
    /// <summary>
    /// macOS platform
    /// </summary>
    macOS = 512,
    
    /// <summary>
    /// Web platform
    /// </summary>
    Web = 1024,
    
    /// <summary>
    /// JavaScript platform
    /// </summary>
    JavaScript = 2048,
    
    /// <summary>
    /// Swift platform (iOS/macOS)
    /// </summary>
    Swift = 4096,
    
    /// <summary>
    /// Kotlin platform (Android)
    /// </summary>
    Kotlin = 8192,
    
    /// <summary>
    /// All platforms
    /// </summary>
    All = DotNet | Unity | WebAssembly | Mobile | Server | Browser | Native | Windows | Linux | macOS | Web | JavaScript | Swift | Kotlin
}
