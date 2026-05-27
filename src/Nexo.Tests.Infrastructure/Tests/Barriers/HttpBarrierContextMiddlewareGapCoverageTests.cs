#if NET8_0_OR_GREATER
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Runtime.Barriers;
using Nexo.Runtime.Barriers.Identity;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Barriers;

[Trait("Category", "Integration")]
public sealed class HttpBarrierContextMiddlewareGapCoverageTests : IDisposable
{
    private string? _savedEnvVar;

    public HttpBarrierContextMiddlewareGapCoverageTests()
    {
        _savedEnvVar = Environment.GetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(
            "NEXO_BARRIER_MIDDLEWARE_ENABLED",
            _savedEnvVar);
    }

    [Fact]
    public async Task Enabled_with_numeric_flag_runs_resolution()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "1");

        var result = new BarrierResolutionResult("public", "TestResolver", BarrierAuthoritySource.Header, "header");
        var auditLog = new CapturingAuditLog();
        var middleware = new HttpBarrierContextMiddleware(
            new StubPipeline(result),
            auditLog,
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var nextCalled = false;
        await middleware.InvokeAsync(new DefaultHttpContext(), _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
        auditLog.Events.Should().Contain(e => e.EventType == BarrierAuditEventType.ContextInitialized);
    }

    [Fact]
    public async Task Enabled_when_pipeline_throws_still_calls_next()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        var middleware = new HttpBarrierContextMiddleware(
            new ThrowingPipeline(),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var nextCalled = false;
        await middleware.InvokeAsync(new DefaultHttpContext(), _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Enabled_when_ceiling_exceeded_returns_forbidden()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        var result = new BarrierResolutionResult("internal", "TestResolver", BarrierAuthoritySource.JwtClaim, "detail");
        var auditLog = new CapturingAuditLog();
        var middleware = new HttpBarrierContextMiddleware(
            new StubPipeline(result),
            auditLog,
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var services = new ServiceCollection();
        services.AddSingleton<IBarrierContextAccessor>(new ScopedBarrierContextAccessor(
            CreateHierarchy(),
            Options.Create(new BarrierOptions { HostCeiling = "public" })));
        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        var nextCalled = false;
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        auditLog.Events.Should().Contain(e => e.EventType == BarrierAuditEventType.CeilingExceeded);
    }

    [Fact]
    public async Task Enabled_builds_resolution_context_from_headers_jwt_claims_and_certificate()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        CapturingPipeline? capturedPipeline = null;
        var middleware = new HttpBarrierContextMiddleware(
            capturedPipeline = new CapturingPipeline(new BarrierResolutionResult(
                "public",
                "Capture",
                BarrierAuthoritySource.Header,
                "ok")),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var context = new DefaultHttpContext();
        context.Request.Headers["x-nexo-barrier"] = "internal";
        context.Request.Headers["x-nexo-api-key"] = "secret-key";
        context.Request.Headers["Authorization"] = "Bearer jwt-token-123";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("subscription_tier", "pro"),
        ],
        authenticationType: "Bearer"));
        context.Connection.ClientCertificate = CreateCertificateWithSan("localhost", "agent.local");

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        var resolution = capturedPipeline!.LastContext;
        resolution.Should().NotBeNull();
        resolution!.ExplicitLevel.Should().Be("internal");
        resolution.ApiKey.Should().Be("secret-key");
        resolution.RawJwt.Should().Be("jwt-token-123");
        resolution.JwtClaims.Should().ContainKey("subscription_tier");
        resolution.CertSubjects.Should().ContainSingle(s => s.Contains("CN=localhost", StringComparison.Ordinal));
        resolution.CertSans.Should().Contain(s => s.Contains("agent.local", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Enabled_clears_barrier_ambient_in_finally()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        var ambient = new CapturingAmbient();
        var services = new ServiceCollection();
        services.AddSingleton<IBarrierContextAmbient>(ambient);
        services.AddSingleton<IBarrierContextAccessor>(new CapturingAccessor());
        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        var middleware = new HttpBarrierContextMiddleware(
            new StubPipeline(new BarrierResolutionResult("public", "Test", BarrierAuthoritySource.Header, "ok")),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        ambient.WasCleared.Should().BeTrue();
    }

    [Fact]
    public void Constructor_throws_for_null_dependencies()
    {
        var pipeline = new StubPipeline(null);
        var auditLog = new CapturingAuditLog();
        var hierarchy = CreateHierarchy();
        var logger = new TestLogger<HttpBarrierContextMiddleware>();

        var act = () => new HttpBarrierContextMiddleware(null!, auditLog, hierarchy, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("pipeline");

        act = () => new HttpBarrierContextMiddleware(pipeline, null!, hierarchy, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("auditLog");

        act = () => new HttpBarrierContextMiddleware(pipeline, auditLog, null!, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("hierarchy");

        act = () => new HttpBarrierContextMiddleware(pipeline, auditLog, hierarchy, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task Disabled_with_false_flag_calls_next_without_resolution()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "false");

        var auditLog = new CapturingAuditLog();
        var middleware = new HttpBarrierContextMiddleware(
            new StubPipeline(new BarrierResolutionResult("public", "Test", BarrierAuthoritySource.Header, "ok")),
            auditLog,
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var nextCalled = false;
        await middleware.InvokeAsync(new DefaultHttpContext(), _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
        auditLog.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task Enabled_when_pipeline_returns_null_skips_context_audit()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        var auditLog = new CapturingAuditLog();
        var middleware = new HttpBarrierContextMiddleware(
            new StubPipeline(null),
            auditLog,
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var nextCalled = false;
        await middleware.InvokeAsync(new DefaultHttpContext(), _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
        auditLog.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task Enabled_without_accessor_still_records_audit_and_calls_next()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        var auditLog = new CapturingAuditLog();
        var middleware = new HttpBarrierContextMiddleware(
            new StubPipeline(new BarrierResolutionResult("public", "Test", BarrierAuthoritySource.Header, "ok")),
            auditLog,
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var nextCalled = false;
        await middleware.InvokeAsync(new DefaultHttpContext(), _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
        auditLog.Events.Should().Contain(e => e.EventType == BarrierAuditEventType.ContextInitialized);
    }

    [Fact]
    public async Task Enabled_omits_jwt_when_authorization_is_not_bearer()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        CapturingPipeline? capturedPipeline = null;
        var middleware = new HttpBarrierContextMiddleware(
            capturedPipeline = new CapturingPipeline(null),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Basic dXNlcjpwYXNz";

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        capturedPipeline!.LastContext!.RawJwt.Should().BeNull();
    }

    [Fact]
    public async Task Enabled_omits_jwt_claims_when_user_is_unauthenticated()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        CapturingPipeline? capturedPipeline = null;
        var middleware = new HttpBarrierContextMiddleware(
            capturedPipeline = new CapturingPipeline(null),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("subscription_tier", "pro"),
            ])),
        };

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        capturedPipeline!.LastContext!.JwtClaims.Should().BeEmpty();
    }

    [Fact]
    public async Task Enabled_omits_jwt_claims_when_user_is_null()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        CapturingPipeline? capturedPipeline = null;
        var middleware = new HttpBarrierContextMiddleware(
            capturedPipeline = new CapturingPipeline(null),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var context = new DefaultHttpContext { User = null! };

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        capturedPipeline!.LastContext!.JwtClaims.Should().BeEmpty();
    }

    [Fact]
    public async Task Enabled_omits_jwt_claims_when_user_has_no_identities()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        CapturingPipeline? capturedPipeline = null;
        var middleware = new HttpBarrierContextMiddleware(
            capturedPipeline = new CapturingPipeline(null),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(Array.Empty<ClaimsIdentity>()),
        };

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        capturedPipeline!.LastContext!.JwtClaims.Should().BeEmpty();
    }

    [Fact]
    public async Task Enabled_omits_jwt_claims_when_authenticated_user_has_no_claims()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        CapturingPipeline? capturedPipeline = null;
        var middleware = new HttpBarrierContextMiddleware(
            capturedPipeline = new CapturingPipeline(null),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "Bearer")),
        };

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        capturedPipeline!.LastContext!.JwtClaims.Should().BeEmpty();
    }

    [Fact]
    public async Task Enabled_builds_cert_subject_without_sans_when_certificate_has_no_san_extension()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        CapturingPipeline? capturedPipeline = null;
        var middleware = new HttpBarrierContextMiddleware(
            capturedPipeline = new CapturingPipeline(null),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var context = new DefaultHttpContext
        {
            Connection = { ClientCertificate = CreateCertificateWithoutSan("subject-only") },
        };

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        var resolution = capturedPipeline!.LastContext!;
        resolution.CertSubjects.Should().ContainSingle(s => s.Contains("CN=subject-only", StringComparison.Ordinal));
        resolution.CertSans.Should().BeEmpty();
    }

    [Fact]
    public async Task Enabled_with_uppercase_true_flag_runs_resolution()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "TRUE");

        var auditLog = new CapturingAuditLog();
        var middleware = new HttpBarrierContextMiddleware(
            new StubPipeline(new BarrierResolutionResult("public", "Test", BarrierAuthoritySource.Header, "ok")),
            auditLog,
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var nextCalled = false;
        await middleware.InvokeAsync(new DefaultHttpContext(), _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
        auditLog.Events.Should().Contain(e => e.EventType == BarrierAuditEventType.ContextInitialized);
    }

    [Fact]
    public async Task Enabled_trims_bearer_token_from_authorization_header()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        CapturingPipeline? capturedPipeline = null;
        var middleware = new HttpBarrierContextMiddleware(
            capturedPipeline = new CapturingPipeline(null),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer   jwt-with-spaces   ";

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        capturedPipeline!.LastContext!.RawJwt.Should().Be("jwt-with-spaces");
    }

    [Fact]
    public async Task Enabled_uses_trace_identifier_as_correlation_id()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        CapturingPipeline? capturedPipeline = null;
        var middleware = new HttpBarrierContextMiddleware(
            capturedPipeline = new CapturingPipeline(null),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-correlation-123";

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        capturedPipeline!.LastContext!.CorrelationId.Should().Be("trace-correlation-123");
    }

    [Fact]
    public async Task Enabled_initializes_accessor_when_registered()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        var accessor = new CapturingAccessor();
        var services = new ServiceCollection();
        services.AddSingleton<IBarrierContextAccessor>(accessor);
        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        var middleware = new HttpBarrierContextMiddleware(
            new StubPipeline(new BarrierResolutionResult("internal", "Test", BarrierAuthoritySource.Header, "ok")),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        accessor.Current.Should().NotBeNull();
        accessor.Current!.Level.Should().Be("internal");
    }

    [Fact]
    public async Task Enabled_when_cert_san_parsing_fails_still_builds_subject()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        CapturingPipeline? capturedPipeline = null;
        var middleware = new HttpBarrierContextMiddleware(
            capturedPipeline = new CapturingPipeline(null),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var context = new DefaultHttpContext
        {
            Connection = { ClientCertificate = CreateCertificateWithoutSan("san-fallback-subject") },
        };

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        var resolution = capturedPipeline!.LastContext!;
        resolution.CertSubjects.Should().ContainSingle(s => s.Contains("CN=san-fallback-subject", StringComparison.Ordinal));
        resolution.CertSans.Should().BeEmpty();
    }

    [Fact]
    public async Task Disabled_when_env_var_unset_calls_next_without_resolution()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", null);

        var auditLog = new CapturingAuditLog();
        var middleware = new HttpBarrierContextMiddleware(
            new StubPipeline(new BarrierResolutionResult("public", "Test", BarrierAuthoritySource.Header, "ok")),
            auditLog,
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var nextCalled = false;
        await middleware.InvokeAsync(new DefaultHttpContext(), _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
        auditLog.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task Enabled_accepts_lowercase_bearer_scheme()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        CapturingPipeline? capturedPipeline = null;
        var middleware = new HttpBarrierContextMiddleware(
            capturedPipeline = new CapturingPipeline(null),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "bearer lowercase-token";

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        capturedPipeline!.LastContext!.RawJwt.Should().Be("lowercase-token");
    }

    [Fact]
    public async Task Enabled_keeps_first_duplicate_claim_value()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        CapturingPipeline? capturedPipeline = null;
        var middleware = new HttpBarrierContextMiddleware(
            capturedPipeline = new CapturingPipeline(null),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var identity = new ClaimsIdentity(authenticationType: "Bearer");
        identity.AddClaim(new Claim("role", "first"));
        identity.AddClaim(new Claim("role", "second"));
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity),
        };

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        capturedPipeline!.LastContext!.JwtClaims["role"].Should().Be("first");
    }

    [Fact]
    public async Task Enabled_copies_all_request_headers_into_resolution_context()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        CapturingPipeline? capturedPipeline = null;
        var middleware = new HttpBarrierContextMiddleware(
            capturedPipeline = new CapturingPipeline(null),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var context = new DefaultHttpContext();
        context.Request.Headers["x-custom-header"] = "custom-value";
        context.Request.Headers["x-nexo-barrier"] = "public";

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        capturedPipeline!.LastContext!.Headers.Should().ContainKey("x-custom-header");
        capturedPipeline.LastContext.Headers["x-custom-header"].Should().Be("custom-value");
    }

    [Fact]
    public async Task Enabled_audit_event_includes_resolution_detail()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        var auditLog = new CapturingAuditLog();
        var middleware = new HttpBarrierContextMiddleware(
            new StubPipeline(new BarrierResolutionResult("public", "Test", BarrierAuthoritySource.Header, "detail-from-resolver")),
            auditLog,
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        await middleware.InvokeAsync(new DefaultHttpContext(), _ => Task.CompletedTask);

        auditLog.Events.Should().ContainSingle(e =>
            e.EventType == BarrierAuditEventType.ContextInitialized &&
            e.Detail == "detail-from-resolver");
    }

    [Fact]
    public async Task Enabled_without_request_services_still_clears_safely()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        var middleware = new HttpBarrierContextMiddleware(
            new StubPipeline(new BarrierResolutionResult("public", "Test", BarrierAuthoritySource.Header, "ok")),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var context = new DefaultHttpContext { RequestServices = null! };

        var act = async () => await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Enabled_treats_bearer_only_authorization_as_empty_jwt()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        CapturingPipeline? capturedPipeline = null;
        var middleware = new HttpBarrierContextMiddleware(
            capturedPipeline = new CapturingPipeline(null),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer ";

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        capturedPipeline!.LastContext!.RawJwt.Should().Be("");
    }

    [Fact]
    public async Task Enabled_when_next_throws_still_clears_ambient()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        var ambient = new CapturingAmbient();
        var services = new ServiceCollection();
        services.AddSingleton<IBarrierContextAmbient>(ambient);
        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        var middleware = new HttpBarrierContextMiddleware(
            new StubPipeline(new BarrierResolutionResult("public", "Test", BarrierAuthoritySource.Header, "ok")),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var act = async () => await middleware.InvokeAsync(context, _ => throw new InvalidOperationException("next failed"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        ambient.WasCleared.Should().BeTrue();
    }

    [Fact]
    public async Task Enabled_passes_request_aborted_token_to_pipeline()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        var cts = new CancellationTokenSource();
        cts.Cancel();
        var capturingPipeline = new CapturingPipeline(null);
        var middleware = new HttpBarrierContextMiddleware(
            capturingPipeline,
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var context = new DefaultHttpContext();
        context.RequestAborted = cts.Token;

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        capturingPipeline.LastCancellationToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task Disabled_with_unrecognized_env_value_calls_next_without_resolution()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "yes");

        var auditLog = new CapturingAuditLog();
        var middleware = new HttpBarrierContextMiddleware(
            new StubPipeline(new BarrierResolutionResult("public", "Test", BarrierAuthoritySource.Header, "ok")),
            auditLog,
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var nextCalled = false;
        await middleware.InvokeAsync(new DefaultHttpContext(), _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
        auditLog.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task Enabled_when_audit_log_throws_propagates_exception()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        var middleware = new HttpBarrierContextMiddleware(
            new StubPipeline(new BarrierResolutionResult("public", "Test", BarrierAuthoritySource.Header, "ok")),
            new ThrowingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        var act = async () => await middleware.InvokeAsync(new DefaultHttpContext(), _ => Task.CompletedTask);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*audit failed*");
    }

    [Fact]
    public async Task Disabled_still_clears_barrier_ambient_in_finally()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "false");

        var ambient = new CapturingAmbient();
        var services = new ServiceCollection();
        services.AddSingleton<IBarrierContextAmbient>(ambient);
        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        var middleware = new HttpBarrierContextMiddleware(
            new StubPipeline(new BarrierResolutionResult("public", "Test", BarrierAuthoritySource.Header, "ok")),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        ambient.WasCleared.Should().BeTrue();
    }

    [Fact]
    public async Task Enabled_without_client_certificate_leaves_cert_lists_empty()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        CapturingPipeline? capturedPipeline = null;
        var middleware = new HttpBarrierContextMiddleware(
            capturedPipeline = new CapturingPipeline(null),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        await middleware.InvokeAsync(new DefaultHttpContext(), _ => Task.CompletedTask);

        capturedPipeline!.LastContext!.CertSubjects.Should().BeEmpty();
        capturedPipeline.LastContext.CertSans.Should().BeEmpty();
    }

    [Fact]
    public async Task Enabled_omits_explicit_level_and_api_key_when_headers_missing()
    {
        Environment.SetEnvironmentVariable("NEXO_BARRIER_MIDDLEWARE_ENABLED", "true");

        CapturingPipeline? capturedPipeline = null;
        var middleware = new HttpBarrierContextMiddleware(
            capturedPipeline = new CapturingPipeline(null),
            new CapturingAuditLog(),
            CreateHierarchy(),
            new TestLogger<HttpBarrierContextMiddleware>());

        await middleware.InvokeAsync(new DefaultHttpContext(), _ => Task.CompletedTask);

        capturedPipeline!.LastContext!.ExplicitLevel.Should().BeNull();
        capturedPipeline.LastContext.ApiKey.Should().BeNull();
    }

    private static BarrierHierarchy CreateHierarchy()
        => new([
            new BarrierLevel("public", 0),
            new BarrierLevel("internal", 1),
            new BarrierLevel("confidential", 2),
        ]);

    private static X509Certificate2 CreateCertificateWithSan(string commonName, string dnsName)
        => CreateOpenSslCertificate(
            commonName,
            $"-addext \"subjectAltName=DNS:{commonName},DNS:{dnsName}\"");

    private static X509Certificate2 CreateCertificateWithoutSan(string commonName)
        => CreateOpenSslCertificate(commonName, argumentsSuffix: string.Empty);

    private static X509Certificate2 CreateOpenSslCertificate(string commonName, string argumentsSuffix)
    {
        var dir = Path.Combine(Path.GetTempPath(), "nexo-middleware-cert-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var certPem = Path.Combine(dir, "cert.pem");
        var keyPem = Path.Combine(dir, "key.pem");

        try
        {
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "openssl",
                Arguments =
                    $"req -x509 -newkey rsa:2048 -nodes -keyout \"{keyPem}\" -out \"{certPem}\" -days 1 " +
                    $"-subj /CN={commonName} {argumentsSuffix}".TrimEnd(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });
            process.Should().NotBeNull();
            process!.WaitForExit(10_000).Should().BeTrue();
            process.ExitCode.Should().Be(0);

#if NET9_0_OR_GREATER
            return X509CertificateLoader.LoadCertificateFromFile(certPem);
#else
            return new X509Certificate2(certPem);
#endif
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    private sealed class StubPipeline(BarrierResolutionResult? result) : IBarrierIdentityResolverPipeline
    {
        public ValueTask<BarrierResolutionResult?> ResolveAsync(
            BarrierResolutionContext context,
            CancellationToken cancellationToken = default)
            => new(result);
    }

    private sealed class CapturingPipeline(BarrierResolutionResult? result) : IBarrierIdentityResolverPipeline
    {
        public BarrierResolutionContext? LastContext { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public ValueTask<BarrierResolutionResult?> ResolveAsync(
            BarrierResolutionContext context,
            CancellationToken cancellationToken = default)
        {
            LastContext = context;
            LastCancellationToken = cancellationToken;
            return new(result);
        }
    }

    private sealed class ThrowingPipeline : IBarrierIdentityResolverPipeline
    {
        public ValueTask<BarrierResolutionResult?> ResolveAsync(
            BarrierResolutionContext context,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("resolution failed");
    }

    private sealed class CapturingAccessor : IBarrierContextAccessor
    {
        public BarrierContext? Current { get; private set; }

        public void Initialize(BarrierContext context) => Current = context;
    }

    private sealed class CapturingAmbient : IBarrierContextAmbient
    {
        public BarrierContext? Current { get; private set; }
        public bool WasCleared { get; private set; }

        public void SetCurrent(BarrierContext? context)
        {
            Current = context;
            if (context is null)
                WasCleared = true;
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

    private sealed class ThrowingAuditLog : IBarrierAuditLog
    {
        public ValueTask RecordAsync(BarrierAuditEvent auditEvent, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("audit failed");
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
