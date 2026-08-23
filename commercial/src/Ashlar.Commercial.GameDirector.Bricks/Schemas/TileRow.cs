namespace GameDirector.Bricks.Schemas;

/// <summary>Tile row value.</summary>
/// <param name="X">X.</param>
/// <param name="Y">Y.</param>
/// <param name="TraversalOptions">Traversal options.</param>
public sealed record TileRow(int X, int Y, int TraversalOptions);
