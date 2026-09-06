using System.Text.Json;
using FluentAssertions;
using Ashlar.Certification.Contracts;
using Ashlar.Infrastructure.Certification;
using NSec.Cryptography;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>C1b: replay the adversarial corpus. Verdict drift is a regression.</summary>
[Trait("Category", "Certification")]
public sealed class AdversarialCorpusTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public void Ledger_ListsEveryFixtureDirectory()
    {
        var root = CorpusRoot();
        var ledger = JsonSerializer.Deserialize<LedgerDto>(
            File.ReadAllText(Path.Combine(root, "ledger.json")), JsonOptions)
            ?? throw new InvalidOperationException("ledger.json missing");
        var dirs = Directory.GetDirectories(Path.Combine(root, "fixtures"))
            .Select(Path.GetFileName)
            .Where(n => n is not null && !n.StartsWith('_'))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        var ids = ledger.Fixtures.Select(f => f.Id).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        ids.Should().Equal(dirs);
        foreach (var id in ids)
        {
            File.Exists(Path.Combine(root, "fixtures", id!, "expect.json")).Should().BeTrue(id);
        }
    }

    [Fact]
    public async Task EveryFixture_MatchesExpectJson()
    {
        var root = CorpusRoot();
        var failures = new List<string>();
        foreach (var expectPath in Directory.GetFiles(Path.Combine(root, "fixtures"), "expect.json", SearchOption.AllDirectories))
        {
            var expect = JsonSerializer.Deserialize<ExpectDto>(File.ReadAllText(expectPath), JsonOptions)
                ?? throw new InvalidOperationException(expectPath);
            var fixtureDir = Path.GetDirectoryName(expectPath)!;
            try
            {
                await ReplayAsync(fixtureDir, expect);
            }
            catch (Exception ex)
            {
                failures.Add($"{expect.Id}: {ex.Message}");
            }
        }

        failures.Should().BeEmpty(string.Join("\n", failures));
    }

    private static async Task ReplayAsync(string fixtureDir, ExpectDto expect)
    {
        var projectDir = Path.Combine(fixtureDir, "project");
        var source = Directory.GetFiles(projectDir, "*.cs").First();
        var sourceText = await File.ReadAllTextAsync(source).ConfigureAwait(false);

        if (expect.Phase == "discover")
        {
            var artifact = GateEmittedArtifactCompiler.Compile(
                sourceText,
                BrickCertificationProjectLoader.DefaultCompilationReferences());
            artifact.BrickTypeName.Should().Contain(expect.ReasonContains);
            return;
        }

        var witness = Directory.GetFiles(projectDir, "*.json")
            .FirstOrDefault(p => Path.GetFileName(p).Contains("witness", StringComparison.OrdinalIgnoreCase))
            ?? Path.Combine(CorpusRoot(), "fixtures", "_shared", "witness.json");

        if (expect.Phase == "load")
        {
            var act = async () => await BrickCertificationProjectLoader.LoadAsync(projectDir, witness);
            var ex = await act.Should().ThrowAsync<InvalidOperationException>();
            ex.Which.Message.Should().Contain(expect.ReasonContains);
            return;
        }

        var request = await BrickCertificationProjectLoader.LoadAsync(projectDir, witness);
        var (privateKey, _) = CreateEd25519Key();
        var decision = await new CertificationGate(new CertificationRecordSigner(ed25519PrivateKeyBase64: privateKey)).CertifyAsync(request);
        if (expect.Expect == "admit")
        {
            decision.Admitted.Should().BeTrue(decision.Record.Reason);
            decision.Record.Inputs.Should().Contain(i => i.Kind == CertificationInputKinds.GateEmittedArtifact);
            request.EmittedArtifact.Should().NotBeNull();
        }
        else
        {
            decision.Admitted.Should().BeFalse();
            decision.Record.Reason.Should().Contain(expect.ReasonContains);
        }
    }

    private static string CorpusRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "tests", "adversarial-corpus");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("tests/adversarial-corpus not found");
    }

    private sealed class LedgerDto
    {
        public List<LedgerRow> Fixtures { get; set; } = [];
    }

    private sealed class LedgerRow
    {
        public string Id { get; set; } = "";
    }

    private sealed class ExpectDto
    {
        public string Id { get; set; } = "";
        public string Phase { get; set; } = "";
        public string Expect { get; set; } = "";
        public string ReasonContains { get; set; } = "";
    }

    private static (string PrivateKeyBase64, string PublicKeyBase64) CreateEd25519Key()
    {
        using var key = Key.Create(
            SignatureAlgorithm.Ed25519,
            new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
        return (
            Convert.ToBase64String(key.Export(KeyBlobFormat.RawPrivateKey)),
            Convert.ToBase64String(key.PublicKey.Export(KeyBlobFormat.RawPublicKey)));
    }
}
