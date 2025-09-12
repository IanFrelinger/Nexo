using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Common;

namespace Nexo.Core.Domain.Models.Extensions
{
    /// <summary>
    /// Represents generated code for an extension
    /// </summary>
    public class GeneratedCode : BaseResult
    {
        /// <summary>
        /// The generated C# code
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// The file name for the generated code
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// The file extension for the generated code
        /// </summary>
        public string FileExtension { get; set; } = ".cs";

        /// <summary>
        /// Additional files generated (e.g., project files, config files)
        /// </summary>
        public Dictionary<string, string> AdditionalFiles { get; set; } = new();

        /// <summary>
        /// Metadata about the generation process
        /// </summary>
        public Dictionary<string, object> GenerationMetadata { get; set; } = new();

        /// <summary>
        /// Warnings generated during code generation
        /// </summary>
        public new List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Whether the code was successfully generated
        /// </summary>
        public new bool IsSuccess => !string.IsNullOrWhiteSpace(Code);

        /// <summary>
        /// Gets the full file path
        /// </summary>
        public string FullPath => $"{FileName}{FileExtension}";

        /// <summary>
        /// Adds a warning to the generated code
        /// </summary>
        /// <param name="warning">Warning message</param>
        public new void AddWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                Warnings.Add(warning);
            }
        }

        /// <summary>
        /// Adds multiple warnings to the generated code
        /// </summary>
        /// <param name="warnings">Warning messages</param>
        public void AddWarnings(IEnumerable<string> warnings)
        {
            if (warnings != null)
            {
                foreach (var warning in warnings)
                {
                    AddWarning(warning);
                }
            }
        }

        /// <summary>
        /// Adds an additional file to the generated code
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <param name="content">Content of the file</param>
        public void AddAdditionalFile(string fileName, string content)
        {
            if (!string.IsNullOrWhiteSpace(fileName) && !string.IsNullOrWhiteSpace(content))
            {
                AdditionalFiles[fileName] = content;
            }
        }

        /// <summary>
        /// Gets a summary of the generated code
        /// </summary>
        /// <returns>Summary string</returns>
        public string GetSummary()
        {
            var lines = new List<string>
            {
                $"Generated Code: {FileName}{FileExtension}",
                $"Code Length: {Code.Length} characters",
                $"Lines of Code: {Code.Split('\n').Length}",
                $"Additional Files: {AdditionalFiles.Count}",
                $"Warnings: {Warnings.Count}"
            };

            if (Warnings.Count > 0)
            {
                lines.Add("Warnings:");
                lines.AddRange(Warnings.Select(w => $"  - {w}"));
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}
