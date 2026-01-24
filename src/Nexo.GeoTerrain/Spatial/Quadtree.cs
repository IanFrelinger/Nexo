using System.Collections;

namespace Nexo.GeoTerrain.Spatial;

/// <summary>
/// A quadtree for efficient spatial queries on geographic bounds.
/// Used for accelerating elevation grid queries, vector feature filtering, and instance placement.
/// </summary>
public sealed class Quadtree<T> : IEnumerable<T>
{
    private readonly int _maxDepth;
    private readonly int _maxItemsPerNode;
    private QuadtreeNode? _root;

    public Quadtree(GeoBounds bounds, int maxDepth = 10, int maxItemsPerNode = 10)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(bounds);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDepth, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxItemsPerNode, 1);
#else
        if (bounds == null) throw new ArgumentNullException(nameof(bounds));
        if (maxDepth < 1) throw new ArgumentOutOfRangeException(nameof(maxDepth));
        if (maxItemsPerNode < 1) throw new ArgumentOutOfRangeException(nameof(maxItemsPerNode));
#endif
        bounds.Validate();

        _maxDepth = maxDepth;
        _maxItemsPerNode = maxItemsPerNode;
        _root = new QuadtreeNode(bounds, 0);
    }

    /// <summary>
    /// Inserts an item with its associated bounds.
    /// </summary>
    public void Insert(T item, GeoBounds itemBounds)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(itemBounds);
#else
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (itemBounds == null) throw new ArgumentNullException(nameof(itemBounds));
#endif
        itemBounds.Validate();

        if (_root == null)
            throw new InvalidOperationException("Quadtree root is null.");

        InsertRecursive(_root!, item, itemBounds);
    }

    private void InsertRecursive(QuadtreeNode node, T item, GeoBounds itemBounds)
    {
        if (!node.Bounds.Intersects(itemBounds))
            return;

        if (node.IsLeaf)
        {
            node.Items.Add((item, itemBounds));
            if (node.Items.Count > _maxItemsPerNode && node.Depth < _maxDepth)
            {
                Subdivide(node, _maxDepth, _maxItemsPerNode);
            }
        }
        else
        {
            // Insert into all children that intersect
            foreach (var child in node.Children!)
            {
                InsertRecursive(child, item, itemBounds);
            }
        }
    }

    private static void Subdivide(QuadtreeNode node, int maxDepth, int maxItemsPerNode)
    {
        var bounds = node.Bounds;
        var midLat = (bounds.MinLatitude.Degrees + bounds.MaxLatitude.Degrees) / 2.0;
        var midLon = (bounds.MinLongitude.Degrees + bounds.MaxLongitude.Degrees) / 2.0;

        var sw = new GeoBounds
        {
            MinLatitude = bounds.MinLatitude,
            MaxLatitude = new Latitude(midLat),
            MinLongitude = bounds.MinLongitude,
            MaxLongitude = new Longitude(midLon)
        };

        var se = new GeoBounds
        {
            MinLatitude = bounds.MinLatitude,
            MaxLatitude = new Latitude(midLat),
            MinLongitude = new Longitude(midLon),
            MaxLongitude = bounds.MaxLongitude
        };

        var nw = new GeoBounds
        {
            MinLatitude = new Latitude(midLat),
            MaxLatitude = bounds.MaxLatitude,
            MinLongitude = bounds.MinLongitude,
            MaxLongitude = new Longitude(midLon)
        };

        var ne = new GeoBounds
        {
            MinLatitude = new Latitude(midLat),
            MaxLatitude = bounds.MaxLatitude,
            MinLongitude = new Longitude(midLon),
            MaxLongitude = bounds.MaxLongitude
        };

        node.Children = new[]
        {
            new QuadtreeNode(sw, node.Depth + 1),
            new QuadtreeNode(se, node.Depth + 1),
            new QuadtreeNode(nw, node.Depth + 1),
            new QuadtreeNode(ne, node.Depth + 1)
        };

        // Redistribute items to children
        var itemsToRedistribute = node.Items.ToList();
        node.Items.Clear();

        foreach (var (item, itemBounds) in itemsToRedistribute)
        {
            foreach (var child in node.Children)
            {
                if (child.Bounds.Intersects(itemBounds))
                {
                    child.Items.Add((item, itemBounds));
                }
            }
        }
    }

    /// <summary>
    /// Queries all items that intersect the given bounds.
    /// </summary>
    public IReadOnlyList<T> Query(GeoBounds queryBounds)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(queryBounds);
#else
        if (queryBounds == null) throw new ArgumentNullException(nameof(queryBounds));
#endif
        queryBounds.Validate();

        var results = new List<T>();
        if (_root != null)
        {
            QueryRecursive(_root, queryBounds, results);
        }
        return results;
    }

    private static void QueryRecursive(QuadtreeNode node, GeoBounds queryBounds, List<T> results)
    {
        if (!node.Bounds.Intersects(queryBounds))
            return;

        if (node.IsLeaf)
        {
            foreach (var (item, itemBounds) in node.Items)
            {
                if (queryBounds.Intersects(itemBounds))
                {
                    results.Add(item);
                }
            }
        }
        else
        {
            foreach (var child in node.Children!)
            {
                QueryRecursive(child, queryBounds, results);
            }
        }
    }

    /// <summary>
    /// Clears all items from the quadtree.
    /// </summary>
    public void Clear()
    {
        if (_root != null)
        {
            _root.Items.Clear();
            _root.Children = null;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        if (_root == null) yield break;
        foreach (var item in EnumerateRecursive(_root))
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static IEnumerable<T> EnumerateRecursive(QuadtreeNode node)
    {
        if (node.IsLeaf)
        {
            foreach (var (item, _) in node.Items)
            {
                yield return item;
            }
        }
        else
        {
            foreach (var child in node.Children!)
            {
                foreach (var item in EnumerateRecursive(child))
                {
                    yield return item;
                }
            }
        }
    }

    private sealed class QuadtreeNode
    {
        public GeoBounds Bounds { get; }
        public int Depth { get; }
        public List<(T Item, GeoBounds Bounds)> Items { get; }
        public QuadtreeNode[]? Children { get; set; }

        public bool IsLeaf => Children == null;

        public QuadtreeNode(GeoBounds bounds, int depth)
        {
            Bounds = bounds;
            Depth = depth;
            Items = new List<(T, GeoBounds)>();
        }
    }
}

