#if NET8_0_OR_GREATER
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Runtime.Barriers.Identity;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Barriers;

[Trait("Category", "Integration")]
public sealed class HttpBarrierContextMiddlewareTests : IDisposable
{
    private string? _savedEnvVar;

    public HttpBarrierContextMiddlewareTests()
    {
        _savedEnvVar = Environment.GetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED");
    }

    public void Dispose()
    {
        if (_savedEnvVar is null)
            Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", null);
        else
            Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", _savedEnvVar);
    }

    [Fact]
    public async Task Disabled_CallsNext_WithoutResolution()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", null);

        var pipeline = new StubPipeline(null);
        var auditLog = new CapturingAuditLog();
        var hierarchy = CreateHierarchy();
        var logger = new TestLogger<HttpBarrierContextMiddleware>();

        var middleware = new HttpBarrierContextMiddleware(pipeline, auditLog, hierarchy, logger);

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context, next);

        nextCalled.Should().BeTrue("middleware should pass through when disabled");
        auditLog.Events.Should().BeEmpty("no audit events when disabled");
    }

    [Fact]
    public async Task Enabled_WithMatch_InitializesContext()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        var result = new BarrierResolutionResult("internal", "TestResolver", BarrierAuthoritySource.JwtClaim, "test detail");
        var pipeline = new StubPipeline(result);
        var auditLog = new CapturingAuditLog();
        var hierarchy = CreateHierarchy();
        var logger = new TestLogger<HttpBarrierContextMiddleware>();
        var accessor = new CapturingAccessor();

        var services = new ServiceCollection();
        services.AddSingleton<IBarrierContextAccessor>(accessor);
        var serviceProvider = services.BuildServiceProvider();

        var middleware = new HttpBarrierContextMiddleware(pipeline, auditLog, hierarchy, logger);

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var context = new DefaultHttpContext { RequestServices = serviceProvider };

        await middleware.InvokeAsync(context, next);

        nextCalled.Should().BeTrue("middleware should call next after resolution");
        accessor.Current.Should().NotBeNull("accessor should be initialized when pipeline matches");
        accessor.Current!.Level.Should().Be("internal");
        auditLog.Events.Should().Contain(e => e.EventType == BarrierAuditEventType.ContextInitialized);
    }

    private static BarrierHierarchy CreateHierarchy()
        => new([
            new BarrierLevel("public", 0),
            new BarrierLevel("internal", 1),
            new BarrierLevel("confidential", 2)
        ]);

    private sealed class StubPipeline : IBarrierIdentityResolverPipeline
    {
        private readonly BarrierResolutionResult? _result;

        public StubPipeline(BarrierResolutionResult? result) => _result = result;

        public ValueTask<BarrierResolutionResult?> ResolveAsync(
            BarrierResolutionContext context,
            CancellationToken cancellationToken = default)
            => new(_result);
    }

    private sealed class CapturingAccessor : IBarrierContextAccessor
    {
        public BarrierContext? Current { get; private set; }

        public void Initialize(BarrierContext context)
        {
            if (Current is not null)
                throw new InvalidOperationException("Already initialized.");
            Current = context;
        }
    }

    private sealed class CapturingAuditLog : IBarrierAuditLog
    {
        public List<BarrierAuditEvent> Events { get; } = [];

        public ValueTask RecordAsync(BarrierAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return default;
        }
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
#endif
