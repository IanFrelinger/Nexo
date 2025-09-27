using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.FeatureFactory.DomainLogic;
using Nexo.Core.Domain.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.Validation.Validators;

/// <summary>
/// Validates domain services
/// </summary>
public partial class ServiceValidator
{
    private readonly ILogger<ServiceValidator> _logger;

    public ServiceValidator(ILogger<ServiceValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ServiceValidationResult> ValidateServicesAsync(List<string> services, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating {ServiceCount} services", services.Count);

            var result = new ServiceValidationResult
            {
                Success = true,
                ValidatedAt = DateTime.UtcNow
            };

            foreach (var service in services)
            {
                var serviceIssues = await ValidateServiceAsync(service, cancellationToken);
                result.Issues.AddRange(serviceIssues);
            }

            result.Success = result.Issues.Count == 0;
            result.Message = result.Success ? "Service validation completed successfully" : $"Service validation completed with {result.Issues.Count} issues";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during service validation");
            return new ServiceValidationResult
            {
                Success = false,
                Message = $"Service validation failed: {ex.Message}",
                ValidatedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<List<ServiceIssue>> ValidateServiceAsync(string service, CancellationToken cancellationToken)
    {
        var issues = new List<ServiceIssue>();

        try
        {
            _logger.LogDebug("Validating service: {ServiceName}", service);

            // Check service name validity
            if (string.IsNullOrWhiteSpace(service))
            {
                issues.Add(new ServiceIssue
                {
                    ServiceName = service,
                    IssueType = ServiceIssueType.InvalidName,
                    Message = "Service name cannot be null or empty",
                    Severity = IssueSeverity.Error
                });
            }

            // Check naming conventions
            if (!string.IsNullOrEmpty(service) && !IsValidServiceName(service))
            {
                issues.Add(new ServiceIssue
                {
                    ServiceName = service,
                    IssueType = ServiceIssueType.NamingConvention,
                    Message = "Service name should follow PascalCase convention",
                    Severity = IssueSeverity.Warning
                });
            }

            // Check for reserved keywords
            if (IsReservedKeyword(service))
            {
                issues.Add(new ServiceIssue
                {
                    ServiceName = service,
                    IssueType = ServiceIssueType.ReservedKeyword,
                    Message = $"Service name '{service}' is a reserved keyword",
                    Severity = IssueSeverity.Error
                });
            }

            // Check service naming patterns
            if (!string.IsNullOrEmpty(service) && !IsValidServiceNamingPattern(service))
            {
                issues.Add(new ServiceIssue
                {
                    ServiceName = service,
                    IssueType = ServiceIssueType.InvalidNamingPattern,
                    Message = "Service name should end with 'Service' suffix",
                    Severity = IssueSeverity.Warning
                });
            }

            return issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating service: {ServiceName}", service);
            issues.Add(new ServiceIssue
            {
                ServiceName = service,
                IssueType = ServiceIssueType.ValidationError,
                Message = $"Error validating service: {ex.Message}",
                Severity = IssueSeverity.Error
            });
            return issues;
        }
    }

    private bool IsValidServiceName(string serviceName)
    {
        if (string.IsNullOrEmpty(serviceName))
            return false;

        // Check if starts with uppercase letter
        if (!char.IsUpper(serviceName[0]))
            return false;

        // Check if contains only letters, numbers, and underscores
        return serviceName.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private bool IsValidServiceNamingPattern(string serviceName)
    {
        if (string.IsNullOrEmpty(serviceName))
            return false;

        // Check if service name ends with 'Service' suffix
        return serviceName.EndsWith("Service", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsReservedKeyword(string serviceName)
    {
        var reservedKeywords = new HashSet<string>
        {
            "class", "interface", "enum", "struct", "namespace", "using",
            "public", "private", "protected", "internal", "static", "readonly",
            "const", "virtual", "abstract", "override", "sealed", "partial"
        };

        return reservedKeywords.Contains(serviceName.ToLower());
    }
}
