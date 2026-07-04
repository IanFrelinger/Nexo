using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Mesh;

[CollectionDefinition("MeshLabDocker")]
public sealed class MeshLabDockerCollection : ICollectionFixture<MeshLabDockerFixture>;
