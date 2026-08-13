using FluentAssertions;
using Nexo.Core.Application.Generation;
using Nexo.Core.Domain.Bricks.Ports;
using Xunit;

namespace Nexo.Tests.Application.Tests.Generation;

/// <summary>
/// Constraint-manifest pre-gate (trust-loop R3.5 second use): every configured rule is
/// enforced deterministically on candidate ingest, failure reasons restate the violated
/// instruction verbatim (R3.4), and the rendered instructions cover every rule — the
/// same object drives prompt and gate, so they cannot drift.
/// </summary>
public sealed class BrickConstraintManifestValidatorTests
{
    private static readonly BrickConstraintManifest Manifest = new()
    {
        RequiredBaseType = "DomainBrick",
        AllowedUsings = new[] { "Nexo.Core.Domain.Bricks", "Nexo.Core.Domain.Execution" },
        RequiredNamespace = "Nexo.Certified.DamageResolver",
        RequiredClassNameSuffix = "Brick",
        ForbiddenApiTokens = new[] { "DateTime.Now", "Random" },
        RequiredDeclarations = new[] { "ExecuteAsync" }
    };

    private const string CompliantSource = """
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Certified.DamageResolver;

public sealed class ResolveDamageBrick : DomainBrick
{
    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var output = new BrickOutput();
        return Task.FromResult(output);
    }
}
""";

    [Fact]
    public async Task CompliantCandidate_Passes()
    {
        var (ok, reason) = await Validate(CompliantSource);

        ok.Should().BeTrue(reason);
        reason.Should().BeNull();
    }

    [Fact]
    public async Task ForbiddenApi_IsRejected_RestatingTheInstructionVerbatim()
    {
        var (ok, reason) = await Validate(CompliantSource.Replace(
            "var output = new BrickOutput();",
            "var now = DateTime.Now; var output = new BrickOutput();"));

        ok.Should().BeFalse();
        reason.Should().Contain(Manifest.ForbiddenApiInstruction("DateTime.Now"),
            "R3.4: the violated constraint must be restated verbatim");
    }

    [Fact]
    public async Task NonAllowlistedUsing_IsRejected()
    {
        var (ok, reason) = await Validate("using System;\n" + CompliantSource);

        ok.Should().BeFalse();
        reason.Should().Contain("using System;");
        reason.Should().Contain(Manifest.UsingsInstruction());
    }

    [Fact]
    public async Task WrongNamespace_IsRejected()
    {
        var (ok, reason) = await Validate(CompliantSource.Replace(
            "namespace Nexo.Certified.DamageResolver;",
            "namespace Nexo.Somewhere.Else;"));

        ok.Should().BeFalse();
        reason.Should().Contain(Manifest.NamespaceInstruction());
    }

    [Fact]
    public async Task MissingNamespace_IsRejected_NeverInferred()
    {
        var (ok, reason) = await Validate(CompliantSource.Replace(
            "namespace Nexo.Certified.DamageResolver;",
            ""));

        ok.Should().BeFalse();
        reason.Should().Contain("No namespace declaration found");
    }

    [Fact]
    public async Task WrongBaseType_IsRejected()
    {
        var (ok, reason) = await Validate(CompliantSource.Replace(
            "public sealed class ResolveDamageBrick : DomainBrick",
            "public sealed class ResolveDamageBrick"));

        ok.Should().BeFalse();
        reason.Should().Contain(Manifest.BaseTypeInstruction());
    }

    [Fact]
    public async Task WrongClassNameSuffix_IsRejected()
    {
        var (ok, reason) = await Validate(CompliantSource.Replace("ResolveDamageBrick", "ResolveDamage"));

        ok.Should().BeFalse();
        reason.Should().Contain(Manifest.ClassNameInstruction());
    }

    [Fact]
    public async Task MissingRequiredDeclaration_IsRejected()
    {
        var (ok, reason) = await Validate(CompliantSource.Replace("ExecuteAsync", "RunAsync"));

        ok.Should().BeFalse();
        reason.Should().Contain(Manifest.RequiredDeclarationInstruction("ExecuteAsync"));
    }

    [Fact]
    public async Task EmptyCandidate_IsRejected()
    {
        var (ok, reason) = await Validate("");

        ok.Should().BeFalse();
        reason.Should().Contain("empty");
    }

    [Fact]
    public async Task MultipleViolations_AreAllReported()
    {
        var (ok, reason) = await Validate("using System;\nnamespace Wrong;\nclass Thing { }");

        ok.Should().BeFalse();
        reason.Should().Contain(Manifest.UsingsInstruction());
        reason.Should().Contain(Manifest.NamespaceInstruction());
        reason.Should().Contain(Manifest.BaseTypeInstruction());
        reason.Should().Contain(Manifest.ClassNameInstruction());
    }

    [Fact]
    public void RenderedInstructions_CoverEveryConfiguredRule()
    {
        var instructions = Manifest.RenderInstructions();

        instructions.Should().Contain(Manifest.BaseTypeInstruction());
        instructions.Should().Contain(Manifest.UsingsInstruction());
        instructions.Should().Contain(Manifest.NamespaceInstruction());
        instructions.Should().Contain(Manifest.ClassNameInstruction());
        foreach (var token in Manifest.ForbiddenApiTokens)
            instructions.Should().Contain(Manifest.ForbiddenApiInstruction(token));
        foreach (var declaration in Manifest.RequiredDeclarations)
            instructions.Should().Contain(Manifest.RequiredDeclarationInstruction(declaration));
    }

    [Fact]
    public async Task UnconfiguredRules_AreNeitherRenderedNorEnforced()
    {
        var empty = new BrickConstraintManifest();

        empty.RenderInstructions().Should().BeEmpty();
        var validator = new BrickConstraintManifestValidator(empty);
        var (ok, _) = await validator.ValidateAsync(
            new GeneratedArtifact { Content = "anything at all" }, CancellationToken.None);
        ok.Should().BeTrue("a manifest with no rules constrains nothing");
    }

    private static async Task<(bool ok, string? reason)> Validate(string content)
    {
        var validator = new BrickConstraintManifestValidator(Manifest);
        return await validator.ValidateAsync(new GeneratedArtifact { Content = content }, CancellationToken.None);
    }
}
