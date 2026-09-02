using FluentAssertions;
using Ashlar.CLI.Commands;
using Ashlar.Manifest.Ledger;
using Ashlar.Manifest.Signing;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// The kernel's two LOSSY ledger refusals — a chain shorter than its anchor, and a ledger whose
/// entries are gone from under a live anchor — deliberately cannot be cleared by
/// <c>ashlar verify</c>, because clearing them there would let a fresh valid-looking head be written
/// over a history someone deleted. The deliberate acceptance of that loss lives in
/// <c>InstanceLedger.ReanchorAsync</c>, and until <c>ashlar ledger reanchor</c> existed the refusal
/// could only name a C# method. An operator standing at a terminal cannot run a method: a refusal
/// naming an unrunnable fix is the defect this whole pass exists to remove, so these pin that the
/// command the refusal names is a command that runs, and that it does what the message promises.
/// </summary>
public sealed class LedgerReanchorCommandTests : IDisposable
{
    private readonly string _root;
    private readonly string _keyDir;
    private readonly string _stateRoot;
    private readonly SigningIdentity _signer;

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public LedgerReanchorCommandTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ashlar-ledgercmd-" + Guid.NewGuid().ToString("N"));
        _keyDir = Path.Combine(_root, "keys");
        _stateRoot = Path.Combine(_root, "project", ".ashlar");
        Directory.CreateDirectory(_stateRoot);
        _signer = OperatorKey.Generate(_keyDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private DirectoryInfo Project => new(Path.Combine(_root, "project"));

    private InstanceLedger Ledger() => new(_stateRoot);

    private async Task AppendAsync(int count)
    {
        var ledger = Ledger();
        for (var i = 0; i < count; i++)
        {
            await ledger.AppendVerificationAsync(
                _signer, $"subject-{i}", verified: true, [], Now.AddMinutes(i));
        }
    }

    /// <summary>Deletes the newest entry — what truncation leaves behind, and what the anchor detects.</summary>
    private void TruncateNewestEntry()
    {
        var dir = Path.Combine(_stateRoot, "ledger");
        var newest = Directory.EnumerateFiles(dir, "*.json")
            .Where(f => !f.EndsWith(".json.tmp", StringComparison.Ordinal))
            .OrderBy(f => f, StringComparer.Ordinal)
            .Last();
        File.Delete(newest);
    }

    [Fact]
    public async Task The_refusal_names_a_command_and_that_command_exists()
    {
        await AppendAsync(3);
        TruncateNewestEntry();

        var refusal = Assert.Throws<InvalidOperationException>(() => Ledger().VerifyChain());

        refusal.Message.Should().Contain("ashlar ledger reanchor",
            "a refusal has to name a fix the person reading it can type, not only a kernel identifier");
        refusal.Message.Should().Contain("ReanchorAsync",
            "the API name stays for anyone driving the kernel directly");
        refusal.Message.Should().Contain("restore",
            "restoring is still the repair that KEEPS the history, and is still named first");
    }

    [Fact]
    public async Task Status_prints_the_refusal_verbatim_and_exits_65()
    {
        await AppendAsync(3);
        TruncateNewestEntry();
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = LedgerCommand.Status(Project, stdout, stderr);

        exit.Should().Be(65);
        stderr.ToString().Should().Contain("ashlar ledger reanchor");
    }

    [Fact]
    public async Task Status_on_an_intact_ledger_is_clean()
    {
        await AppendAsync(2);
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = LedgerCommand.Status(Project, stdout, stderr);

        exit.Should().Be(0, stderr.ToString());
        stdout.ToString().Should().Contain("2 signed entries");
    }

    [Fact]
    public void Status_on_a_project_that_was_never_certified_says_so_rather_than_refusing()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = LedgerCommand.Status(Project, stdout, stderr);

        exit.Should().Be(0, "absence is not corruption");
        stdout.ToString().Should().Contain("never been certified");
    }

    [Fact]
    public async Task Reanchor_without_yes_shows_what_would_be_accepted_and_changes_nothing()
    {
        await AppendAsync(3);
        TruncateNewestEntry();
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = await LedgerCommand.ReanchorAsync(Project, _keyDir, yes: false, CancellationToken.None, stdout, stderr);

        exit.Should().Be(1);
        var text = stderr.ToString();
        text.Should().Contain("--yes");
        text.Should().Contain("does NOT recover anything",
            "the operator has to understand that this accepts the loss rather than undoing it");
        text.Should().Contain("restore .ashlar/ledger");
        Assert.Throws<InvalidOperationException>(() => Ledger().VerifyChain());
    }

    [Fact]
    public async Task Reanchor_with_yes_accepts_the_shortened_history_and_the_ledger_then_verifies()
    {
        await AppendAsync(3);
        TruncateNewestEntry();
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = await LedgerCommand.ReanchorAsync(Project, _keyDir, yes: true, CancellationToken.None, stdout, stderr);

        exit.Should().Be(0, stderr.ToString());
        stdout.ToString().Should().Contain("what was accepted",
            "a re-anchor that does not record what it swallowed is the erasure it exists to make deliberate");
        // The named fix must actually fix it — that is the whole point of naming it.
        var chain = Ledger().VerifyChain();
        chain.Count.Should().Be(2);
    }

    [Fact]
    public async Task Reanchor_refuses_a_forged_entry_even_with_yes()
    {
        await AppendAsync(2);
        var dir = Path.Combine(_stateRoot, "ledger");
        var first = Directory.EnumerateFiles(dir, "*.json").OrderBy(f => f, StringComparer.Ordinal).First();
        File.WriteAllText(first, File.ReadAllText(first).Replace("subject-0", "subject-x", StringComparison.Ordinal));
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = await LedgerCommand.ReanchorAsync(Project, _keyDir, yes: true, CancellationToken.None, stdout, stderr);

        exit.Should().Be(65, "a re-anchor accepts a SHORTER history, never a forged one");
        stderr.ToString().Should().Contain("re-anchor refused");
    }

    [Fact]
    public async Task Reanchor_on_an_intact_ledger_does_nothing_and_says_why()
    {
        await AppendAsync(2);
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = await LedgerCommand.ReanchorAsync(Project, _keyDir, yes: true, CancellationToken.None, stdout, stderr);

        exit.Should().Be(0);
        stdout.ToString().Should().Contain("nothing to re-anchor");
    }

    [Fact]
    public async Task Reanchor_with_no_operator_key_refuses_and_names_keys_init()
    {
        await AppendAsync(3);
        TruncateNewestEntry();
        var empty = Path.Combine(_root, "no-keys");
        Directory.CreateDirectory(empty);
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = await LedgerCommand.ReanchorAsync(Project, empty, yes: true, CancellationToken.None, stdout, stderr);

        exit.Should().Be(1);
        stderr.ToString().Should().Contain("ashlar keys init",
            "a signed act with no key is refused, and the refusal names how to get one");
    }
}
