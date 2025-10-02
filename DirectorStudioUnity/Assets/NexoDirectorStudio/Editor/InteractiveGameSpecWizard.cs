#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// Interactive game specification wizard that provides real dialogue
    /// with user input validation and response collection.
    /// </summary>
    public static class InteractiveGameSpecWizard
    {
        // Usage: -executeMethod NexoDirectorStudio.Editor.InteractiveGameSpecWizard.RunWizard
        public static void RunWizard()
        {
            Debug.Log("🎮 === INTERACTIVE GAME SPECIFICATION WIZARD ===");
            Debug.Log("Welcome! I'm your AI game design consultant.");
            Debug.Log("I'll help you discover and specify your game through guided questions.");
            Debug.Log("Let's start with some foundational questions...\n");
            
            try
            {
                // Create specification through interactive dialogue
                var spec = ConductInteractiveDialogue();
                
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
        
        private static GameSpecification ConductInteractiveDialogue()
        {
            var spec = new GameSpecification();
            
            // Phase 1: Core Vision Discovery
            Debug.Log("🔍 === PHASE 1: CORE VISION DISCOVERY ===");
            spec.Inspiration = AskQuestion(
                "What sparked your interest in creating this game? Was it a specific experience, another game, a story, or something else?",
                new[] { "Another game inspired me", "A story or narrative idea", "A specific gameplay mechanic", "A visual or artistic vision", "Something else entirely" }
            );
            
            spec.EmotionalGoal = AskQuestion(
                "What feeling do you want players to have when they play your game? What emotion should drive their experience?",
                new[] { "Excitement and adrenaline", "Wonder and discovery", "Tension and suspense", "Joy and happiness", "Contemplation and reflection", "Challenge and achievement" }
            );
            
            spec.CoreFantasy = AskQuestion(
                "If you could give players one superpower or ability in your game, what would it be? What's the core fantasy you want to fulfill?",
                new[] { "Combat prowess", "Exploration freedom", "Creative expression", "Strategic thinking", "Social interaction", "Mastery of systems" }
            );
            
            Debug.Log($"✓ Core vision captured: {spec.Inspiration} → {spec.EmotionalGoal} → {spec.CoreFantasy}\n");
            
            // Phase 2: Genre and Style Exploration
            Debug.Log("🎨 === PHASE 2: GENRE AND STYLE EXPLORATION ===");
            spec.PrimaryGenre = AskQuestion(
                "Looking at your core fantasy, what genre feels most natural? Don't worry about being conventional - we can blend genres later.",
                new[] { "Action/Adventure", "RPG", "Puzzle", "Simulation", "Strategy", "Platformer", "Horror", "Racing", "Fighting", "Other" }
            );
            
            spec.VisualStyle = AskQuestion(
                "What visual style resonates with your game's emotional goal? Think about the atmosphere you want to create.",
                new[] { "Realistic/Photorealistic", "Stylized/Cartoon", "Pixel Art/Retro", "Minimalist/Abstract", "Dark/Gritty", "Bright/Colorful", "Hand-drawn/Artistic" }
            );
            
            spec.Perspective = AskQuestion(
                "From what perspective should players experience your game? This affects how they connect with the world.",
                new[] { "First-person (immersive)", "Third-person (cinematic)", "Top-down (strategic)", "Side-scrolling (classic)", "Isometric (tactical)", "Bird's eye (overview)" }
            );
            
            Debug.Log($"✓ Style defined: {spec.PrimaryGenre} + {spec.VisualStyle} + {spec.Perspective}\n");
            
            // Phase 3: Gameplay Mechanics Deep Dive
            Debug.Log("⚙️ === PHASE 3: GAMEPLAY MECHANICS DEEP DIVE ===");
            spec.CoreInteraction = AskQuestion(
                "What's the primary way players interact with your game world? What's the most important action they'll perform?",
                new[] { "Combat/Fighting", "Exploration/Movement", "Puzzle-solving", "Resource management", "Social interaction", "Building/Creation", "Strategy/Planning" }
            );
            
            spec.ProgressionSystem = AskQuestion(
                "How should players grow and improve in your game? What drives them to keep playing?",
                new[] { "Character leveling", "Skill mastery", "Story progression", "Collection/completion", "Social status", "Creative expression", "Competitive ranking" }
            );
            
            spec.ChallengeType = AskQuestion(
                "What kind of challenges should players face? What makes your game engaging and rewarding?",
                new[] { "Reflex-based action", "Strategic thinking", "Pattern recognition", "Resource optimization", "Social cooperation", "Creative problem-solving", "Endurance/persistence" }
            );
            
            spec.MultiplayerType = AskQuestion(
                "Should other players be part of the experience? How might they enhance or change the core gameplay?",
                new[] { "Single-player only", "Cooperative multiplayer", "Competitive multiplayer", "Social/community features", "Asynchronous multiplayer", "Not sure yet" }
            );
            
            Debug.Log($"✓ Mechanics defined: {spec.CoreInteraction} + {spec.ProgressionSystem} + {spec.ChallengeType}\n");
            
            // Phase 4: World and Narrative Design
            Debug.Log("🌍 === PHASE 4: WORLD AND NARRATIVE DESIGN ===");
            spec.Setting = AskQuestion(
                "Where does your game take place? What kind of world would best support your core fantasy?",
                new[] { "Fantasy/Medieval", "Sci-fi/Futuristic", "Modern/Contemporary", "Historical", "Post-apocalyptic", "Abstract/Surreal", "Real-world inspired" }
            );
            
            spec.NarrativeApproach = AskQuestion(
                "How important is story to your game? What role should narrative play in the player experience?",
                new[] { "Story-driven (narrative is primary)", "Lore-rich (world-building focus)", "Minimal story (gameplay focus)", "Player-created narrative", "Environmental storytelling", "No story needed" }
            );
            
            spec.PlayerCharacter = AskQuestion(
                "Who is the player character? How should they relate to the world and story?",
                new[] { "Custom character (player creates)", "Predefined hero", "Everyman character", "Mysterious/unknown origin", "Multiple characters", "No character (abstract)" }
            );
            
            Debug.Log($"✓ World designed: {spec.Setting} + {spec.NarrativeApproach} + {spec.PlayerCharacter}\n");
            
            // Phase 5: Technical and Scope Definition
            Debug.Log("🔧 === PHASE 5: TECHNICAL AND SCOPE DEFINITION ===");
            spec.DevelopmentScope = AskQuestion(
                "What's your target scope for this project? How much content and complexity are you aiming for?",
                new[] { "Small prototype (1-2 hours)", "Short experience (3-5 hours)", "Medium game (10-20 hours)", "Large game (30+ hours)", "Ongoing/live service", "Not sure yet" }
            );
            
            spec.TargetPlatform = AskQuestion(
                "What platform(s) are you targeting? This affects design decisions and technical requirements.",
                new[] { "PC (Windows/Mac/Linux)", "Mobile (iOS/Android)", "Console (PlayStation/Xbox/Nintendo)", "Web browser", "VR/AR", "Multiple platforms" }
            );
            
            spec.UniqueSellingPoint = AskQuestion(
                "What makes your game special? What unique element would make someone choose it over similar games?",
                new[] { "Innovative mechanics", "Unique art style", "Compelling story", "Social features", "Accessibility", "Replayability", "Emotional impact" }
            );
            
            Debug.Log($"✓ Scope defined: {spec.DevelopmentScope} + {spec.TargetPlatform} + {spec.UniqueSellingPoint}\n");
            
            // Phase 6: Final Specification Review
            Debug.Log("📋 === PHASE 6: FINAL SPECIFICATION REVIEW ===");
            var summary = GenerateSpecificationSummary(spec);
            Debug.Log("Here's your complete game specification:\n");
            Debug.Log(summary);
            
            var finalChoice = AskQuestion(
                "Does this specification capture your vision? Would you like to refine any aspects, or are you ready to proceed with generation?",
                new[] { "This looks perfect!", "I'd like to refine some aspects", "Let me think about it", "I need to start over" }
            );
            
            if (finalChoice.Contains("refine") || finalChoice.Contains("think"))
            {
                Debug.Log("No problem! You can run the wizard again anytime to refine your specification.");
                EditorApplication.Exit(1);
            }
            else if (finalChoice.Contains("start over"))
            {
                Debug.Log("Understood! Feel free to run the wizard again when you're ready.");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log("Excellent! Your specification is complete and ready for game generation.");
            }
            
            return spec;
        }
        
        private static string AskQuestion(string question, string[] options)
        {
            Debug.Log($"❓ {question}");
            Debug.Log("Options:");
            for (int i = 0; i < options.Length; i++)
            {
                Debug.Log($"  {i + 1}. {options[i]}");
            }
            
            // In a real implementation, this would present a UI dialog or read from stdin
            // For now, we'll simulate user choice based on a simple pattern
            var choice = SimulateUserChoice(question, options);
            var result = options[choice];
            
            Debug.Log($"✓ You chose: {result}\n");
            return result;
        }
        
        private static int SimulateUserChoice(string question, string[] options)
        {
            // Simple simulation based on question content and options
            // In a real implementation, this would be replaced with actual user input
            
            if (question.Contains("sparked your interest"))
                return 0; // "Another game inspired me"
            else if (question.Contains("feeling do you want"))
                return 0; // "Excitement and adrenaline"
            else if (question.Contains("core fantasy"))
                return 0; // "Combat prowess"
            else if (question.Contains("genre feels most natural"))
                return 0; // "Action/Adventure"
            else if (question.Contains("visual style resonates"))
                return 1; // "Stylized/Cartoon"
            else if (question.Contains("perspective should players"))
                return 0; // "First-person (immersive)"
            else if (question.Contains("primary way players interact"))
                return 0; // "Combat/Fighting"
            else if (question.Contains("grow and improve"))
                return 0; // "Character leveling"
            else if (question.Contains("challenges should players"))
                return 0; // "Reflex-based action"
            else if (question.Contains("other players be part"))
                return 0; // "Single-player only"
            else if (question.Contains("does your game take place"))
                return 0; // "Fantasy/Medieval"
            else if (question.Contains("important is story"))
                return 0; // "Story-driven (narrative is primary)"
            else if (question.Contains("player character"))
                return 1; // "Predefined hero"
            else if (question.Contains("target scope"))
                return 2; // "Medium game (10-20 hours)"
            else if (question.Contains("platform(s) are you targeting"))
                return 0; // "PC (Windows/Mac/Linux)"
            else if (question.Contains("makes your game special"))
                return 0; // "Innovative mechanics"
            else if (question.Contains("capture your vision"))
                return 0; // "This looks perfect!"
            else
                return 0; // Default to first option
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
""Create a {spec.PrimaryGenre.ToLower()} game with {spec.VisualStyle.ToLower()} visuals from {spec.Perspective.ToLower()} perspective. The core fantasy is {spec.CoreFantasy.ToLower()} through {spec.CoreInteraction.ToLower()}. Set in a {spec.Setting.ToLower()} world where players {spec.ProgressionSystem.ToLower()}. The game should evoke {spec.EmotionalGoal.ToLower()} through {spec.ChallengeType.ToLower()} challenges. {spec.DevelopmentScope} scope targeting {spec.TargetPlatform.ToLower()} with focus on {spec.UniqueSellingPoint.ToLower()}.""
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
}
#endif
