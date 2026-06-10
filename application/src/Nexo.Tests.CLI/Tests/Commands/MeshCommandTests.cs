using System.CommandLine;
using System.Text.Json;
using Nexo.CLI.Commands;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

public sealed class MeshCommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestSetTrustTierPreservesPeerFieldsAsync().ConfigureAwait(false);
            await TestPeersListsLocalInstancesAsync().ConfigureAwait(false);
            return new TestResult
            {
                Name = nameof(MeshCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "Mesh command tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(MeshCommandTests),
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
                Name = nameof(MeshCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestPeersListsLocalInstancesAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"nexo-mesh-peers-{Guid.NewGuid():N}");
        var instancesPath = Path.Combine(tempRoot, "instances.json");
        Directory.CreateDirectory(tempRoot);

        var payload = """
                      [
                        {
                          "peerId": "peer-a",
                          "endpoint": "http://peer-a:5000",
                          "capabilities": ["nexo-cli"],
                          "trustTier": "Trusted",
                          "admitted": true
                        }
                      ]
                      """;
        await File.WriteAllTextAsync(instancesPath, payload).ConfigureAwait(false);

        var previousPath = Environment.GetEnvironmentVariable("NEXO_MESH_INSTANCES_PATH");
        Environment.SetEnvironmentVariable("NEXO_MESH_INSTANCES_PATH", instancesPath);
        var previousOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new MeshCommand());
            var exitCode = await root.InvokeAsync("mesh peers").ConfigureAwait(false);
            AssertEqual(0, exitCode);
            var output = writer.ToString();
            AssertTrue(output.Contains("peer-a", StringComparison.Ordinal), "Should list local peer id.");
            AssertTrue(output.Contains("instances.json", StringComparison.OrdinalIgnoreCase), "Should label local source.");
        }
        finally
        {
            Console.SetOut(previousOut);
            Environment.SetEnvironmentVariable("NEXO_MESH_INSTANCES_PATH", previousPath);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private async Task TestSetTrustTierPreservesPeerFieldsAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"nexo-mesh-tests-{Guid.NewGuid():N}");
        var instancesPath = Path.Combine(tempRoot, "instances.json");
        Directory.CreateDirectory(tempRoot);

        var payload = """
                      [
                        {
                          "peerId": "peer-1",
                          "endpoint": "http://peer-1:5000",
                          "capabilities": ["nexo-cli", "compose"],
                          "trustTier": "Unknown",
                          "metadata": { "region": "us-east-1" }
                        }
                      ]
                      """;
        await File.WriteAllTextAsync(instancesPath, payload).ConfigureAwait(false);

        var previousPath = Environment.GetEnvironmentVariable("NEXO_MESH_INSTANCES_PATH");
        Environment.SetEnvironmentVariable("NEXO_MESH_INSTANCES_PATH", instancesPath);
        var previousOut = Console.Out;
        var previousErr = Console.Error;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new MeshCommand());

            var exitCode = await root.InvokeAsync("mesh --set-trust-tier peer-1:trusted").ConfigureAwait(false);
            AssertEqual(0, exitCode);
            AssertTrue(writer.ToString().Contains("Updated trust tier", StringComparison.OrdinalIgnoreCase));

            using var updated = JsonDocument.Parse(await File.ReadAllTextAsync(instancesPath).ConfigureAwait(false));
            var first = updated.RootElement[0];
            AssertEqual("peer-1", first.GetProperty("peerId").GetString() ?? string.Empty);
            AssertEqual("http://peer-1:5000", first.GetProperty("endpoint").GetString() ?? string.Empty);
            AssertTrue(first.GetProperty("capabilities").GetArrayLength() == 2, "Capabilities should be preserved.");
            AssertEqual("trusted", (first.GetProperty("trustTier").GetString() ?? string.Empty).ToLowerInvariant());
            AssertEqual("us-east-1", first.GetProperty("metadata").GetProperty("region").GetString() ?? string.Empty);
        }
        finally
        {
            Console.SetOut(previousOut);
            Console.SetError(previousErr);
            Environment.SetEnvironmentVariable("NEXO_MESH_INSTANCES_PATH", previousPath);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }
}
