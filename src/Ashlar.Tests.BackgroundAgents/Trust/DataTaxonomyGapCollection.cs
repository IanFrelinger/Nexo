using FluentAssertions;
using Ashlar.BackgroundAgents.DataSensitivity;
using System.Collections.Concurrent;
using System.Reflection;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.Trust;

/// <summary>Data taxonomy gap collection.</summary>
[CollectionDefinition("DataTaxonomyGap", DisableParallelization = true)]
public sealed class DataTaxonomyGapCollection;
