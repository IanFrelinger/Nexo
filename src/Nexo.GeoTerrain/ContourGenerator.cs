namespace Nexo.GeoTerrain;

/// <summary>
/// Deterministic contour (isoline) generator using marching squares.
/// </summary>
public static class ContourGenerator
{
    public static IReadOnlyList<ContourLine> Generate(ElevationGrid grid, ContourGenerationOptions? options = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(grid);
#else
        if (grid is null) throw new ArgumentNullException(nameof(grid));
#endif
        options ??= new ContourGenerationOptions();
        if (options.IntervalMeters <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), options.IntervalMeters, "IntervalMeters must be > 0.");
        if (options.EndpointQuantizationMeters <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), options.EndpointQuantizationMeters, "EndpointQuantizationMeters must be > 0.");

        var (min, max) = FindMinMax(grid, options.TreatNoDataAsZero);
        if (min is null || max is null) return Array.Empty<ContourLine>();

        var minLevel = options.MinElevationMeters ?? min.Value;
        var maxLevel = options.MaxElevationMeters ?? max.Value;
        if (maxLevel < minLevel) return Array.Empty<ContourLine>();

        // Start at the first "nice" level >= minLevel.
        var interval = options.IntervalMeters;
        var start = Math.Ceiling(minLevel / interval) * interval;

        var results = new List<ContourLine>();
        for (var level = start; level <= maxLevel + (interval * 0.5); level += interval)
        {
            var lines = GenerateForLevel(grid, (float)level, options);
            results.AddRange(lines);
        }

        return results;
    }

    private static (double? Min, double? Max) FindMinMax(ElevationGrid grid, bool treatNoDataAsZero)
    {
        double? min = null;
        double? max = null;

        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var v = grid.GetHeightMeters(x, y);
                if (float.IsNaN(v))
                {
                    if (treatNoDataAsZero) v = 0f;
                    else continue;
                }

                var dv = (double)v;
                min = min is null ? dv : Math.Min(min.Value, dv);
                max = max is null ? dv : Math.Max(max.Value, dv);
            }
        }

        return (min, max);
    }

    private readonly struct Segment
    {
        public Segment(Vector3 a, Vector3 b)
        {
            A = a;
            B = b;
        }

        public Vector3 A { get; }
        public Vector3 B { get; }
    }

    private static IReadOnlyList<ContourLine> GenerateForLevel(ElevationGrid grid, float levelMeters, ContourGenerationOptions options)
    {
        var segments = new List<Segment>();

        var w = grid.Width;
        var h = grid.Height;
        var sx = (float)grid.Spacing.MetersX;
        var sy = (float)grid.Spacing.MetersY;
        var yConst = levelMeters * options.VerticalScale;

        for (var y = 0; y < h - 1; y++)
        {
            for (var x = 0; x < w - 1; x++)
            {
                var v0 = grid.GetHeightMeters(x, y);         // bottom-left
                var v1 = grid.GetHeightMeters(x + 1, y);     // bottom-right
                var v2 = grid.GetHeightMeters(x + 1, y + 1); // top-right
                var v3 = grid.GetHeightMeters(x, y + 1);     // top-left

                if (float.IsNaN(v0) || float.IsNaN(v1) || float.IsNaN(v2) || float.IsNaN(v3))
                {
                    if (!options.TreatNoDataAsZero)
                        throw new InvalidOperationException($"NoData/NaN elevation in contour cell at ({x},{y}).");
                    if (float.IsNaN(v0)) v0 = 0f;
                    if (float.IsNaN(v1)) v1 = 0f;
                    if (float.IsNaN(v2)) v2 = 0f;
                    if (float.IsNaN(v3)) v3 = 0f;
                }

                var caseIndex = 0;
                if (v0 >= levelMeters) caseIndex |= 1;
                if (v1 >= levelMeters) caseIndex |= 2;
                if (v2 >= levelMeters) caseIndex |= 4;
                if (v3 >= levelMeters) caseIndex |= 8;

                if (caseIndex == 0 || caseIndex == 15) continue;

                var x0 = x * sx;
                var x1 = (x + 1) * sx;
                var z0 = y * sy;
                var z1 = (y + 1) * sy;

                // Corner positions in local meters; Y is constant per contour level.
                var p0 = new Vector3(x0, yConst, z0);
                var p1 = new Vector3(x1, yConst, z0);
                var p2 = new Vector3(x1, yConst, z1);
                var p3 = new Vector3(x0, yConst, z1);

                // Edge intersection points: 0=bottom(p0-p1), 1=right(p1-p2), 2=top(p3-p2), 3=left(p0-p3)
                var ep = new Vector3[4];
                var has = new bool[4];

                Vector3 EdgePoint(int edge)
                {
                    if (has[edge]) return ep[edge];
                    Vector3 a, b;
                    float va, vb;
                    switch (edge)
                    {
                        case 0: a = p0; b = p1; va = v0; vb = v1; break;
                        case 1: a = p1; b = p2; va = v1; vb = v2; break;
                        case 2: a = p3; b = p2; va = v3; vb = v2; break;
                        case 3: a = p0; b = p3; va = v0; vb = v3; break;
                        default: throw new ArgumentOutOfRangeException(nameof(edge));
                    }

                    var pt = Interp(a, va, b, vb, levelMeters, yConst);
                    ep[edge] = pt;
                    has[edge] = true;
                    return pt;
                }

                void AddSeg(int eA, int eB)
                {
                    var aPt = EdgePoint(eA);
                    var bPt = EdgePoint(eB);
                    segments.Add(new Segment(aPt, bPt));
                }

                switch (caseIndex)
                {
                    case 1: AddSeg(3, 0); break;
                    case 2: AddSeg(0, 1); break;
                    case 3: AddSeg(3, 1); break;
                    case 4: AddSeg(1, 2); break;
                    case 5:
                    {
                        var center = (v0 + v1 + v2 + v3) * 0.25f;
                        if (center >= levelMeters)
                        {
                            // Connect around low corners
                            AddSeg(0, 1);
                            AddSeg(2, 3);
                        }
                        else
                        {
                            // Two separate high regions
                            AddSeg(3, 0);
                            AddSeg(1, 2);
                        }
                        break;
                    }
                    case 6: AddSeg(0, 2); break;
                    case 7: AddSeg(3, 2); break;
                    case 8: AddSeg(2, 3); break;
                    case 9: AddSeg(0, 2); break;
                    case 10:
                    {
                        var center = (v0 + v1 + v2 + v3) * 0.25f;
                        if (center >= levelMeters)
                        {
                            // Two separate high regions
                            AddSeg(3, 0);
                            AddSeg(1, 2);
                        }
                        else
                        {
                            // Connect around low corners
                            AddSeg(0, 1);
                            AddSeg(2, 3);
                        }
                        break;
                    }
                    case 11: AddSeg(1, 2); break;
                    case 12: AddSeg(3, 1); break;
                    case 13: AddSeg(0, 1); break;
                    case 14: AddSeg(3, 0); break;
                }
            }
        }

        return Stitch(grid, levelMeters, options, segments);
    }

    private static Vector3 Interp(Vector3 a, float va, Vector3 b, float vb, float level, float yConst)
    {
        if (va == vb)
        {
            // Degenerate: pick midpoint.
            var mx = (a.X + b.X) * 0.5f;
            var mz = (a.Z + b.Z) * 0.5f;
            return new Vector3(mx, yConst, mz);
        }

        var t = (level - va) / (vb - va);
        if (t < 0f) t = 0f;
        if (t > 1f) t = 1f;

        var x = a.X + (b.X - a.X) * t;
        var z = a.Z + (b.Z - a.Z) * t;
        return new Vector3(x, yConst, z);
    }

    private static IReadOnlyList<ContourLine> Stitch(ElevationGrid grid, float levelMeters, ContourGenerationOptions options, List<Segment> segments)
    {
        if (segments.Count == 0) return Array.Empty<ContourLine>();

        var q = options.EndpointQuantizationMeters;

        long Key(Vector3 p)
        {
            var xi = (long)Math.Round(p.X / q);
            var zi = (long)Math.Round(p.Z / q);
            unchecked
            {
                return (xi * 73856093L) ^ (zi * 19349663L);
            }
        }

        var polylines = new List<List<Vector3>>();
        var endpointToPolyline = new Dictionary<long, (int Polyline, bool IsStart)>();

        void RegisterEndpoints(int idx)
        {
            var pl = polylines[idx];
            if (pl.Count < 2) return;
            endpointToPolyline[Key(pl[0])] = (idx, true);
            endpointToPolyline[Key(pl[pl.Count - 1])] = (idx, false);
        }

        void UnregisterEndpoints(int idx)
        {
            var pl = polylines[idx];
            if (pl.Count < 2) return;
            endpointToPolyline.Remove(Key(pl[0]));
            endpointToPolyline.Remove(Key(pl[pl.Count - 1]));
        }

        static void EnsureEnd(List<Vector3> pl, Vector3 end)
        {
            var last = pl[pl.Count - 1];
            if (last != end) pl.Add(end);
        }

        foreach (var seg in segments)
        {
            var a = seg.A;
            var b = seg.B;
            var ka = Key(a);
            var kb = Key(b);

            var hasA = endpointToPolyline.TryGetValue(ka, out var ma);
            var hasB = endpointToPolyline.TryGetValue(kb, out var mb);

            if (!hasA && !hasB)
            {
                var pl = new List<Vector3>(2) { a, b };
                var idx = polylines.Count;
                polylines.Add(pl);
                RegisterEndpoints(idx);
                continue;
            }

            if (hasA && !hasB)
            {
                var idx = ma.Polyline;
                var pl = polylines[idx];
                UnregisterEndpoints(idx);
                if (ma.IsStart)
                {
                    pl.Reverse();
                }
                EnsureEnd(pl, b);
                RegisterEndpoints(idx);
                continue;
            }

            if (!hasA && hasB)
            {
                var idx = mb.Polyline;
                var pl = polylines[idx];
                UnregisterEndpoints(idx);
                if (!mb.IsStart)
                {
                    pl.Reverse();
                }
                pl.Insert(0, a);
                RegisterEndpoints(idx);
                continue;
            }

            // Both endpoints match existing polylines.
            var idxA = ma.Polyline;
            var idxB = mb.Polyline;
            if (idxA == idxB)
            {
                // Segment closes/duplicates within same polyline; ensure connection and move on.
                var pl = polylines[idxA];
                UnregisterEndpoints(idxA);
                if (ma.IsStart) pl.Reverse();
                EnsureEnd(pl, b);
                RegisterEndpoints(idxA);
                continue;
            }

            // Merge two different polylines.
            var plA = polylines[idxA];
            var plB = polylines[idxB];
            UnregisterEndpoints(idxA);
            UnregisterEndpoints(idxB);

            // Orient A so it ends at 'a'
            if (ma.IsStart) plA.Reverse();
            // Orient B so it starts at 'b'
            if (!mb.IsStart) plB.Reverse();

            // Connect A -> B (skip duplicate start point)
            EnsureEnd(plA, b);
            for (var i = 1; i < plB.Count; i++)
                plA.Add(plB[i]);

            polylines[idxB] = new List<Vector3>(0); // tombstone
            RegisterEndpoints(idxA);
        }

        var lines = new List<ContourLine>();
        for (var i = 0; i < polylines.Count; i++)
        {
            var pl = polylines[i];
            if (pl.Count < 2) continue;
            lines.Add(new ContourLine
            {
                ElevationMeters = levelMeters,
                Points = pl
            });
        }

        return lines;
    }
}

