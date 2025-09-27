using Nexo.Agent.Tools.Visual.Contracts;

namespace Nexo.Agent.Tools.Visual.Implementations;

/// <summary>
/// Request enhancement functionality for Unity visual analyzer.
/// </summary>
public sealed partial class UnityVisualAnalyzer
{
    private VisualAnalysisRequest EnhanceRequestForUnity(VisualAnalysisRequest request)
    {
        var enhancedPrompt = request.AnalysisType switch
        {
            "ui" => @"Analyze this Unity game UI screenshot and provide insights about:
1. HUD elements (health bar, ammo counter, crosshair, minimap)
2. UI element visibility and contrast ratios
3. Layout and spacing issues
4. Accessibility concerns (color contrast, text size)
5. Visual hierarchy and information architecture
6. Unity UI Toolkit specific elements
7. Mobile vs desktop UI considerations

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements",
            
            "gameplay" => @"Analyze this Unity gameplay screenshot and provide insights about:
1. Game state and progression indicators
2. Player health, ammo, and inventory status
3. Enemy positions and behaviors
4. Visual effects and particle systems
5. Lighting and atmosphere
6. Performance indicators (frame rate, quality settings)
7. Gameplay mechanics visibility
8. Camera positioning and field of view

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements",
            
            "performance" => @"Analyze this Unity screenshot for performance indicators:
1. Frame rate and performance metrics
2. Visual quality settings (textures, shadows, lighting)
3. Rendering artifacts or glitches
4. UI responsiveness and smoothness
5. Memory usage indicators
6. GPU/CPU intensive elements
7. Optimization opportunities
8. Platform-specific performance considerations

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements",
            
            "accessibility" => @"Analyze this Unity game screenshot for accessibility:
1. Color contrast ratios (WCAG compliance)
2. Text readability and font sizes
3. UI element sizes and touch targets
4. Navigation clarity and flow
5. Visual indicators and feedback
6. Colorblind-friendly design
7. Motion and animation considerations
8. Audio-visual synchronization

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements",
            
            _ => request.Prompt ?? @"Analyze this Unity game screenshot and provide general insights about:
1. Visual composition and art direction
2. Key game elements and objects
3. Color palette and lighting
4. UI/UX design quality
5. Performance and optimization opportunities
6. Accessibility considerations

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements"
        };

        return request with { Prompt = enhancedPrompt };
    }

    private VisualComparisonRequest EnhanceComparisonRequestForUnity(VisualComparisonRequest request)
    {
        var enhancedType = request.ComparisonType switch
        {
            "ui" => "Unity UI comparison",
            "gameplay" => "Unity gameplay comparison",
            "performance" => "Unity performance comparison",
            _ => request.ComparisonType
        };

        return request with { ComparisonType = enhancedType };
    }
}
