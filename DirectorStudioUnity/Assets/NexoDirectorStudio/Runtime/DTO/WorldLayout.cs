using System;
using System.Collections.Generic;
using UnityEngine;

namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents the spatial layout of the game world, including tiles, objects, and navigation.
    /// This is the output of the world building phase.
    /// </summary>
    public sealed record WorldLayout(
        string Id,
        string Name,
        string GamePlanId,
        Vector3 Dimensions,
        IReadOnlyList<TileData> Tiles,
        IReadOnlyList<ObjectData> Objects,
        IReadOnlyList<NavigationNode> NavigationNodes,
        LightingData Lighting,
        CameraData Camera,
        int Seed,
        DateTimeOffset GeneratedAt)
    {
        public string Id { get; init; } = Id;
        public string Name { get; init; } = Name;
        public string GamePlanId { get; init; } = GamePlanId;
        public Vector3 Dimensions { get; init; } = Dimensions;
        public IReadOnlyList<TileData> Tiles { get; init; } = Tiles;
        public IReadOnlyList<ObjectData> Objects { get; init; } = Objects;
        public IReadOnlyList<NavigationNode> NavigationNodes { get; init; } = NavigationNodes;
        public LightingData Lighting { get; init; } = Lighting;
        public CameraData Camera { get; init; } = Camera;
        public int Seed { get; init; } = Seed;
        public DateTimeOffset GeneratedAt { get; init; } = GeneratedAt;
    }
}