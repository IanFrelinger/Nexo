using Microsoft.Extensions.Logging;
using Ashlar.Orchestration.Architect.Models;

namespace Ashlar.Orchestration.Architect;

/// <summary>
/// Seeds the cache with initial decomposition examples.
/// </summary>
public sealed class DecompositionExampleSeeder
{
    private readonly DecompositionRetriever _retriever;
    private readonly ILogger<DecompositionExampleSeeder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DecompositionExampleSeeder"/> class.
    /// </summary>
    /// <param name="retriever">The decomposition retriever for storing examples.</param>
    /// <param name="logger">The logger instance.</param>
    public DecompositionExampleSeeder(
        DecompositionRetriever retriever,
        ILogger<DecompositionExampleSeeder> logger)
    {
        _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Seeds initial decomposition examples into the cache.
    /// 
    /// Creates and stores 20 example decompositions covering various domains:
    /// - Game systems (combat, economy, AI, gameplay)
    /// - Infrastructure and security
    /// - Social and progression features
    /// 
    /// These examples are used for RAG retrieval to improve decomposition quality.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous seeding operation.</returns>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var examples = CreateInitialExamples();
        var count = 0;

        foreach (var example in examples)
        {
            await _retriever.StoreExampleAsync(example, cancellationToken);
            count++;
        }

        _logger.LogInformation("Seeded {Count} decomposition examples", count);
    }

    private static List<DecompositionExample> CreateInitialExamples()
    {
        return new List<DecompositionExample>
        {
            CreateExtractionShooterExample(),
            CreateECommerceExample(),
            CreateAIGameExample(),
            CreateSecuritySystemExample(),
            CreateInfrastructureExample(),
            CreateCombatSystemExample(),
            CreateEconomySystemExample(),
            CreateMultiplayerExample(),
            CreateQuestSystemExample(),
            CreateInventorySystemExample(),
            CreateCraftingSystemExample(),
            CreateSocialFeaturesExample(),
            CreateProgressionSystemExample(),
            CreateMatchmakingExample(),
            CreateAnalyticsExample(),
            CreateContentManagementExample(),
            CreatePaymentSystemExample(),
            CreateNotificationSystemExample(),
            CreateLeaderboardExample(),
            CreateTutorialSystemExample()
        };
    }

    private static DecompositionExample CreateExtractionShooterExample()
    {
        var agents = new List<AgentSpawnSpec>
        {
            new()
            {
                AgentId = "combat-1",
                Domain = "Combat",
                Goal = "Design weapon damage and balance system",
                Description = "Create weapon stats, damage calculations, and balance mechanics",
                Constraints = new[]
                {
                    new AgentConstraint { Type = "Balance", Description = "Weapons must be balanced for PvP", IsMandatory = true }
                },
                ResourceRequirements = new ResourceRequirements { EstimatedComputeSeconds = 300 }
            },
            new()
            {
                AgentId = "economy-1",
                Domain = "Economy",
                Goal = "Design extraction economy and loot system",
                Description = "Create loot tables, extraction mechanics, and risk/reward balance",
                Dependencies = new[] { "combat-1" },
                ResourceRequirements = new ResourceRequirements { EstimatedComputeSeconds = 240 }
            },
            new()
            {
                AgentId = "ai-1",
                Domain = "AI",
                Goal = "Design AI enemy behaviors",
                Description = "Create AI patrol patterns, combat behaviors, and difficulty scaling",
                Dependencies = new[] { "combat-1" },
                ResourceRequirements = new ResourceRequirements { EstimatedComputeSeconds = 360 }
            }
        };

        return new DecompositionExample
        {
            Id = "extraction-shooter-1",
            Request = "Design an extraction shooter game with PvPvE combat, loot system, and AI enemies",
            Result = new DecompositionResult
            {
                Agents = agents,
                OriginalRequest = "Design an extraction shooter game with PvPvE combat, loot system, and AI enemies",
                Reasoning = "Decomposed into Combat (weapons), Economy (loot/extraction), and AI (enemies) domains",
                Confidence = 0.9
            },
            DomainTags = new[] { "Combat", "Economy", "AI", "Gameplay" },
            Keywords = new[] { "extraction", "shooter", "pvp", "pve", "combat", "loot", "enemy", "weapon", "ai" },
            QualityScore = 0.95
        };
    }

