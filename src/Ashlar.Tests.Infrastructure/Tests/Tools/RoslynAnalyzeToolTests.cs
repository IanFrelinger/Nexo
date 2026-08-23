using System.Text.Json;
using Ashlar.Abstractions;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;
using Ashlar.Tools.Dev;

namespace Ashlar.Tests.Infrastructure.Tests.Tools;

/// <summary>Tests for roslyn analyze tool.</summary>
public sealed class RoslynAnalyzeToolTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test flags wrong namespace async.</summary>
            await TestFlagsWrongNamespaceAsync(cancellationToken);
            /// <summary>Test accepts valid generated command async.</summary>
            await TestAcceptsValidGeneratedCommandAsync(cancellationToken);

            return new TestResult
            {
                Name = nameof(RoslynAnalyzeToolTests),
                Category = "Tools",
                Passed = true,
                Message = "RoslynAnalyzeTool tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(RoslynAnalyzeToolTests),
                Category = "Tools",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(RoslynAnalyzeToolTests),
                Category = "Tools",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestFlagsWrongNamespaceAsync(CancellationToken ct)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "ashlar-roslyn-tool", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        var rel = "BadCommand.cs";
        var path = Path.Combine(tmp, rel);

        await File.WriteAllTextAsync(path, """
using System.CommandLine;
namespace Wrong.Namespace;
/// <summary>Tests for bad command.</summary>
public sealed class BadCommand : Command
{
    public BadCommand() : base("bad", "bad") { }
}
""", ct);

        var tool = new RoslynAnalyzeTool();
        var call = new ToolCall(tool.Id, JsonSerializer.SerializeToElement(new
        {
            root = tmp,
            files = new[] { rel },
            rules = new
            {
                requiredNamespace = "Ashlar.CLI.Commands.DemoGenerated",
                requireFileScopedNamespace = true,
                requiredBaseType = "Command",
                requirePublic = true,
                requireSealed = true,
                requiredClassName = "BadCommand",
                requiredCommandName = "bad"
            }
        }));

        var res = await tool.InvokeAsync(call, WorldSnapshot.ForRepo(tmp), ct);
        var json = JsonSerializer.Serialize(res.Payload);
        using var doc = JsonDocument.Parse(json);
        AssertTrue(doc.RootElement.GetProperty("ok").GetBoolean() == false, "Expected ok=false for wrong namespace/style");
        AssertTrue(doc.RootElement.GetProperty("violations").GetArrayLength() > 0, "Expected violations to be present");
    }

    private async Task TestAcceptsValidGeneratedCommandAsync(CancellationToken ct)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "ashlar-roslyn-tool", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        var rel = "HelloGen123Command.cs";
        var path = Path.Combine(tmp, rel);

        await File.WriteAllTextAsync(path, """
using System.CommandLine;
using System.Text.Json;

namespace Ashlar.CLI.Commands.DemoGenerated;

/// <summary>Tests for hello gen123 command.</summary>
public sealed class HelloGen123Command : Command
{
    public HelloGen123Command() : base("hello-gen-123", "Demo-generated command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "hello-gen-123", message = "hi" }));
            Environment.ExitCode = 0;
        });
    }
}
""", ct);

        var tool = new RoslynAnalyzeTool();
        var call = new ToolCall(tool.Id, JsonSerializer.SerializeToElement(new
        {
            root = tmp,
            files = new[] { rel },
            rules = new
            {
                requiredNamespace = "Ashlar.CLI.Commands.DemoGenerated",
                requireFileScopedNamespace = true,
                requiredBaseType = "Command",
                requirePublic = true,
                requireSealed = true,
                requiredClassName = "HelloGen123Command",
                requiredCommandName = "hello-gen-123"
            }
        }));

        var res = await tool.InvokeAsync(call, WorldSnapshot.ForRepo(tmp), ct);
        var json = JsonSerializer.Serialize(res.Payload);
        using var doc = JsonDocument.Parse(json);
        AssertTrue(doc.RootElement.GetProperty("ok").GetBoolean(), "Expected ok=true for valid generated command");
        AssertEqual(0, doc.RootElement.GetProperty("violations").GetArrayLength(), "Expected zero violations");
    }
}

