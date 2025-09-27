using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Nexo.Core.Contracts;
using Nexo.Core.Pipeline;
using Nexo.Core.Configuration;

namespace Nexo.Core.Extensions
{
    /// <summary>
    /// Extension methods for registering pipeline services in dependency injection.
    /// </summary>
    public static class PipelineServiceExtensions
    {
        /// <summary>
        /// Adds pipeline services to the service collection.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">Optional configuration section for repair loop options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddPipelineServices(this IServiceCollection services, IConfigurationSection? configuration = null)
        {
            // Register repair loop configuration
            if (configuration != null)
            {
                services.Configure<RepairLoopOptions>(options => configuration.Bind(options));
            }
            else
            {
                services.Configure<RepairLoopOptions>(options => { }); // Use defaults
            }

            // Register the pipeline orchestrator as a generic service
            services.AddTransient(typeof(ExtensionPipeline<,>));

            // Register default empty collections for gates and policies if none are registered
            services.AddSingleton<IEnumerable<ICompilationGate>>(provider =>
            {
                var gates = provider.GetServices<ICompilationGate>().ToList();
                if (!gates.Any())
                {
                    // Return a default empty compilation gate that always passes
                    return new[] { new DefaultCompilationGate() };
                }
                return gates;
            });

            services.AddSingleton<IEnumerable<IPolicyGate<object>>>(provider =>
            {
                var policies = provider.GetServices<IPolicyGate<object>>().ToList();
                if (!policies.Any())
                {
                    // Return a default empty policy gate that always passes
                    return new[] { new DefaultPolicyGate<object>() };
                }
                return policies;
            });

            return services;
        }

        /// <summary>
        /// Adds pipeline services with specific types.
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TArtifact">The artifact type</typeparam>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">Optional configuration section for repair loop options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddPipelineServices<TRequest, TArtifact>(this IServiceCollection services, IConfigurationSection? configuration = null)
        {
            // Register repair loop configuration
            if (configuration != null)
            {
                services.Configure<RepairLoopOptions>(options => configuration.Bind(options));
            }
            else
            {
                services.Configure<RepairLoopOptions>(options => { }); // Use defaults
            }

            // Register the specific pipeline type
            services.AddTransient<ExtensionPipeline<TRequest, TArtifact>>();

            // Register default empty collections for gates and policies if none are registered
            services.AddSingleton<IEnumerable<ICompilationGate>>(provider =>
            {
                var gates = provider.GetServices<ICompilationGate>().ToList();
                if (!gates.Any())
                {
                    return new[] { new DefaultCompilationGate() };
                }
                return gates;
            });

            services.AddSingleton<IEnumerable<IPolicyGate<TArtifact>>>(provider =>
            {
                var policies = provider.GetServices<IPolicyGate<TArtifact>>().ToList();
                if (!policies.Any())
                {
                    return new[] { new DefaultPolicyGate<TArtifact>() };
                }
                return policies;
            });

            return services;
        }
    }

    /// <summary>
    /// Default compilation gate that always passes (used when no gates are registered).
    /// </summary>
    public partial class DefaultCompilationGate : ICompilationGate
    {
        /// <summary>
        /// Validates the source code and always returns a passing result.
        /// </summary>
        /// <param name="sourceCode">The source code to validate</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>A validation result that always passes</returns>
        public ValueTask<ValidationResult> ValidateAsync(string sourceCode, CancellationToken ct = default)
        {
            return ValueTask.FromResult(new ValidationResult(
                true,
                "DefaultCompilationGate",
                new[] { "No compilation gates configured - defaulting to pass" }
            ));
        }
    }

    /// <summary>
    /// Default policy gate that always passes (used when no policies are registered).
    /// </summary>
    public partial class DefaultPolicyGate<TArtifact> : IPolicyGate<TArtifact>
    {
        /// <summary>
        /// Evaluates the artifact and always returns a passing policy outcome.
        /// </summary>
        /// <param name="artifact">The artifact to evaluate</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>A policy outcome that always passes</returns>
        public Task<PolicyOutcome> EvaluateAsync(TArtifact artifact, CancellationToken ct = default)
        {
            return Task.FromResult(new PolicyOutcome(
                true,
                "DefaultPolicyGate",
                new[] { "No policy gates configured - defaulting to pass" },
                1.0
            ));
        }
    }
}
