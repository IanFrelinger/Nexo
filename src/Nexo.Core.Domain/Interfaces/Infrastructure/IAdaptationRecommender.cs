using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Interface for adaptation recommendation system
/// </summary>
public interface IAdaptationRecommender
{
    /// <summary>
    /// Generate recommendations based on learning insights
    /// </summary>
    Task<IEnumerable<AdaptationRecommendation>> GenerateRecommendationsAsync(IEnumerable<LearningInsight> insights);
    
    /// <summary>
    /// Get immediate recommendations for urgent issues
    /// </summary>
    Task<IEnumerable<AdaptationRecommendation>> GetImmediateRecommendationsAsync();
    
    /// <summary>
    /// Get recommendations for specific context
    /// </summary>
    Task<IEnumerable<AdaptationRecommendation>> GetContextualRecommendationsAsync(string context);
}

/// <summary>
/// Adaptation recommendation record
/// </summary>
public record AdaptationRecommendation
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public AdaptationType Type { get; init; }
    public int Priority { get; init; }
    public double Confidence { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
