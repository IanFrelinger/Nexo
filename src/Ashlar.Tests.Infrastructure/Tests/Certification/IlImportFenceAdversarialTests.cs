using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Ashlar.Certification.Contracts;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Infrastructure.Certification;
using Ashlar.Infrastructure.Testing.CodeAnalysis;
using Ashlar.Tests.Infrastructure.Certification.Fixtures;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>Round-11 attack pass: P/Invoke, calli, module initializers, signatures, wrap/options.</summary>
[Trait("Category", "Certification")]
public sealed class IlImportFenceAdversarialTests
{
    [Fact]
    public void Fence_RefusesPInvoke_EvenWhenMethodHasNoBody()
    {
        var artifact = Compile(PInvokeSource());
        var act = () => IlImportFence.Inspect(artifact.AssemblyBytes);
        act.Should().Throw<InvalidOperationException>().WithMessage("*P/Invoke*");
    }

    [Fact]
    public void Fence_RefusesModuleInitializerAttribute()
    {
        var artifact = Compile(ModuleInitializerSource());
        var act = () => IlImportFence.Inspect(artifact.AssemblyBytes);
        act.Should().Throw<InvalidOperationException>().WithMessage("*ModuleInitializer*");
    }

    [Fact]
    public void Fence_RefusesUnusedIoSignature()
    {
        var artifact = Compile(StreamSignatureSource());
        var act = () => IlImportFence.Inspect(artifact.AssemblyBytes);
        act.Should().Throw<InvalidOperationException>().WithMessage("*System.IO*");
    }

    [Fact]
    public void Fence_RefusesLdtokenOfForbiddenType()
    {
        var artifact = Compile(LdtokenFileSource());
        var act = () => IlImportFence.Inspect(artifact.AssemblyBytes);
        act.Should().Throw<InvalidOperationException>().WithMessage("*System.IO*");
    }

    [Fact]
    public void Fence_RefusesInjectedCalli()
    {
        var artifact = Compile(HonestSource());
        IlImportFence.Inspect(artifact.AssemblyBytes);

        using var module = ModuleDefinition.ReadModule(new MemoryStream(artifact.AssemblyBytes, writable: false));
        var method = module.Types
            .SelectMany(t => t.Methods)
            .First(m => m.Name == "ExecuteAsync" && m.HasBody);
        var il = method.Body.GetILProcessor();
        var callSite = new CallSite(module.TypeSystem.Void)
        {
            CallingConvention = MethodCallingConvention.C
        };
        il.InsertBefore(method.Body.Instructions[0], il.Create(OpCodes.Calli, callSite));

        using var mutated = new MemoryStream();
        module.Write(mutated);
        var act = () => IlImportFence.Inspect(mutated.ToArray());
        act.Should().Throw<InvalidOperationException>().WithMessage("*calli*");
    }

    [Fact]
    public void Fence_AllowsLambdaOnHonestBrick()
    {
        var artifact = Compile(LambdaSource());
        var act = () => IlImportFence.Inspect(artifact.AssemblyBytes);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task CompileParity_CSharp13Escape_FailsOnGateAndMutantCompiler()
    {
        var source = HonestSource().Replace(
            "var output = new BrickOutput { Summary = \"ok\" };",
            "string sneak = \"\\e\"; var output = new BrickOutput { Summary = sneak };",
            StringComparison.Ordinal);

        var gate = () => GateEmittedArtifactCompiler.Compile(
            source, BrickCertificationProjectLoader.DefaultCompilationReferences());
        gate.Should().Throw<InvalidOperationException>().WithMessage("*gate-emitted compile failed*");

        var compiler = new RoslynCodeAnalysisService(NullLogger<RoslynCodeAnalysisService>.Instance);
        var path = Path.Combine(Path.GetTempPath(), "ashlar-parity-" + Guid.NewGuid().ToString("N") + ".dll");
        var wrapped = CandidateSourceWrapper.Wrap(source);
        var compile = await compiler.CompileAsync(
            wrapped, "ParityProbe", path, BrickCertificationProjectLoader.DefaultCompilationReferences());
        compile.Success.Should().BeFalse("mutants must use BrickCompileOptions.ParseOptions (C# 12)");
        compile.Errors.Should().Contain(e => e.Contains("CS1009") || e.Contains("unrecognized escape") || e.Contains("e"));
    }

    [Fact]
    public async Task Loader_RefusesSecondAuthorSourceFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ashlar-multi-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "Brick.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(dir, "Brick.cs"), HonestSource());
        await File.WriteAllTextAsync(Path.Combine(dir, "Extra.cs"), "internal static class Extra { public static int X => 1; }");
        var witness = Path.Combine(dir, "witness.json");
        await File.WriteAllTextAsync(witness, """{"brickId":"honest","cases":[{"input":{"n":1},"expectedOutput":{"n":2}}]}""");

        var act = async () => await BrickCertificationProjectLoader.LoadAsync(dir, witness);
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Contain("single-source");
    }

