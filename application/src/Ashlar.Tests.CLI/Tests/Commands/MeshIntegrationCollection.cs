using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// Groups the mesh tests that bind real sockets / servers (Kestrel, HttpListener, UDP multicast) and
/// generate certificates, so they run SERIALLY rather than contending for ports and crypto resources
/// under xUnit's default cross-class parallelism. Pure-logic mesh tests (registry, MergePeerUrls,
/// beacon parsing, tailnet parse) stay outside it and keep running in parallel.
/// </summary>
[CollectionDefinition("MeshIntegration", DisableParallelization = true)]
public sealed class MeshIntegrationCollection
{
}
