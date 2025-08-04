using System;
using System.Collections.Generic;
using Nexo.Feature.Web.Enums;

namespace Nexo.Feature.Web.Interfaces
{
    /// <summary>
    /// Interface for providing framework-specific code templates.
    /// </summary>
    public interface IFrameworkTemplateProvider
    {
        /// <summary>
        /// Gets a template for the specified framework and component type.
        /// </summary>
        /// <param name="framework">The target framework.</param>
        /// <param name="componentType">The component type.</param>
        /// <returns>The template string.</returns>
        string GetTemplate(WebFrameworkType framework, WebComponentType componentType);
        
        /// <summary>
        /// Gets TypeScript types template for the framework.
        /// </summary>
        /// <param name="framework">The target framework.</param>
        /// <param name="componentType">The component type.</param>
        /// <returns>The TypeScript types template.</returns>
        string GetTypeScriptTemplate(WebFrameworkType framework, WebComponentType componentType);
        
        /// <summary>
        /// Gets CSS/styling template for the framework.
        /// </summary>
        /// <param name="framework">The target framework.</param>
        /// <param name="componentType">The component type.</param>
        /// <returns>The styling template.</returns>
        string GetStylingTemplate(WebFrameworkType framework, WebComponentType componentType);
        
        /// <summary>
        /// Gets unit test template for the framework.
        /// </summary>
        /// <param name="framework">The target framework.</param>
        /// <param name="componentType">The component type.</param>
        /// <returns>The test template.</returns>
        string GetTestTemplate(WebFrameworkType framework, WebComponentType componentType);
        
        /// <summary>
        /// Gets documentation template for the framework.
        /// </summary>
        /// <param name="framework">The target framework.</param>
        /// <param name="componentType">The component type.</param>
        /// <returns>The documentation template.</returns>
        string GetDocumentationTemplate(WebFrameworkType framework, WebComponentType componentType);
        
        /// <summary>
        /// Checks if a template exists for the specified framework and component type.
        /// </summary>
        /// <param name="framework">The target framework.</param>
        /// <param name="componentType">The component type.</param>
        /// <returns>True if template exists, false otherwise.</returns>
        bool TemplateExists(WebFrameworkType framework, WebComponentType componentType);
        
        /// <summary>
        /// Gets all available templates for a framework.
        /// </summary>
        /// <param name="framework">The target framework.</param>
        /// <returns>Dictionary of component types to template availability.</returns>
        Dictionary<WebComponentType, bool> GetAvailableTemplates(WebFrameworkType framework);
    }
} 