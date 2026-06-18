using Nexo.Spike.S0.Models;

namespace Nexo.Spike.S0.Roles;

/// <summary>
/// Enforces structural role separation: test author never sees implementer prompts;
/// implementer never edits tests after RED freezes.
/// </summary>
public static class RoleScope
{
    public static RoleView ForTestAuthor(BrickSpec spec) => new(
        RoleName: "test-author",
        VisibleSpec: spec,
        CanEditTests: true,
        CanEditImplementation: false,
        VisibleTestAuthorPrompt: true,
        VisibleImplementerPrompt: false);

    public static RoleView ForImplementer(BrickSpec spec) => new(
        RoleName: "implementer",
        VisibleSpec: spec,
        CanEditTests: false,
        CanEditImplementation: true,
        VisibleTestAuthorPrompt: false,
        VisibleImplementerPrompt: true);
}

public sealed record RoleView(
    string RoleName,
    BrickSpec VisibleSpec,
    bool CanEditTests,
    bool CanEditImplementation,
    bool VisibleTestAuthorPrompt,
    bool VisibleImplementerPrompt);
