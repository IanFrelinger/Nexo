using Nexo.GeoTerrain;
using Nexo.GeoVector.Geometry;
using Nexo.GeoVector.Models;

namespace Nexo.GeoVector.Spatial;

/// <summary>
/// Quadtree spatial index for efficient spatial queries on vector features.
/// Improves performance for large datasets by reducing linear search to logarithmic.
/// </summary>
public sealed class Quadtree
{
    private readonly Node _root;
    private readonly int _maxDepth;
    private readonly int _maxItemsPerNode;

    public Quadtree(GeoBounds bounds, int maxDepth = 10, int maxItemsPerNode = 10)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(bounds);
#else
        if (bounds == null) throw new ArgumentNullException(nameof(bounds));
#endif
        bounds.Validate();
        if (maxDepth < 1) throw new ArgumentOutOfRangeException(nameof(maxDepth), "maxDepth must be at least 1");
        if (maxItemsPerNode < 1) throw new ArgumentOutOfRangeException(nameof(maxItemsPerNode), "maxItemsPerNode must be at least 1");

        _root = new Node(bounds);
        _maxDepth = maxDepth;
        _maxItemsPerNode = maxItemsPerNode;
    }

    /// <summary>
    /// Inserts a feature into the quadtree.
    /// </summary>
    public void Insert(GeoFeature feature)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(feature);
#else
        if (feature == null) throw new ArgumentNullException(nameof(feature));
#endif
        Insert(_root, feature, 0);
    }

    /// <summary>
    /// Inserts multiple features into the quadtree.
    /// </summary>
    public void InsertRange(IEnumerable<GeoFeature> features)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(features);
#else
        if (features == null) throw new ArgumentNullException(nameof(features));
#endif
        foreach (var feature in features)
        {
            Insert(feature);
        }
    }

    /// <summary>
    /// Queries features that intersect with the given bounds.
    /// </summary>
    public IReadOnlyList<GeoFeature> Query(GeoBounds bounds)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(bounds);
#else
        if (bounds == null) throw new ArgumentNullException(nameof(bounds));
#endif
        bounds.Validate();

        var results = new List<GeoFeature>();
        Query(_root, bounds, results);
        return results;
    }

    /// <summary>
    /// Queries features that contain or are near the given point.
    /// </summary>
    public IReadOnlyList<GeoFeature> Query(GeoPoint point, double toleranceDegrees = 0.001)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(point);
#else
        if (point == null) throw new ArgumentNullException(nameof(point));
#endif

        var bounds = new GeoBounds
        {
            MinLatitude = new Latitude(point.Latitude.Degrees - toleranceDegrees),
            MinLongitude = new Longitude(point.Longitude.Degrees - toleranceDegrees),
            MaxLatitude = new Latitude(point.Latitude.Degrees + toleranceDegrees),
            MaxLongitude = new Longitude(point.Longitude.Degrees + toleranceDegrees)
        };
        
        return Query(bounds);
    }

    private void Insert(Node node, GeoFeature feature, int depth)
    {
        if (!Intersects(node.Bounds, feature.Geometry))
        {
            return; // Feature doesn't intersect this node
        }

        if (node.IsLeaf)
        {
            node.Items.Add(feature);
            
            // Split if needed
            if (node.Items.Count > _maxItemsPerNode && depth < _maxDepth)
            {
                Split(node, depth);
            }
        }
        else
        {
            // Insert into appropriate child
            foreach (var child in node.Children!)
            {
                Insert(child, feature, depth + 1);
            }
        }
    }

    private void Split(Node node, int depth)
    {
        var bounds = node.Bounds;
        var centerLat = (bounds.MinLatitude.Degrees + bounds.MaxLatitude.Degrees) / 2.0;
        var centerLon = (bounds.MinLongitude.Degrees + bounds.MaxLongitude.Degrees) / 2.0;

        var children = new Node[4];
        children[0] = new Node(new GeoBounds
        {
            MinLatitude = new Latitude(bounds.MinLatitude.Degrees),
            MinLongitude = new Longitude(bounds.MinLongitude.Degrees),
            MaxLatitude = new Latitude(centerLat),
            MaxLongitude = new Longitude(centerLon)
        }); // SW
        children[1] = new Node(new GeoBounds
        {
            MinLatitude = new Latitude(centerLat),
            MinLongitude = new Longitude(bounds.MinLongitude.Degrees),
            MaxLatitude = new Latitude(bounds.MaxLatitude.Degrees),
            MaxLongitude = new Longitude(centerLon)
        }); // NW
        children[2] = new Node(new GeoBounds
        {
            MinLatitude = new Latitude(bounds.MinLatitude.Degrees),
            MinLongitude = new Longitude(centerLon),
            MaxLatitude = new Latitude(centerLat),
            MaxLongitude = new Longitude(bounds.MaxLongitude.Degrees)
        }); // SE
        children[3] = new Node(new GeoBounds
        {
            MinLatitude = new Latitude(centerLat),
            MinLongitude = new Longitude(centerLon),
            MaxLatitude = new Latitude(bounds.MaxLatitude.Degrees),
            MaxLongitude = new Longitude(bounds.MaxLongitude.Degrees)
        }); // NE

        node.Children = children;
        node.IsLeaf = false;

        // Redistribute items to children
        var itemsToRedistribute = node.Items.ToList();
        node.Items.Clear();

        foreach (var item in itemsToRedistribute)
        {
            foreach (var child in children)
            {
                Insert(child, item, depth + 1);
            }
        }
    }

    private static void Query(Node node, GeoBounds bounds, List<GeoFeature> results)
    {
        if (!Intersects(node.Bounds, bounds))
        {
            return; // No intersection
        }

        if (node.IsLeaf)
        {
            // Check all items in leaf node
            foreach (var item in node.Items)
            {
                if (Intersects(bounds, item.Geometry))
                {
                    results.Add(item);
                }
            }
        }
        else
        {
            // Query children
            foreach (var child in node.Children!)
            {
                Query(child, bounds, results);
            }
        }
    }

    private static bool Intersects(GeoBounds bounds, IGeoGeometry geometry)
    {
        // Quick bounds check
        if (geometry is GeoPolygon poly)
        {
            return poly.Points.Any(bounds.Contains);
        }
        if (geometry is GeoPolyline line)
        {
            return line.Points.Any(bounds.Contains);
        }
        return true; // Unknown geometry type, include it
    }

    private static bool Intersects(GeoBounds bounds1, GeoBounds bounds2)
    {
        return bounds1.MinLongitude.Degrees <= bounds2.MaxLongitude.Degrees &&
               bounds1.MaxLongitude.Degrees >= bounds2.MinLongitude.Degrees &&
               bounds1.MinLatitude.Degrees <= bounds2.MaxLatitude.Degrees &&
               bounds1.MaxLatitude.Degrees >= bounds2.MinLatitude.Degrees;
    }

    private sealed class Node
    {
        public GeoBounds Bounds { get; }
        public List<GeoFeature> Items { get; }
        public Node[]? Children { get; set; }
        public bool IsLeaf { get; set; }

        public Node(GeoBounds bounds)
        {
            Bounds = bounds;
            Items = new List<GeoFeature>();
            IsLeaf = true;
        }
    }
}
