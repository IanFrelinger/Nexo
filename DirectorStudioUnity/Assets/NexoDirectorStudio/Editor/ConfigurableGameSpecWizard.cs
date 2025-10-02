#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// Configurable game specification wizard that accepts user answers
    /// via command line arguments for validation and testing.
    /// </summary>
    public static class ConfigurableGameSpecWizard
    {
        // Usage: -executeMethod NexoDirectorStudio.Editor.ConfigurableGameSpecWizard.RunWizard
        //        [-inspiration "Another game inspired me"] [-emotionalGoal "Excitement and adrenaline"] etc.
        public static void RunWizard()
        {
            Debug.Log("🎮 === CONFIGURABLE GAME SPECIFICATION WIZARD ===");
            Debug.Log("Welcome! I'm your AI game design consultant.");
            Debug.Log("I'll help you discover and specify your game through guided questions.");
            Debug.Log("Let's start with some foundational questions...\n");
            
            try
            {
                // Create specification from command line arguments
                var spec = CreateSpecificationFromArgs();
                
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
        
        private static GameSpecification CreateSpecificationFromArgs()
        {
            var args = Environment.GetCommandLineArgs();
            
            Debug.Log("🔍 === PHASE 1: CORE VISION DISCOVERY ===");
            var inspiration = ReadArg(args, "--inspiration") ?? "Another game inspired me";
            var emotionalGoal = ReadArg(args, "--emotionalGoal") ?? "Excitement and adrenaline";
            var coreFantasy = ReadArg(args, "--coreFantasy") ?? "Combat prowess";
            
            Debug.Log($"❓ What sparked your interest in creating this game?");
            Debug.Log($"✓ You chose: {inspiration}");
            Debug.Log($"❓ What feeling do you want players to have?");
            Debug.Log($"✓ You chose: {emotionalGoal}");
            Debug.Log($"❓ What's the core fantasy you want to fulfill?");
            Debug.Log($"✓ You chose: {coreFantasy}");
            Debug.Log("");
            
            Debug.Log("🎨 === PHASE 2: GENRE AND STYLE EXPLORATION ===");
            var primaryGenre = ReadArg(args, "--primaryGenre") ?? "Action/Adventure";
            var visualStyle = ReadArg(args, "--visualStyle") ?? "Stylized/Cartoon";
            var perspective = ReadArg(args, "--perspective") ?? "First-person (immersive)";
            
            Debug.Log($"❓ What genre feels most natural?");
            Debug.Log($"✓ You chose: {primaryGenre}");
            Debug.Log($"❓ What visual style resonates with your game?");
            Debug.Log($"✓ You chose: {visualStyle}");
            Debug.Log($"❓ From what perspective should players experience your game?");
            Debug.Log($"✓ You chose: {perspective}");
            Debug.Log("");
            
            Debug.Log("⚙️ === PHASE 3: GAMEPLAY MECHANICS DEEP DIVE ===");
            var coreInteraction = ReadArg(args, "--coreInteraction") ?? "Combat/Fighting";
            var progressionSystem = ReadArg(args, "--progressionSystem") ?? "Character leveling";
            var challengeType = ReadArg(args, "--challengeType") ?? "Reflex-based action";
            var multiplayerType = ReadArg(args, "--multiplayerType") ?? "Single-player only";
            
            Debug.Log($"❓ What's the primary way players interact with your game world?");
            Debug.Log($"✓ You chose: {coreInteraction}");
            Debug.Log($"❓ How should players grow and improve in your game?");
            Debug.Log($"✓ You chose: {progressionSystem}");
            Debug.Log($"❓ What kind of challenges should players face?");
            Debug.Log($"✓ You chose: {challengeType}");
            Debug.Log($"❓ Should other players be part of the experience?");
            Debug.Log($"✓ You chose: {multiplayerType}");
            Debug.Log("");
            
            Debug.Log("🌍 === PHASE 4: WORLD AND NARRATIVE DESIGN ===");
            var setting = ReadArg(args, "--setting") ?? "Fantasy/Medieval";
            var narrativeApproach = ReadArg(args, "--narrativeApproach") ?? "Story-driven (narrative is primary)";
            var playerCharacter = ReadArg(args, "--playerCharacter") ?? "Predefined hero";
            
            Debug.Log($"❓ Where does your game take place?");
            Debug.Log($"✓ You chose: {setting}");
            Debug.Log($"❓ How important is story to your game?");
            Debug.Log($"✓ You chose: {narrativeApproach}");
            Debug.Log($"❓ Who is the player character?");
            Debug.Log($"✓ You chose: {playerCharacter}");
            Debug.Log("");
            
            Debug.Log("🔧 === PHASE 5: TECHNICAL AND SCOPE DEFINITION ===");
            var developmentScope = ReadArg(args, "--developmentScope") ?? "Medium game (10-20 hours)";
            var targetPlatform = ReadArg(args, "--targetPlatform") ?? "PC (Windows/Mac/Linux)";
            var uniqueSellingPoint = ReadArg(args, "--uniqueSellingPoint") ?? "Innovative mechanics";
            
            Debug.Log($"❓ What's your target scope for this project?");
            Debug.Log($"✓ You chose: {developmentScope}");
            Debug.Log($"❓ What platform(s) are you targeting?");
            Debug.Log($"✓ You chose: {targetPlatform}");
            Debug.Log($"❓ What makes your game special?");
            Debug.Log($"✓ You chose: {uniqueSellingPoint}");
            Debug.Log("");
            
            Debug.Log("📋 === PHASE 6: FINAL SPECIFICATION REVIEW ===");
            var finalChoice = ReadArg(args, "--finalChoice") ?? "This looks perfect!";
            Debug.Log($"❓ Does this specification capture your vision?");
            Debug.Log($"✓ You chose: {finalChoice}");
            Debug.Log("");
            
            return new GameSpecification
            {
                // Core Vision
                Inspiration = inspiration,
                EmotionalGoal = emotionalGoal,
                CoreFantasy = coreFantasy,
                
                // Style & Genre
                PrimaryGenre = primaryGenre,
                VisualStyle = visualStyle,
                Perspective = perspective,
                
                // Gameplay Mechanics
                CoreInteraction = coreInteraction,
                ProgressionSystem = progressionSystem,
                ChallengeType = challengeType,
                MultiplayerType = multiplayerType,
                
                // World & Narrative
                Setting = setting,
                NarrativeApproach = narrativeApproach,
                PlayerCharacter = playerCharacter,
                
                // Technical Scope
                DevelopmentScope = developmentScope,
                TargetPlatform = targetPlatform,
                UniqueSellingPoint = uniqueSellingPoint,
                
                // Metadata
                CreatedAt = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Version = "1.0"
            };
        }
        
        private static string ReadArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }
            return null;
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
