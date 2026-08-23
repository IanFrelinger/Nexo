using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Commercial.Fleet.Contracts.Models;
using Ashlar.Commercial.Fleet.Contracts.Ports;
using Ashlar.Core.Application.Observation.Models;
using Ashlar.Core.Application.Observation.Ports;
using Ashlar.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Ashlar.Commercial.Tests.Fleet;

/// <summary>Null disposable.</summary>
internal sealed class NullDisposable : IDisposable
{
    public static readonly NullDisposable Instance = new();
    /// <summary>Dispose.</summary>
    public void Dispose() { }
}