    private static DecompositionExample CreateECommerceExample()
    {
        var agents = new List<AgentSpawnSpec>
        {
            new()
            {
                AgentId = "infrastructure-1",
                Domain = "Infrastructure",
                Goal = "Design scalable e-commerce backend",
                Description = "Create microservices architecture, database schema, and API design",
                ResourceRequirements = new ResourceRequirements { EstimatedComputeSeconds = 600 }
            },
            new()
            {
                AgentId = "security-1",
                Domain = "Security",
                Goal = "Implement payment security and authentication",
                Description = "Design secure payment processing, user authentication, and data encryption",
                Dependencies = new[] { "infrastructure-1" },
                ResourceRequirements = new ResourceRequirements { EstimatedComputeSeconds = 480 }
            }
        };

        return new DecompositionExample
        {
            Id = "ecommerce-1",
            Request = "Build an e-commerce platform with secure payments and user authentication",
            Result = new DecompositionResult
            {
                Agents = agents,
                OriginalRequest = "Build an e-commerce platform with secure payments and user authentication",
                Reasoning = "Decomposed into Infrastructure (backend) and Security (payments/auth) domains",
                Confidence = 0.85
            },
            DomainTags = new[] { "Infrastructure", "Security" },
            Keywords = new[] { "ecommerce", "platform", "payment", "authentication", "security", "backend", "api" },
            QualityScore = 0.9
        };
    }

    private static DecompositionExample CreateAIGameExample()
    {
        var agents = new List<AgentSpawnSpec>
        {
            new()
            {
                AgentId = "ai-2",
                Domain = "AI",
                Goal = "Design intelligent NPC behaviors",
                Description = "Create decision trees, pathfinding, and adaptive behaviors",
                ResourceRequirements = new ResourceRequirements { EstimatedComputeSeconds = 450 }
            },
            new()
            {
                AgentId = "gameplay-1",
                Domain = "Gameplay",
                Goal = "Design quest and mission system",
                Description = "Create quest chains, objectives, and progression mechanics",
                Dependencies = new[] { "ai-2" },
                ResourceRequirements = new ResourceRequirements { EstimatedComputeSeconds = 300 }
            }
        };

        return new DecompositionExample
        {
            Id = "ai-game-1",
            Request = "Create an AI-driven game with intelligent NPCs and quest system",
            Result = new DecompositionResult
            {
                Agents = agents,
                OriginalRequest = "Create an AI-driven game with intelligent NPCs and quest system",
                Reasoning = "Decomposed into AI (NPCs) and Gameplay (quests) domains",
                Confidence = 0.88
            },
            DomainTags = new[] { "AI", "Gameplay" },
            Keywords = new[] { "ai", "npc", "intelligent", "quest", "mission", "behavior", "game" },
            QualityScore = 0.88
        };
    }

    // Additional examples with simpler implementations
    private static DecompositionExample CreateSecuritySystemExample()
    {
        return new DecompositionExample
        {
            Id = "security-1",
            Request = "Implement authentication and authorization system",
            Result = new DecompositionResult
            {
                Agents = new[]
                {
                    new AgentSpawnSpec
                    {
                        AgentId = "security-2",
                        Domain = "Security",
                        Goal = "Design authentication system",
                        Description = "Implement JWT tokens, password hashing, and session management"
                    }
                },
                OriginalRequest = "Implement authentication and authorization system",
                Confidence = 0.9
            },
            DomainTags = new[] { "Security" },
            Keywords = new[] { "authentication", "authorization", "security", "jwt", "password" },
            QualityScore = 0.92
        };
    }

