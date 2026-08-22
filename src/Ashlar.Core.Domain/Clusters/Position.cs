using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Core.Domain.Clusters;

/// <summary>
/// Position for visualization.
/// </summary>
public class Position
{
    public double X { get; init; } = 0;
    public double Y { get; init; } = 0;
    
    public Position() { }
    
    public Position(double x, double y)
    {
        X = x;
        Y = y;
    }
}
