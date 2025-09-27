using System;
using System.Collections.Generic;
using System.Linq;
using Playground.Server.Models;

namespace Playground.Server.Services;

/// <summary>
/// Game logic code generation functionality
/// </summary>
public partial class GameCodeGenerator
{
    private string GenerateGameLogicCode(List<string> features)
    {
        var code = new List<string>();

        if (features.Contains("Physics"))
        {
            code.Add(@"
        // Update physics
        Physics2D.Simulate(Time.fixedDeltaTime);");
        }

        if (features.Contains("Animation"))
        {
            code.Add(@"
        // Update animations
        AnimationManager.UpdateAnimations();");
        }

        if (features.Contains("AI"))
        {
            code.Add(@"
        // Update AI
        UpdateEnemyAI();");
        }

        if (features.Contains("Collision"))
        {
            code.Add(@"
        // Check collisions
        CheckCollisions();");
        }

        return string.Join("\n        ", code);
    }
}
