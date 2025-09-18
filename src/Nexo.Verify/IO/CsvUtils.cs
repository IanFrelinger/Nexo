using CsvHelper;
using System.Globalization;

namespace Nexo.Verify.IO
{
    /// <summary>
    /// Utility functions for CSV file operations
    /// </summary>
    public static class CsvUtils
    {
        /// <summary>
        /// Read the header row from a CSV file
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <returns>Array of column names</returns>
        public static string[] ReadHeader(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"CSV file not found: {filePath}");

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            
            csv.Read();
            csv.ReadHeader();
            
            return csv.HeaderRecord ?? Array.Empty<string>();
        }

        /// <summary>
        /// Check if a CSV file has all required columns
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <param name="requiredColumns">List of required column names</param>
        /// <returns>True if all required columns are present</returns>
        public static bool HasRequiredColumns(string filePath, IEnumerable<string> requiredColumns)
        {
            var headers = ReadHeader(filePath);
            var requiredSet = requiredColumns.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var headerSet = headers.ToHashSet(StringComparer.OrdinalIgnoreCase);
            
            return requiredSet.IsSubsetOf(headerSet);
        }

        /// <summary>
        /// Get the number of data rows in a CSV file (excluding header)
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <returns>Number of data rows</returns>
        public static int GetRowCount(string filePath)
        {
            if (!File.Exists(filePath))
                return 0;

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            
            csv.Read();
            csv.ReadHeader();
            
            int rowCount = 0;
            while (csv.Read())
            {
                rowCount++;
            }
            
            return rowCount;
        }
    }
}
