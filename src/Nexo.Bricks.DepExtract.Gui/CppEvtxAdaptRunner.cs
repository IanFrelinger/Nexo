using Nexo.Bricks.DepExtract.Profile;
using Nexo.Core.Application.Generation;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
using Nexo.Core.Domain.Execution;

namespace Nexo.Bricks.DepExtract.Gui;

/// <summary>
/// Thin Gui helper: runs <see cref="GenerativeArtifactBrick"/> with
/// <c>target=cpp-evtx</c> and maps outputs to the legacy adapt response shape.
/// </summary>
public static class CppEvtxAdaptRunner
{
    public sealed record AdaptResult(
        string AdapterCode,
        string OutputPath,
        double SourceApiCoverage,
        bool LooksLikeTemplateEcho,
        string[] PossibleHallucinations,
        string Summary,
        bool? CompileOk,
        string? CompileLog,
        GenerationStrategy Strategy);

    public static async Task<AdaptResult> RunAsync(
        GenerativeArtifactBrick generator,
        string parserDir,
        string[] entryFiles,
        string outputPath,
        bool preferScaffold,
        bool requireCompile,
        string? operatorGuidance,
        string? pocoContext,
        int? maxCompileRetries,
        IExecutionContext context,
        CancellationToken ct)
    {
        var grounding = CppAdapterPrompt.AssembleGrounding(parserDir, entryFiles);
        var entryInclude = entryFiles.Length > 0
            ? entryFiles[0].Replace('\\', '/').TrimStart('/')
            : null;

        var overrides = new Dictionary<string, object>
        {
            ["entryFiles"] = entryFiles,
        };
        if (!string.IsNullOrWhiteSpace(operatorGuidance))
            overrides["operatorGuidance"] = operatorGuidance!;
        if (!string.IsNullOrWhiteSpace(pocoContext))
            overrides["pocoContext"] = pocoContext!;
        if (!string.IsNullOrWhiteSpace(entryInclude))
            overrides["entryInclude"] = entryInclude!;

        var values = new Dictionary<string, object>
        {
            ["target"] = CppEvtxProfileFactory.TargetId,
            ["grounding"] = grounding,
            ["outputPath"] = outputPath,
            ["preferDeterministic"] = preferScaffold,
            ["overrides"] = overrides,
        };
        if (maxCompileRetries is { } mcr)
            values["maxRepairAttempts"] = mcr;

        CompileGateRequestContext.RequireSuccess = requireCompile;
        try
        {
            var output = await generator.ExecuteAsync(
                new BrickInput(values), ImplementationType.Agentic, context, ct).ConfigureAwait(false);

            var artifact = output.Get<GeneratedArtifact>("artifact")
                ?? throw DepExtractException.EmptyModelResponse();
            var code = artifact.Content ?? "";
            if (string.IsNullOrWhiteSpace(code))
                throw DepExtractException.EmptyModelResponse();

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(outputPath, code, ct).ConfigureAwait(false);

            var prov = output.Get<GenerativeProvenance>(GenerativeBrick.ProvenanceOutputKey);
            var coverage = CppAdapterPrompt.SourceApiCoverage(code, grounding);
            var hallucinations = CppAdapterPrompt.FindPossibleHallucinations(code, grounding);
            var echo = coverage < 0.15;
            var verified = output.Get<bool>("verified");
            var compileLog = prov?.Warnings is { Count: > 0 }
                ? string.Join("\n", prov.Warnings)
                : null;

            return new AdaptResult(
                code,
                outputPath,
                coverage,
                echo,
                hallucinations,
                output.Summary ?? "",
                verified,
                compileLog,
                prov?.Strategy ?? GenerationStrategy.Model);
        }
        finally
        {
            CompileGateRequestContext.RequireSuccess = null;
        }
    }
}
