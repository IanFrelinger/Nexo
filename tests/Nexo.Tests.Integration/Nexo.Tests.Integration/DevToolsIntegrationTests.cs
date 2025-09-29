using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Nexo.Abstractions;
using Nexo.Runtime;
using Nexo.Tools.Dev;
using Nexo.Tools.Dev.Deltas;
using Nexo.Policies.Dev;
using Nexo.Agents.Dev;

namespace Nexo.Tests.Integration
{
    /// <summary>
    /// Integration tests for development tools and agents
    /// </summary>
    public class DevToolsIntegrationTests : IDisposable
    {
        private readonly string _testRoot;

        public DevToolsIntegrationTests()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), "NexoDevTests", Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_testRoot);
        }

        [Fact]
        public async Task RepoFsEnsureFileTool_CreateNewFile_ShouldSucceed()
        {
            // Arrange
            var tool = new RepoFsEnsureFileTool();
            var testPath = "test-file.txt";
            var testContent = "Hello, Nexo!";
            var args = System.Text.Json.JsonSerializer.Serialize(new { root = _testRoot, path = testPath, content = testContent });
            var call = new ToolCall("repo.fs.ensure_file", System.Text.Json.JsonDocument.Parse(args).RootElement);
            var snapshot = new WorldSnapshot(0, new System.Collections.Generic.Dictionary<string, object?>());

            // Act
            var result = await tool.InvokeAsync(call, snapshot, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Delta);
            Assert.True(File.Exists(Path.Combine(_testRoot, testPath)));
            Assert.Equal(testContent, await File.ReadAllTextAsync(Path.Combine(_testRoot, testPath)));
            
            var delta = (RepoDelta)result.Delta;
            Assert.Contains($"ensure:{testPath} created", delta.Log);
        }

        [Fact]
        public async Task RepoFsEnsureFileTool_FileExists_ShouldNotOverwrite()
        {
            // Arrange
            var tool = new RepoFsEnsureFileTool();
            var testPath = "existing-file.txt";
            var existingContent = "Existing content";
            var newContent = "New content";
            
            // Create existing file
            var existingFile = Path.Combine(_testRoot, testPath);
            await File.WriteAllTextAsync(existingFile, existingContent);
            
            var args = System.Text.Json.JsonSerializer.Serialize(new { root = _testRoot, path = testPath, content = newContent });
            var call = new ToolCall("repo.fs.ensure_file", System.Text.Json.JsonDocument.Parse(args).RootElement);
            var snapshot = new WorldSnapshot(0, new System.Collections.Generic.Dictionary<string, object?>());

            // Act
            var result = await tool.InvokeAsync(call, snapshot, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Delta);
            Assert.Equal(existingContent, await File.ReadAllTextAsync(Path.Combine(_testRoot, testPath)));
            
            var delta = (RepoDelta)result.Delta;
            Assert.Contains($"ensure:{testPath} exists", delta.Log);
        }

        [Fact]
        public async Task DevDirectorAgent_HealMode_ShouldExecuteCorrectly()
        {
            // Arrange
            var tools = new CapabilityRegistry();
            tools.Register(new DotnetBuildTool());
            tools.Register(new DotnetTestTool());
            tools.Register(new RepoFsWriteTool());
            tools.Register(new RepoFsSearchReplaceTool());
            tools.Register(new RepoFsEnsureFileTool());
            tools.Register(new RepoGitCommitTool());
            tools.Register(new DocsUpdateTool());

            var policies = new PolicyEngine(new IPolicy[] {
                new PathAllowlist(), 
                new MaxWriteSize(300_000), 
                new BuildMustPassBeforeCommit()
            });

            var agent = new DevDirectorAgent(DevMode.Heal);
            var host = new AgentHost(new[] { agent }, tools, policies);

            var snapshot = new WorldSnapshot(0, new System.Collections.Generic.Dictionary<string, object?>
            {
                ["RepoRoot"] = _testRoot,
                ["EditPath"] = "test.cs",
                ["FindText"] = "old",
                ["ReplaceText"] = "new",
                ["LastBuildOk"] = false,
                ["LastTestsOk"] = false
            });

            // Act
            var delta = await host.StepAsync(snapshot, CancellationToken.None);

            // Assert
            Assert.NotNull(delta);
            Assert.Contains("build:exit=", string.Join(" ", delta.Log));
            Assert.Contains("test:exit=", string.Join(" ", delta.Log));
        }

        [Fact]
        public async Task DevDirectorAgent_ExtendMode_ShouldExecuteCorrectly()
        {
            // Arrange
            var tools = new CapabilityRegistry();
            tools.Register(new DotnetBuildTool());
            tools.Register(new DotnetTestTool());
            tools.Register(new RepoFsWriteTool());
            tools.Register(new RepoFsSearchReplaceTool());
            tools.Register(new RepoFsEnsureFileTool());
            tools.Register(new RepoGitCommitTool());
            tools.Register(new DocsUpdateTool());

            var policies = new PolicyEngine(new IPolicy[] {
                new PathAllowlist(), 
                new MaxWriteSize(300_000), 
                new BuildMustPassBeforeCommit()
            });

            var agent = new DevDirectorAgent(DevMode.Extend);
            var host = new AgentHost(new[] { agent }, tools, policies);

            var snapshot = new WorldSnapshot(0, new System.Collections.Generic.Dictionary<string, object?>
            {
                ["RepoRoot"] = _testRoot,
                ["NewTestPath"] = "tests/FeatureXTests.cs",
                ["NewImplPath"] = "src/FeatureX.cs",
                ["LastBuildOk"] = false,
                ["LastTestsOk"] = false
            });

            // Act
            var delta = await host.StepAsync(snapshot, CancellationToken.None);

            // Assert
            Assert.NotNull(delta);
            Assert.Contains("ensure:", string.Join(" ", delta.Log));
            Assert.Contains("build:exit=", string.Join(" ", delta.Log));
            Assert.Contains("test:exit=", string.Join(" ", delta.Log));
        }

        [Fact]
        public void PathAllowlistPolicy_AllowedPath_ShouldApprove()
        {
            // Arrange
            var policy = new PathAllowlist();
            var args = System.Text.Json.JsonSerializer.Serialize(new { root = _testRoot, path = "src/test.cs", content = "test" });
            var call = new ToolCall("repo.fs.write", System.Text.Json.JsonDocument.Parse(args).RootElement);
            var snapshot = new WorldSnapshot(0, new System.Collections.Generic.Dictionary<string, object?>());

            // Act
            var result = policy.Approve(call, snapshot, out var reason);

            // Assert
            Assert.True(result);
            Assert.Equal("OK", reason);
        }

        [Fact]
        public void PathAllowlistPolicy_DisallowedPath_ShouldReject()
        {
            // Arrange
            var policy = new PathAllowlist();
            var args = System.Text.Json.JsonSerializer.Serialize(new { root = _testRoot, path = "forbidden/test.cs", content = "test" });
            var call = new ToolCall("repo.fs.write", System.Text.Json.JsonDocument.Parse(args).RootElement);
            var snapshot = new WorldSnapshot(0, new System.Collections.Generic.Dictionary<string, object?>());

            // Act
            var result = policy.Approve(call, snapshot, out var reason);

            // Assert
            Assert.False(result);
            Assert.Contains("Path not allowed", reason);
        }

        [Fact]
        public void MaxWriteSizePolicy_WithinLimit_ShouldApprove()
        {
            // Arrange
            var policy = new MaxWriteSize(1000);
            var content = new string('a', 500); // 500 characters
            var args = System.Text.Json.JsonSerializer.Serialize(new { root = _testRoot, path = "test.cs", content });
            var call = new ToolCall("repo.fs.write", System.Text.Json.JsonDocument.Parse(args).RootElement);
            var snapshot = new WorldSnapshot(0, new System.Collections.Generic.Dictionary<string, object?>());

            // Act
            var result = policy.Approve(call, snapshot, out var reason);

            // Assert
            Assert.True(result);
            Assert.Equal("OK", reason);
        }

        [Fact]
        public void MaxWriteSizePolicy_ExceedsLimit_ShouldReject()
        {
            // Arrange
            var policy = new MaxWriteSize(1000);
            var content = new string('a', 1500); // 1500 characters
            var args = System.Text.Json.JsonSerializer.Serialize(new { root = _testRoot, path = "test.cs", content });
            var call = new ToolCall("repo.fs.write", System.Text.Json.JsonDocument.Parse(args).RootElement);
            var snapshot = new WorldSnapshot(0, new System.Collections.Generic.Dictionary<string, object?>());

            // Act
            var result = policy.Approve(call, snapshot, out var reason);

            // Assert
            Assert.False(result);
            Assert.Contains("Write too large", reason);
        }

        [Fact]
        public void BuildMustPassBeforeCommitPolicy_GreenBuild_ShouldApprove()
        {
            // Arrange
            var policy = new BuildMustPassBeforeCommit();
            var args = System.Text.Json.JsonSerializer.Serialize(new { message = "test commit", root = _testRoot });
            var call = new ToolCall("repo.git.commit", System.Text.Json.JsonDocument.Parse(args).RootElement);
            var snapshot = new WorldSnapshot(0, new System.Collections.Generic.Dictionary<string, object?>
            {
                ["LastBuildOk"] = true,
                ["LastTestsOk"] = true
            });

            // Act
            var result = policy.Approve(call, snapshot, out var reason);

            // Assert
            Assert.True(result);
            Assert.Equal("OK", reason);
        }

        [Fact]
        public void BuildMustPassBeforeCommitPolicy_RedBuild_ShouldReject()
        {
            // Arrange
            var policy = new BuildMustPassBeforeCommit();
            var args = System.Text.Json.JsonSerializer.Serialize(new { message = "test commit", root = _testRoot });
            var call = new ToolCall("repo.git.commit", System.Text.Json.JsonDocument.Parse(args).RootElement);
            var snapshot = new WorldSnapshot(0, new System.Collections.Generic.Dictionary<string, object?>
            {
                ["LastBuildOk"] = false,
                ["LastTestsOk"] = true
            });

            // Act
            var result = policy.Approve(call, snapshot, out var reason);

            // Assert
            Assert.False(result);
            Assert.Contains("Cannot commit: build/tests not green", reason);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, true);
            }
        }
    }
}