    [Fact]
    public void FenceInput_IsAllowlistV3()
    {
        var input = IlImportFence.ToInput();
        input.Id.Should().Be("allowlist-v3");
        input.Hash.Should().Be(BrickContentHasher.ComputeSha256(IlImportFence.InventoryBlob));
        IlImportFence.InventoryBlob.Should().Contain("deny-pinvoke=true");
        IlImportFence.InventoryBlob.Should().Contain("deny-calli=true");
        IlImportFence.InventoryBlob.Should().Contain("deny-localloc=true");
        IlImportFence.InventoryBlob.Should().Contain("deny-async-void=true");
        IlImportFence.InventoryBlob.Should().Contain("System.Threading.Thread");
        IlImportFence.InventoryBlob.Should().Contain("System.Threading.Timer");
        IlImportFence.InventoryBlob.Should().Contain("Task::Run");
        IlImportFence.InventoryBlob.Should().Contain("TaskFactory::StartNew");
    }

    [Fact]
    public void Fence_RefusesThreadStart()
    {
        var artifact = Compile(ThreadStartSource());
        var act = () => IlImportFence.Inspect(artifact.AssemblyBytes);
        act.Should().Throw<InvalidOperationException>().WithMessage("*System.Threading.Thread*");
    }

    [Fact]
    public void Fence_RefusesLocalloc()
    {
        var artifact = Compile(StackallocSource());
        var act = () => IlImportFence.Inspect(artifact.AssemblyBytes);
        act.Should().Throw<InvalidOperationException>().WithMessage("*localloc*");
    }

    [Fact]
    public void Fence_RefusesTimer()
    {
        var artifact = Compile(TimerSource());
        var act = () => IlImportFence.Inspect(artifact.AssemblyBytes);
        act.Should().Throw<InvalidOperationException>().WithMessage("*System.Threading.Timer*");
    }

    [Fact]
    public void Fence_RefusesTaskRun()
    {
        var artifact = Compile(TaskRunSource());
        var act = () => IlImportFence.Inspect(artifact.AssemblyBytes);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Task::Run*");
    }

    [Fact]
    public void Fence_RefusesTaskFactoryStartNew()
    {
        var artifact = Compile(TaskFactoryStartNewSource());
        var act = () => IlImportFence.Inspect(artifact.AssemblyBytes);
        act.Should().Throw<InvalidOperationException>().WithMessage("*TaskFactory::StartNew*");
    }

    [Fact]
    public void Fence_RefusesAsyncVoid()
    {
        var artifact = Compile(AsyncVoidSource());
        var act = () => IlImportFence.Inspect(artifact.AssemblyBytes);
        act.Should().Throw<InvalidOperationException>().WithMessage("*async void*");
    }

    [Fact]
    public void AnalyzerAndGate_ShareClosedWorldParseOptions()
    {
        BrickCompileOptions.ForAnalyzerFence().OutputKind.Should().Be(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary);
        BrickCompileOptions.ForAnalyzerFence().AllowUnsafe.Should().BeFalse();
        BrickCompileOptions.ForAnalyzerFence().OptimizationLevel.Should().Be(Microsoft.CodeAnalysis.OptimizationLevel.Release);
        BrickCompileOptions.ForAnalyzerFence().CheckOverflow.Should().BeFalse();
        BrickCompileOptions.ParseOptions.LanguageVersion.Should().Be(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12);
    }

    [Fact]
    public async Task Exporter_WritesGateEmittedDll_BoundByStrictVerify()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ashlar-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "Brick.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(dir, "Brick.cs"), MutationProbeBrickSource.Code);
        var witness = Path.Combine(dir, "witness.json");
        await File.WriteAllTextAsync(witness, """
            {"brickId":"mutation-probe-brick","cases":[{"input":{"logText":"ERROR a\nERROR b"},"expectedOutput":{"errorCount":2,"firstErrorMessage":"a"}}]}
            """);

