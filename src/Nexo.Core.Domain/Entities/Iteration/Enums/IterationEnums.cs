using System;

namespace Nexo.Core.Domain.Entities.Iteration.Enums
{
    /// <summary>
    /// Platform compatibility flags
    /// </summary>
    [Flags]
    public enum PlatformCompatibility
    {
        None = 0,
        DotNet = 1,
        Unity = 2,
        WebAssembly = 4,
        Mobile = 8,
        Server = 16,
        Browser = 32,
        Native = 64,
        All = DotNet | Unity | WebAssembly | Mobile | Server | Browser | Native
    }

    /// <summary>
    /// Iteration priority enumeration
    /// </summary>
    public enum IterationPriority
    {
        Performance,
        Readability,
        Maintainability
    }

    /// <summary>
    /// Platform target enumeration
    /// </summary>
    public enum PlatformTarget
    {
        DotNet,
        Unity,
        Unity2022,
        Unity2023,
        WebAssembly,
        Mobile,
        Server,
        Browser,
        Native,
        JavaScript,
        Swift,
        Kotlin,
        Python,
        Java,
        Go,
        Rust,
        Cpp,
        Windows,
        Linux,
        macOS,
        iOS,
        Android,
        CSharp,
        Web
    }
}
