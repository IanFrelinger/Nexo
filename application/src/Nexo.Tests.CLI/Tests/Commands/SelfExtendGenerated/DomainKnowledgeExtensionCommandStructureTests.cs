using Nexo.CLI.Commands.SelfExtendGenerated;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands.SelfExtendGenerated;

public sealed class DomainKnowledgeExtensionCommandStructureTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new DomainKnowledgeExtensionCommand();
            AssertEqual("ext-domain-knowledge", command.Name, "Command name should match scaffold");

            AssertTrue(command is IComposableExtensionCommand, "Command must implement IComposableExtensionCommand");
            var composable = (IComposableExtensionCommand)command;
            AssertEqual("domain-knowledge", composable.ExtensionId, "ExtensionId should match scaffold");
            AssertEqual(0, composable.Dependencies.Count, "Dependency count should match scaffold");
            AssertEqual(0, composable.Dependencies.Count, "Dependencies should be empty for this extension");

            return Task.FromResult(new TestResult
            {
                Name = nameof(DomainKnowledgeExtensionCommandStructureTests),
                Category = "SelfExtendGenerated",
                Passed = true,
                Message = "Generated command structure is valid."
            });
        }
        catch (AssertionException ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(DomainKnowledgeExtensionCommandStructureTests),
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
                Name = nameof(DomainKnowledgeExtensionCommandStructureTests),
                Category = "SelfExtendGenerated",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            });
        }
    }
}