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
/// Validates value objects and their properties
/// </summary>
public class ValueObjectValidator
{
    private readonly ILogger<ValueObjectValidator> _logger;

    public ValueObjectValidator(ILogger<ValueObjectValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ValueObjectValidationResult> ValidateValueObjectsAsync(List<string> valueObjects, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating {ValueObjectCount} value objects", valueObjects.Count);

            var result = new ValueObjectValidationResult
            {
                Success = true,
                ValidatedAt = DateTime.UtcNow
            };

            foreach (var valueObject in valueObjects)
            {
                var valueObjectIssues = await ValidateValueObjectAsync(new ValueObject { Name = valueObject }, cancellationToken);
                result.Issues.AddRange(valueObjectIssues);
            }

            result.Success = result.Issues.Count == 0;
            result.Message = result.Success ? "Value object validation completed successfully" : $"Value object validation completed with {result.Issues.Count} issues";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during value object validation");
            return new ValueObjectValidationResult
            {
                Success = false,
                Message = $"Value object validation failed: {ex.Message}",
                ValidatedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<List<ValueObjectIssue>> ValidateValueObjectAsync(ValueObject valueObject, CancellationToken cancellationToken)
    {
        var issues = new List<ValueObjectIssue>();

        try
        {
            _logger.LogDebug("Validating value object: {ValueObjectName}", valueObject.Name);

            // Check value object name validity
            if (string.IsNullOrWhiteSpace(valueObject.Name))
            {
                issues.Add(new ValueObjectIssue
                {
                    ValueObjectName = valueObject.Name,
                    IssueType = ValueObjectIssueType.InvalidName,
                    Message = "Value object name cannot be null or empty",
                    Severity = IssueSeverity.Error
                });
            }

            // Check naming conventions
            if (!string.IsNullOrEmpty(valueObject.Name) && !IsValidValueObjectName(valueObject.Name))
            {
                issues.Add(new ValueObjectIssue
                {
                    ValueObjectName = valueObject.Name,
                    IssueType = ValueObjectIssueType.NamingConvention,
                    Message = "Value object name should follow PascalCase convention",
                    Severity = IssueSeverity.Warning
                });
            }

            // Check for reserved keywords
            if (IsReservedKeyword(valueObject.Name))
            {
                issues.Add(new ValueObjectIssue
                {
                    ValueObjectName = valueObject.Name,
                    IssueType = ValueObjectIssueType.ReservedKeyword,
                    Message = $"Value object name '{valueObject.Name}' is a reserved keyword",
                    Severity = IssueSeverity.Error
                });
            }

            // Check value object properties
            if (valueObject.Properties != null)
            {
                foreach (var property in valueObject.Properties)
                {
                    var propertyIssues = ValidateValueObjectProperty(property);
                    issues.AddRange(propertyIssues);
                }
            }

            return issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating value object: {ValueObjectName}", valueObject.Name);
            issues.Add(new ValueObjectIssue
            {
                ValueObjectName = valueObject.Name,
                IssueType = ValueObjectIssueType.ValidationError,
                Message = $"Error validating value object: {ex.Message}",
                Severity = IssueSeverity.Error
            });
            return issues;
        }
    }

    private List<ValueObjectIssue> ValidateValueObjectProperty(ValueObjectProperty property)
    {
        var issues = new List<ValueObjectIssue>();

        try
        {
            // Check property name validity
            if (string.IsNullOrWhiteSpace(property.Name))
            {
                issues.Add(new ValueObjectIssue
                {
                    ValueObjectName = property.Name,
                    IssueType = ValueObjectIssueType.InvalidPropertyName,
                    Message = "Property name cannot be null or empty",
                    Severity = IssueSeverity.Error
                });
            }

            // Check property type validity
            if (string.IsNullOrWhiteSpace(property.Type))
            {
                issues.Add(new ValueObjectIssue
                {
                    ValueObjectName = property.Name,
                    IssueType = ValueObjectIssueType.InvalidPropertyType,
                    Message = "Property type cannot be null or empty",
                    Severity = IssueSeverity.Error
                });
            }

            // Check naming conventions
            if (!string.IsNullOrEmpty(property.Name) && !IsValidPropertyName(property.Name))
            {
                issues.Add(new ValueObjectIssue
                {
                    ValueObjectName = property.Name,
                    IssueType = ValueObjectIssueType.PropertyNamingConvention,
                    Message = "Property name should follow camelCase convention",
                    Severity = IssueSeverity.Warning
                });
            }

            return issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating value object property: {PropertyName}", property.Name);
            issues.Add(new ValueObjectIssue
            {
                ValueObjectName = property.Name,
                IssueType = ValueObjectIssueType.ValidationError,
                Message = $"Error validating property: {ex.Message}",
                Severity = IssueSeverity.Error
            });
            return issues;
        }
    }

    private bool IsValidValueObjectName(string valueObjectName)
    {
        if (string.IsNullOrEmpty(valueObjectName))
            return false;

        // Check if starts with uppercase letter
        if (!char.IsUpper(valueObjectName[0]))
            return false;

        // Check if contains only letters, numbers, and underscores
        return valueObjectName.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private bool IsValidPropertyName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return false;

        // Check if starts with lowercase letter
        if (!char.IsLower(propertyName[0]))
            return false;

        // Check if contains only letters, numbers, and underscores
        return propertyName.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private bool IsReservedKeyword(string valueObjectName)
    {
        var reservedKeywords = new HashSet<string>
        {
            "class", "interface", "enum", "struct", "namespace", "using",
            "public", "private", "protected", "internal", "static", "readonly",
            "const", "virtual", "abstract", "override", "sealed", "partial"
        };

        return reservedKeywords.Contains(valueObjectName.ToLower());
    }
}
