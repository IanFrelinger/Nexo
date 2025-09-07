namespace Nexo.Feature.Factory.Domain.Enums
{
    /// <summary>
    /// Represents the target platform for code generation.
    /// </summary>
    public enum TargetPlatform
    {
        /// <summary>
        /// .NET/C# platform
        /// </summary>
        DotNet,

        /// <summary>
        /// Unity game engine
        /// </summary>
        Unity,

        /// <summary>
        /// Web platform (JavaScript/TypeScript)
        /// </summary>
        Web,

        /// <summary>
        /// iOS platform (Swift)
        /// </summary>
        iOS,

        /// <summary>
        /// Android platform (Kotlin/Java)
        /// </summary>
        Android,

        /// <summary>
        /// Cross-platform (multiple platforms)
        /// </summary>
        CrossPlatform
    }
}
