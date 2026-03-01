using System.Text.Json;
using Nexo.Abstractions;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Tools.Dev;

namespace Nexo.Tests.Infrastructure.Tests.Tools;

public sealed class RoslynAnalyzeToolTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestFlagsWrongNamespaceAsync(cancellationToken);
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
        var tmp = Path.Combine(Path.GetTempPath(), "nexo-roslyn-tool", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        var rel = "BadCommand.cs";
        var path = Path.Combine(tmp, rel);

        await File.WriteAllTextAsync(path, """
using System.CommandLine;
namespace Wrong.Namespace;
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
                requiredNamespace = "Nexo.CLI.Commands.DemoGenerated",
                requireFileScopedNamespace = true,
                requiredBaseType = "Command",
                requirePublic = true,
                requireSealed = true,
                requiredClassName = "BadCommand",
                requiredCommandName = "bad"
            }
        }));

        var res = await tool.InvokeAsync(call, new WorldSnapshot(0, new Dictionary<string, object?>()), ct);
        var json = JsonSerializer.Serialize(res.Payload);
        using var doc = JsonDocument.Parse(json);
        AssertTrue(doc.RootElement.GetProperty("ok").GetBoolean() == false, "Expected ok=false for wrong namespace/style");
        AssertTrue(doc.RootElement.GetProperty("violations").GetArrayLength() > 0, "Expected violations to be present");
    }

    private async Task TestAcceptsValidGeneratedCommandAsync(CancellationToken ct)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "nexo-roslyn-tool", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        var rel = "HelloGen123Command.cs";
        var path = Path.Combine(tmp, rel);

        await File.WriteAllTextAsync(path, """
using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.DemoGenerated;

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
                requiredNamespace = "Nexo.CLI.Commands.DemoGenerated",
                requireFileScopedNamespace = true,
                requiredBaseType = "Command",
                requirePublic = true,
                requireSealed = true,
                requiredClassName = "HelloGen123Command",
                requiredCommandName = "hello-gen-123"
            }
        }));

        var res = await tool.InvokeAsync(call, new WorldSnapshot(0, new Dictionary<string, object?>()), ct);
        var json = JsonSerializer.Serialize(res.Payload);
        using var doc = JsonDocument.Parse(json);
        AssertTrue(doc.RootElement.GetProperty("ok").GetBoolean(), "Expected ok=true for valid generated command");
        AssertEqual(0, doc.RootElement.GetProperty("violations").GetArrayLength(), "Expected zero violations");
    }
}

