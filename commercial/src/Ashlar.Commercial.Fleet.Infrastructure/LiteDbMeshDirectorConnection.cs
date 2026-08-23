using LiteDB;
using Ashlar.Commercial.Fleet.Contracts.Models;

namespace Ashlar.Commercial.Fleet.Infrastructure;

/// <summary>Lite db mesh director connection.</summary>
internal static class LiteDbMeshDirectorConnection
{
    internal const string TasksCollection = "mesh_tasks";
    internal const string FleetCollection = "mesh_fleet_nodes";

    /// <summary>To connection string operation.</summary>
    public static string ToConnectionString(string pathOrConnectionString)
    {
        if (string.IsNullOrWhiteSpace(pathOrConnectionString))
            throw new ArgumentNullException(nameof(pathOrConnectionString));
        var trimmed = pathOrConnectionString.Trim();
        return trimmed.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"Filename={trimmed}";
    }
}
