using Nexo.Core.Application.Composition.Models;

namespace Nexo.Infrastructure.Composition;

/// <summary>Validates component descriptor metadata required for pipeline composition registration.</summary>
internal static class ComponentDescriptorValidator
{
    /// <summary>Returns validation issues for the supplied descriptor; empty when valid.</summary>
    public static IReadOnlyList<ComponentValidationIssue> Validate(ComponentDescriptor descriptor)
    {
        var issues = new List<ComponentValidationIssue>();
        if (string.IsNullOrWhiteSpace(descriptor.Id))
            issues.Add(new ComponentValidationIssue("metadata.missing-id", "(unknown)", "Component id is required."));

        if (string.IsNullOrWhiteSpace(descriptor.Capability))
            issues.Add(new ComponentValidationIssue("metadata.missing-capability", descriptor.Id, "Capability is required."));

        if (string.IsNullOrWhiteSpace(descriptor.DisplayName))
            issues.Add(new ComponentValidationIssue("metadata.missing-display-name", descriptor.Id, "DisplayName is required."));

        if (string.IsNullOrWhiteSpace(descriptor.Summary))
            issues.Add(new ComponentValidationIssue("metadata.missing-summary", descriptor.Id, "Summary is required."));

        if (string.IsNullOrWhiteSpace(descriptor.ImplementationType))
            issues.Add(new ComponentValidationIssue("metadata.missing-implementation", descriptor.Id, "ImplementationType is required."));
        else if (string.Equals(descriptor.ImplementationType.Trim(), "TBD", StringComparison.OrdinalIgnoreCase))
            issues.Add(new ComponentValidationIssue("metadata.placeholder-implementation", descriptor.Id, "ImplementationType cannot be 'TBD'."));

        if (!Version.TryParse(descriptor.Version, out _))
            issues.Add(new ComponentValidationIssue("metadata.invalid-version", descriptor.Id, $"Version '{descriptor.Version}' is not a valid semantic version."));

        if (descriptor.SupportLevel == ComponentSupportLevel.Stable)
        {
            if (string.IsNullOrWhiteSpace(descriptor.InputSchema))
                issues.Add(new ComponentValidationIssue("metadata.missing-input-schema", descriptor.Id, "InputSchema is required when SupportLevel is Stable."));
            if (string.IsNullOrWhiteSpace(descriptor.OutputSchema))
                issues.Add(new ComponentValidationIssue("metadata.missing-output-schema", descriptor.Id, "OutputSchema is required when SupportLevel is Stable."));
        }

        return issues;
    }
}