    private static DecompositionExample CreateInfrastructureExample()
    {
        return new DecompositionExample
        {
            Id = "infrastructure-1",
            Request = "Set up cloud infrastructure with auto-scaling",
            Result = new DecompositionResult
            {
                Agents = new[]
                {
                    new AgentSpawnSpec
                    {
                        AgentId = "infrastructure-2",
                        Domain = "Infrastructure",
                        Goal = "Design auto-scaling infrastructure",
                        Description = "Create Kubernetes deployment, load balancing, and monitoring"
                    }
                },
                OriginalRequest = "Set up cloud infrastructure with auto-scaling",
                Confidence = 0.87
            },
            DomainTags = new[] { "Infrastructure" },
            Keywords = new[] { "cloud", "infrastructure", "scaling", "kubernetes", "deployment" },
            QualityScore = 0.9
        };
    }

    private static DecompositionExample CreateCombatSystemExample()
    {
        return new DecompositionExample
        {
            Id = "combat-1",
            Request = "Design combat system with weapons and damage",
            Result = new DecompositionResult
            {
                Agents = new[]
                {
                    new AgentSpawnSpec
                    {
                        AgentId = "combat-2",
                        Domain = "Combat",
                        Goal = "Design weapon and damage system",
                        Description = "Create weapon stats, damage calculations, and combat mechanics"
                    }
                },
                OriginalRequest = "Design combat system with weapons and damage",
                Confidence = 0.92
            },
            DomainTags = new[] { "Combat" },
            Keywords = new[] { "combat", "weapon", "damage", "battle", "fight" },
            QualityScore = 0.93
        };
    }

    private static DecompositionExample CreateEconomySystemExample()
    {
        return new DecompositionExample
        {
            Id = "economy-1",
            Request = "Create in-game economy with currency and trading",
            Result = new DecompositionResult
            {
                Agents = new[]
                {
                    new AgentSpawnSpec
                    {
                        AgentId = "economy-2",
                        Domain = "Economy",
                        Goal = "Design currency and trading system",
                        Description = "Create currency mechanics, pricing, and trade systems"
                    }
                },
                OriginalRequest = "Create in-game economy with currency and trading",
                Confidence = 0.89
            },
            DomainTags = new[] { "Economy" },
            Keywords = new[] { "economy", "currency", "trading", "money", "price" },
            QualityScore = 0.91
        };
    }

    private static DecompositionExample CreateMultiplayerExample()
    {
        return new DecompositionExample
        {
            Id = "multiplayer-1",
            Request = "Implement multiplayer matchmaking and lobby system",
            Result = new DecompositionResult
            {
                Agents = new[]
                {
                    new AgentSpawnSpec
                    {
                        AgentId = "gameplay-2",
                        Domain = "Gameplay",
                        Goal = "Design matchmaking and lobby",
                        Description = "Create matchmaking algorithms, lobby management, and session handling"
                    }
                },
                OriginalRequest = "Implement multiplayer matchmaking and lobby system",
                Confidence = 0.86
            },
            DomainTags = new[] { "Gameplay" },
            Keywords = new[] { "multiplayer", "matchmaking", "lobby", "session" },
            QualityScore = 0.89
        };
    }

    private static DecompositionExample CreateQuestSystemExample()
    {
        return new DecompositionExample
        {
            Id = "quest-1",
            Request = "Design quest and mission system",
            Result = new DecompositionResult
            {
                Agents = new[]
                {
                    new AgentSpawnSpec
                    {
                        AgentId = "gameplay-3",
                        Domain = "Gameplay",
                        Goal = "Design quest system",
                        Description = "Create quest chains, objectives, rewards, and progression"
                    }
                },
                OriginalRequest = "Design quest and mission system",
                Confidence = 0.91
            },
            DomainTags = new[] { "Gameplay" },
            Keywords = new[] { "quest", "mission", "objective", "reward" },
            QualityScore = 0.92
        };
    }

