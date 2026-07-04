using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Validation.Models;
using Nexo.Infrastructure.Validation.Adapters;
using Nexo.Infrastructure.Validation.Parsers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Validation;

/// <summary>Tests for validation adapter cwd collection.</summary>
[CollectionDefinition("ValidationAdapterCwd", DisableParallelization = true)]
public sealed class ValidationAdapterCwdCollection;
