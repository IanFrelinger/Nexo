using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ashlar.Contracts;
using Ashlar.Ingress.DynamoDb;
using Testcontainers.DynamoDb;
using Xunit;
using Xunit.Abstractions;

namespace Ashlar.Tests.Infrastructure.Tests.Ingress;

[CollectionDefinition("IngressDynamoDbDocker")]
public sealed class IngressDynamoDbDockerCollection : ICollectionFixture<IngressDynamoDbDockerFixture>;
