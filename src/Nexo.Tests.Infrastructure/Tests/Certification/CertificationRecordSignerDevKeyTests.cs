using FluentAssertions;
using Microsoft.Extensions.Logging;
using Nexo.Certification.Contracts;
using Nexo.Infrastructure.Certification;
using Nexo.Infrastructure.Certification.Composition;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The dev-key warning (M15). Certification records are HMAC-signed with a COMMITTED default
/// key unless <c>NEXO_CERT_DEV_HMAC_KEY</c> is set; a host running on that key must be told so
/// at signer construction, and a host with a real key must not be nagged. Lives in the
/// serialized "EnvironmentVariables" collection because it manipulates the key variable.
/// </summary>
[Trait("Category", "Certification")]
[Collection("EnvironmentVariables")]
public sealed class CertificationRecordSignerDevKeyTests
{
    [Fact]
    public void NoKeyAnywhere_UsesTheDevKey_AndWarnsNamingTheEnvVar()
    {
        using var unset = EnvironmentVariableScope.Unset(CertificationRecordSigning.HmacKeyEnvVar);
        var logger = new ListLogger<CertificationRecordSigner>();

        var signer = new CertificationRecordSigner(logger: logger);

        signer.UsesDevKey.Should().BeTrue("no explicit key and no env var means the committed constant");
        logger.Warnings.Should().ContainSingle()
            .Which.Should().Contain(CertificationRecordSigning.HmacKeyEnvVar)
            .And.Contain("COMMITTED", "the warning must say the key is public, not merely 'default'");
        logger.Warnings.Single().Should().NotContain(CertificationRecordSigning.DefaultDevKey,
            "the log must not spell the key out, even the dev one");
    }

    [Fact]
    public void ExplicitKey_IsNotTheDevKey_AndStaysQuiet()
    {
        using var unset = EnvironmentVariableScope.Unset(CertificationRecordSigning.HmacKeyEnvVar);
        var logger = new ListLogger<CertificationRecordSigner>();

        var signer = new CertificationRecordSigner("operator-secret", logger: logger);

        signer.UsesDevKey.Should().BeFalse();
        logger.Warnings.Should().BeEmpty("a host that configured a key must not be nagged");
    }

    [Fact]
    public void EnvironmentKey_IsNotTheDevKey_AndStaysQuiet_ForBothSigners()
    {
        using var set = new EnvironmentVariableScope(CertificationRecordSigning.HmacKeyEnvVar, "operator-secret-from-env");
        var brickLogger = new ListLogger<CertificationRecordSigner>();
        var compositionLogger = new ListLogger<CompositionCertificationRecordSigner>();

        var brick = new CertificationRecordSigner(logger: brickLogger);
        var composition = new CompositionCertificationRecordSigner(brick, compositionLogger);

        brick.UsesDevKey.Should().BeFalse();
        composition.UsesDevKey.Should().BeFalse();
        brickLogger.Warnings.Should().BeEmpty();
        compositionLogger.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void CompositionSigner_WarnsOnTheDevKey_LikeTheBrickSigner()
    {
        using var unset = EnvironmentVariableScope.Unset(CertificationRecordSigning.HmacKeyEnvVar);
        var logger = new ListLogger<CompositionCertificationRecordSigner>();

        var signer = new CompositionCertificationRecordSigner(logger: logger);

        signer.UsesDevKey.Should().BeTrue();
        logger.Warnings.Should().ContainSingle().Which.Should().Contain(CertificationRecordSigning.HmacKeyEnvVar);
    }

    [Fact]
    public void ContractsHelper_TreatsABlankKeyAsNoBetterThanTheDevKey()
    {
        using var unset = EnvironmentVariableScope.Unset(CertificationRecordSigning.HmacKeyEnvVar);

        CertificationRecordSigning.UsesDevKey().Should().BeTrue();
        CertificationRecordSigning.UsesDevKey("   ").Should().BeTrue("a blank explicit key falls through to the default");
        CertificationRecordSigning.UsesDevKey("real").Should().BeFalse();
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Warnings { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Warning)
                Warnings.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
