using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Commercial.Fleet.Contracts.Models;
using Nexo.Commercial.Fleet.Contracts.Ports;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Nexo.Commercial.Tests.Fleet;

/// <summary>Null disposable.</summary>
internal sealed class NullDisposable : IDisposable
{
    public static readonly NullDisposable Instance = new();
    /// <summary>Dispose.</summary>
    public void Dispose() { }
}
