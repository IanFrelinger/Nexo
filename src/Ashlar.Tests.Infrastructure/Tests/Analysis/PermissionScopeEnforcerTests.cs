using FluentAssertions;
using Ashlar.Core.Application.Analysis.Models;
using Ashlar.Core.Application.Analysis.Ports;
using Ashlar.Infrastructure.Analysis.BrickAnalyzer;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Analysis;

/// <summary>Tests for permission scope enforcer.</summary>
public class PermissionScopeEnforcerTests
{
    private readonly IPermissionScopeEnforcer _enforcer = new PermissionScopeEnforcer();

    [Fact]
    public void IsAllowed_ApplicationScope_ProcessStart_ReturnsFalse()
    {
        _enforcer.IsAllowed(PermissionScope.Application, "ProcessStart").Should().BeFalse();
    }

    [Fact]
    public void IsAllowed_OsScope_ProcessStart_ReturnsTrue()
    {
        _enforcer.IsAllowed(PermissionScope.OperatingSystem, "ProcessStart").Should().BeTrue();
    }

    [Fact]
    public void IsAllowed_ApplicationScope_FileRead_ReturnsTrue()
    {
        _enforcer.IsAllowed(PermissionScope.Application, "FileRead").Should().BeTrue();
    }
}
