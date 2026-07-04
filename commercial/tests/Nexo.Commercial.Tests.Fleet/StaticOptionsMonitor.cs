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
