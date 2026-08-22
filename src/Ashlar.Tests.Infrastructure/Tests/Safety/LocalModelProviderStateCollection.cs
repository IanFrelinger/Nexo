using FluentAssertions;
using Ashlar.Infrastructure.Execution;
using System.Reflection;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Safety;

[CollectionDefinition("LocalModelProviderState", DisableParallelization = true)]
public sealed class LocalModelProviderStateCollection;
