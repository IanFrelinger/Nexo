using FluentAssertions;
using Nexo.Bricks.DepExtract.Profile;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

/// <summary>Port (d): C++ drafter + grounding + KnownContractVocabulary provenance.</summary>
public sealed class CppDrafterProfileTests
{
    [Fact]
    public void AssembleGrounding_BundlesEntryFiles()
    {
        var dir = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "gnd-" + Guid.NewGuid().ToString("n"))).FullName;
        try
        {
            File.WriteAllText(Path.Combine(dir, "Parser.hpp"), "class Parser { void advance(); };\n");
            var text = CppAdapterPrompt.AssembleGrounding(dir, new[] { "Parser.hpp" });
            text.Should().Contain("// ==== Parser.hpp ====");
            text.Should().Contain("class Parser");
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void DeterministicDrafter_AttachesScaffoldProvenance()
    {
        var grounding = """
            class SimpleStream {
            public:
                explicit SimpleStream(const std::string& path);
                bool advance();
                long last_type() const;
            };
            """;
        var drafter = new CppDeterministicDrafter();
        drafter.TryDraft(new GenerationRequest
        {
            TargetId = "cpp-evtx",
            Grounding = grounding,
            Overrides = new Dictionary<string, object> { ["entryInclude"] = "SimpleStream.hpp" }
        }, out var artifact).Should().BeTrue();

        artifact.Should().NotBeNull();
        artifact!.Content.Should().Contain("CustomEventReader");
        artifact.Content.Should().Contain("#include \"SimpleStream.hpp\"");
        artifact.Provenance.Should().NotBeNull();
        artifact.Provenance!.Strategy.Should().Be(GenerationStrategy.Templated);
        artifact.Provenance.Assumptions.Should().NotBeEmpty();
        artifact.Provenance.Confidence.Should().NotBeNull();
        artifact.Provenance.RequiresHumanReview.Should().BeTrue();
    }

    [Fact]
    public async Task ArtifactDrafter_FlagsUnsupportedReferences_InProvenance()
    {
        var grounding = """
            // ==== Parser.hpp ====
            class RealParser {
            public:
                explicit RealParser(const std::string& path);
                bool advance();
            };
            """;
        var fakeModel = new FakeModel("""
            #pragma once
            #include "processing.hpp"
            namespace fileproc {
            class CustomEventReader : public IEventReader {
            public:
                explicit CustomEventReader(const std::filesystem::path& path) { (void)path; }
                void set_requested_fields(const std::vector<std::string>& names) override { (void)names; }
                bool decodes_named_fields() const override { return false; }
                bool next(Event& out, size_t max_depth) override {
                    (void)max_depth;
                    parser_.inventTotallyFakeApi();
                    out.data.clear();
                    return false;
                }
            private:
                RealParser parser_{""};
                bool can_resume() const override { return false; }
                uint64_t position() const override { return 0; }
                void resume_at(uint64_t off) override { (void)off; }
            };
            inline void use_custom_reader() {}
            }
            """);

        var drafter = new CppArtifactDrafter(fakeModel, new CppArtifactDrafterOptions());
        var artifact = await drafter.DraftAsync(
            new GenerationRequest { TargetId = "cpp-evtx", Grounding = grounding },
            Array.Empty<string>(),
            CancellationToken.None);

        artifact.Provenance.Should().NotBeNull();
        artifact.Provenance!.UnsupportedReferences.Should().Contain("inventTotallyFakeApi");
        artifact.Provenance.Confidence.Should().NotBeNull();
        artifact.Provenance.Strategy.Should().Be(GenerationStrategy.Model);
    }

    [Fact]
    public async Task ArtifactDrafter_FeedsCompileFeedback_IntoUserPrompt()
    {
        string? capturedUser = null;
        var fake = new FakeModel(
            "#pragma once\n#include \"processing.hpp\"\nclass CustomEventReader : public IEventReader {};\ninline void use_custom_reader(){}\n",
            (sys, user) => capturedUser = user);

        var drafter = new CppArtifactDrafter(fake, new CppArtifactDrafterOptions());
        await drafter.DraftAsync(
            new GenerationRequest { Grounding = "class P { void advance(); };" },
            new[] { "error: 'advance' was not declared" },
            CancellationToken.None);

        capturedUser.Should().Contain("Previous compile/validation feedback");
        capturedUser.Should().Contain("was not declared");
        capturedUser.Should().Contain("class P");
    }

    [Fact]
    public void DockerSandboxProvider_WritesQtShimsIntoScratchShimDir()
    {
        var poco = CreateMinimalPoco();
        var parser = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "par-" + Guid.NewGuid().ToString("n"))).FullName;
        var scratch = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "sc-" + Guid.NewGuid().ToString("n"))).FullName;
        try
        {
            File.WriteAllText(
                Path.Combine(parser, "Entry.hpp"),
                "#include <QMessageBox>\nclass Entry { void advance(); };\n");

            var provider = new DockerSandboxProvider(new DockerSandboxProviderOptions
            {
                PocoContext = poco,
                ParserDir = parser
            });

            provider.BuildSpec(
                new GeneratedArtifact { Content = "#pragma once\nclass CustomEventReader {};\n" },
                scratch);

            var shimDir = Path.Combine(scratch, "qt-shims");
            Directory.Exists(shimDir).Should().BeTrue();
            Directory.EnumerateFiles(shimDir, "*", SearchOption.AllDirectories)
                .Any(f => Path.GetFileName(f).Contains("QMessageBox", StringComparison.OrdinalIgnoreCase))
                .Should().BeTrue();
            // Never write shims into the parser source tree.
            Directory.EnumerateFiles(parser, "QMessageBox", SearchOption.AllDirectories).Should().BeEmpty();
        }
        finally
        {
            try { Directory.Delete(poco, true); } catch { /* ignore */ }
            try { Directory.Delete(parser, true); } catch { /* ignore */ }
            try { Directory.Delete(scratch, true); } catch { /* ignore */ }
        }
    }

    private static string CreateMinimalPoco()
    {
        var root = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "poco-d-" + Guid.NewGuid().ToString("n"))).FullName;
        Directory.CreateDirectory(Path.Combine(root, "common"));
        File.WriteAllText(Path.Combine(root, "common", "processing.hpp"), "#pragma once\n");
        return root;
    }

    private sealed class FakeModel : ICppModelClient
    {
        private readonly string _response;
        private readonly Action<string, string>? _onCall;

        public FakeModel(string response, Action<string, string>? onCall = null)
        {
            _response = response;
            _onCall = onCall;
        }

        public Task<string> CompleteAsync(
            string systemPrompt,
            string userPrompt,
            LLMConfig config,
            CancellationToken cancellationToken = default)
        {
            _onCall?.Invoke(systemPrompt, userPrompt);
            return Task.FromResult(_response);
        }
    }
}
