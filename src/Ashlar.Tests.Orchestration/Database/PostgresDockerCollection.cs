using System.ComponentModel;
using System.Diagnostics;
using Npgsql;
using Xunit;

namespace Ashlar.Tests.Orchestration.Database;

[CollectionDefinition("PostgresDocker", DisableParallelization = true)]
public sealed class PostgresDockerCollection : ICollectionFixture<PostgresDockerFixture>;
