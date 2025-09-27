using System;
using System.Collections.Generic;
using System.Linq;

namespace Playground.Server.Services
{
    /// <summary>
    /// Domain analysis and architecture decision functionality for FeatureFactoryService.
    /// </summary>
    public partial class FeatureFactoryService
    {
        /// <summary>
        /// Generates domain analysis based on feature description.
        /// </summary>
        private DomainAnalysis GenerateDomainAnalysis(string description)
        {
            var analysis = new DomainAnalysis();

            // Simulate AI analysis based on description keywords
            if (description.ToLower().Contains("user") || description.ToLower().Contains("customer"))
            {
                analysis.Entities.Add(new DomainEntity { Name = "User", Properties = new List<string> { "Id", "Email", "Name", "CreatedAt" } });
                analysis.Entities.Add(new DomainEntity { Name = "Customer", Properties = new List<string> { "Id", "UserId", "Company", "Subscription" } });
            }

            if (description.ToLower().Contains("order") || description.ToLower().Contains("purchase"))
            {
                analysis.Entities.Add(new DomainEntity { Name = "Order", Properties = new List<string> { "Id", "CustomerId", "Items", "Total", "Status" } });
                analysis.ValueObjects.Add(new ValueObject { Name = "Money", Properties = new List<string> { "Amount", "Currency" } });
            }

            if (description.ToLower().Contains("notification") || description.ToLower().Contains("email"))
            {
                analysis.Entities.Add(new DomainEntity { Name = "Notification", Properties = new List<string> { "Id", "UserId", "Message", "Type", "SentAt" } });
                analysis.DomainServices.Add(new DomainService { Name = "NotificationService", Description = "Handles notification delivery" });
            }

            // Add some generic business rules
            analysis.BusinessRules.Add(new BusinessRule { Id = "BR001", Description = "All entities must have a unique identifier" });
            analysis.BusinessRules.Add(new BusinessRule { Id = "BR002", Description = "User email addresses must be valid" });
            analysis.BusinessRules.Add(new BusinessRule { Id = "BR003", Description = "Orders cannot be modified after confirmation" });

            return analysis;
        }

        /// <summary>
        /// Generates architecture decision based on feature description.
        /// </summary>
        private ArchitectureDecision GenerateArchitectureDecision(string description)
        {
            var complexity = CalculateComplexity(description);
            
            return new ArchitectureDecision
            {
                Strategy = complexity > 0.7 ? "Hybrid" : "Generated",
                ConfidenceScore = 85.5 + (new Random().NextDouble() * 10),
                RecommendedPatterns = new List<string> { "Repository", "Unit of Work", "CQRS", "Event Sourcing" },
                PerformanceConsiderations = new List<string> { "Caching", "Async Processing", "Database Optimization" },
                SecurityConsiderations = new List<string> { "Authentication", "Authorization", "Data Encryption" }
            };
        }

        /// <summary>
        /// Calculates complexity score for feature description.
        /// </summary>
        private double CalculateComplexity(string description)
        {
            var keywords = new[] { "complex", "advanced", "enterprise", "distributed", "microservice", "real-time" };
            var complexity = keywords.Count(k => description.ToLower().Contains(k)) * 0.2;
            return Math.Min(complexity, 1.0);
        }
    }
}
