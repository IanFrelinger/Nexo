using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Provenance.Graph.Ingestion;
using Ashlar.Provenance.Graph.Neo4j;
using Ashlar.Provenance.Graph.Ports;
using Ashlar.Provenance.Graph.Sdk.Extensions;
using Ashlar.Provenance.Graph.Sources;

var repoRoot = args.Length > 0 ? args[0] : FindRepoRoot();
var certDir = Path.Combine(repoRoot, "samples", "provenance-graph");
var policyName = "SelfProducedBrickCertificationPolicy";
var policyVersion = "1.0.0";
var source = new PhysicalAtomDirectorySourceAdapter(certDir);

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddSingleton<IProvenanceSourceAdapter>(source);
services.AddSingleton<IProvenanceChainHeadAuthority, SourceProvenanceChainHeadAuthority>();
services.AddNeo4jProvenanceGraph(new Neo4jProvenanceGraphOptions
{
    Enabled = true,
    Uri = Environment.GetEnvironmentVariable("NEO4J_URI") ?? "bolt://localhost:7687",
    Username = Environment.GetEnvironmentVariable("NEO4J_USERNAME") ?? "neo4j",
    Password = Environment.GetEnvironmentVariable("NEO4J_PASSWORD") ?? "provenance-graph"
});

var provider = services.BuildServiceProvider();
var store = provider.GetRequiredService<Ashlar.Provenance.Graph.Ports.IProvenanceGraphStore>();
var queries = provider.GetRequiredService<Ashlar.Provenance.Graph.Ports.IProvenanceGraphQueries>();
var projector = provider.GetRequiredService<ProvenanceProjector>();
var logger = provider.GetRequiredService<ILogger<Program>>();

await store.EnsureSchemaAsync();

logger.LogInformation("Loading authoritative certificate artifacts from {Directory}", certDir);
var bundles = await source.ReadAsync();

if (bundles.Count == 0)
{
    logger.LogWarning("No bundle files found under {CertDir}", certDir);
    return 1;
}

var report = await projector.ProjectAsync(bundles);
logger.LogInformation("Projection complete: accepted={Accepted}, rejected={Rejected}, chainHead={ChainHead}",
    report.AcceptedCount, report.Rejections.Count, report.ChainHeadHash);

foreach (var rejection in report.Rejections)
    logger.LogWarning("Rejected {Hash}: {Code} — {Reason}", rejection.CertificateHash, rejection.FailureCode, rejection.Reason);

var result = await queries.ArtifactsUnderPolicyAsync(policyName, policyVersion);

Console.WriteLine();
Console.WriteLine("=== ArtifactsUnderPolicy Demo ===");
Console.WriteLine($"Policy:     {result.PolicyId}@{result.PolicyVersion}");
Console.WriteLine($"Chain head: {result.ChainHeadHash}");
Console.WriteLine($"Artifacts ({result.ArtifactIds.Count}):");
foreach (var id in result.ArtifactIds)
    Console.WriteLine($"  - {id}");

return report.Rejections.Count > 0 ? 2 : 0;

static string FindRepoRoot()
{
    var dir = AppContext.BaseDirectory;
    while (!string.IsNullOrEmpty(dir))
    {
        if (File.Exists(Path.Combine(dir, "Ashlar.sln")))
            return dir;
        dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
    }

    return Directory.GetCurrentDirectory();
}
