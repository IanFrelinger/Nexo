namespace Nexo.GeoTerrain.Processing;

/// <summary>
/// Basic terrain erosion simulation using simplified thermal and hydraulic erosion.
/// </summary>
public static class TerrainErosion
{
    /// <summary>
    /// Applies thermal erosion (slope-based smoothing) to an elevation grid.
    /// Simulates material sliding down steep slopes.
    /// </summary>
    public static ElevationGrid ApplyThermalErosion(
        ElevationGrid grid,
        float talusAngle = 0.5f,
        int iterations = 1)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(grid);
#else
        if (grid == null)
            throw new ArgumentNullException(nameof(grid));
#endif

        var heights = grid.CloneHeightsMeters();
        var talusThreshold = Math.Tan(talusAngle * Math.PI / 180.0) * grid.Spacing.MetersX;

        for (var iter = 0; iter < iterations; iter++)
        {
            for (var y = 1; y < grid.Height - 1; y++)
            {
                for (var x = 1; x < grid.Width - 1; x++)
                {
                    var h = heights[y * grid.Width + x];
                    if (float.IsNaN(h)) continue;

                    var hL = heights[y * grid.Width + (x - 1)];
                    var hR = heights[y * grid.Width + (x + 1)];
                    var hD = heights[(y - 1) * grid.Width + x];
                    var hU = heights[(y + 1) * grid.Width + x];

                    // Check if slope exceeds talus angle
                    var maxDiff = 0.0f;
                    var transfer = 0.0f;

                    if (!float.IsNaN(hL))
                    {
                        var diff = h - hL;
                        if (diff > talusThreshold && diff > maxDiff)
                        {
                            maxDiff = diff;
                            transfer = (diff - (float)talusThreshold) * 0.5f;
                        }
                    }

                    if (!float.IsNaN(hR))
                    {
                        var diff = h - hR;
                        if (diff > talusThreshold && diff > maxDiff)
                        {
                            maxDiff = diff;
                            transfer = (diff - (float)talusThreshold) * 0.5f;
                        }
                    }

                    if (!float.IsNaN(hD))
                    {
                        var diff = h - hD;
                        if (diff > talusThreshold && diff > maxDiff)
                        {
                            maxDiff = diff;
                            transfer = (diff - (float)talusThreshold) * 0.5f;
                        }
                    }

                    if (!float.IsNaN(hU))
                    {
                        var diff = h - hU;
                        if (diff > talusThreshold && diff > maxDiff)
                        {
                            maxDiff = diff;
                            transfer = (diff - (float)talusThreshold) * 0.5f;
                        }
                    }

                    if (transfer > 0.001f)
                    {
                        heights[y * grid.Width + x] -= transfer;
                        // Distribute to lowest neighbor
                        var lowest = float.PositiveInfinity;
                        var lowestX = x;
                        var lowestY = y;

                        if (!float.IsNaN(hL) && hL < lowest) { lowest = hL; lowestX = x - 1; lowestY = y; }
                        if (!float.IsNaN(hR) && hR < lowest) { lowest = hR; lowestX = x + 1; lowestY = y; }
                        if (!float.IsNaN(hD) && hD < lowest) { lowest = hD; lowestX = x; lowestY = y - 1; }
                        if (!float.IsNaN(hU) && hU < lowest) { lowest = hU; lowestX = x; lowestY = y + 1; }

                        if (lowest < float.PositiveInfinity)
                        {
                            heights[lowestY * grid.Width + lowestX] += transfer;
                        }
                    }
                }
            }
        }

        return new ElevationGrid(grid.Width, grid.Height, grid.Bounds, grid.Spacing, heights);
    }

    /// <summary>
    /// Applies basic hydraulic erosion simulation.
    /// Simulates water flow and sediment transport.
    /// </summary>
    public static ElevationGrid ApplyHydraulicErosion(
        ElevationGrid grid,
        float rainAmount = 0.01f,
        float evaporationRate = 0.5f,
        float sedimentCapacity = 0.1f,
        int iterations = 10)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(grid);
