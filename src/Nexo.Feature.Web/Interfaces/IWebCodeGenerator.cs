using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Feature.Web.Models;

namespace Nexo.Feature.Web.Interfaces
{
    /// <summary>
    /// Interface for web code generation services.
    /// </summary>
    public interface IWebCodeGenerator
    {
        /// <summary>
        /// Generates web code based on the provided request.
        /// </summary>
        /// <param name="request">The code generation request.</param>
        /// <returns>The code generation result.</returns>
        Task<WebCodeGenerationResult> GenerateCodeAsync(WebCodeGenerationRequest request);
        
        /// <summary>
        /// Validates the code generation request.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <returns>True if the request is valid, false otherwise.</returns>
        bool ValidateRequest(WebCodeGenerationRequest request);
        
        /// <summary>
        /// Gets supported frameworks for code generation.
        /// </summary>
        /// <returns>List of supported framework types.</returns>
        IEnumerable<string> GetSupportedFrameworks();
        
        /// <summary>
        /// Gets supported component types for a given framework.
        /// </summary>
        /// <param name="framework">The framework type.</param>
        /// <returns>List of supported component types.</returns>
        IEnumerable<string> GetSupportedComponentTypes(string framework);
    }
} 