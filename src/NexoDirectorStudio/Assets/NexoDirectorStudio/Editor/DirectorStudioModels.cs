using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Profiles;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// UI state for Director Studio window.
    /// </summary>
    public class DirectorStudioUIState
    {
        public string DesignBrief { get; set; } = "";
        public IGenreProfile SelectedGenre { get; set; }
        public GamePlan CurrentGamePlan { get; set; }
        public ValidationReport ValidationReport { get; set; }
        public bool IsGenerating { get; set; } = false;
        public string StatusMessage { get; set; } = "";
        public Vector2 ScrollPosition { get; set; }
        public int SelectedTab { get; set; } = 0;
        public List<ValidationSuggestion> AutoFixSuggestions { get; set; } = new();
        public bool ShowAutoFixPreview { get; set; } = false;
        public string AutoFixPreview { get; set; } = "";
    }

    /// <summary>
    /// UI configuration for Director Studio window.
    /// </summary>
    public static class DirectorStudioUIConfig
    {
        public const string WindowTitle = "Director Studio";
        public const string MenuPath = "Nexo/Director Studio";
        public static readonly string[] TabNames = { "Design", "Plan", "Validation", "Fix" };
        public static readonly Vector2 MinWindowSize = new Vector2(800, 600);
    }

    /// <summary>
    /// Interface for Director Studio UI components.
    /// </summary>
    public interface IDirectorStudioUIComponent
    {
        void Draw();
        void OnEnable();
        void OnDisable();
    }

    /// <summary>
    /// Interface for Director Studio operations.
    /// </summary>
    public interface IDirectorStudioOperations
    {
        System.Threading.Tasks.Task GenerateGamePlanAsync();
        System.Threading.Tasks.Task ValidateGamePlanAsync();
        void ApplyAutoFix(ValidationSuggestion suggestion);
        void StartPlaytest();
    }
}
