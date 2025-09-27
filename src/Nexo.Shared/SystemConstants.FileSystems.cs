using System;
using System.Collections.Generic;

namespace Nexo.Shared
{
    /// <summary>
    /// File system types and identifiers with case-insensitive matching.
    /// </summary>
    public static partial class SystemConstants
    {
        public static class FileSystems
        {
            public const string NTFS = "NTFS";
            public const string FAT32 = "FAT32";
            public const string EXFAT = "exFAT";
            public const string EXT4 = "ext4";
            public const string EXT3 = "ext3";
            public const string EXT2 = "ext2";
            public const string XFS = "XFS";
            public const string BTRFS = "Btrfs";
            public const string ZFS = "ZFS";
            public const string APFS = "APFS";
            public const string HFS = "HFS";
            public const string HFSPlus = "HFS+";
            public const string Unknown = "Unknown";

            /// <summary>
            /// Gets all filesystem variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> AllVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                NTFS, FAT32, EXFAT, EXT4, EXT3, EXT2, XFS, BTRFS, ZFS, APFS, HFS, HFSPlus,
                "ntfs", "fat32", "exfat", "ext4", "ext3", "ext2", "xfs", "btrfs", "zfs", "apfs", "hfs", "hfs+"
            };

            /// <summary>
            /// Tries to match a filesystem name case-insensitively.
            /// </summary>
            /// <param name="filesystemName">The filesystem name to match.</param>
            /// <returns>The standardized filesystem name or Unknown if not found.</returns>
            public static string MatchFileSystem(string filesystemName)
            {
                if (string.IsNullOrWhiteSpace(filesystemName))
                    return Unknown;

                var normalizedName = filesystemName.Trim();
                
                if (AllVariations.Contains(normalizedName))
                    return normalizedName.ToUpperInvariant();
                
                return Unknown;
            }
        }
    }
}
