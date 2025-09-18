using Nexo.Verify.Models;
using Nexo.Verify.IO;

namespace Nexo.Verify.Asserts
{
    /// <summary>
    /// Assert that a file has the required structure (e.g., CSV columns)
    /// </summary>
    public static class StructureContainsAssert
    {
        /// <summary>
        /// Verify that a CSV file contains all required columns
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <param name="requiredColumns">List of required column names</param>
        /// <param name="console">Console for output</param>
        /// <returns>True if all required columns are present</returns>
        public static bool Verify(string filePath, IEnumerable<string> requiredColumns, IConsole console)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    console.WriteLine($"ERROR: File not found: {filePath}");
                    return false;
                }

                var hasRequiredColumns = CsvUtils.HasRequiredColumns(filePath, requiredColumns);
                
                if (hasRequiredColumns)
                {
                    console.WriteLine($"✓ File contains all required columns: {Path.GetFileName(filePath)}");
                    return true;
                }
                else
                {
                    console.WriteLine($"✗ File missing required columns: {Path.GetFileName(filePath)}");
                    
                    var headers = CsvUtils.ReadHeader(filePath);
                    var missingColumns = requiredColumns.Except(headers, StringComparer.OrdinalIgnoreCase);
                    
                    console.WriteLine($"  Required: {string.Join(", ", requiredColumns)}");
                    console.WriteLine($"  Present:  {string.Join(", ", headers)}");
                    console.WriteLine($"  Missing:  {string.Join(", ", missingColumns)}");
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                console.WriteLine($"ERROR: Failed to verify structure for {filePath}: {ex.Message}");
                return false;
            }
        }
    }
}
