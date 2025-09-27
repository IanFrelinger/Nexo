using Microsoft.Extensions.Logging;
using Nexo.Agent.Tools.Visual.Contracts;

namespace Nexo.Agent.Tools.Visual.Implementations;

/// <summary>
/// Prompt generation for analysis, comparison, and OCR functionality
/// </summary>
public sealed partial class OllamaVisualAnalyzer
{
    private string CreateAnalysisPrompt(string analysisType, string? customPrompt)
    {
        if (!string.IsNullOrEmpty(customPrompt))
        {
            return customPrompt;
        }

        return analysisType switch
        {
            "ui" => @"Analyze this UI screenshot and provide insights about:
1. UI element visibility and contrast
2. Layout and spacing issues
3. Accessibility concerns
4. Visual hierarchy
5. Color scheme and branding

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements",
            
            "gameplay" => @"Analyze this gameplay screenshot and provide insights about:
1. Game state and progression
2. UI elements (health, ammo, minimap)
3. Visual effects and particles
4. Lighting and atmosphere
5. Performance indicators

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements",
            
            "performance" => @"Analyze this screenshot for performance indicators:
1. Frame rate indicators
2. Visual quality settings
3. Rendering artifacts
4. UI responsiveness
5. Memory usage indicators

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements",
            
            "accessibility" => @"Analyze this screenshot for accessibility:
1. Color contrast ratios
2. Text readability
3. UI element sizes
4. Navigation clarity
5. Visual indicators

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements",
            
            _ => @"Analyze this image and provide general insights about:
1. Visual composition
2. Key elements and objects
3. Color and lighting
4. Potential issues or improvements

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements"
        };
    }

    private string CreateComparisonPrompt(string comparisonType, double threshold)
    {
        return comparisonType switch
        {
            "ui" => $@"Compare these two UI screenshots and identify differences:
1. UI element changes (added, removed, modified)
2. Layout changes
3. Color or styling changes
4. Content changes
5. Navigation changes

Threshold for significance: {threshold}

Respond in JSON format with:
- similarityScore: 0.0 to 1.0
- differences: array of {{type, boundingBox, description, confidence}}
- summary: brief overview",
            
            "gameplay" => $@"Compare these two gameplay screenshots and identify differences:
1. Game state changes
2. UI element changes
3. Visual effect changes
4. Lighting changes
5. Object position changes

Threshold for significance: {threshold}

Respond in JSON format with:
- similarityScore: 0.0 to 1.0
- differences: array of {{type, boundingBox, description, confidence}}
- summary: brief overview",
            
            _ => $@"Compare these two images and identify differences:
1. Object changes
2. Color changes
3. Layout changes
4. Content changes

Threshold for significance: {threshold}

Respond in JSON format with:
- similarityScore: 0.0 to 1.0
- differences: array of {{type, boundingBox, description, confidence}}
- summary: brief overview"
        };
    }

    private string CreateOcrPrompt(string? language, bool includeConfidence)
    {
        var confidenceText = includeConfidence ? " Include confidence scores for each text region." : "";
        return $@"Extract all text from this image in {language ?? "English"}.{confidenceText}

Respond in JSON format with:
- extractedText: all text as a single string
- textRegions: array of {{text, boundingBox, confidence, language}}";
    }
}
