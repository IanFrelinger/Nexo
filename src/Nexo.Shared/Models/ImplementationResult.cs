using System;
using System.Collections.Generic;

namespace Nexo.Shared.Models
{
    /// <summary>
    /// Represents the result of a feature implementation process.
    /// </summary>
    public sealed class ImplementationResult
    {
        public string FeatureName { get; set; }
        public List<string> GeneratedFiles { get; set; }
        public List<string> ModifiedFiles { get; set; }
        public List<string> ExecutedCommands { get; set; }
        public bool IsSuccess { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime CompletedAt { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public ImplementationResult()
        {
            GeneratedFiles = new List<string>();
            ModifiedFiles = new List<string>();
            ExecutedCommands = new List<string>();
            Errors = new List<string>();
            Warnings = new List<string>();
        }
    }
} 