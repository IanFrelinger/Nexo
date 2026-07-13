using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Provenance.Graph.Ingestion;
using Nexo.Provenance.Graph.Loading;
using Nexo.Provenance.Graph.Models;
using Nexo.Provenance.Graph.Neo4j;
using Nexo.Provenance.Graph.Sdk.Extensions;

var repoRoot = args.Length > 0 ? args[0] : FindRepoRoot();
var certDir = Path.Combine(repoRoot, "samples", "physical-atom-cert");
var policyName = "SelfProducedBrickCertificationPolicy";
var policyVersion = "1.0.0";

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddNeo4jProvenanceGraph(new Neo4jProvenanceGraphOptions
{
    Enabled = true,
    Uri = Environment.GetEnvironmentVariable("NEO4J_URI") ?? "bolt://localhost:7687",
    Username = Environment.GetEnvironmentVariable("NEO4J_USERNAME") ?? "neo4j",
    Password = Environment.GetEnvironmentVariable("NEO4J_PASSWORD") ?? "provenance-graph"
});

var provider = services.BuildServiceProvider();
var store = provider.GetRequiredService<Nexo.Provenance.Graph.Ports.IProvenanceGraphStore>();
var queries = provider.GetRequiredService<Nexo.Provenance.Graph.Ports.IProvenanceGraphQueries>();
var projector = provider.GetRequiredService<ProvenanceProjector>();
var logger = provider.GetRequiredService<ILogger<Program>>();

await store.EnsureSchemaAsync();

var bundles = PhysicalAtomBundleLoader.FindBundleFiles(certDir)
    .Select(path =>
    {
        logger.LogInformation("Loading cert artifact: {Path}", path);
        return PhysicalAtomBundleLoader.LoadFromJsonFile(
            path,
            ArtifactKind.Atom,
            b =>
            {
                // Demo policy annotation for ArtifactsUnderPolicy query
            });
    })
    .Select(b => b with
    {
        PolicyName = policyName,
        PolicyVersion = policyVersion,
        ProducerAgentId = "nexo-demo-agent",
        ProducerAgentKind = AgentKind.Self,
        IssuedAt = DateTimeOffset.UtcNow
    })
    .ToList();

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

var result = await queries.ArtifactsUnderPolicyAsync(policyName, policyVersion, report.ChainHeadHash);

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
        if (File.Exists(Path.Combine(dir, "Nexo.sln")))
            return dir;
        dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
    }

    return Directory.GetCurrentDirectory();
}
