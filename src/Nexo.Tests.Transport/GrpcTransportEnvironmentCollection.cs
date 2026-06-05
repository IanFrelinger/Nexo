using Xunit;

namespace Nexo.Tests.Transport;

/// <summary>
/// Serializes gRPC transport tests that mutate process-wide <c>DOTNET_ENVIRONMENT</c> /
/// <c>ASPNETCORE_ENVIRONMENT</c> (parallel runs caused AllowInsecure validation flakes in CI).
/// </summary>
[CollectionDefinition("GrpcTransportEnvironment", DisableParallelization = true)]
public sealed class GrpcTransportEnvironmentCollection;
