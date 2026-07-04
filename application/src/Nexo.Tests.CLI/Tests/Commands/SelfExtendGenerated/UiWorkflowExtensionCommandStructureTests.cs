using Nexo.CLI.Commands.SelfExtendGenerated;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands.SelfExtendGenerated;

/// <summary>Tests for ui workflow extension command structure.</summary>
public sealed class UiWorkflowExtensionCommandStructureTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new UiWorkflowExtensionCommand();
            /// <summary>Assert equal.</summary>
            /// <param name="scaffold"">Scaffold".</param>
            AssertEqual("ext-ui-workflow", command.Name, "Command name should match scaffold");

            /// <summary>Assert true.</summary>
            /// <param name="IComposableExtensionCommand">Composable extension command.</param>
            /// <param name="IComposableExtensionCommand"">Composable extension command".</param>
            AssertTrue(command is IComposableExtensionCommand, "Command must implement IComposableExtensionCommand");
            var composable = (IComposableExtensionCommand)command;
            /// <summary>Assert equal.</summary>
            /// <param name="scaffold"">Scaffold".</param>
            AssertEqual("ui-workflow", composable.ExtensionId, "ExtensionId should match scaffold");
            /// <summary>Assert equal.</summary>
            /// <param name="scaffold"">Scaffold".</param>
            AssertEqual(2, composable.Dependencies.Count, "Dependency count should match scaffold");
            AssertTrue(composable.Dependencies.Contains("domain-knowledge", StringComparer.Ordinal), "Expected dependency 'domain-knowledge'");
            AssertTrue(composable.Dependencies.Contains("ui-shell", StringComparer.Ordinal), "Expected dependency 'ui-shell'");

            return Task.FromResult(new TestResult
            {
                Name = nameof(UiWorkflowExtensionCommandStructureTests),
                Category = "SelfExtendGenerated",
                Passed = true,
                Message = "Generated command structure is valid."
            });
        }
        catch (AssertionException ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(UiWorkflowExtensionCommandStructureTests),
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
                Name = nameof(UiWorkflowExtensionCommandStructureTests),
                Category = "SelfExtendGenerated",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            });
        }
    }
}