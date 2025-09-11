using System;

namespace Nexo.Core.Domain.Enums.Environment
{
    /// <summary>
    /// Environment type enumeration
    /// </summary>
    public enum EnvironmentType
    {
        Unknown = 0,
        DotNet = 1,
        Unity = 2,
        WebAssembly = 3,
        Node = 4,
        Python = 5,
        Java = 6,
        Cpp = 7,
        Rust = 8,
        Go = 9
    }
}
