using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Shared
{
    /// <summary>
    /// Centralized collection of system-related constants with case-insensitive and spelling-tolerant matching.
    /// </summary>
    public static class SystemConstants
    {
        /// <summary>
        /// Operating system names and identifiers with multiple variations for case-insensitive matching.
        /// </summary>
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

        /// <summary>
        /// Runtime names and identifiers with multiple variations for case-insensitive matching.
        /// </summary>
        public static class Runtimes
        {
            // .NET variations
            public const string DotNet = ".NET";
            public const string DotNetCore = ".NET Core";
            public const string DotNetFramework = ".NET Framework";
            public const string DotNet5 = ".NET 5";
            public const string DotNet6 = ".NET 6";
            public const string DotNet7 = ".NET 7";
            public const string DotNet8 = ".NET 8";
            public const string DotNet9 = ".NET 9";
            public const string Net = ".NET";
            public const string NetCore = ".NET Core";
            public const string NetFramework = ".NET Framework";
            
            // Java variations
            public const string Java = "Java";
            public const string Java8 = "Java 8";
            public const string Java11 = "Java 11";
            public const string Java17 = "Java 17";
            public const string Java21 = "Java 21";
            public const string JDK = "JDK";
            public const string JRE = "JRE";
            
            // Other runtime variations
            public const string NodeJS = "Node.js";
            public const string Node = "Node";
            public const string Python = "Python";
            public const string Python2 = "Python 2";
            public const string Python3 = "Python 3";
            public const string Py = "Py";
            
            public const string Go = "Go";
            public const string Golang = "Golang";
            public const string Rust = "Rust";
            public const string Cpp = "C++";
            public const string C = "C";
            
            public const string PHP = "PHP";
            public const string Ruby = "Ruby";
            public const string Swift = "Swift";
            public const string Kotlin = "Kotlin";
            public const string Scala = "Scala";
            
            public const string Unknown = "Unknown";

            /// <summary>
            /// Gets all .NET variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> DotNetVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                DotNet, DotNetCore, DotNetFramework, DotNet5, DotNet6, DotNet7, DotNet8, DotNet9,
                Net, NetCore, NetFramework,
                ".net", ".net core", ".net framework", "dotnet", "dotnetcore", "dotnetframework"
            };

            /// <summary>
            /// Gets all Java variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> JavaVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Java, Java8, Java11, Java17, Java21, JDK, JRE,
                "java", "jdk", "jre", "java8", "java11", "java17", "java21"
            };

            /// <summary>
            /// Gets all Node.js variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> NodeJSVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                NodeJS, Node,
                "nodejs", "node.js", "node"
            };

            /// <summary>
            /// Gets all Python variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> PythonVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Python, Python2, Python3, Py,
                "python", "python2", "python3", "py"
            };

            /// <summary>
            /// Tries to match a runtime name case-insensitively.
            /// </summary>
            /// <param name="runtimeName">The runtime name to match.</param>
            /// <returns>The standardized runtime name or Unknown if not found.</returns>
            public static string MatchRuntime(string runtimeName)
            {
                if (string.IsNullOrWhiteSpace(runtimeName))
                    return Unknown;

                var normalizedName = runtimeName.Trim();
                
                if (DotNetVariations.Contains(normalizedName))
                    return DotNet;
                
                if (JavaVariations.Contains(normalizedName))
                    return Java;
                
                if (NodeJSVariations.Contains(normalizedName))
                    return NodeJS;
                
                if (PythonVariations.Contains(normalizedName))
                    return Python;
                
                if (normalizedName.Equals(Go, StringComparison.OrdinalIgnoreCase) || 
                    normalizedName.Equals(Golang, StringComparison.OrdinalIgnoreCase))
                    return Go;
                
                if (normalizedName.Equals(Rust, StringComparison.OrdinalIgnoreCase))
                    return Rust;
                
                if (normalizedName.Equals(Cpp, StringComparison.OrdinalIgnoreCase) || 
                    normalizedName.Equals("cpp", StringComparison.OrdinalIgnoreCase))
                    return Cpp;
                
                if (normalizedName.Equals(C, StringComparison.OrdinalIgnoreCase))
                    return C;
                
                if (normalizedName.Equals(PHP, StringComparison.OrdinalIgnoreCase))
                    return PHP;
                
                if (normalizedName.Equals(Ruby, StringComparison.OrdinalIgnoreCase))
                    return Ruby;
                
                if (normalizedName.Equals(Swift, StringComparison.OrdinalIgnoreCase))
                    return Swift;
                
                if (normalizedName.Equals(Kotlin, StringComparison.OrdinalIgnoreCase))
                    return Kotlin;
                
                if (normalizedName.Equals(Scala, StringComparison.OrdinalIgnoreCase))
                    return Scala;
                
                return Unknown;
            }
        }

        /// <summary>
        /// Container runtime names and identifiers with case-insensitive matching.
        /// </summary>
        public static class ContainerRuntimes
        {
            public const string Docker = "docker";
            public const string Podman = "podman";
            public const string Containerd = "containerd";
            public const string CRIO = "cri-o";
            public const string LXC = "lxc";
            public const string LXD = "lxd";
            public const string Runc = "runc";
            public const string None = "none";

            /// <summary>
            /// Gets all container runtime variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> AllVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Docker, Podman, Containerd, CRIO, LXC, LXD, Runc, None,
                "DOCKER", "PODMAN", "CONTAINERD", "CRI-O", "LXC", "LXD", "RUNC"
            };

            /// <summary>
            /// Tries to match a container runtime name case-insensitively.
            /// </summary>
            /// <param name="runtimeName">The container runtime name to match.</param>
            /// <returns>The standardized container runtime name or None if not found.</returns>
            public static string MatchContainerRuntime(string runtimeName)
            {
                if (string.IsNullOrWhiteSpace(runtimeName))
                    return None;

                var normalizedName = runtimeName.Trim();
                
                if (AllVariations.Contains(normalizedName))
                    return normalizedName.ToLowerInvariant();
                
                return None;
            }
        }

        /// <summary>
        /// Architecture names and identifiers with case-insensitive matching.
        /// </summary>
        public static class Architectures
        {
            public const string X86 = "x86";
            public const string X64 = "x64";
            public const string AMD64 = "amd64";
            public const string ARM = "arm";
            public const string ARM64 = "arm64";
            public const string AARCH64 = "aarch64";
            public const string PPC64 = "ppc64";
            public const string PPC64LE = "ppc64le";
            public const string S390X = "s390x";
            public const string RISCV64 = "riscv64";
            public const string Unknown = "unknown";

            /// <summary>
            /// Gets all architecture variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> AllVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                X86, X64, AMD64, ARM, ARM64, AARCH64, PPC64, PPC64LE, S390X, RISCV64,
                "X86", "X64", "AMD64", "ARM", "ARM64", "AARCH64", "PPC64", "PPC64LE", "S390X", "RISCV64"
            };

            /// <summary>
            /// Tries to match an architecture name case-insensitively.
            /// </summary>
            /// <param name="architectureName">The architecture name to match.</param>
            /// <returns>The standardized architecture name or Unknown if not found.</returns>
            public static string MatchArchitecture(string architectureName)
            {
                if (string.IsNullOrWhiteSpace(architectureName))
                    return Unknown;

                var normalizedName = architectureName.Trim();
                
                if (AllVariations.Contains(normalizedName))
                    return normalizedName.ToLowerInvariant();
                
                return Unknown;
            }
        }

        /// <summary>
        /// File system types and identifiers with case-insensitive matching.
        /// </summary>
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

        /// <summary>
        /// Network protocol names and identifiers with case-insensitive matching.
        /// </summary>
        public static class Protocols
        {
            public const string HTTP = "http";
            public const string HTTPS = "https";
            public const string FTP = "ftp";
            public const string SFTP = "sftp";
            public const string SSH = "ssh";
            public const string TCP = "tcp";
            public const string UDP = "udp";
            public const string WebSocket = "ws";
            public const string WebSocketSecure = "wss";
            public const string GRPC = "grpc";
            public const string GRPCS = "grpcs";

            /// <summary>
            /// Gets all protocol variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> AllVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                HTTP, HTTPS, FTP, SFTP, SSH, TCP, UDP, WebSocket, WebSocketSecure, GRPC, GRPCS,
                "HTTP", "HTTPS", "FTP", "SFTP", "SSH", "TCP", "UDP", "WS", "WSS", "GRPC", "GRPCS"
            };

            /// <summary>
            /// Tries to match a protocol name case-insensitively.
            /// </summary>
            /// <param name="protocolName">The protocol name to match.</param>
            /// <returns>The standardized protocol name or empty string if not found.</returns>
            public static string MatchProtocol(string protocolName)
            {
                if (string.IsNullOrWhiteSpace(protocolName))
                    return string.Empty;

                var normalizedName = protocolName.Trim();
                
                if (AllVariations.Contains(normalizedName))
                    return normalizedName.ToLowerInvariant();
                
                return string.Empty;
            }
        }

        /// <summary>
        /// Database system names and identifiers with case-insensitive matching.
        /// </summary>
        public static class Databases
        {
            public const string SQLServer = "SQL Server";
            public const string PostgreSQL = "PostgreSQL";
            public const string MySQL = "MySQL";
            public const string MariaDB = "MariaDB";
            public const string Oracle = "Oracle";
            public const string SQLite = "SQLite";
            public const string MongoDB = "MongoDB";
            public const string Redis = "Redis";
            public const string Cassandra = "Cassandra";
            public const string Elasticsearch = "Elasticsearch";
            public const string InfluxDB = "InfluxDB";
            public const string CosmosDB = "Cosmos DB";
            public const string DynamoDB = "DynamoDB";

            /// <summary>
            /// Gets all database variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> AllVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                SQLServer, PostgreSQL, MySQL, MariaDB, Oracle, SQLite, MongoDB, Redis, 
                Cassandra, Elasticsearch, InfluxDB, CosmosDB, DynamoDB,
                "sql server", "postgresql", "mysql", "mariadb", "oracle", "sqlite", "mongodb", "redis",
                "cassandra", "elasticsearch", "influxdb", "cosmos db", "dynamodb"
            };

            /// <summary>
            /// Tries to match a database name case-insensitively.
            /// </summary>
            /// <param name="databaseName">The database name to match.</param>
            /// <returns>The standardized database name or empty string if not found.</returns>
            public static string MatchDatabase(string databaseName)
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return string.Empty;

                var normalizedName = databaseName.Trim();
                
                if (AllVariations.Contains(normalizedName))
                    return normalizedName;
                
                return string.Empty;
            }
        }

        /// <summary>
        /// Cloud provider names and identifiers with case-insensitive matching.
        /// </summary>
        public static class CloudProviders
        {
            public const string AWS = "AWS";
            public const string Azure = "Azure";
            public const string GoogleCloud = "Google Cloud";
            public const string GCP = "GCP";
            public const string DigitalOcean = "DigitalOcean";
            public const string Linode = "Linode";
            public const string Vultr = "Vultr";
            public const string Heroku = "Heroku";
            public const string IBMCloud = "IBM Cloud";
            public const string OracleCloud = "Oracle Cloud";
            public const string AlibabaCloud = "Alibaba Cloud";
            public const string TencentCloud = "Tencent Cloud";
            public const string OnPremises = "On-Premises";

            /// <summary>
            /// Gets all cloud provider variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> AllVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                AWS, Azure, GoogleCloud, GCP, DigitalOcean, Linode, Vultr, Heroku, 
                IBMCloud, OracleCloud, AlibabaCloud, TencentCloud, OnPremises,
                "aws", "azure", "google cloud", "gcp", "digitalocean", "linode", "vultr", "heroku",
                "ibm cloud", "oracle cloud", "alibaba cloud", "tencent cloud", "on-premises"
            };

            /// <summary>
            /// Tries to match a cloud provider name case-insensitively.
            /// </summary>
            /// <param name="providerName">The cloud provider name to match.</param>
            /// <returns>The standardized cloud provider name or empty string if not found.</returns>
            public static string MatchCloudProvider(string providerName)
            {
                if (string.IsNullOrWhiteSpace(providerName))
                    return string.Empty;

                var normalizedName = providerName.Trim();
                
                if (AllVariations.Contains(normalizedName))
                    return normalizedName;
                
                return string.Empty;
            }
        }

        /// <summary>
        /// CI/CD platform names and identifiers with case-insensitive matching.
        /// </summary>
        public static class CiCdPlatforms
        {
            public const string GitHubActions = "GitHub Actions";
            public const string GitLabCI = "GitLab CI";
            public const string AzureDevOps = "Azure DevOps";
            public const string Jenkins = "Jenkins";
            public const string CircleCI = "CircleCI";
            public const string TravisCI = "Travis CI";
            public const string TeamCity = "TeamCity";
            public const string Bamboo = "Bamboo";
            public const string Concourse = "Concourse";
            public const string Drone = "Drone";
            public const string Buildkite = "Buildkite";
            public const string AppVeyor = "AppVeyor";

            /// <summary>
            /// Gets all CI/CD platform variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> AllVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                GitHubActions, GitLabCI, AzureDevOps, Jenkins, CircleCI, TravisCI, 
                TeamCity, Bamboo, Concourse, Drone, Buildkite, AppVeyor,
                "github actions", "gitlab ci", "azure devops", "jenkins", "circleci", "travis ci",
                "teamcity", "bamboo", "concourse", "drone", "buildkite", "appveyor"
            };

            /// <summary>
            /// Tries to match a CI/CD platform name case-insensitively.
            /// </summary>
            /// <param name="platformName">The CI/CD platform name to match.</param>
            /// <returns>The standardized CI/CD platform name or empty string if not found.</returns>
            public static string MatchCiCdPlatform(string platformName)
            {
                if (string.IsNullOrWhiteSpace(platformName))
                    return string.Empty;

                var normalizedName = platformName.Trim();
                
                if (AllVariations.Contains(normalizedName))
                    return normalizedName;
                
                return string.Empty;
            }
        }

        /// <summary>
        /// Package manager names and identifiers with case-insensitive matching.
        /// </summary>
        public static class PackageManagers
        {
            public const string NuGet = "NuGet";
            public const string NPM = "npm";
            public const string Yarn = "yarn";
            public const string PNPM = "pnpm";
            public const string Maven = "Maven";
            public const string Gradle = "Gradle";
            public const string PIP = "pip";
            public const string Conda = "conda";
            public const string Composer = "Composer";
            public const string Gem = "gem";
            public const string Cargo = "cargo";
            public const string GoModules = "go modules";
            public const string Chocolatey = "Chocolatey";
            public const string Homebrew = "Homebrew";
            public const string APT = "apt";
            public const string YUM = "yum";
            public const string DNF = "dnf";
            public const string Pacman = "pacman";

            /// <summary>
            /// Gets all package manager variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> AllVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                NuGet, NPM, Yarn, PNPM, Maven, Gradle, PIP, Conda, Composer, Gem, Cargo, 
                GoModules, Chocolatey, Homebrew, APT, YUM, DNF, Pacman,
                "nuget", "npm", "yarn", "pnpm", "maven", "gradle", "pip", "conda", "composer", 
                "gem", "cargo", "go modules", "chocolatey", "homebrew", "apt", "yum", "dnf", "pacman"
            };

            /// <summary>
            /// Tries to match a package manager name case-insensitively.
            /// </summary>
            /// <param name="managerName">The package manager name to match.</param>
            /// <returns>The standardized package manager name or empty string if not found.</returns>
            public static string MatchPackageManager(string managerName)
            {
                if (string.IsNullOrWhiteSpace(managerName))
                    return string.Empty;

                var normalizedName = managerName.Trim();
                
                if (AllVariations.Contains(normalizedName))
                    return normalizedName;
                
                return string.Empty;
            }
        }
    }
} 