#else
        if (grid == null)
            throw new ArgumentNullException(nameof(grid));
#endif

        var heights = grid.CloneHeightsMeters();
        var water = new float[heights.Length];
        var sediment = new float[heights.Length];

        // Initialize water
        for (var i = 0; i < water.Length; i++)
        {
            if (!float.IsNaN(heights[i]))
            {
                water[i] = rainAmount;
            }
        }

        for (var iter = 0; iter < iterations; iter++)
        {
            // Flow water downhill
            for (var y = 1; y < grid.Height - 1; y++)
            {
                for (var x = 1; x < grid.Width - 1; x++)
                {
                    var idx = y * grid.Width + x;
                    if (float.IsNaN(heights[idx]) || water[idx] < 0.0001f) continue;

                    var h = heights[idx] + water[idx];
                    var hL = GetHeightWithWater(heights, water, grid.Width, x - 1, y);
                    var hR = GetHeightWithWater(heights, water, grid.Width, x + 1, y);
                    var hD = GetHeightWithWater(heights, water, grid.Width, x, y - 1);
                    var hU = GetHeightWithWater(heights, water, grid.Width, x, y + 1);

                    var totalDiff = 0.0f;
                    var flowL = Math.Max(0, h - hL);
                    var flowR = Math.Max(0, h - hR);
                    var flowD = Math.Max(0, h - hD);
                    var flowU = Math.Max(0, h - hU);
                    totalDiff = flowL + flowR + flowD + flowU;

                    if (totalDiff > 0.0001f)
                    {
                        var flowRate = 0.25f; // Flow rate factor
                        var flowAmount = Math.Min(water[idx], totalDiff * flowRate);

                        if (flowL > 0) { var f = flowAmount * (flowL / totalDiff); water[idx] -= f; water[(y * grid.Width + (x - 1))] += f; }
                        if (flowR > 0) { var f = flowAmount * (flowR / totalDiff); water[idx] -= f; water[(y * grid.Width + (x + 1))] += f; }
                        if (flowD > 0) { var f = flowAmount * (flowD / totalDiff); water[idx] -= f; water[((y - 1) * grid.Width + x)] += f; }
                        if (flowU > 0) { var f = flowAmount * (flowU / totalDiff); water[idx] -= f; water[((y + 1) * grid.Width + x)] += f; }
                    }

                    // Erode and deposit sediment
                    var slope = CalculateSlope(heights, grid.Width, x, y);
                    var capacity = sedimentCapacity * slope * water[idx];

                    if (sediment[idx] < capacity)
                    {
                        var erosion = Math.Min(capacity - sediment[idx], 0.01f);
                        heights[idx] -= erosion;
                        sediment[idx] += erosion;
                    }
                    else
                    {
                        var deposition = (sediment[idx] - capacity) * 0.1f;
                        heights[idx] += deposition;
                        sediment[idx] -= deposition;
                    }

                    // Evaporate water
                    water[idx] *= (1.0f - evaporationRate);
                }
            }
        }

        return new ElevationGrid(grid.Width, grid.Height, grid.Bounds, grid.Spacing, heights);
    }

    private static float GetHeightWithWater(float[] heights, float[] water, int width, int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= heights.Length / width)
            return float.PositiveInfinity;

        var idx = y * width + x;
        if (float.IsNaN(heights[idx]))
            return float.PositiveInfinity;

        return heights[idx] + water[idx];
    }

    private static float CalculateSlope(float[] heights, int width, int x, int y)
    {
        var h = heights[y * width + x];
        if (float.IsNaN(h)) return 0.0f;

        var maxSlope = 0.0f;
        var neighbors = new[] { (x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1) };

        foreach (var (nx, ny) in neighbors)
        {
            if (nx < 0 || nx >= width || ny < 0 || ny >= heights.Length / width)
                continue;

            var nh = heights[ny * width + nx];
            if (!float.IsNaN(nh))
            {
                var slope = Math.Abs(h - nh);
                if (slope > maxSlope) maxSlope = slope;
            }
        }

        return maxSlope;
    }
}
