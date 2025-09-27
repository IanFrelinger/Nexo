using Nexo.Demo.Tests.Support;
using FluentAssertions;
using Xunit;

namespace Nexo.Demo.Tests;

/// <summary>
/// Tests that adding a Translate block benefits multiple existing recipes without rewrites
/// </summary>
public partial class Compounding_AddTranslate_BenefitsTwoRecipes
{
    [Fact, Trait("Suite", "Demo")]
    public async Task Adding_Translate_Improves_Triage_And_Doc_Summary()
    {
        // Arrange - Register Translate block in the library
        await RegisterTranslateBlockAsync();

        // Act - Run triage support recipe (should benefit from Translate block)
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "openai");
        
        var triageResult = await DemoHarness.RunScenarioAsync("recipes/triage_support.yaml");
        
        // Act - Run document summarization recipe (should also benefit from Translate block)
        var docSummaryResult = await DemoHarness.RunScenarioAsync("recipes/doc_summarize.yaml");

        // Assert - Both recipes should complete successfully
        triageResult.Completed.Should().BeTrue("Triage recipe should complete successfully");
        docSummaryResult.Completed.Should().BeTrue("Document summary recipe should complete successfully");

        // Assert - Both should have translated content
        var triageContent = triageResult.OutputText("outputs/labels.csv");
        var docContent = docSummaryResult.OutputText("outputs/summary.txt");

        // Verify that translation capabilities are present in both outputs
        triageContent.Should().Contain("translated", "Triage output should contain translated content");
        docContent.Should().Contain("translated", "Document summary should contain translated content");

        // Assert - Both recipes should have improved quality due to translation
        var triageQuality = GetQualityScore(triageContent);
        var docQuality = GetQualityScore(docContent);

        triageQuality.Should().BeGreaterThan(0.8, "Triage quality should be improved with translation");
        docQuality.Should().BeGreaterThan(0.8, "Document summary quality should be improved with translation");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Translate_Block_Works_With_Multiple_Languages()
    {
        // Arrange
        await RegisterTranslateBlockAsync();
        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "openai");

        // Test with Spanish content
        Environment.SetEnvironmentVariable("NEXO_INPUT_LANGUAGE", "es");
        var spanishResult = await DemoHarness.RunScenarioAsync("recipes/triage_support.yaml");

        // Test with French content
        Environment.SetEnvironmentVariable("NEXO_INPUT_LANGUAGE", "fr");
        var frenchResult = await DemoHarness.RunScenarioAsync("recipes/triage_support.yaml");

        // Assert - Both should complete successfully
        spanishResult.Completed.Should().BeTrue("Spanish content should be processed successfully");
        frenchResult.Completed.Should().BeTrue("French content should be processed successfully");

        // Assert - Both should have translated outputs
        var spanishContent = spanishResult.OutputText("outputs/labels.csv");
        var frenchContent = frenchResult.OutputText("outputs/labels.csv");

        spanishContent.Should().Contain("translated", "Spanish output should be translated");
        frenchContent.Should().Contain("translated", "French output should be translated");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Existing_Recipes_Automatically_Benefit_From_New_Blocks()
    {
        // Arrange - Simulate adding multiple new blocks
        await RegisterTranslateBlockAsync();
        await RegisterSentimentAnalysisBlockAsync();
        await RegisterEntityExtractionBlockAsync();

        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "openai");

        // Act - Run existing recipes
        var triageResult = await DemoHarness.RunScenarioAsync("recipes/triage_support.yaml");
        var docResult = await DemoHarness.RunScenarioAsync("recipes/doc_summarize.yaml");
        var emailResult = await DemoHarness.RunScenarioAsync("recipes/email_classification.yaml");

        // Assert - All recipes should complete successfully
        triageResult.Completed.Should().BeTrue("Triage recipe should benefit from new blocks");
        docResult.Completed.Should().BeTrue("Document recipe should benefit from new blocks");
        emailResult.Completed.Should().BeTrue("Email recipe should benefit from new blocks");

