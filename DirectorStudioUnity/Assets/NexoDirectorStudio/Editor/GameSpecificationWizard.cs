#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// Interactive game specification wizard that uses Socratic dialogue
    /// to help users discover and define their game vision through guided questions.
    /// </summary>
    public static class GameSpecificationWizard
    {
        // Usage: -executeMethod NexoDirectorStudio.Editor.GameSpecificationWizard.RunWizard
        public static void RunWizard()
        {
            Debug.Log("🎮 === GAME SPECIFICATION WIZARD ===");
            Debug.Log("Welcome! I'm your AI game design consultant.");
            Debug.Log("I'll help you discover and specify your game through guided questions.");
            Debug.Log("Let's start with some foundational questions...\n");
            
            try
            {
                // Create a mock specification based on common game patterns
                var spec = CreateMockSpecification();
                
                // Save the specification
                SaveSpecification(spec);
                
                Debug.Log("✅ Game specification complete! Your detailed spec has been saved.");
                Debug.Log("📁 Check game-specification.json for your complete specification.");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Specification wizard failed: {ex.Message}");
                EditorApplication.Exit(1);
            }
        }
        
        private static GameSpecification CreateMockSpecification()
        {
            Debug.Log("🔍 === PHASE 1: CORE VISION DISCOVERY ===");
            Debug.Log("❓ What sparked your interest in creating this game?");
            Debug.Log("✓ Simulating: Another game inspired me");
            Debug.Log("❓ What feeling do you want players to have?");
            Debug.Log("✓ Simulating: Excitement and adrenaline");
            Debug.Log("❓ What's the core fantasy you want to fulfill?");
            Debug.Log("✓ Simulating: Combat prowess");
            Debug.Log("");
            
            Debug.Log("🎨 === PHASE 2: GENRE AND STYLE EXPLORATION ===");
            Debug.Log("❓ What genre feels most natural?");
            Debug.Log("✓ Simulating: Action/Adventure");
            Debug.Log("❓ What visual style resonates with your game?");
            Debug.Log("✓ Simulating: Stylized/Cartoon");
            Debug.Log("❓ From what perspective should players experience your game?");
            Debug.Log("✓ Simulating: First-person (immersive)");
            Debug.Log("");
            
            Debug.Log("⚙️ === PHASE 3: GAMEPLAY MECHANICS DEEP DIVE ===");
            Debug.Log("❓ What's the primary way players interact with your game world?");
            Debug.Log("✓ Simulating: Combat/Fighting");
            Debug.Log("❓ How should players grow and improve in your game?");
            Debug.Log("✓ Simulating: Character leveling");
            Debug.Log("❓ What kind of challenges should players face?");
            Debug.Log("✓ Simulating: Reflex-based action");
            Debug.Log("❓ Should other players be part of the experience?");
            Debug.Log("✓ Simulating: Single-player only");
            Debug.Log("");
            
            Debug.Log("🌍 === PHASE 4: WORLD AND NARRATIVE DESIGN ===");
            Debug.Log("❓ Where does your game take place?");
            Debug.Log("✓ Simulating: Fantasy/Medieval");
            Debug.Log("❓ How important is story to your game?");
            Debug.Log("✓ Simulating: Story-driven (narrative is primary)");
            Debug.Log("❓ Who is the player character?");
            Debug.Log("✓ Simulating: Predefined hero");
            Debug.Log("");
            
            Debug.Log("🔧 === PHASE 5: TECHNICAL AND SCOPE DEFINITION ===");
            Debug.Log("❓ What's your target scope for this project?");
            Debug.Log("✓ Simulating: Medium game (10-20 hours)");
            Debug.Log("❓ What platform(s) are you targeting?");
            Debug.Log("✓ Simulating: PC (Windows/Mac/Linux)");
            Debug.Log("❓ What makes your game special?");
            Debug.Log("✓ Simulating: Innovative mechanics");
            Debug.Log("");
            
            Debug.Log("📋 === PHASE 6: FINAL SPECIFICATION REVIEW ===");
            Debug.Log("✓ Specification captured successfully!");
            Debug.Log("");
            
            return new GameSpecification
            {
                // Core Vision
                Inspiration = "Another game inspired me",
                EmotionalGoal = "Excitement and adrenaline",
                CoreFantasy = "Combat prowess",
                
                // Style & Genre
                PrimaryGenre = "Action/Adventure",
                VisualStyle = "Stylized/Cartoon",
                Perspective = "First-person (immersive)",
                
                // Gameplay Mechanics
                CoreInteraction = "Combat/Fighting",
                ProgressionSystem = "Character leveling",
                ChallengeType = "Reflex-based action",
                MultiplayerType = "Single-player only",
                
                // World & Narrative
                Setting = "Fantasy/Medieval",
                NarrativeApproach = "Story-driven (narrative is primary)",
                PlayerCharacter = "Predefined hero",
                
                // Technical Scope
                DevelopmentScope = "Medium game (10-20 hours)",
                TargetPlatform = "PC (Windows/Mac/Linux)",
                UniqueSellingPoint = "Innovative mechanics",
                
                // Metadata
                CreatedAt = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Version = "1.0"
            };
        }
        
        private static IEnumerator ConductSpecificationDialogue()
        {
            Debug.Log("🎮 === GAME SPECIFICATION WIZARD ===");
            Debug.Log("Welcome! I'm your AI game design consultant.");
            Debug.Log("I'll help you discover and specify your game through guided questions.");
            Debug.Log("Let's start with some foundational questions...\n");
            
            var spec = new GameSpecification();
            var dialogue = new SocraticDialogue();
            
            // Phase 1: Core Vision Discovery
            yield return DiscoverCoreVision(dialogue, spec);
            
            // Phase 2: Genre and Style Exploration
            yield return ExploreGenreAndStyle(dialogue, spec);
            
            // Phase 3: Gameplay Mechanics Deep Dive
            yield return DefineGameplayMechanics(dialogue, spec);
            
            // Phase 4: World and Narrative Design
            yield return DesignWorldAndNarrative(dialogue, spec);
            
            // Phase 5: Technical and Scope Definition
            yield return DefineTechnicalScope(dialogue, spec);
            
            // Phase 6: Final Specification Review
            yield return ReviewFinalSpecification(dialogue, spec);
            
            // Save the specification
            SaveSpecification(spec);
            
            Debug.Log("✅ Game specification complete! Your detailed spec has been saved.");
            EditorApplication.Exit(0);
        }
        
        private static IEnumerator DiscoverCoreVision(SocraticDialogue dialogue, GameSpecification spec)
        {
            Debug.Log("🔍 === PHASE 1: CORE VISION DISCOVERY ===");
            
            // Question 1: Initial inspiration
            var q1 = dialogue.AskQuestion(
                "What sparked your interest in creating this game? Was it a specific experience, another game, a story, or something else?",
                new[] { "Another game inspired me", "A story or narrative idea", "A specific gameplay mechanic", "A visual or artistic vision", "Something else entirely" }
            );
            yield return q1;
            spec.Inspiration = q1.Result;
            
            // Question 2: Emotional goal
            var q2 = dialogue.AskQuestion(
                "What feeling do you want players to have when they play your game? What emotion should drive their experience?",
                new[] { "Excitement and adrenaline", "Wonder and discovery", "Tension and suspense", "Joy and happiness", "Contemplation and reflection", "Challenge and achievement" }
            );
            yield return q2;
            spec.EmotionalGoal = q2.Result;
            
            // Question 3: Core fantasy
            var q3 = dialogue.AskQuestion(
                "If you could give players one superpower or ability in your game, what would it be? What's the core fantasy you want to fulfill?",
                new[] { "Combat prowess", "Exploration freedom", "Creative expression", "Strategic thinking", "Social interaction", "Mastery of systems" }
            );
            yield return q3;
            spec.CoreFantasy = q3.Result;
            
            Debug.Log($"✓ Core vision captured: {spec.Inspiration} → {spec.EmotionalGoal} → {spec.CoreFantasy}\n");
        }
        
        private static IEnumerator ExploreGenreAndStyle(SocraticDialogue dialogue, GameSpecification spec)
        {
            Debug.Log("🎨 === PHASE 2: GENRE AND STYLE EXPLORATION ===");
            
            // Question 4: Genre exploration
            var q4 = dialogue.AskQuestion(
                "Looking at your core fantasy, what genre feels most natural? Don't worry about being conventional - we can blend genres later.",
                new[] { "Action/Adventure", "RPG", "Puzzle", "Simulation", "Strategy", "Platformer", "Horror", "Racing", "Fighting", "Other" }
            );
            yield return q4;
            spec.PrimaryGenre = q4.Result;
            
            // Question 5: Visual style
            var q5 = dialogue.AskQuestion(
                "What visual style resonates with your game's emotional goal? Think about the atmosphere you want to create.",
                new[] { "Realistic/Photorealistic", "Stylized/Cartoon", "Pixel Art/Retro", "Minimalist/Abstract", "Dark/Gritty", "Bright/Colorful", "Hand-drawn/Artistic" }
            );
            yield return q5;
            spec.VisualStyle = q5.Result;
            
            // Question 6: Perspective preference
            var q6 = dialogue.AskQuestion(
                "From what perspective should players experience your game? This affects how they connect with the world.",
                new[] { "First-person (immersive)", "Third-person (cinematic)", "Top-down (strategic)", "Side-scrolling (classic)", "Isometric (tactical)", "Bird's eye (overview)" }
            );
            yield return q6;
            spec.Perspective = q6.Result;
            
            Debug.Log($"✓ Style defined: {spec.PrimaryGenre} + {spec.VisualStyle} + {spec.Perspective}\n");
        }
        
        private static IEnumerator DefineGameplayMechanics(SocraticDialogue dialogue, GameSpecification spec)
        {
            Debug.Log("⚙️ === PHASE 3: GAMEPLAY MECHANICS DEEP DIVE ===");
            
            // Question 7: Core interaction
            var q7 = dialogue.AskQuestion(
                "What's the primary way players interact with your game world? What's the most important action they'll perform?",
                new[] { "Combat/Fighting", "Exploration/Movement", "Puzzle-solving", "Resource management", "Social interaction", "Building/Creation", "Strategy/Planning" }
            );
            yield return q7;
            spec.CoreInteraction = q7.Result;
            
            // Question 8: Progression system
            var q8 = dialogue.AskQuestion(
                "How should players grow and improve in your game? What drives them to keep playing?",
                new[] { "Character leveling", "Skill mastery", "Story progression", "Collection/completion", "Social status", "Creative expression", "Competitive ranking" }
            );
            yield return q8;
            spec.ProgressionSystem = q8.Result;
            
            // Question 9: Challenge type
            var q9 = dialogue.AskQuestion(
                "What kind of challenges should players face? What makes your game engaging and rewarding?",
                new[] { "Reflex-based action", "Strategic thinking", "Pattern recognition", "Resource optimization", "Social cooperation", "Creative problem-solving", "Endurance/persistence" }
            );
            yield return q9;
            spec.ChallengeType = q9.Result;
            
            // Question 10: Multiplayer consideration
            var q10 = dialogue.AskQuestion(
                "Should other players be part of the experience? How might they enhance or change the core gameplay?",
                new[] { "Single-player only", "Cooperative multiplayer", "Competitive multiplayer", "Social/community features", "Asynchronous multiplayer", "Not sure yet" }
            );
            yield return q10;
            spec.MultiplayerType = q10.Result;
            
            Debug.Log($"✓ Mechanics defined: {spec.CoreInteraction} + {spec.ProgressionSystem} + {spec.ChallengeType}\n");
        }
        
        private static IEnumerator DesignWorldAndNarrative(SocraticDialogue dialogue, GameSpecification spec)
        {
            Debug.Log("🌍 === PHASE 4: WORLD AND NARRATIVE DESIGN ===");
            
            // Question 11: Setting
            var q11 = dialogue.AskQuestion(
                "Where does your game take place? What kind of world would best support your core fantasy?",
                new[] { "Fantasy/Medieval", "Sci-fi/Futuristic", "Modern/Contemporary", "Historical", "Post-apocalyptic", "Abstract/Surreal", "Real-world inspired" }
            );
            yield return q11;
            spec.Setting = q11.Result;
            
            // Question 12: Narrative approach
            var q12 = dialogue.AskQuestion(
                "How important is story to your game? What role should narrative play in the player experience?",
                new[] { "Story-driven (narrative is primary)", "Lore-rich (world-building focus)", "Minimal story (gameplay focus)", "Player-created narrative", "Environmental storytelling", "No story needed" }
            );
            yield return q12;
            spec.NarrativeApproach = q12.Result;
            
            // Question 13: Player character
            var q13 = dialogue.AskQuestion(
                "Who is the player character? How should they relate to the world and story?",
                new[] { "Custom character (player creates)", "Predefined hero", "Everyman character", "Mysterious/unknown origin", "Multiple characters", "No character (abstract)" }
            );
            yield return q13;
            spec.PlayerCharacter = q13.Result;
            
            Debug.Log($"✓ World designed: {spec.Setting} + {spec.NarrativeApproach} + {spec.PlayerCharacter}\n");
        }
        
        private static IEnumerator DefineTechnicalScope(SocraticDialogue dialogue, GameSpecification spec)
        {
            Debug.Log("🔧 === PHASE 5: TECHNICAL AND SCOPE DEFINITION ===");
            
            // Question 14: Development scope
            var q14 = dialogue.AskQuestion(
                "What's your target scope for this project? How much content and complexity are you aiming for?",
                new[] { "Small prototype (1-2 hours)", "Short experience (3-5 hours)", "Medium game (10-20 hours)", "Large game (30+ hours)", "Ongoing/live service", "Not sure yet" }
            );
            yield return q14;
            spec.DevelopmentScope = q14.Result;
            
            // Question 15: Target platform
            var q15 = dialogue.AskQuestion(
                "What platform(s) are you targeting? This affects design decisions and technical requirements.",
                new[] { "PC (Windows/Mac/Linux)", "Mobile (iOS/Android)", "Console (PlayStation/Xbox/Nintendo)", "Web browser", "VR/AR", "Multiple platforms" }
            );
            yield return q15;
            spec.TargetPlatform = q15.Result;
            
            // Question 16: Unique selling point
            var q16 = dialogue.AskQuestion(
                "What makes your game special? What unique element would make someone choose it over similar games?",
                new[] { "Innovative mechanics", "Unique art style", "Compelling story", "Social features", "Accessibility", "Replayability", "Emotional impact" }
            );
            yield return q16;
            spec.UniqueSellingPoint = q16.Result;
            
            Debug.Log($"✓ Scope defined: {spec.DevelopmentScope} + {spec.TargetPlatform} + {spec.UniqueSellingPoint}\n");
        }
        
        private static IEnumerator ReviewFinalSpecification(SocraticDialogue dialogue, GameSpecification spec)
        {
            Debug.Log("📋 === PHASE 6: FINAL SPECIFICATION REVIEW ===");
            
            // Generate comprehensive summary
            var summary = GenerateSpecificationSummary(spec);
            Debug.Log("Here's your complete game specification:\n");
            Debug.Log(summary);
            
            // Final validation question
            var finalQ = dialogue.AskQuestion(
                "Does this specification capture your vision? Would you like to refine any aspects, or are you ready to proceed with generation?",
                new[] { "This looks perfect!", "I'd like to refine some aspects", "Let me think about it", "I need to start over" }
            );
            yield return finalQ;
            
            if (finalQ.Result.Contains("refine") || finalQ.Result.Contains("think"))
            {
                Debug.Log("No problem! You can run the wizard again anytime to refine your specification.");
                EditorApplication.Exit(1);
            }
            else if (finalQ.Result.Contains("start over"))
            {
                Debug.Log("Understood! Feel free to run the wizard again when you're ready.");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log("Excellent! Your specification is complete and ready for game generation.");
            }
        }
        
        private static string GenerateSpecificationSummary(GameSpecification spec)
        {
            return $@"
🎮 GAME SPECIFICATION SUMMARY
============================

CORE VISION:
• Inspiration: {spec.Inspiration}
• Emotional Goal: {spec.EmotionalGoal}
• Core Fantasy: {spec.CoreFantasy}

STYLE & GENRE:
• Primary Genre: {spec.PrimaryGenre}
• Visual Style: {spec.VisualStyle}
• Perspective: {spec.Perspective}

GAMEPLAY MECHANICS:
• Core Interaction: {spec.CoreInteraction}
• Progression System: {spec.ProgressionSystem}
• Challenge Type: {spec.ChallengeType}
• Multiplayer: {spec.MultiplayerType}

WORLD & NARRATIVE:
• Setting: {spec.Setting}
• Narrative Approach: {spec.NarrativeApproach}
• Player Character: {spec.PlayerCharacter}

TECHNICAL SCOPE:
• Development Scope: {spec.DevelopmentScope}
• Target Platform: {spec.TargetPlatform}
• Unique Selling Point: {spec.UniqueSellingPoint}

GENERATED PROMPT FOR PIPELINE:
""Create a {spec.PrimaryGenre.ToLower()} game with {spec.VisualStyle.ToLower()} {spec.VisualStyle.ToLower()} visuals from {spec.Perspective.ToLower()} perspective. The core fantasy is {spec.CoreFantasy.ToLower()} through {spec.CoreInteraction.ToLower()}. Set in a {spec.Setting.ToLower()} world where players {spec.ProgressionSystem.ToLower()}. The game should evoke {spec.EmotionalGoal.ToLower()} through {spec.ChallengeType.ToLower()} challenges. {spec.DevelopmentScope} scope targeting {spec.TargetPlatform.ToLower()} with focus on {spec.UniqueSellingPoint.ToLower()}.""
";
        }
        
        private static void SaveSpecification(GameSpecification spec)
        {
            var specPath = Path.GetFullPath("game-specification.json");
            var json = JsonUtility.ToJson(spec, true);
            Directory.CreateDirectory(Path.GetDirectoryName(specPath));
            File.WriteAllText(specPath, json);
            Debug.Log($"📁 Specification saved to: {specPath}");
        }
    }
    
    /// <summary>
    /// Socratic dialogue system for guided questioning
    /// </summary>
    public class SocraticDialogue
    {
        public QuestionResult AskQuestion(string question, string[] options)
        {
            Debug.Log($"❓ {question}");
            Debug.Log("Options:");
            for (int i = 0; i < options.Length; i++)
            {
                Debug.Log($"  {i + 1}. {options[i]}");
            }
            
            // In a real implementation, this would present a UI dialog
            // For now, we'll simulate the user choosing the first option
            var choice = 0; // Simulate user choice
            var result = options[choice];
            
            Debug.Log($"✓ You chose: {result}\n");
            return new QuestionResult { Result = result, ChoiceIndex = choice };
        }
    }
    
    /// <summary>
    /// Result of a dialogue question
    /// </summary>
    public class QuestionResult
    {
        public string Result { get; set; }
        public int ChoiceIndex { get; set; }
    }
    
    /// <summary>
    /// Comprehensive game specification data structure
    /// </summary>
    [System.Serializable]
    public class GameSpecification
    {
        [Header("Core Vision")]
        public string Inspiration;
        public string EmotionalGoal;
        public string CoreFantasy;
        
        [Header("Style & Genre")]
        public string PrimaryGenre;
        public string VisualStyle;
        public string Perspective;
        
        [Header("Gameplay Mechanics")]
        public string CoreInteraction;
        public string ProgressionSystem;
        public string ChallengeType;
        public string MultiplayerType;
        
        [Header("World & Narrative")]
        public string Setting;
        public string NarrativeApproach;
        public string PlayerCharacter;
        
        [Header("Technical Scope")]
        public string DevelopmentScope;
        public string TargetPlatform;
        public string UniqueSellingPoint;
        
        [Header("Metadata")]
        public string CreatedAt;
        public string Version;
        
        public GameSpecification()
        {
            CreatedAt = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            Version = "1.0";
        }
    }
}
#endif
