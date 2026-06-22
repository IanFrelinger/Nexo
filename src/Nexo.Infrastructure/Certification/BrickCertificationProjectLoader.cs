using System.Reflection;
using System.Text.Json;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Core.Domain.Bricks;

namespace Nexo.Infrastructure.Certification;

public static class BrickCertificationProjectLoader
{
    public static async Task<CertificationRequest> LoadAsync(
        string brickProjectDirectory,
        string witnessSpecPath,
        CancellationToken cancellationToken = default)
    {
        var projectDir = Path.GetFullPath(brickProjectDirectory);
        var csproj = Directory.GetFiles(projectDir, "*.csproj").FirstOrDefault()
            ?? throw new FileNotFoundException($"No .csproj in {projectDir}");
        var sourceFile = Directory.GetFiles(projectDir, "*.cs")
            .FirstOrDefault(f => !f.EndsWith(".AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException($"No .cs source in {projectDir}");

        var sourceCode = await File.ReadAllTextAsync(sourceFile, cancellationToken).ConfigureAwait(false);
        var witnessJson = await File.ReadAllTextAsync(witnessSpecPath, cancellationToken).ConfigureAwait(false);
        var witnessDto = JsonSerializer.Deserialize<WitnessSpecDto>(witnessJson, JsonOptions)
            ?? throw new InvalidOperationException("Witness spec is empty");

        var buildDir = Path.Combine(Path.GetTempPath(), "nexo-cert-build", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(buildDir);

        var build = await RunDotnetBuildAsync(csproj, buildDir, cancellationToken).ConfigureAwait(false);
        if (build.ExitCode != 0)
            throw new InvalidOperationException($"dotnet build failed: {build.Output}");

        var dllPath = Directory.GetFiles(buildDir, "*.dll", SearchOption.AllDirectories)
            .FirstOrDefault(f => Path.GetFileName(f).Contains(Path.GetFileNameWithoutExtension(csproj), StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException("Built brick assembly not found");

        var assembly = Assembly.LoadFrom(dllPath);
        var brickType = assembly.GetTypes().FirstOrDefault(t => typeof(Brick).IsAssignableFrom(t) && !t.IsAbstract)
            ?? throw new InvalidOperationException("No Brick type in assembly");

        var brick = (Brick)Activator.CreateInstance(brickType)!;
        var references = CollectReferences(buildDir, dllPath);

        var witness = new WitnessSpec(
            witnessDto.BrickId ?? brick.Id,
            witnessDto.Cases.Select(c => new WitnessCase(
                NormalizeDictionary(c.Input),
                NormalizeDictionary(c.ExpectedOutput))).ToList());

        return new CertificationRequest
        {
            Brick = brick,
            Witness = witness,
            SourceCode = sourceCode,
            ProjectPath = csproj,
            CompilationReferences = references,
            BrickTypeName = brickType.FullName
        };
    }

    private static async Task<(int ExitCode, string Output)> RunDotnetBuildAsync(
        string csproj,
        string outputDir,
        CancellationToken cancellationToken)
    {
        var configFile = Environment.GetEnvironmentVariable("NEXO_CERT_NUGET_CONFIG");
        var configArg = string.IsNullOrWhiteSpace(configFile)
            ? string.Empty
            : $" --configfile \"{configFile}\"";

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments =
                $"build \"{csproj}\" -c Release -o \"{outputDir}\" -v q{configArg} " +
                "-p:TreatWarningsAsErrors=false -p:NuGetAudit=false",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var process = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet build");
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return (process.ExitCode, stdout + stderr);
    }

    private static List<string> CollectReferences(string buildDir, string primaryDll)
    {
        var refs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { primaryDll };
        foreach (var dll in Directory.GetFiles(buildDir, "*.dll", SearchOption.AllDirectories))
            refs.Add(dll);
        return refs.ToList();
    }

    private sealed class WitnessSpecDto
    {
        public string? BrickId { get; set; }
        public List<WitnessCaseDto> Cases { get; set; } = [];
    }

    private sealed class WitnessCaseDto
    {
        public Dictionary<string, object> Input { get; set; } = new();
        public Dictionary<string, object> ExpectedOutput { get; set; } = new();
    }

    private static Dictionary<string, object> NormalizeDictionary(Dictionary<string, object> values)
    {
        var normalized = new Dictionary<string, object>(values.Count);
        foreach (var (key, value) in values)
            normalized[key] = NormalizeValue(value);
        return normalized;
    }

    private static object NormalizeValue(object value) => value switch
    {
        JsonElement element => FromJsonElement(element),
        _ => value
    };

    private static object FromJsonElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString()!,
        JsonValueKind.Number when element.TryGetInt32(out var int32) => int32,
        JsonValueKind.Number when element.TryGetInt64(out var number) => number,
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => element.GetRawText()
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