    private static DecompositionExample CreateInventorySystemExample()
    {
        return new DecompositionExample
        {
            Id = "inventory-1",
            Request = "Create inventory management system",
            Result = new DecompositionResult
            {
                Agents = new[]
                {
                    new AgentSpawnSpec
                    {
                        AgentId = "gameplay-4",
                        Domain = "Gameplay",
                        Goal = "Design inventory system",
                        Description = "Create item storage, stacking, sorting, and management"
                    }
                },
                OriginalRequest = "Create inventory management system",
                Confidence = 0.88
            },
            DomainTags = new[] { "Gameplay" },
            Keywords = new[] { "inventory", "item", "storage", "management" },
            QualityScore = 0.9
        };
    }

    private static DecompositionExample CreateCraftingSystemExample()
    {
        return new DecompositionExample
        {
            Id = "crafting-1",
            Request = "Design crafting and recipe system",
            Result = new DecompositionResult
            {
                Agents = new[]
                {
                    new AgentSpawnSpec
                    {
                        AgentId = "gameplay-5",
                        Domain = "Gameplay",
                        Goal = "Design crafting system",
                        Description = "Create recipes, material requirements, and crafting mechanics"
                    }
                },
                OriginalRequest = "Design crafting and recipe system",
                Confidence = 0.87
            },
            DomainTags = new[] { "Gameplay" },
            Keywords = new[] { "crafting", "recipe", "material", "item" },
            QualityScore = 0.89
        };
    }

    private static DecompositionExample CreateSocialFeaturesExample()
    {
        return new DecompositionExample
        {
            Id = "social-1",
            Request = "Implement social features: friends, chat, and guilds",
            Result = new DecompositionResult
            {
                Agents = new[]
                {
                    new AgentSpawnSpec
                    {
                        AgentId = "gameplay-6",
                        Domain = "Gameplay",
                        Goal = "Design social features",
                        Description = "Create friend system, chat, guilds, and social interactions"
                    }
                },
                OriginalRequest = "Implement social features: friends, chat, and guilds",
                Confidence = 0.85
            },
            DomainTags = new[] { "Gameplay" },
            Keywords = new[] { "social", "friend", "chat", "guild" },
            QualityScore = 0.87
        };
    }

    private static DecompositionExample CreateProgressionSystemExample()
    {
        return new DecompositionExample
        {
            Id = "progression-1",
            Request = "Design player progression and leveling system",
            Result = new DecompositionResult
            {
                Agents = new[]
                {
                    new AgentSpawnSpec
                    {
                        AgentId = "gameplay-7",
                        Domain = "Gameplay",
                        Goal = "Design progression system",
                        Description = "Create XP, levels, skills, and character progression"
                    }
                },
                OriginalRequest = "Design player progression and leveling system",
                Confidence = 0.9
            },
            DomainTags = new[] { "Gameplay" },
            Keywords = new[] { "progression", "level", "xp", "skill", "character" },
            QualityScore = 0.91
        };
    }

    private static DecompositionExample CreateMatchmakingExample()
    {
        return new DecompositionExample
        {
            Id = "matchmaking-1",
            Request = "Create competitive matchmaking with skill-based matching",
            Result = new DecompositionResult
            {
                Agents = new[]
                {
                    new AgentSpawnSpec
                    {
                        AgentId = "gameplay-8",
                        Domain = "Gameplay",
                        Goal = "Design skill-based matchmaking",
                        Description = "Create MMR system, skill rating, and matchmaking algorithms"
                    }
                },
                OriginalRequest = "Create competitive matchmaking with skill-based matching",
                Confidence = 0.88
            },
            DomainTags = new[] { "Gameplay" },
            Keywords = new[] { "matchmaking", "competitive", "skill", "mmr", "rating" },
            QualityScore = 0.9
        };
    }

    private static DecompositionExample CreateAnalyticsExample()
    {
        return new DecompositionExample
        {
            Id = "analytics-1",
            Request = "Implement analytics and metrics tracking",
            Result = new DecompositionResult
            {
                Agents = new[]
                {
                    new AgentSpawnSpec
                    {
                        AgentId = "infrastructure-3",
                        Domain = "Infrastructure",
                        Goal = "Design analytics system",
                        Description = "Create event tracking, metrics collection, and reporting"
                    }
                },
                OriginalRequest = "Implement analytics and metrics tracking",
                Confidence = 0.86
            },
            DomainTags = new[] { "Infrastructure" },
            Keywords = new[] { "analytics", "metrics", "tracking", "event", "reporting" },
            QualityScore = 0.88
        };
    }

