using FluentAssertions;
using Nexo.Infrastructure.Execution;
using System.Reflection;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Safety;

[CollectionDefinition("LocalModelProviderState", DisableParallelization = true)]
public sealed class LocalModelProviderStateCollection;
