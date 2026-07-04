using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.Abstractions;
using Nexo.Core.Application.Maintenance.Models;
using Nexo.Core.Application.Maintenance.Ports;
using Nexo.Core.Domain.Exceptions;
using Nexo.Infrastructure.Agent.Adapters;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Agent;

/// <summary>Tests for agent executor adapter cwd collection.</summary>
[CollectionDefinition("AgentExecutorAdapterCwd", DisableParallelization = true)]
public sealed class AgentExecutorAdapterCwdCollection;
