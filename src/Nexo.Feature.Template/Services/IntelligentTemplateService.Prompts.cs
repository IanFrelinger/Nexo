using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Feature.Template.Services
{
    /// <summary>
    /// Prompt generation functionality
    /// </summary>
    public partial class IntelligentTemplateService
    {
        private string CreateTemplateGenerationPrompt(string description, IDictionary<string, object> parameters)
        {
            var parametersInfo = parameters != null ? $"Parameters: {string.Join(", ", parameters.Values)}" : "";
            
            return $@"Generate a comprehensive template based on the following description:

Description: {description}
{parametersInfo}

Please create a template that includes:
1. Proper file structure and organization
2. Best practices and conventions
3. Error handling and validation
4. Documentation and comments
5. Configuration management
6. Testing considerations
7. Security considerations
8. Performance optimizations
9. Maintainability features
10. Extensibility patterns

The template should be:
- Production-ready
- Follow industry standards
- Include comprehensive documentation
- Be easily customizable
- Support multiple environments
- Include proper error handling
- Follow SOLID principles

Format the response as complete, compilable code with proper structure and organization.";
        }

        private string CreateTemplateAdaptationPrompt(string template, IDictionary<string, object> requirements)
        {
            var requirementsInfo = string.Join("\n", requirements.Select(kvp => $"- {kvp.Key}: {kvp.Value}"));
            
            return $@"Adapt the following template based on the specified requirements:

Original Template:
{template}

Requirements:
{requirementsInfo}

Please adapt the template to:
1. Meet all specified requirements
2. Maintain the original structure and quality
3. Add any missing functionality
4. Update configuration as needed
5. Ensure compatibility with requirements
6. Preserve best practices
7. Add necessary documentation
8. Include required dependencies
9. Update error handling
10. Optimize for the specific use case

The adapted template should:
- Fulfill all requirements
- Maintain code quality
- Be production-ready
- Include proper documentation
- Follow best practices

Format the response as the complete adapted template.";
        }

        private string CreateTemplateImprovementPrompt(string template, IDictionary<string, object> context)
        {
            var contextInfo = context != null ? $"Context: {string.Join(", ", context.Values)}" : "";
            
            return $@"Analyze the following template and provide improvement suggestions:

Template:
{template}

{contextInfo}

Please provide improvement suggestions for:
1. Code quality and best practices
2. Performance optimizations
3. Security enhancements
4. Maintainability improvements
5. Error handling
6. Documentation
7. Testing coverage
8. Configuration management
9. Dependency management
10. Architectural improvements

For each suggestion, provide:
- The specific improvement
- The reasoning behind it
- Expected benefits
- Implementation guidance

Format your response as a numbered list of specific, actionable improvements.";
        }

        private string CreateProjectStructurePrompt(string projectType, IDictionary<string, object> requirements)
        {
            var requirementsInfo = requirements != null ? $"Requirements: {string.Join(", ", requirements.Values)}" : "";
            
            return $@"Generate a complete project structure for a {projectType} project:

{requirementsInfo}

Please create a project structure that includes:
1. Directory organization
2. File naming conventions
3. Project file structure
4. Configuration files
5. Documentation structure
6. Test organization
7. Build configuration
8. Deployment configuration
9. CI/CD configuration
10. Development tools configuration

The structure should:
- Follow industry best practices
- Be scalable and maintainable
- Support team collaboration
- Include proper separation of concerns
- Support multiple environments
- Include proper documentation
- Follow naming conventions
- Support testing strategies

Format the response as a complete directory structure with file contents and explanations.";
        }

        private string CreateConfigurationTemplatePrompt(string configurationType, IDictionary<string, object> settings)
        {
            var settingsInfo = settings != null ? $"Settings: {string.Join(", ", settings.Values)}" : "";
            
            return $@"Generate a configuration template for {configurationType}:

{settingsInfo}

Please create a configuration template that includes:
1. Environment-specific settings
2. Security configurations
3. Performance settings
4. Logging configuration
5. Database configuration
6. External service configuration
7. Feature flags
8. Monitoring configuration
9. Error handling settings
10. Development tools configuration

The configuration should:
- Be environment-aware
- Include proper validation
- Support secure defaults
- Be easily maintainable
- Include documentation
- Support different deployment scenarios
- Follow configuration best practices

Format the response as a complete configuration template with proper structure and documentation.";
        }

        private string CreateDocumentationTemplatePrompt(string documentationType, IDictionary<string, object> context)
        {
            var contextInfo = context != null ? $"Context: {string.Join(", ", context.Values)}" : "";
            
            return $@"Generate a documentation template for {documentationType}:

{contextInfo}

Please create a documentation template that includes:
1. Overview and purpose
2. Installation instructions
3. Configuration guide
4. Usage examples
5. API documentation
6. Troubleshooting guide
7. Performance considerations
8. Security considerations
9. Deployment guide
10. Contributing guidelines

The documentation should:
- Be comprehensive and clear
- Include code examples
- Be easily navigable
- Include troubleshooting
- Follow documentation best practices
- Be maintainable
- Include proper formatting
- Support multiple audiences

Format the response as a complete documentation template with proper structure and content.";
        }
    }
}