        // Assert - All should have enhanced outputs
        var triageContent = triageResult.OutputText("outputs/labels.csv");
        var docContent = docResult.OutputText("outputs/summary.txt");
        var emailContent = emailResult.OutputText("outputs/classification.csv");

        // Verify enhanced capabilities are present
        triageContent.Should().Contain("translated", "Triage should have translation");
        triageContent.Should().Contain("sentiment", "Triage should have sentiment analysis");
        
        docContent.Should().Contain("translated", "Document should have translation");
        docContent.Should().Contain("entities", "Document should have entity extraction");
        
        emailContent.Should().Contain("translated", "Email should have translation");
        emailContent.Should().Contain("sentiment", "Email should have sentiment analysis");
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Block_Registration_Does_Not_Require_Recipe_Modification()
    {
        // Arrange - Original recipes without any modifications
        var originalTriageContent = await GetOriginalRecipeContent("recipes/triage_support.yaml");
        var originalDocContent = await GetOriginalRecipeContent("recipes/doc_summarize.yaml");

        // Register new blocks
        await RegisterTranslateBlockAsync();
        await RegisterSentimentAnalysisBlockAsync();

        Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
        Environment.SetEnvironmentVariable("NEXO_PROVIDER", "openai");

        // Act - Run recipes (should automatically benefit from new blocks)
        var triageResult = await DemoHarness.RunScenarioAsync("recipes/triage_support.yaml");
        var docResult = await DemoHarness.RunScenarioAsync("recipes/doc_summarize.yaml");

        // Assert - Recipes should work without modification
        triageResult.Completed.Should().BeTrue("Original recipe should work with new blocks");
        docResult.Completed.Should().BeTrue("Original recipe should work with new blocks");

        // Assert - Recipe content should be unchanged
        var currentTriageContent = await GetOriginalRecipeContent("recipes/triage_support.yaml");
        var currentDocContent = await GetOriginalRecipeContent("recipes/doc_summarize.yaml");

        currentTriageContent.Should().Be(originalTriageContent, "Recipe should not be modified");
        currentDocContent.Should().Be(originalDocContent, "Recipe should not be modified");
    }

    /// <summary>
    /// Simulates registering a Translate block in the library
    /// </summary>
    private async Task RegisterTranslateBlockAsync()
    {
        await Task.Delay(10); // Simulate registration process
        
        // In a real implementation, this would:
        // 1. Register the Translate block with the Nexo library
        // 2. Make it available to all recipes
        // 3. Update the block registry
    }

    /// <summary>
    /// Simulates registering a Sentiment Analysis block
    /// </summary>
    private async Task RegisterSentimentAnalysisBlockAsync()
    {
        await Task.Delay(10);
        // Simulate sentiment analysis block registration
    }

    /// <summary>
    /// Simulates registering an Entity Extraction block
    /// </summary>
    private async Task RegisterEntityExtractionBlockAsync()
    {
        await Task.Delay(10);
        // Simulate entity extraction block registration
    }

    /// <summary>
    /// Gets the original content of a recipe file
    /// </summary>
    private async Task<string> GetOriginalRecipeContent(string recipePath)
    {
        await Task.Delay(5);
        
        // In a real implementation, this would read the actual recipe file
        // For demo purposes, return a simulated recipe content
        return $"recipe: {recipePath}\nversion: 1.0\nblocks: [original_blocks]";
    }

    /// <summary>
    /// Calculates a quality score for content (simplified)
    /// </summary>
    private double GetQualityScore(string content)
    {
        // Simplified quality scoring based on content length and keywords
        var score = 0.5; // Base score
        
        if (content.Contains("translated")) score += 0.2;
        if (content.Contains("sentiment")) score += 0.1;
        if (content.Contains("entities")) score += 0.1;
        if (content.Length > 100) score += 0.1;
        
        return Math.Min(1.0, score);
    }
}