        var request = await BrickCertificationProjectLoader.LoadAsync(dir, witness);
        request.EmittedArtifact.Should().NotBeNull();
        var recordPath = Path.Combine(dir, "certification-record.json");
        var unsigned = new CertificationRecordData
        {
            Status = "FAIL",
            Stage = "S0-S2",
            Admitted = false,
            Signed = false,
            Timestamp = DateTimeOffset.UtcNow,
            BrickId = request.Brick.Id,
            ContentHash = BrickContentHasher.ComputeSha256(request.SourceCode),
            SchemaVersion = 2,
            Inputs =
            [
                new CertificationInput
                {
                    Kind = CertificationInputKinds.GateEmittedArtifact,
                    Id = request.EmittedArtifact!.BrickTypeName,
                    Hash = request.EmittedArtifact.AssemblySha256
                }
            ]
        };

        await CertifiedArtifactExporter.WriteAsync(recordPath, unsigned, request.EmittedArtifact);
        var dll = Path.Combine(dir, CertifiedArtifactExporter.ArtifactFileName);
        File.Exists(dll).Should().BeTrue();
        BrickContentHasher.ComputeSha256(await File.ReadAllBytesAsync(dll))
            .Should().Be(request.EmittedArtifact.AssemblySha256);
    }

    private static GateEmittedArtifact Compile(string source) =>
        GateEmittedArtifactCompiler.Compile(
            source, BrickCertificationProjectLoader.DefaultCompilationReferences());

    private static string HonestSource() => Honest("HonestBrick", "honest");

    private static string LambdaSource() => Honest(
        "LambdaBrick",
        "lambda",
        executeExtra: "Func<int, int> add = x => x + 1; n = add(n);");

    private static string PInvokeSource() => Honest(
        "PInvokeBrick",
        "pinvoke",
        extraMembers: """
            [System.Runtime.InteropServices.DllImport("libc")]
            private static extern void exit(int code);
            """,
        executeExtra: "exit(0);");

    private static string ModuleInitializerSource() => Honest(
        "ModuleInitBrick",
        "module-init",
        extraMembers: """
            [System.Runtime.CompilerServices.ModuleInitializer]
            internal static void Init() { }
            """);

    private static string StreamSignatureSource() => Honest(
        "StreamSigBrick",
        "stream-sig",
        extraMembers: "private static void Touch(System.IO.Stream s) { _ = s; }");

    private static string LdtokenFileSource() => Honest(
        "LdtokenBrick",
        "ldtoken",
        extraMembers: "private static readonly object FileToken = typeof(System.IO.File);",
        executeExtra: "object boxed = FileToken;");

    private static string ThreadStartSource() => Honest(
        "ThreadStartBrick",
        "thread-start",
        executeExtra: "new System.Threading.Thread(() => { }).Start();");

    private static string StackallocSource() => Honest(
        "StackallocBrick",
        "stackalloc",
        executeExtra: "Span<byte> buf = stackalloc byte[64]; n += buf.Length;");

    private static string TimerSource() => Honest(
        "TimerBrick",
        "timer",
        executeExtra: "new Timer(_ => { }, null, 0, Timeout.Infinite);");

    private static string TaskRunSource() => Honest(
        "TaskRunBrick",
        "task-run",
        executeExtra: "_ = Task.Run(() => { });");

    private static string TaskFactoryStartNewSource() => Honest(
        "StartNewBrick",
        "start-new",
        executeExtra: "Task.Factory.StartNew(() => { });");

    private static string AsyncVoidSource() => Honest(
        "AsyncVoidBrick",
        "async-void",
        extraMembers: """
            private static async void Fire() { await Task.Delay(1); }
            """,
        executeExtra: "Fire();");

    private static string Honest(string className, string id, string extraMembers = "", string executeExtra = "") =>
        $$"""
        using System;
        using Ashlar.Core.Domain.Bricks;
        using Ashlar.Core.Domain.Execution;

        namespace Ashlar.Tests.Infrastructure.Certification.Fixtures;

        public sealed class {{className}} : DomainBrick
        {
            public {{className}}()
            {
                Id = "{{id}}";
                Name = "{{className}}";
                Version = "1.0.0";
                Category = BrickCategory.Analysis;
                Description = "adversarial fixture";
                Interface = new BrickInterface
                {
                    Inputs = [new BrickInputDefinition("n", "int", "n")],
                    Outputs = [new BrickOutputDefinition("n", "int", "n")]
                };
            }

            {{extraMembers}}

            public override Task<BrickOutput> ExecuteAsync(
                BrickInput input,
                ImplementationType implementation,
                IExecutionContext context,
                CancellationToken cancellationToken = default)
            {
                var n = input.Get<int>("n");
                {{executeExtra}}
                var output = new BrickOutput { Summary = "ok" };
                output.Set("n", n);
                return Task.FromResult(output);
            }
        }
        """;
}
