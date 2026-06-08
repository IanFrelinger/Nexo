using Xunit;

namespace Nexo.Commercial.Tests.Fleet;

/// <summary>Serializes LiteDB fleet tests — parallel runs against temp files can flake LiteDB's mapper.</summary>
[CollectionDefinition(nameof(LiteDbFleetCollection), DisableParallelization = true)]
public sealed class LiteDbFleetCollection;
