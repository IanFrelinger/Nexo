using System;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Help and utilities functionality for hardware command.
    /// </summary>
    public partial class HardwareCommand
    {
        /// <summary>
        /// Shows help information
        /// </summary>
        private static void ShowHelp()
        {
            Console.WriteLine("Computer Nexo Hardware Requirements");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("   nexo hardware                    - Show hardware dashboard");
            Console.WriteLine("   nexo hardware check             - Check system requirements");
            Console.WriteLine("   nexo hardware cloud             - Show cloud fallback options");
            Console.WriteLine("   nexo hardware recommend         - Show recommendations");
            Console.WriteLine("   nexo hardware cost [hours]      - Estimate cloud costs");
            Console.WriteLine("   nexo hardware tiers             - Show performance tiers");
            Console.WriteLine();
        }

        /// <summary>
        /// Formats bytes into human-readable format
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }
    }
}
