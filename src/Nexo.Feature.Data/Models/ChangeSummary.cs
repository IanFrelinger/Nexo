namespace Nexo.Feature.Data.Models
{
    /// <summary>
    /// Summary of entity changes in the database context
    /// </summary>
    public class ChangeSummary
    {
        /// <summary>
        /// Number of added entities
        /// </summary>
        public int AddedCount { get; set; }

        /// <summary>
        /// Number of modified entities
        /// </summary>
        public int ModifiedCount { get; set; }

        /// <summary>
        /// Number of deleted entities
        /// </summary>
        public int DeletedCount { get; set; }

        /// <summary>
        /// Number of unchanged entities
        /// </summary>
        public int UnchangedCount { get; set; }

        /// <summary>
        /// Number of detached entities
        /// </summary>
        public int DetachedCount { get; set; }

        /// <summary>
        /// Total number of tracked entities
        /// </summary>
        public int TotalCount => AddedCount + ModifiedCount + DeletedCount + UnchangedCount + DetachedCount;

        /// <summary>
        /// Whether there are any changes to be saved
        /// </summary>
        public bool HasChanges => AddedCount > 0 || ModifiedCount > 0 || DeletedCount > 0;

        /// <summary>
        /// Gets a summary string of the changes
        /// </summary>
        /// <returns>Formatted summary string</returns>
        public override string ToString()
        {
            var parts = new List<string>();

            if (AddedCount > 0)
                parts.Add($"{AddedCount} added");
            if (ModifiedCount > 0)
                parts.Add($"{ModifiedCount} modified");
            if (DeletedCount > 0)
                parts.Add($"{DeletedCount} deleted");
            if (UnchangedCount > 0)
                parts.Add($"{UnchangedCount} unchanged");
            if (DetachedCount > 0)
                parts.Add($"{DetachedCount} detached");

            return parts.Count > 0 ? string.Join(", ", parts) : "No changes";
        }

        /// <summary>
        /// Gets a detailed summary with entity types
        /// </summary>
        /// <param name="entityChanges">Dictionary of entity type to change counts</param>
        /// <returns>Detailed summary string</returns>
        public string GetDetailedSummary(Dictionary<string, int> entityChanges)
        {
            var summary = ToString();
            
            if (entityChanges.Any())
            {
                var details = entityChanges.Select(kvp => $"{kvp.Key}: {kvp.Value}");
                summary += $" | Details: {string.Join(", ", details)}";
            }

            return summary;
        }
    }
} 