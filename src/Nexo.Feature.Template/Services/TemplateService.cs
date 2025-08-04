using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Template.Interfaces;

namespace Nexo.Feature.Template.Services
{
    /// <summary>
    /// Base template service implementation that provides template management capabilities.
    /// </summary>
    public class TemplateService : ITemplateService
    {
        private readonly ILogger<TemplateService> _logger;
        private readonly string _templateDirectory;

        public TemplateService(ILogger<TemplateService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _templateDirectory = Path.Combine(Environment.CurrentDirectory, "templates");
            
            // Ensure template directory exists
            if (!Directory.Exists(_templateDirectory))
            {
                Directory.CreateDirectory(_templateDirectory);
            }
        }

        public async Task<string> GetTemplateAsync(string templateName, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting template: {TemplateName}", templateName);

            try
            {
                if (string.IsNullOrEmpty(templateName))
                {
                    throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));
                }

                var templatePath = Path.Combine(_templateDirectory, $"{templateName}.template");
                
                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"Template '{templateName}' not found", templatePath);
                }

                return await Task.Run(() => File.ReadAllText(templatePath), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template: {TemplateName}", templateName);
                throw;
            }
        }

        public async Task SaveTemplateAsync(string templateName, string content, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Saving template: {TemplateName}", templateName);

            try
            {
                if (string.IsNullOrEmpty(templateName))
                {
                    throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));
                }

                if (string.IsNullOrEmpty(content))
                {
                    throw new ArgumentException("Template content cannot be null or empty", nameof(content));
                }

                var templatePath = Path.Combine(_templateDirectory, $"{templateName}.template");
                await Task.Run(() => File.WriteAllText(templatePath, content), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving template: {TemplateName}", templateName);
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting available templates");

            try
            {
                var templateFiles = Directory.GetFiles(_templateDirectory, "*.template");
                var templateNames = new List<string>();

                foreach (var file in templateFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    templateNames.Add(fileName);
                }

                await Task.CompletedTask; // Async operation placeholder
                return templateNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available templates");
                throw;
            }
        }

        public async Task DeleteTemplateAsync(string templateName, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting template: {TemplateName}", templateName);

            try
            {
                if (string.IsNullOrEmpty(templateName))
                {
                    throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));
                }

                var templatePath = Path.Combine(_templateDirectory, $"{templateName}.template");
                
                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"Template '{templateName}' not found", templatePath);
                }

                File.Delete(templatePath);
                await Task.CompletedTask; // Async operation placeholder
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template: {TemplateName}", templateName);
                throw;
            }
        }

        public async Task<bool> ValidateTemplateAsync(string templateName, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Validating template: {TemplateName}", templateName);

            try
            {
                if (string.IsNullOrEmpty(templateName))
                {
                    return false;
                }

                var templatePath = Path.Combine(_templateDirectory, $"{templateName}.template");
                
                if (!File.Exists(templatePath))
                {
                    return false;
                }

                var content = await Task.Run(() => File.ReadAllText(templatePath), cancellationToken);
                
                // Basic validation - check if content is not empty and contains basic structure
                return !string.IsNullOrWhiteSpace(content) && content.Length > 10;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating template: {TemplateName}", templateName);
                return false;
            }
        }
    }
} 