    private static DecompositionExample CreateContentManagementExample()
    {
        return new DecompositionExample
        {
            Id = "content-1",
            Request = "Build content management system for game assets",
            Result = new DecompositionResult
            {
                Agents = new[]
                {
                    new AgentSpawnSpec
                    {
                        AgentId = "infrastructure-4",
                        Domain = "Infrastructure",
                        Goal = "Design CMS for game assets",
                        Description = "Create asset management, versioning, and deployment pipeline"
                    }
                },
                OriginalRequest = "Build content management system for game assets",
                Confidence = 0.84
            },
            DomainTags = new[] { "Infrastructure" },
            Keywords = new[] { "content", "management", "asset", "cms", "deployment" },
            QualityScore = 0.86
        };
    }

    private static DecompositionExample CreatePaymentSystemExample()
    {
        return new DecompositionExample
        {
            Id = "payment-1",
            Request = "Integrate payment processing with multiple providers",
            Result = new DecompositionResult
            {
                Agents = new[]
                {
                    new AgentSpawnSpec
                    {
                        AgentId = "security-3",
                        Domain = "Security",
                        Goal = "Design payment integration",
                        Description = "Create payment gateway integration, transaction handling, and security"
                    }
                },
                OriginalRequest = "Integrate payment processing with multiple providers",
                Confidence = 0.89
            },
            DomainTags = new[] { "Security" },
            Keywords = new[] { "payment", "transaction", "gateway", "provider", "security" },
            QualityScore = 0.91
        };
    }

    private static DecompositionExample CreateNotificationSystemExample()
    {
        return new DecompositionExample
        {
            Id = "notification-1",
            Request = "Implement push notification system",
            Result = new DecompositionResult
            {
                Agents = new[]
                {
                    new AgentSpawnSpec
                    {
                        AgentId = "infrastructure-5",
                        Domain = "Infrastructure",
                        Goal = "Design notification system",
                        Description = "Create push notifications, email, and in-app messaging"
                    }
                },
                OriginalRequest = "Implement push notification system",
                Confidence = 0.87
            },
            DomainTags = new[] { "Infrastructure" },
            Keywords = new[] { "notification", "push", "email", "messaging" },
            QualityScore = 0.89
        };
    }

    private static DecompositionExample CreateLeaderboardExample()
    {
        return new DecompositionExample
        {
            Id = "leaderboard-1",
            Request = "Create leaderboard and ranking system",
            Result = new DecompositionResult
            {
                Agents = new[]
                {
                    new AgentSpawnSpec
                    {
                        AgentId = "gameplay-9",
                        Domain = "Gameplay",
                        Goal = "Design leaderboard system",
                        Description = "Create ranking algorithms, leaderboard display, and seasonal resets"
                    }
                },
                OriginalRequest = "Create leaderboard and ranking system",
                Confidence = 0.88
            },
            DomainTags = new[] { "Gameplay" },
            Keywords = new[] { "leaderboard", "ranking", "score", "season" },
            QualityScore = 0.9
        };
    }

    private static DecompositionExample CreateTutorialSystemExample()
    {
        return new DecompositionExample
        {
            Id = "tutorial-1",
            Request = "Design tutorial and onboarding system",
            Result = new DecompositionResult
            {
                Agents = new[]
                {
                    new AgentSpawnSpec
                    {
                        AgentId = "gameplay-10",
                        Domain = "Gameplay",
                        Goal = "Design tutorial system",
                        Description = "Create step-by-step tutorials, tooltips, and onboarding flow"
                    }
                },
                OriginalRequest = "Design tutorial and onboarding system",
                Confidence = 0.86
            },
            DomainTags = new[] { "Gameplay" },
            Keywords = new[] { "tutorial", "onboarding", "guide", "tooltip" },
            QualityScore = 0.88
        };
    }
}

