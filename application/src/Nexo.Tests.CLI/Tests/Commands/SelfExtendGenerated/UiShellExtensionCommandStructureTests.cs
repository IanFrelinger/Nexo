using Nexo.CLI.Commands.SelfExtendGenerated;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands.SelfExtendGenerated;

public sealed class UiShellExtensionCommandStructureTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new UiShellExtensionCommand();
            AssertEqual("ext-ui-shell", command.Name, "Command name should match scaffold");

            AssertTrue(command is IComposableExtensionCommand, "Command must implement IComposableExtensionCommand");
            var composable = (IComposableExtensionCommand)command;
            AssertEqual("ui-shell", composable.ExtensionId, "ExtensionId should match scaffold");
            AssertEqual(1, composable.Dependencies.Count, "Dependency count should match scaffold");
            AssertTrue(composable.Dependencies.Contains("domain-knowledge", StringComparer.Ordinal), "Expected dependency 'domain-knowledge'");

            return Task.FromResult(new TestResult
            {
                Name = nameof(UiShellExtensionCommandStructureTests),
                Category = "SelfExtendGenerated",
                Passed = true,
                Message = "Generated command structure is valid."
            });
        }
        catch (AssertionException ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(UiShellExtensionCommandStructureTests),
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
                Name = nameof(UiShellExtensionCommandStructureTests),
                Category = "SelfExtendGenerated",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            });
        }
    }
}