namespace Nexo.Core.Domain.Enums.Extensions
{
    /// <summary>
    /// Represents the type of extension being generated
    /// </summary>
    public enum ExtensionType
    {
        /// <summary>
        /// Code analyzer extension
        /// </summary>
        Analyzer,
        
        /// <summary>
        /// Code generator extension
        /// </summary>
        Generator,
        
        /// <summary>
        /// Code processor extension
        /// </summary>
        Processor,
        
        /// <summary>
        /// Code formatter extension
        /// </summary>
        Formatter,
        
        /// <summary>
        /// Code linter extension
        /// </summary>
        Linter,
        
        /// <summary>
        /// Code transformer extension
        /// </summary>
        Transformer,
        
        /// <summary>
        /// Custom extension type
        /// </summary>
        Custom
    }
}
