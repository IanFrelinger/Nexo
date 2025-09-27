using System;
using System.Collections.Generic;

namespace Nexo.Shared
{
    /// <summary>
    /// Operating system names and identifiers with multiple variations for case-insensitive matching.
    /// </summary>
    public static partial class SystemConstants
    {
        public static class OperatingSystems
        {
            // Windows variations
            public const string Windows = "Windows";
            public const string WindowsNT = "Windows NT";
            public const string WindowsServer = "Windows Server";
            public const string Windows10 = "Windows 10";
            public const string Windows11 = "Windows 11";
            public const string WindowsServer2019 = "Windows Server 2019";
            public const string WindowsServer2022 = "Windows Server 2022";
            public const string Win = "Win";
            public const string WinNT = "WinNT";
            public const string WinServer = "WinServer";
            
            // Linux variations
            public const string Linux = "Linux";
            public const string Ubuntu = "Ubuntu";
            public const string CentOS = "CentOS";
            public const string Centos = "Centos";
            public const string RedHat = "Red Hat";
            public const string Redhat = "Redhat";
            public const string Debian = "Debian";
            public const string Fedora = "Fedora";
            public const string SUSE = "SUSE";
            public const string SuSE = "SuSE";
            public const string Alpine = "Alpine";
            public const string AmazonLinux = "Amazon Linux";
            public const string AmazonLinux2 = "Amazon Linux 2";
            public const string AmazonLinux3 = "Amazon Linux 3";
            
            // macOS variations
            public const string macOS = "macOS";
            public const string MacOSX = "Mac OS X";
            public const string MacOS = "MacOS";
            public const string Darwin = "Darwin";
            public const string OSX = "OS X";
            
            // BSD variations
            public const string FreeBSD = "FreeBSD";
            public const string Freebsd = "Freebsd";
            public const string NetBSD = "NetBSD";
            public const string Netbsd = "Netbsd";
            public const string OpenBSD = "OpenBSD";
            public const string Openbsd = "Openbsd";
            
            // Mobile variations
            public const string Android = "Android";
            public const string iOS = "iOS";
            public const string IOS = "IOS";
            
            public const string Unknown = "Unknown";

            /// <summary>
            /// Gets all Windows variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> WindowsVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Windows, WindowsNT, WindowsServer, Windows10, Windows11, 
                WindowsServer2019, WindowsServer2022, Win, WinNT, WinServer,
                "windows", "win", "winnt", "winserver", "win10", "win11"
            };

            /// <summary>
            /// Gets all Linux variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> LinuxVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Linux, Ubuntu, CentOS, Centos, RedHat, Redhat, Debian, Fedora, 
                SUSE, SuSE, Alpine, AmazonLinux, AmazonLinux2, AmazonLinux3,
                "linux", "ubuntu", "centos", "redhat", "debian", "fedora", 
                "suse", "alpine", "amazonlinux", "amazon linux"
            };

            /// <summary>
            /// Gets all macOS variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> MacOSVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                macOS, MacOSX, MacOS, Darwin, OSX,
                "macos", "mac os x", "darwin", "osx", "mac"
            };

            /// <summary>
            /// Gets all BSD variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> BSDVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                FreeBSD, Freebsd, NetBSD, Netbsd, OpenBSD, Openbsd,
                "freebsd", "netbsd", "openbsd"
            };

            /// <summary>
            /// Gets all mobile variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> MobileVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Android, iOS, IOS,
                "android", "ios"
            };

            /// <summary>
            /// Tries to match an operating system name case-insensitively.
            /// </summary>
            /// <param name="osName">The operating system name to match.</param>
            /// <returns>The standardized operating system name or Unknown if not found.</returns>
            public static string MatchOperatingSystem(string osName)
            {
                if (string.IsNullOrWhiteSpace(osName))
                    return Unknown;

                var normalizedName = osName.Trim();
                
                if (WindowsVariations.Contains(normalizedName))
                    return Windows;
                
                if (LinuxVariations.Contains(normalizedName))
                    return Linux;
                
                if (MacOSVariations.Contains(normalizedName))
                    return macOS;
                
                if (BSDVariations.Contains(normalizedName))
                    return FreeBSD;
                
                if (MobileVariations.Contains(normalizedName))
                    return Android; // Default to Android for mobile
                
                return Unknown;
            }
        }
    }
}
