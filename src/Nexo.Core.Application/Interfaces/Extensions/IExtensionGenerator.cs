using Nexo.Core.Domain.Models.Extensions;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Extensions
{
    /// <summary>
    /// Interface for generating AI-powered extensions
    /// </summary>
    public interface IExtensionGenerator
    {
        /// <summary>
        /// Generates extension code based on the request
        /// </summary>
        /// <param name="request">The extension request</param>
        /// <returns>Generated code result</returns>
        Task<GeneratedCode> GenerateAsync(ExtensionRequest request);
    }
}
