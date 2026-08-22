using Ashlar.CLI.Commands.SelfExtendGenerated;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.CLI.Tests.Commands.SelfExtendGenerated;

/// <summary>Tests for self extend ui demo bundle command structure.</summary>
public sealed class SelfExtendUiDemoBundleCommandStructureTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new SelfExtendUiDemoBundleCommand();
            /// <summary>Assert equal.</summary>
            /// <param name="scaffold"">Scaffold".</param>
            AssertEqual("self-extend-ui-demo-bundle", command.Name, "Bundle command name should match scaffold");
            AssertTrue(command.Subcommands.Any(s => string.Equals(s.Name, "ext-domain-knowledge", StringComparison.Ordinal)), "Expected subcommand 'ext-domain-knowledge'");
            AssertTrue(command.Subcommands.Any(s => string.Equals(s.Name, "ext-ui-shell", StringComparison.Ordinal)), "Expected subcommand 'ext-ui-shell'");
            AssertTrue(command.Subcommands.Any(s => string.Equals(s.Name, "ext-ui-workflow", StringComparison.Ordinal)), "Expected subcommand 'ext-ui-workflow'");
            AssertTrue(command.Subcommands.Any(s => string.Equals(s.Name, "ext-feature-hotload", StringComparison.Ordinal)), "Expected subcommand 'ext-feature-hotload'");

            return Task.FromResult(new TestResult
            {
                Name = nameof(SelfExtendUiDemoBundleCommandStructureTests),
                Category = "SelfExtendGenerated",
                Passed = true,
                Message = "Bundle command composition is valid."
            });
        }
        catch (AssertionException ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(SelfExtendUiDemoBundleCommandStructureTests),
                Category = "SelfExtendGenerated",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(SelfExtendUiDemoBundleCommandStructureTests),
                Category = "SelfExtendGenerated",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            });
        }
    }
}