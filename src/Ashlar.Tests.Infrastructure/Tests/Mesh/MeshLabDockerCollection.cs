using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Mesh;

[CollectionDefinition("MeshLabDocker")]
public sealed class MeshLabDockerCollection : ICollectionFixture<MeshLabDockerFixture>;
