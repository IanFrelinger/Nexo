using Ashlar.Abstractions;

namespace Ashlar.Tools.Dev;

/// <summary>
/// Supplies the game layer's tools to the background-agent toolbox.
///
/// <para>This closes a gap left by the extraction. <c>RepoFsToolboxFactory</c> used to do
/// <c>tools.Register(new TileMapRenderTool())</c> in both CreateMinimal and
/// CreateWithBuildTest, hardwiring a game tool into the kernel's default toolbox. Those two
/// lines were removed on the grounds that the game package would supply the tool through
/// <see cref="IToolSource"/> instead — but nothing actually implemented that interface, so
/// between then and now <c>repo.tile_map.render</c> was reachable by no one. This is the
/// implementation that was assumed to exist.</para>
///
/// <para>Register it in the consuming application:</para>
/// <code>
/// services.AddSingleton&lt;IToolSource, GameToolSource&gt;();
/// </code>
/// <para><c>SelfExtendRunnerAdapter</c> resolves <c>IEnumerable&lt;IToolSource&gt;</c> and
/// passes the flattened result to the toolbox factory as <c>extraTools</c>, so registration
/// is all that is required — no kernel change.</para>
///
/// <para>NOT COMPILED. The extracted tree has no project file yet, so nothing here has been
/// built or tested. Treat it as a starting point, not as working code.</para>
/// </summary>
public sealed class GameToolSource : IToolSource
{
    /// <inheritdoc />
    public IReadOnlyList<ITool> GetTools() => new ITool[] { new TileMapRenderTool() };
}
