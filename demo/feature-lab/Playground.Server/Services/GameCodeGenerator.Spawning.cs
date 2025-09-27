using System;
using System.Collections.Generic;
using System.Linq;
using Playground.Server.Models;

namespace Playground.Server.Services;

/// <summary>
/// Object spawning code generation functionality
/// </summary>
public partial class GameCodeGenerator
{
    private string GenerateSpawnCode(List<string> features)
    {
        var code = new List<string>();

        if (features.Contains("Enemies"))
        {
            code.Add(@"
        // Spawn enemies
        for (int i = 0; i < 5; i++)
        {
            Vector3 spawnPos = new Vector3(Random.Range(-10f, 10f), Random.Range(0f, 5f), 0);
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            activeEnemies.Add(enemy);
        }");
        }

        if (features.Contains("Collectibles"))
        {
            code.Add(@"
        // Spawn collectibles
        for (int i = 0; i < 10; i++)
        {
            Vector3 spawnPos = new Vector3(Random.Range(-10f, 10f), Random.Range(0f, 5f), 0);
            GameObject collectible = Instantiate(collectiblePrefab, spawnPos, Quaternion.identity);
            activeCollectibles.Add(collectible);
        }");
        }

        if (features.Contains("Power-ups"))
        {
            code.Add(@"
        // Spawn power-ups
        SpawnPowerUps();");
        }

        return string.Join("\n        ", code);
    }
}
