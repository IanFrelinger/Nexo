using System.CommandLine;
using System.Text.Json;
using Ashlar.CLI.Commands;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>Tests for mesh command.</summary>
public sealed class MeshCommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestSetTrustTierPreservesPeerFieldsAsync().ConfigureAwait(false);
            await TestPeersListsLocalInstancesAsync().ConfigureAwait(false);
            await TestHealthMissingUrlExitsNonZeroAsync().ConfigureAwait(false);
            await TestHealthMalformedUrlIsLegibleNonZeroAsync().ConfigureAwait(false);
            await TestHealthRefusedConnectionExitsNonZeroAsync().ConfigureAwait(false);
            await TestSetTrustTierInvalidValueExitsNonZeroAsync().ConfigureAwait(false);
            await TestSetTrustTierMissingPeerExitsNonZeroAsync().ConfigureAwait(false);
            await TestImportMissingFileExitsNonZeroAsync().ConfigureAwait(false);
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
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ashlar-mesh-peers-{Guid.NewGuid():N}");
        var instancesPath = Path.Combine(tempRoot, "instances.json");
        Directory.CreateDirectory(tempRoot);

        var payload = """
                      [
                        {
                          "peerId": "peer-a",
                          "endpoint": "http://peer-a:5000",
                          "capabilities": ["ashlar-cli"],
                          "trustTier": "Trusted",
                          "admitted": true
                        }
                      ]
                      """;
        await File.WriteAllTextAsync(instancesPath, payload).ConfigureAwait(false);

        var previousPath = Environment.GetEnvironmentVariable("ASHLAR_MESH_INSTANCES_PATH");
        Environment.SetEnvironmentVariable("ASHLAR_MESH_INSTANCES_PATH", instancesPath);
        var writer = new StringWriter();  // not disposed on purpose: a disposed writer left in Console.Out poisons later tests
        Console.SetOut(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new MeshCommand());
            var exitCode = await root.InvokeAsync("mesh peers").ConfigureAwait(false);
            /// <summary>Assert equal.</summary>
            AssertEqual(0, exitCode);
            var output = writer.ToString();
            AssertTrue(output.Contains("peer-a", StringComparison.Ordinal), "Should list local peer id.");
            AssertTrue(output.Contains("instances.json", StringComparison.OrdinalIgnoreCase), "Should label local source.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Environment.SetEnvironmentVariable("ASHLAR_MESH_INSTANCES_PATH", previousPath);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    // #455: the exit code must actually stick. The handler used to set Environment.ExitCode, which
    // System.CommandLine overwrites back to 0 after a handler returns — so a missing --url exited 0.
    private async Task TestHealthMissingUrlExitsNonZeroAsync()
    {
        var writer = new StringWriter();  // not disposed: a disposed writer left on Console poisons later tests
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new MeshCommand());
            var exitCode = await root.InvokeAsync("mesh health").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "A missing --url must exit non-zero, not 0.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    // #455: a malformed --url used to reach `new Uri(..., Absolute)` unguarded and throw an
    // UriFormatException with a stack trace. It must now be a legible non-zero refusal.
    private async Task TestHealthMalformedUrlIsLegibleNonZeroAsync()
    {
        var writer = new StringWriter();  // not disposed: see note above
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new MeshCommand());
            var exitCode = await root.InvokeAsync("mesh health --url not-a-valid-url").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "A malformed --url must exit non-zero.");
            var output = writer.ToString();
            AssertTrue(output.Contains("not a valid URL", StringComparison.OrdinalIgnoreCase),
                "A malformed --url must be refused legibly.");
            AssertTrue(!output.Contains("UriFormatException", StringComparison.Ordinal)
                && !output.Contains("   at ", StringComparison.Ordinal),
                "A malformed --url must not print a stack trace.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    // #455: a refused connection must exit non-zero (it used to exit 0 for the same reason).
    private async Task TestHealthRefusedConnectionExitsNonZeroAsync()
    {
        var writer = new StringWriter();  // not disposed: see note above
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new MeshCommand());
            // A closed loopback port refuses immediately; the low timeout bounds a stubborn environment.
            var exitCode = await root
                .InvokeAsync("mesh health --url http://127.0.0.1:59321 --timeout-seconds 5")
                .ConfigureAwait(false);
            AssertTrue(exitCode != 0, "A refused connection must exit non-zero, not 0.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestSetTrustTierPreservesPeerFieldsAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ashlar-mesh-tests-{Guid.NewGuid():N}");
        var instancesPath = Path.Combine(tempRoot, "instances.json");
        Directory.CreateDirectory(tempRoot);

        var payload = """
                      [
                        {
                          "peerId": "peer-1",
                          "endpoint": "http://peer-1:5000",
                          "capabilities": ["ashlar-cli", "compose"],
                          "trustTier": "Unknown",
                          "metadata": { "region": "us-east-1" }
                        }
                      ]
                      """;
        await File.WriteAllTextAsync(instancesPath, payload).ConfigureAwait(false);

        var previousPath = Environment.GetEnvironmentVariable("ASHLAR_MESH_INSTANCES_PATH");
        Environment.SetEnvironmentVariable("ASHLAR_MESH_INSTANCES_PATH", instancesPath);
        var writer = new StringWriter();  // not disposed on purpose: a disposed writer left in Console.Out poisons later tests
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new MeshCommand());

            var exitCode = await root.InvokeAsync("mesh --set-trust-tier peer-1:trusted").ConfigureAwait(false);
            /// <summary>Assert equal.</summary>
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
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            Environment.SetEnvironmentVariable("ASHLAR_MESH_INSTANCES_PATH", previousPath);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private async Task TestSetTrustTierInvalidValueExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new MeshCommand());
            var exitCode = await root.InvokeAsync("mesh --set-trust-tier bad-value").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "An invalid --set-trust-tier value must exit non-zero, not 0.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestSetTrustTierMissingPeerExitsNonZeroAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ashlar-mesh-tests-{Guid.NewGuid():N}");
        var instancesPath = Path.Combine(tempRoot, "instances.json");
        Directory.CreateDirectory(tempRoot);
        await File.WriteAllTextAsync(instancesPath, "[]").ConfigureAwait(false);
        var previousPath = Environment.GetEnvironmentVariable("ASHLAR_MESH_INSTANCES_PATH");
        Environment.SetEnvironmentVariable("ASHLAR_MESH_INSTANCES_PATH", instancesPath);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new MeshCommand());
            var exitCode = await root.InvokeAsync("mesh --set-trust-tier missing-peer:trusted").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "A missing peer on --set-trust-tier must exit non-zero, not 0.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            Environment.SetEnvironmentVariable("ASHLAR_MESH_INSTANCES_PATH", previousPath);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    // #455: a missing sneakernet file used to log a warning, print "Imported 0", and exit 0.
    private async Task TestImportMissingFileExitsNonZeroAsync()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"ashlar-no-such-{Guid.NewGuid():N}.nxpkg");
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new MeshCommand());
            var exitCode = await root.InvokeAsync($"mesh import {missing}").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "mesh import of a missing file must exit non-zero, not 0.");
            var output = writer.ToString();
            AssertTrue(output.Contains("not found", StringComparison.OrdinalIgnoreCase),
                "A missing import file must be refused legibly.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }
}
