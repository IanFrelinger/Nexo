using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Contracts;
using Nexo.Ingress.DynamoDb;
using Testcontainers.DynamoDb;
using Xunit;
using Xunit.Abstractions;

namespace Nexo.Tests.Infrastructure.Tests.Ingress;

[CollectionDefinition("IngressDynamoDbDocker")]
public sealed class IngressDynamoDbDockerCollection : ICollectionFixture<IngressDynamoDbDockerFixture>;
