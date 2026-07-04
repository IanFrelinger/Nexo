using FluentAssertions;
using Nexo.BackgroundAgents.DataSensitivity;
using System.Collections.Concurrent;
using System.Reflection;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Trust;

/// <summary>Data taxonomy gap collection.</summary>
[CollectionDefinition("DataTaxonomyGap", DisableParallelization = true)]
public sealed class DataTaxonomyGapCollection;
