using System;

namespace Nexo.Core.Contracts
{
    /// <summary>
    /// Represents a generated extension artifact.
    /// </summary>
    /// <param name="Id">Unique identifier for the extension</param>
    /// <param name="Name">Name of the extension</param>
    /// <param name="SourceCode">The generated source code</param>
    /// <param name="CompiledAssembly">The compiled assembly bytes (if available)</param>
    /// <param name="GeneratedAt">Timestamp when the extension was generated</param>
    public record GeneratedExtension(string Id, string Name, string SourceCode, byte[]? CompiledAssembly, DateTime GeneratedAt);
}
