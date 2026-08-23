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

/// <summary>Static options monitor.</summary>
internal sealed class StaticOptionsMonitor<T>(T value) : Microsoft.Extensions.Options.IOptionsMonitor<T> where T : class
{
    /// <summary>Current value.</summary>
    public T CurrentValue { get; } = value;
    /// <summary>Gets the value.</summary>
    /// <param name="name">Name.</param>
    public T Get(string? name) => CurrentValue;
    /// <summary>On change.</summary>
    /// <param name="listener">Listener.</param>
    public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;
}
