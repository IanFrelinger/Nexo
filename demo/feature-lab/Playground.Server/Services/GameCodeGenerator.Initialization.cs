using System;
using System.Collections.Generic;
using System.Linq;
using Playground.Server.Models;

namespace Playground.Server.Services;

/// <summary>
/// Game initialization code generation functionality
/// </summary>
public partial class GameCodeGenerator
{
    private string GenerateInitializationCode(List<string> features)
    {
        var code = new List<string>();

        if (features.Contains("2D Graphics"))
        {
            code.Add(@"
        // Initialize 2D graphics
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = 5f;");
        }

        if (features.Contains("Physics"))
        {
            code.Add(@"
        // Initialize physics
        Physics2D.gravity = new Vector2(0, -9.81f);");
        }

        if (features.Contains("Animation"))
        {
            code.Add(@"
        // Initialize animation system
        AnimationManager.Initialize();");
        }

        if (features.Contains("Audio"))
        {
            code.Add(@"
        // Initialize audio system
        AudioManager.Initialize();");
        }

        return string.Join("\n        ", code);
    }
}
