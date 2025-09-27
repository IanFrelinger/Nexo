using System;
using System.Collections.Generic;

namespace Nexo.Shared
{
    /// <summary>
    /// Runtime names and identifiers with multiple variations for case-insensitive matching.
    /// </summary>
    public static partial class SystemConstants
    {
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
    }
}
