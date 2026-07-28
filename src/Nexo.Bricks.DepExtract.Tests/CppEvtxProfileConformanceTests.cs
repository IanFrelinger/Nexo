using Microsoft.Extensions.DependencyInjection;
using Nexo.Agents.TestKit;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Domain.Bricks.Ports;

namespace Nexo.Bricks.DepExtract.Tests;

/// <summary>
/// Part (e): the C++/EVTX profile against the SAME shared conformance suite the SQL
/// profile subclasses. Two profiles with nothing in common at the toolchain level —
/// one builds containers, the other never leaves the process — held to one contract.
/// </summary>
public sealed class CppEvtxProfileConformanceTests
    : AgentProfileConformanceTests<CppEvtxProfileFactory>
{
    /// <summary>A minimal pull-style parser surface the scaffold path accepts.</summary>
    private const string ParserHeader = """
        #pragma once
        #include <cstdint>
        #include <string>

        struct Record {
            double timestamp = 0;
            uint8_t type = 0;
        };

        class SampleStream {
        public:
            explicit SampleStream(const std::string& path);
            bool advance(Record& out);
            uint64_t position() const;
        };
        """;

    /// <inheritdoc />
    protected override string ExpectedTargetId => CppEvtxProfileFactory.TargetId;

    /// <inheritdoc />
    protected override GenerationRequest OfflineRequest(OfflineAgentEnvironment environment) =>
        new()
        {
            TargetId = CppEvtxProfileFactory.TargetId,
            Grounding = ParserHeader
        };

    /// <summary>
    /// The C++ profile reaches for a sandbox runner and a file set. Offline, those
    /// edges are TestKit fakes — the profile must still resolve to a usable shape.
    /// </summary>
    protected override IServiceProvider BuildServices(OfflineAgentEnvironment environment)
    {
        var services = new ServiceCollection();
        AddOfflineDefaults(services);
        services.AddSingleton<ISandboxedCommandRunner>(
            new FakeSandboxedCommandRunner(FakeSandboxedCommandRunner.Success()));
        services.AddSingleton<IManagedFileSet>(new FakeManagedFileSet());
        return services.BuildServiceProvider();
    }
}
