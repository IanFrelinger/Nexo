using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// Unity Editor window with chat interface for planning agent interaction.
    /// </summary>
    public class IterativeDemoChatWindow : EditorWindow
    {
        private Vector2 chatScrollPosition;
        private Vector2 recommendationScrollPosition;
        private string currentInput = "";
        private List<ChatMessage> chatHistory = new List<ChatMessage>();
        private PlanningAgentState agentState = PlanningAgentState.Waiting;
        private PlanningQuestion? currentQuestion;
        private string? currentRecommendation;
        private List<string> recommendedFeatures = new List<string>();
        private List<string> iterationPriorities = new List<string>();
        private double planningProgress = 0.0;
        private bool showRecommendation = false;

        [MenuItem("Nexo/Planning Chat Window")]
        public static void ShowWindow()
        {
            IterativeDemoChatWindow window = GetWindow<IterativeDemoChatWindow>("Planning Chat");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnEnable()
        {
            // Start with welcome message
            chatHistory.Add(new ChatMessage
            {
                Sender = "Assistant",
                Text = "👋 Welcome! I'm here to help guide you through creating your game. Let me ask you a few questions to understand what you'd like to build.",
                Timestamp = DateTime.Now
            });
            
            // Initialize planning agent
            StartPlanning();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // Header
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 16;
            EditorGUILayout.LabelField("🎮 Game Development Planning Assistant", headerStyle);
            EditorGUILayout.Space(5);
            
            // Progress bar
            if (planningProgress > 0)
            {
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), (float)planningProgress, 
                    $"Planning Progress: {planningProgress:P0}");
                EditorGUILayout.Space(5);
            }
            
            // Chat area
            EditorGUILayout.LabelField("Conversation", EditorStyles.boldLabel);
            chatScrollPosition = EditorGUILayout.BeginScrollView(chatScrollPosition, GUILayout.Height(250));
            
            foreach (var message in chatHistory)
            {
                DrawMessage(message);
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(10);
            
            // Current question or recommendation
            if (showRecommendation && !string.IsNullOrEmpty(currentRecommendation))
            {
                EditorGUILayout.LabelField("Recommendation", EditorStyles.boldLabel);
                recommendationScrollPosition = EditorGUILayout.BeginScrollView(recommendationScrollPosition, GUILayout.Height(150));
                
                EditorGUILayout.LabelField(currentRecommendation, EditorStyles.wordWrappedLabel);
                
                if (recommendedFeatures.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Recommended Features:", EditorStyles.boldLabel);
                    foreach (var feature in recommendedFeatures)
                    {
                        EditorGUILayout.LabelField($"  • {feature}", EditorStyles.wordWrappedLabel);
                    }
                }
                
                if (iterationPriorities.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Iteration Priorities:", EditorStyles.boldLabel);
                    foreach (var priority in iterationPriorities)
                    {
                        EditorGUILayout.LabelField($"  • {priority}", EditorStyles.wordWrappedLabel);
                    }
                }
                
                EditorGUILayout.EndScrollView();
                EditorGUILayout.Space(10);
                
                if (GUILayout.Button("Start Development", GUILayout.Height(30)))
                {
                    StartDevelopment();
                }
            }
            else if (currentQuestion != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Current Question", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(currentQuestion.Text, EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space(5);
                
                // Show options if available
                if (currentQuestion.Options != null && currentQuestion.Options.Length > 0)
                {
                    EditorGUILayout.LabelField("Quick Options:", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal();
                    
                    int optionsPerRow = 2;
                    for (int i = 0; i < currentQuestion.Options.Length; i++)
                    {
                        if (i > 0 && i % optionsPerRow == 0)
                        {
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                        }
                        
                        if (GUILayout.Button(currentQuestion.Options[i], GUILayout.Height(25)))
                        {
                            SubmitResponse(currentQuestion.Options[i]);
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.Space(10);
            }
            
            // Input area
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Your response:", GUILayout.Width(100));
            currentInput = EditorGUILayout.TextField(currentInput);
            
            GUI.enabled = !string.IsNullOrWhiteSpace(currentInput) && agentState == PlanningAgentState.Waiting;
            if (GUILayout.Button("Send", GUILayout.Width(60), GUILayout.Height(20)))
            {
                SubmitResponse(currentInput);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Reset button
            if (GUILayout.Button("🔄 Start Over"))
            {
                ResetPlanning();
            }
        }

        private void DrawMessage(ChatMessage message)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(message.Sender, EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField(message.Timestamp.ToString("HH:mm:ss"), EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField(message.Text, EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void StartPlanning()
        {
            agentState = PlanningAgentState.Processing;
            
            // Simulate planning agent call
            // In a real implementation, this would call the PlanningAgent via orchestration
            SimulatePlanningAgentResponse();
        }

        private void SimulatePlanningAgentResponse()
        {
            // This simulates the planning agent - in production, this would call the actual agent
            var questions = new[]
            {
                new PlanningQuestion
                {
                    Id = "genre",
                    Text = "What genre of game would you like to create? (e.g., Action, RPG, Puzzle, Strategy, Platformer)",
                    Category = "Core",
                    Options = new[] { "Action", "RPG", "Puzzle", "Strategy", "Platformer", "Other" }
                },
                new PlanningQuestion
                {
                    Id = "scope",
                    Text = "What's the scope of your game?",
                    Category = "Scope",
                    Options = new[] { "Small (prototype)", "Medium (indie game)", "Large (full game)" }
                },
                new PlanningQuestion
                {
                    Id = "target_audience",
                    Text = "Who is your target audience?",
                    Category = "Audience",
                    Options = new[] { "Casual players", "Hardcore gamers", "Children", "Everyone" }
                },
                new PlanningQuestion
                {
                    Id = "art_style",
                    Text = "What art style do you prefer?",
                    Category = "Art",
                    Options = new[] { "Realistic", "Stylized", "Pixel art", "Low poly", "Minimalist" }
                },
                new PlanningQuestion
                {
                    Id = "core_mechanic",
                    Text = "What's the core gameplay mechanic you want to focus on?",
                    Category = "Gameplay",
                    Options = new[] { "Combat", "Exploration", "Puzzle solving", "Building", "Social interaction" }
                },
                new PlanningQuestion
                {
                    Id = "platform",
                    Text = "What platform(s) are you targeting?",
                    Category = "Platform",
                    Options = new[] { "PC", "Mobile", "Console", "Web", "Multiple" }
                }
            };

            if (chatHistory.Count == 1) // First question
            {
                currentQuestion = questions[0];
                planningProgress = 0.0;
                agentState = PlanningAgentState.Waiting;
            }
        }

        private void SubmitResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return;

            // Add user message to chat
            chatHistory.Add(new ChatMessage
            {
                Sender = "You",
                Text = response,
                Timestamp = DateTime.Now
            });

            currentInput = "";
            agentState = PlanningAgentState.Processing;

            // Process response and get next question
            ProcessResponse(response);
        }

        private void ProcessResponse(string response)
        {
            // In production, this would call the PlanningAgent with the response
            // For now, simulate progression through questions
            
            var questions = new[]
            {
                new { Id = "genre", Next = 1 },
                new { Id = "scope", Next = 2 },
                new { Id = "target_audience", Next = 3 },
                new { Id = "art_style", Next = 4 },
                new { Id = "core_mechanic", Next = 5 },
                new { Id = "platform", Next = -1 } // Last question
            };

            int currentIndex = currentQuestion != null ? 
                Array.FindIndex(questions, q => q.Id == currentQuestion.Id) : -1;

            if (currentIndex >= 0 && currentIndex < questions.Length - 1)
            {
                // Move to next question
                var nextIndex = questions[currentIndex].Next;
                if (nextIndex >= 0)
                {
                    SimulatePlanningAgentResponse();
                    currentQuestion = new PlanningQuestion
                    {
                        Id = questions[nextIndex].Id,
                        Text = GetQuestionText(questions[nextIndex].Id),
                        Category = "General",
                        Options = GetQuestionOptions(questions[nextIndex].Id)
                    };
                    planningProgress = (nextIndex + 1) / 6.0;
                    agentState = PlanningAgentState.Waiting;
                }
                else
                {
                    // All questions answered, show recommendation
                    GenerateRecommendation(response);
                }
            }
            else
            {
                GenerateRecommendation(response);
            }
        }

        private void GenerateRecommendation(string lastResponse)
        {
            agentState = PlanningAgentState.Processing;
            
            // Simulate recommendation generation
            currentRecommendation = "Based on your responses, I recommend starting with a simple action game prototype. Focus on getting the core gameplay loop fun first, then iterate based on playtest feedback.";
            recommendedFeatures = new List<string>
            {
                "Basic player movement",
                "Simple combat system",
                "Enemy AI",
                "Level progression"
            };
            iterationPriorities = new List<string>
            {
                "Polish core mechanics",
                "Add visual feedback",
                "Balance difficulty",
                "Expand content"
            };
            
            chatHistory.Add(new ChatMessage
            {
                Sender = "Assistant",
                Text = currentRecommendation,
                Timestamp = DateTime.Now
            });
            
            showRecommendation = true;
            currentQuestion = null;
            planningProgress = 1.0;
            agentState = PlanningAgentState.Waiting;
        }

        private string GetQuestionText(string questionId)
        {
            return questionId switch
            {
                "genre" => "What genre of game would you like to create?",
                "scope" => "What's the scope of your game?",
                "target_audience" => "Who is your target audience?",
                "art_style" => "What art style do you prefer?",
                "core_mechanic" => "What's the core gameplay mechanic you want to focus on?",
                "platform" => "What platform(s) are you targeting?",
                _ => "Tell me more about your game idea."
            };
        }

        private string[]? GetQuestionOptions(string questionId)
        {
            return questionId switch
            {
                "genre" => new[] { "Action", "RPG", "Puzzle", "Strategy", "Platformer" },
                "scope" => new[] { "Small (prototype)", "Medium (indie game)", "Large (full game)" },
                "target_audience" => new[] { "Casual players", "Hardcore gamers", "Children", "Everyone" },
                "art_style" => new[] { "Realistic", "Stylized", "Pixel art", "Low poly", "Minimalist" },
                "core_mechanic" => new[] { "Combat", "Exploration", "Puzzle solving", "Building", "Social interaction" },
                "platform" => new[] { "PC", "Mobile", "Console", "Web", "Multiple" },
                _ => null
            };
        }

        private void StartDevelopment()
        {
            // Create game specification based on responses
            var specPath = Application.dataPath.Replace("/Assets", "") + "/game-specification.json";
            
            EditorUtility.DisplayDialog("Ready to Start!", 
                "Game specification will be created and the iterative development process will begin.", 
                "OK");
            
            // Close this window - iterative development will start automatically
            Close();
            
            // Optionally show a status message
            EditorUtility.DisplayDialog("Development Started", 
                "The iterative game development process has been initiated. Check the console for progress updates.", 
                "OK");
        }

        private void ResetPlanning()
        {
            chatHistory.Clear();
            currentQuestion = null;
            currentRecommendation = null;
            recommendedFeatures.Clear();
            iterationPriorities.Clear();
            planningProgress = 0.0;
            showRecommendation = false;
            agentState = PlanningAgentState.Waiting;
            
            OnEnable(); // Restart
        }
    }

    /// <summary>
    /// Represents a chat message.
    /// </summary>
    public class ChatMessage
    {
        public string Sender { get; set; } = "";
        public string Text { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Planning question model.
    /// </summary>
    public class PlanningQuestion
    {
        public string Id { get; set; } = "";
        public string Text { get; set; } = "";
        public string Category { get; set; } = "";
        public string[]? Options { get; set; }
    }

    /// <summary>
    /// Planning agent state.
    /// </summary>
    public enum PlanningAgentState
    {
        Waiting,
        Processing,
        Complete
    }
}

