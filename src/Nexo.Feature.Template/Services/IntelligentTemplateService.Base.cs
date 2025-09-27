using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Feature.Template.Services
{
    /// <summary>
    /// Base template service functionality
    /// </summary>
    public partial class IntelligentTemplateService
    {
        // ITemplateService implementation
        public async Task<string> GetTemplateAsync(string templateName, CancellationToken cancellationToken = default)
        {
            return await _baseTemplateService.GetTemplateAsync(templateName, cancellationToken);
        }

        public async Task SaveTemplateAsync(string templateName, string content, CancellationToken cancellationToken = default)
        {
            await _baseTemplateService.SaveTemplateAsync(templateName, content, cancellationToken);
        }

        public async Task<IEnumerable<string>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default)
        {
            return await _baseTemplateService.GetAvailableTemplatesAsync(cancellationToken);
        }

        public async Task DeleteTemplateAsync(string templateName, CancellationToken cancellationToken = default)
        {
            await _baseTemplateService.DeleteTemplateAsync(templateName, cancellationToken);
        }

        public async Task<bool> ValidateTemplateAsync(string templateName, CancellationToken cancellationToken = default)
        {
            return await _baseTemplateService.ValidateTemplateAsync(templateName, cancellationToken);
        }
    }
}
