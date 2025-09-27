using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Web.Interfaces;
using Nexo.Feature.Web.Enums;
using System.Text;

namespace Nexo.Feature.Web.Services
{
    /// <summary>
    /// Service for providing framework-specific code templates.
    /// </summary>
    public partial class FrameworkTemplateProvider : IFrameworkTemplateProvider
    {
        private readonly ILogger<FrameworkTemplateProvider> _logger;
        private readonly Dictionary<(WebFrameworkType, WebComponentType), string> _templates;
        private readonly Dictionary<(WebFrameworkType, WebComponentType), string> _typescriptTemplates;
        private readonly Dictionary<(WebFrameworkType, WebComponentType), string> _stylingTemplates;
        private readonly Dictionary<(WebFrameworkType, WebComponentType), string> _testTemplates;
        private readonly Dictionary<(WebFrameworkType, WebComponentType), string> _documentationTemplates;

        public FrameworkTemplateProvider(ILogger<FrameworkTemplateProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _templates = InitializeTemplates();
            _typescriptTemplates = InitializeTypeScriptTemplates();
            _stylingTemplates = InitializeStylingTemplates();
            _testTemplates = InitializeTestTemplates();
            _documentationTemplates = InitializeDocumentationTemplates();
        }

        public string GetTemplate(WebFrameworkType framework, WebComponentType componentType)
        {
            var key = (framework, componentType);
            if (_templates.TryGetValue(key, out var template))
            {
                return template;
            }

            _logger.LogWarning("Template not found for {Framework} {ComponentType}, using default", framework, componentType);
            return GetDefaultTemplate(framework, componentType);
        }

        public string GetTypeScriptTemplate(WebFrameworkType framework, WebComponentType componentType)
        {
            var key = (framework, componentType);
            if (_typescriptTemplates.TryGetValue(key, out var template))
            {
                return template;
            }

            return GetDefaultTypeScriptTemplate(framework, componentType);
        }

        public string GetStylingTemplate(WebFrameworkType framework, WebComponentType componentType)
        {
            var key = (framework, componentType);
            if (_stylingTemplates.TryGetValue(key, out var template))
            {
                return template;
            }

            return GetDefaultStylingTemplate(framework, componentType);
        }

        public string GetTestTemplate(WebFrameworkType framework, WebComponentType componentType)
        {
            var key = (framework, componentType);
            if (_testTemplates.TryGetValue(key, out var template))
            {
                return template;
            }

            return GetDefaultTestTemplate(framework, componentType);
        }

        public string GetDocumentationTemplate(WebFrameworkType framework, WebComponentType componentType)
        {
            var key = (framework, componentType);
            if (_documentationTemplates.TryGetValue(key, out var template))
            {
                return template;
            }

            return GetDefaultDocumentationTemplate(framework, componentType);
        }

        public bool TemplateExists(WebFrameworkType framework, WebComponentType componentType)
        {
            var key = (framework, componentType);
            return _templates.ContainsKey(key);
        }

        public Dictionary<WebComponentType, bool> GetAvailableTemplates(WebFrameworkType framework)
        {
            var result = new Dictionary<WebComponentType, bool>();
            
            foreach (WebComponentType componentType in Enum.GetValues(typeof(WebComponentType)).Cast<WebComponentType>())
            {
                result[componentType] = TemplateExists(framework, componentType);
            }
            
            return result;
        }
    }
}