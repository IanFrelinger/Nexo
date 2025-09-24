using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Playground.Server.Models;

namespace Playground.Server.Services;

/// <summary>
/// Main service that orchestrates demo scenarios and app generation.
/// </summary>
public class LovableDemoService
{
    private readonly ScenarioManager _scenarioManager;
    private readonly WebAppCodeGenerator _webAppGenerator;
    private readonly MobileAppCodeGenerator _mobileAppGenerator;
    private readonly DesktopAppCodeGenerator _desktopAppGenerator;
    private readonly ApiCodeGenerator _apiGenerator;
    private readonly GameCodeGenerator _gameGenerator;

    public LovableDemoService()
    {
        _scenarioManager = new ScenarioManager();
        _webAppGenerator = new WebAppCodeGenerator();
        _mobileAppGenerator = new MobileAppCodeGenerator();
        _desktopAppGenerator = new DesktopAppCodeGenerator();
        _apiGenerator = new ApiCodeGenerator();
        _gameGenerator = new GameCodeGenerator();
    }

    public async Task<List<DemoScenario>> GetScenariosAsync()
    {
        return await _scenarioManager.GetScenariosAsync();
    }

    public async Task<DemoScenario?> GetScenarioAsync(string id)
    {
        return await _scenarioManager.GetScenarioAsync(id);
    }

    public async Task<AppGenerationResult> GenerateAppAsync(string prompt, string appType, List<string> features)
    {
        await Task.Delay(1000); // Simulate processing time

        var generatedCode = appType.ToLower() switch
        {
            "web" => _webAppGenerator.GenerateWebAppCode(prompt, features),
            "mobile" => _mobileAppGenerator.GenerateMobileAppCode(prompt, features),
            "desktop" => _desktopAppGenerator.GenerateDesktopAppCode(prompt, features),
            "api" => _apiGenerator.GenerateApiCode(prompt, features),
            "game" => _gameGenerator.GenerateGameCode(prompt, features),
            _ => GenerateFallbackCode(prompt, appType, features)
        };

        return new AppGenerationResult
        {
            Success = true,
            GeneratedCode = generatedCode,
            AppType = appType,
            Features = features,
            GeneratedAt = DateTime.UtcNow,
            EstimatedComplexity = CalculateComplexity(features),
            RecommendedNextSteps = GenerateRecommendations(features)
        };
    }

    private string GenerateFallbackCode(string prompt, string appType, List<string> features)
    {
        return $@"// Generated {appType} application
// Prompt: {prompt}
// Features: {string.Join(", ", features)}

using System;
using System.Collections.Generic;

namespace GeneratedApp
{{
    public class Program
    {{
        public static void Main(string[] args)
        {{
            Console.WriteLine(""Hello from generated {appType} app!"");
            Console.WriteLine(""Features: {string.Join(", ", features)}"");
        }}
    }}
}}";
    }

    private int CalculateComplexity(List<string> features)
    {
        var complexityScore = features.Count * 10;
        
        if (features.Contains("Authentication")) complexityScore += 20;
        if (features.Contains("Database")) complexityScore += 30;
        if (features.Contains("Real-time")) complexityScore += 25;
        if (features.Contains("Payment")) complexityScore += 35;
        if (features.Contains("AI")) complexityScore += 40;
        
        return Math.Min(complexityScore, 100);
    }

    private List<string> GenerateRecommendations(List<string> features)
    {
        var recommendations = new List<string>();

        if (features.Contains("Authentication"))
        {
            recommendations.Add("Implement JWT token authentication");
            recommendations.Add("Add password hashing with bcrypt");
        }

        if (features.Contains("Database"))
        {
            recommendations.Add("Set up database migrations");
            recommendations.Add("Implement data validation");
        }

        if (features.Contains("Real-time"))
        {
            recommendations.Add("Configure WebSocket connections");
            recommendations.Add("Implement message queuing");
        }

        if (features.Contains("Payment"))
        {
            recommendations.Add("Integrate with payment gateway");
            recommendations.Add("Implement PCI compliance measures");
        }

        if (features.Contains("Mobile"))
        {
            recommendations.Add("Test on multiple device sizes");
            recommendations.Add("Implement offline functionality");
        }

        if (features.Contains("Game"))
        {
            recommendations.Add("Add sound effects and music");
            recommendations.Add("Implement save/load functionality");
        }

        return recommendations;
    }
}
