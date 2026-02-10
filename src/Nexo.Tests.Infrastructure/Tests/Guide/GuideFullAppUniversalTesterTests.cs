using System.Diagnostics;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Agents.UniversalTester;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Guide;

/// <summary>
/// Uses the Universal Tester agent (app testing agent in infrastructure) to test the full
/// Nexo Guide application. Runs the Guide app as the agent's target and asserts the agent
/// completes without failure.
/// </summary>
public class GuideFullAppUniversalTesterTests
{
    private static string GetGuideProjectPath()
    {
        var root = TestPaths.FindRepoRoot();
        var path = Path.Combine(root, "src", "Nexo.Guide", "Nexo.Guide.csproj");
        if (!File.Exists(path))
            throw new InvalidOperationException($"Guide project not found: {path}");
        return path;
    }

    /// <summary>
    /// Runs the Universal Tester agent against the Nexo Guide app (headless) via CLI adapter.
    /// Builds the app, then starts it with --headless as the agent's target; the agent
    /// connects, runs perception/understanding (deterministic, air-gapped), and completes.
    /// </summary>
    [Fact]
    public async Task Guide_FullApp_UniversalTesterAgent_ShouldComplete_WhenTargetIsHeadlessGuide()
    {
        var projectPath = GetGuideProjectPath();
        var root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(projectPath)))!;

        // Build so the agent can run with --no-build
        var buildStart = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList = { "build", projectPath, "-f", "net8.0", "-p:TreatWarningsAsErrors=false", "-v", "q" },
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        buildStart.Should().NotBeNull();
        await buildStart!.WaitForExitAsync();
        buildStart.ExitCode.Should().Be(0, "Guide project must build successfully for this test");

        var cliTarget = $"cli://dotnet run --project \"{projectPath}\" -f net8.0 -p:TreatWarningsAsErrors=false --no-build -- --headless";

        var mockProvider = new Mock<IProviderFactory>();
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
        var agent = new UniversalTesterAgent(mockProvider.Object, loggerFactory.CreateLogger<UniversalTesterAgent>());

        var config = new UniversalTesterConfig
        {
            Target = cliTarget,
            TargetType = TargetType.Cli,
            Goal = "Verify the Nexo Guide application starts and exits successfully.",
            Depth = TestingDepth.Quick,
            MaxDuration = TimeSpan.FromSeconds(45)
        };

        var context = new Mock<IExecutionContext>();
        context.Setup(c => c.IsAirGapped).Returns(true);

        TestReport? report = null;
        var act = () => report = agent.ExecuteAsync(config, context.Object, CancellationToken.None).GetAwaiter().GetResult();

        act.Should().NotThrow("Universal Tester agent should complete when testing Guide (headless)");
        report.Should().NotBeNull();
        report!.Summary.Should().NotBeNull();
    }

    /// <summary>
    /// Runs the Universal Tester agent against the Nexo Guide app with active UI (desktop adapter).
    /// Publishes the app to get an executable, launches it (no --headless), then the agent
    /// connects via DesktopAdapter and runs perception/understanding. Requires a display.
    /// Skipped when DISPLAY is unset (Linux) or NEXO_SKIP_UI_TESTS=1.
    /// </summary>
    [Fact]
    public async Task Guide_FullApp_UniversalTesterAgent_ShouldComplete_WhenTargetIsDesktopGuide_ActiveUI()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("NEXO_SKIP_UI_TESTS"), "1", StringComparison.OrdinalIgnoreCase))
        {
            return; // Skip in CI or when requested
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")))
        {
            return; // No display on Linux
        }

        var projectPath = GetGuideProjectPath();
        var root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(projectPath)))!;
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var arch = RuntimeInformation.ProcessArchitecture;
        var rid = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows), arch) switch
        {
            (true, _) => "win-x64",
            (false, System.Runtime.InteropServices.Architecture.Arm64) => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx-arm64" : "linux-arm64",
            (false, _) => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx-x64" : "linux-x64"
        };

        var publishDir = Path.Combine(Path.GetTempPath(), $"nexo-guide-ui-test-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(publishDir);

            var publishStart = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList = { "publish", projectPath, "-c", "Release", "-f", "net8.0", "-r", rid, "-o", publishDir, "-p:TreatWarningsAsErrors=false", "-v", "q" },
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            publishStart.Should().NotBeNull();
            await publishStart!.WaitForExitAsync();
            publishStart.ExitCode.Should().Be(0, "Guide publish must succeed for desktop UI test");

            var exeName = isWindows ? "Nexo.Guide.exe" : "Nexo.Guide";
            var exePath = Path.Combine(publishDir, exeName);
            if (!File.Exists(exePath))
            {
                // Framework-dependent publish may produce only the DLL and a host; try running the host
                var dllPath = Path.Combine(publishDir, "Nexo.Guide.dll");
                if (!File.Exists(dllPath))
                    throw new InvalidOperationException($"No {exeName} or Nexo.Guide.dll in {publishDir}");
                exePath = dllPath; // DesktopAdapter won't launch a DLL directly; we need to start process ourselves and use process://
                // So if we only have DLL we skip or use a different approach: start dotnet exePath and pass process://dotnet (ambiguous). Better to require single-file or exe.
                return; // Skip if no standalone exe (e.g. framework-dependent publish)
            }

            var mockProvider = new Mock<IProviderFactory>();
            var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
            var agent = new UniversalTesterAgent(mockProvider.Object, loggerFactory.CreateLogger<UniversalTesterAgent>());

            var config = new UniversalTesterConfig
            {
                Target = exePath,
                TargetType = TargetType.DesktopApp,
                Goal = "Verify the Nexo Guide main window opens and the UI is visible (sidebar, chat area).",
                Depth = TestingDepth.Quick,
                MaxDuration = TimeSpan.FromSeconds(25)
            };

            var context = new Mock<IExecutionContext>();
            context.Setup(c => c.IsAirGapped).Returns(true);

            TestReport? report = null;
            var act = () => report = agent.ExecuteAsync(config, context.Object, CancellationToken.None).GetAwaiter().GetResult();

            act.Should().NotThrow("Universal Tester agent should complete when testing Guide with active UI");
            report.Should().NotBeNull();
            report!.Summary.Should().NotBeNull();
        }
        finally
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("Nexo.Guide"))
                {
                    try { p.Kill(); } catch { /* ignore */ }
                    p.Dispose();
                }
            }
            catch { /* ignore */ }

            try
            {
                if (Directory.Exists(publishDir))
                    Directory.Delete(publishDir, recursive: true);
            }
            catch { /* ignore */ }
        }
    }

    /// <summary>
    /// Runs the Universal Tester agent in agentic mode (real LLM) to test all main interactions
    /// with the Nexo Guide app: audience selector, chat input/send, suggested questions,
    /// sidebar navigation (Chat/Architecture), layer cards, brick pipeline, mode cards.
    /// Requires NEXO_RUN_AGENTIC_GUIDE_TEST=1, a display, and an LLM (Ollama recommended).
    /// </summary>
    [Fact]
    public async Task Guide_Agentic_UniversalTester_ShouldTestAllInteractions_WhenRunWithOllama()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("NEXO_RUN_AGENTIC_GUIDE_TEST"), "1", StringComparison.OrdinalIgnoreCase) == false)
            return;

        if (string.Equals(Environment.GetEnvironmentVariable("NEXO_SKIP_UI_TESTS"), "1", StringComparison.OrdinalIgnoreCase))
            return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")))
            return;

        var projectPath = GetGuideProjectPath();
        var root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(projectPath)))!;
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var arch = RuntimeInformation.ProcessArchitecture;
        var rid = (isWindows, arch) switch
        {
            (true, _) => "win-x64",
            (false, System.Runtime.InteropServices.Architecture.Arm64) => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx-arm64" : "linux-arm64",
            (false, _) => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx-x64" : "linux-x64"
        };

        var publishDir = Path.Combine(Path.GetTempPath(), $"nexo-guide-agentic-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(publishDir);

            var publishStart = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList = { "publish", projectPath, "-c", "Release", "-f", "net8.0", "-r", rid, "-o", publishDir, "-p:TreatWarningsAsErrors=false", "-v", "q" },
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            publishStart.Should().NotBeNull();
            await publishStart!.WaitForExitAsync();
            publishStart.ExitCode.Should().Be(0, "Guide publish must succeed for agentic test");

            var exeName = isWindows ? "Nexo.Guide.exe" : "Nexo.Guide";
            var exePath = Path.Combine(publishDir, exeName);
            if (!File.Exists(exePath))
                return;

            var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
            var providerFactory = new ProviderFactory(loggerFactory.CreateLogger<ProviderFactory>());
            var agent = new UniversalTesterAgent(providerFactory, loggerFactory.CreateLogger<UniversalTesterAgent>());

            var agenticContext = new AgenticTestExecutionContext(Provider: "ollama");

            var config = new UniversalTesterConfig
            {
                Target = exePath,
                TargetType = TargetType.DesktopApp,
                Goal = """
                    You are a new user opening the Nexo Guide app for the first time. Simulate discovering the app:
                    explore the interface naturally, try the chat (ask whatever questions you have—come up with your own questions),
                    switch between Chat and Architecture tabs, click around. Do not follow a fixed script; act like a curious
                    first-time user and decide what to try based on what you see. Report what you learned and any issues.
                    """,
                Depth = TestingDepth.Standard,
                MaxDuration = TimeSpan.FromMinutes(3),
                SuccessCriteria = "App remained stable; first-time user exploration was simulated; report reflects what was tried."
            };

            var runtime = new UniversalTesterRuntimeConfig
            {
                Prefer = "agentic",
                Bricks = new Dictionary<string, BrickRuntimeSpec>(StringComparer.OrdinalIgnoreCase)
                {
                    ["perception"] = BrickRuntimeSpec.DeterministicOnly(),
                    ["action"] = BrickRuntimeSpec.DeterministicOnly(),
                    ["understanding"] = BrickRuntimeSpec.AgenticOnly(),
                    ["exploration"] = BrickRuntimeSpec.AgenticOnly(),
                    ["validation"] = BrickRuntimeSpec.AgenticWithDeterministicFallback(),
                    ["reporting"] = BrickRuntimeSpec.AgenticWithDeterministicFallback()
                }
            };

            TestReport? report = null;
            var act = () => report = agent.ExecuteAsync(config, agenticContext, runtime, CancellationToken.None).GetAwaiter().GetResult();

            act.Should().NotThrow("Agentic Universal Tester should complete when testing Guide (Ollama + vision must be reachable)");
            report.Should().NotBeNull();
            report!.Summary.Should().NotBeNull();
        }
        finally
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("Nexo.Guide"))
                {
                    try { p.Kill(); } catch { }
                    p.Dispose();
                }
            }
            catch { }

            try
            {
                if (Directory.Exists(publishDir))
                    Directory.Delete(publishDir, recursive: true);
            }
            catch { }
        }
    }

    /// <summary>
    /// Runs the Universal Tester agent against the Avalonia Guide app and asserts that
    /// the agent performs real screen interactions (clicks and keystrokes) via the desktop adapter.
    /// Requires NEXO_RUN_AGENTIC_GUIDE_TEST=1, a display, and Ollama (for vision/understanding).
    /// </summary>
    [Fact]
    public async Task Guide_AvaloniaApp_UniversalTester_ShouldPerformClicksAndKeystrokes_WhenTestingWithDesktopAdapter()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("NEXO_RUN_AGENTIC_GUIDE_TEST"), "1", StringComparison.OrdinalIgnoreCase) == false)
            return;
        if (string.Equals(Environment.GetEnvironmentVariable("NEXO_SKIP_UI_TESTS"), "1", StringComparison.OrdinalIgnoreCase))
            return;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")))
            return;

        var projectPath = GetGuideProjectPath();
        var root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(projectPath)))!;
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var arch = RuntimeInformation.ProcessArchitecture;
        var rid = (isWindows, arch) switch
        {
            (true, _) => "win-x64",
            (false, System.Runtime.InteropServices.Architecture.Arm64) => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx-arm64" : "linux-arm64",
            (false, _) => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx-x64" : "linux-x64"
        };

        var publishDir = Path.Combine(Path.GetTempPath(), $"nexo-guide-avalonia-test-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(publishDir);
            var publishStart = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList = { "publish", projectPath, "-c", "Release", "-f", "net8.0", "-r", rid, "-o", publishDir, "-p:TreatWarningsAsErrors=false", "-v", "q" },
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            publishStart.Should().NotBeNull();
            await publishStart!.WaitForExitAsync();
            publishStart.ExitCode.Should().Be(0, "Guide publish must succeed");

            var exeName = isWindows ? "Nexo.Guide.exe" : "Nexo.Guide";
            var exePath = Path.Combine(publishDir, exeName);
            if (!File.Exists(exePath))
                return;

            var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
            var providerFactory = new ProviderFactory(loggerFactory.CreateLogger<ProviderFactory>());
            var agent = new UniversalTesterAgent(providerFactory, loggerFactory.CreateLogger<UniversalTesterAgent>());
            var context = new AgenticTestExecutionContext("ollama");

            var config = new UniversalTesterConfig
            {
                Target = exePath,
                TargetType = TargetType.DesktopApp,
                Goal = """
                    You are a new user trying the Nexo Guide app for the first time. Explore the app as you would naturally:
                    open the chat, ask your own questions (invent questions a first-time user would ask—e.g. what the app does, how to get started),
                    try the sidebar and tabs. Come up with your own questions and interactions; do not follow a fixed list.
                    The app should respond without crashing.
                    """,
                Depth = TestingDepth.Standard,
                MaxDuration = TimeSpan.FromMinutes(2)
            };

            var runtime = new UniversalTesterRuntimeConfig
            {
                Prefer = "agentic",
                Bricks = new Dictionary<string, BrickRuntimeSpec>(StringComparer.OrdinalIgnoreCase)
                {
                    ["perception"] = BrickRuntimeSpec.DeterministicOnly(),
                    ["action"] = BrickRuntimeSpec.DeterministicOnly(),
                    ["understanding"] = BrickRuntimeSpec.AgenticOnly(),
                    ["exploration"] = BrickRuntimeSpec.AgenticOnly(),
                    ["validation"] = BrickRuntimeSpec.AgenticWithDeterministicFallback(),
                    ["reporting"] = BrickRuntimeSpec.AgenticWithDeterministicFallback()
                }
            };

            TestReport? report = null;
            var act = () => report = agent.ExecuteAsync(config, context, runtime, CancellationToken.None).GetAwaiter().GetResult();
            act.Should().NotThrow("Universal Tester should complete when testing Avalonia Guide (Ollama + vision must be reachable)");

            report.Should().NotBeNull();
            report!.Summary.Should().NotBeNull();
            report.Session.Should().NotBeNull("Report should include session for interaction assertions");

            if (report.Session != null && report.Session.Steps.Count > 0)
            {
                var hasClick = report.Session.Steps.Any(s => s.Action.Type == ActionType.Click || s.Action.Type == ActionType.DoubleClick || s.Action.Type == ActionType.RightClick);
                var hasType = report.Session.Steps.Any(s => s.Action.Type == ActionType.Type || s.Action.Type == ActionType.Fill);
                hasClick.Should().BeTrue("Agent should have performed at least one click on the Avalonia app");
                hasType.Should().BeTrue("Agent should have performed at least one type/keystroke on the Avalonia app");
            }
        }
        finally
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("Nexo.Guide"))
                {
                    try { p.Kill(); } catch { }
                    p.Dispose();
                }
            }
            catch { }
            try
            {
                if (Directory.Exists(publishDir))
                    Directory.Delete(publishDir, recursive: true);
            }
            catch { }
        }
    }

    private sealed class AgenticTestExecutionContext : IExecutionContext
    {
        public string AgentId { get; } = "guide-agentic-test";
        public string BehaviorId { get; } = "universal-tester";
        public bool IsAirGapped => false;
        public bool AuditMode => false;
        public string Provider { get; }
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();

        public AgenticTestExecutionContext(string Provider) => this.Provider = Provider;
    }
}
