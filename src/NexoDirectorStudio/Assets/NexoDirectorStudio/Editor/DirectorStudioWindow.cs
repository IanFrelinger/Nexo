using UnityEngine;
using UnityEditor;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// Main editor window for Director Studio - enables non-programmers to create game slices
    /// from natural-language briefs across any genre.
    /// </summary>
    public class DirectorStudioWindow : EditorWindow
    {
        private const string WindowTitle = "Director Studio";
        private const string MenuPath = "Nexo/Director Studio";
        
        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<DirectorStudioWindow>(WindowTitle);
            window.Show();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Director Studio", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "Create game slices from natural-language briefs across any genre. " +
                "Enter your brief below and select a genre to get started.",
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            // TODO: Add brief input field
            EditorGUILayout.LabelField("Game Brief:", EditorStyles.label);
            EditorGUILayout.TextArea("Enter your game slice description here...", GUILayout.Height(100));
            
            EditorGUILayout.Space();
            
            // TODO: Add genre picker
            EditorGUILayout.LabelField("Genre:", EditorStyles.label);
            EditorGUILayout.Popup(0, new[] { "Auto-detect", "FPS", "Platformer", "RPG" });
            
            EditorGUILayout.Space();
            
            // TODO: Add action buttons
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = false; // Disabled until implementation
            if (GUILayout.Button("Plan Game Slice"))
            {
                // TODO: Implement planning
            }
            GUI.enabled = true;
            
            GUI.enabled = false; // Disabled until implementation
            if (GUILayout.Button("Playtest"))
            {
                // TODO: Implement playtest
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
    }
}
