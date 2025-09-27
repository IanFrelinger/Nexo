using System;
using System.Collections.Generic;
using System.Linq;
using Playground.Server.Models;

namespace Playground.Server.Services;

/// <summary>
/// Win/lose condition code generation functionality
/// </summary>
public partial class GameCodeGenerator
{
    private string GenerateConditionsCode(List<string> features)
    {
        var code = new List<string>();

        if (features.Contains("Score"))
        {
            code.Add(@"
        // Check score conditions
        if (score >= 1000)
        {
            // Level complete
            Debug.Log(""Level Complete!"");
        }");
        }

        if (features.Contains("Time"))
        {
            code.Add(@"
        // Check time conditions
        if (Time.time >= 300f) // 5 minutes
        {
            GameOver();
        }");
        }

        if (features.Contains("Lives"))
        {
            code.Add(@"
        // Check lives conditions
        if (lives <= 0)
        {
            GameOver();
        }");
        }

        return string.Join("\n        ", code);
    }
}
