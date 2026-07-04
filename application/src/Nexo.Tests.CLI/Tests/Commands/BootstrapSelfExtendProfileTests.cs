using Nexo.CLI.Commands;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

/// <summary>Tests for bootstrap self extend profile.</summary>
public sealed class BootstrapSelfExtendProfileTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestAestheticProfileDoesNotRequireDockerAsync(cancellationToken).ConfigureAwait(false);
            await TestVisualProfileMockRelaxAsync(cancellationToken).ConfigureAwait(false);
            return new TestResult
            {
                Name = nameof(BootstrapSelfExtendProfileTests),
                Category = "CLI",
                Passed = true,
                Message = "Bootstrap self-extend profile expectations validated"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(BootstrapSelfExtendProfileTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(BootstrapSelfExtendProfileTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestAestheticProfileDoesNotRequireDockerAsync(CancellationToken cancellationToken)
    {
        var assessment = await BootstrapRuntime.AssessDemoAsync("self-extend-aesthetic", includeOptional: false, cancellationToken).ConfigureAwait(false);
        /// <summary>Assert true.</summary>
        /// <param name="host."">Host.".</param>
        AssertTrue(assessment.Supported, "Bootstrap assessment should be supported on this host.");
        var docker = assessment.Dependencies.First(d => string.Equals(d.Id, "docker", StringComparison.OrdinalIgnoreCase));
        /// <summary>Assert false.</summary>
        /// <param name="self-extend-visual."">Self-extend-visual.".</param>
        AssertFalse(docker.Required, "Docker must remain optional for self-extend-aesthetic; strict container deps belong to self-extend-visual.");
    }

    private async Task TestVisualProfileMockRelaxAsync(CancellationToken cancellationToken)
    {
        var strict = await BootstrapRuntime.AssessDemoAsync("self-extend-visual", includeOptional: false, cancellationToken, relaxStrictVisualHostDeps: false)
            .ConfigureAwait(false);
        AssertTrue(strict.Dependencies.First(d => d.Id == "docker").Required, "Strict visual profile should require docker without relax flag.");

        var relaxed = await BootstrapRuntime.AssessDemoAsync("self-extend-visual", includeOptional: false, cancellationToken, relaxStrictVisualHostDeps: true)
            .ConfigureAwait(false);
        AssertFalse(relaxed.Dependencies.First(d => d.Id == "docker").Required, "Mock/offline lanes should not elevate docker to required for self-extend-visual.");
    }
}
