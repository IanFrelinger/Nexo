using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Models.Policy;

namespace Nexo.Core.Domain.Interfaces
{
    /// <summary>
    /// Interface for policy engine that validates code against safety and quality policies
    /// </summary>
    public interface IPolicyEngine
    {
        /// <summary>
        /// Loads a policy from a YAML file
        /// </summary>
        Task<PolicyDefinition> LoadPolicyAsync(string policyPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a policy file against its schema
        /// </summary>
        Task<PolicyValidationResult> ValidatePolicyAsync(string policyPath, string schemaPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies safety policies to generated code
        /// </summary>
        Task<SafetyPolicyResult> ApplySafetyPolicyAsync(string code, SafetyPolicy policy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies quality policies to generated code
        /// </summary>
        Task<QualityPolicyResult> ApplyQualityPolicyAsync(string code, QualityPolicy policy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs all policies defined in a manifest
        /// </summary>
        Task<PolicyExecutionResult> ExecutePolicyManifestAsync(string manifestPath, string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Merges multiple policies with override precedence
        /// </summary>
        Task<PolicyDefinition> MergePoliciesAsync(IEnumerable<string> policyPaths, CancellationToken cancellationToken = default);
    }
}
