using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Nexo.Feature.Pipeline.Tests.Runtime
{
    /// <summary>
    /// Detects and provides information about the current runtime environment.
    /// </summary>
    public static class RuntimeDetection
    {
        /// <summary>
        /// Runtime environment types.
        /// </summary>
        public enum RuntimeType
        {
            /// <summary>
            /// Standard .NET runtime.
            /// </summary>
            DotNet,
            
            /// <summary>
            /// Unity Engine runtime.
            /// </summary>
            Unity,
            
            /// <summary>
            /// Mono runtime.
            /// </summary>
            Mono,
            
            /// <summary>
            /// CoreCLR runtime.
            /// </summary>
            CoreCLR,
            
            /// <summary>
            /// Unknown runtime.
            /// </summary>
            Unknown
        }

        /// <summary>
        /// Gets the current runtime type.
        /// </summary>
        public static RuntimeType CurrentRuntime
        {
            get
            {
                try
                {
                    // Check for Unity runtime
                    if (IsUnityRuntime())
                        return RuntimeType.Unity;
                    
                    // Check for Mono runtime
                    if (IsMonoRuntime())
                        return RuntimeType.Mono;
                    
                    // Check for CoreCLR runtime
                    if (IsCoreCLRRuntime())
                        return RuntimeType.CoreCLR;
                    
                    // Default to .NET
                    return RuntimeType.DotNet;
                }
                catch
                {
                    return RuntimeType.Unknown;
                }
            }
        }

        /// <summary>
        /// Gets the runtime version information.
        /// </summary>
        public static string RuntimeVersion
        {
            get
            {
                try
                {
                    return Environment.Version.ToString();
                }
                catch
                {
                    return "Unknown";
                }
            }
        }

        /// <summary>
        /// Gets the framework description.
        /// </summary>
        public static string FrameworkDescription
        {
            get
            {
                try
                {
                    return RuntimeInformation.FrameworkDescription;
                }
                catch
                {
                    return "Unknown";
                }
            }
        }

        /// <summary>
        /// Gets the OS platform information.
        /// </summary>
        public static string OSPlatform
        {
            get
            {
                try
                {
                    return RuntimeInformation.OSDescription;
                }
                catch
                {
                    return "Unknown";
                }
            }
        }

        /// <summary>
        /// Gets the architecture information.
        /// </summary>
        public static string Architecture
        {
            get
            {
                try
                {
                    return RuntimeInformation.OSArchitecture.ToString();
                }
                catch
                {
                    return "Unknown";
                }
            }
        }

        /// <summary>
        /// Checks if the current runtime is Unity.
        /// </summary>
        private static bool IsUnityRuntime()
        {
            try
            {
                // Check for Unity-specific assemblies
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    if (assembly.FullName?.Contains("UnityEngine") == true ||
                        assembly.FullName?.Contains("UnityEditor") == true)
                    {
                        return true;
                    }
                }

                // Check for Unity-specific types
                var unityEngineType = Type.GetType("UnityEngine.Object");
                var unityEditorType = Type.GetType("UnityEditor.Editor");
                
                return unityEngineType != null || unityEditorType != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the current runtime is Mono.
        /// </summary>
        private static bool IsMonoRuntime()
        {
            try
            {
                return Type.GetType("Mono.Runtime") != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the current runtime is CoreCLR.
        /// </summary>
        private static bool IsCoreCLRRuntime()
        {
            try
            {
                return FrameworkDescription.Contains("CoreCLR");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets detailed runtime information as a formatted string.
        /// </summary>
        public static string GetRuntimeInfo()
        {
            return $"Runtime: {CurrentRuntime}, Version: {RuntimeVersion}, Framework: {FrameworkDescription}, OS: {OSPlatform}, Architecture: {Architecture}";
        }

        /// <summary>
        /// Checks if the current runtime supports a specific feature.
        /// </summary>
        /// <param name="feature">The feature to check.</param>
        /// <returns>True if the feature is supported, false otherwise.</returns>
        public static bool SupportsFeature(string feature)
        {
            switch (feature.ToLowerInvariant())
            {
                case "async":
                case "async-await":
                    return true; // All modern runtimes support async/await
                
                case "reflection":
                    return true; // All runtimes support basic reflection
                
                case "dynamic":
                    return CurrentRuntime != RuntimeType.Unity; // Unity has limited dynamic support
                
                case "linq":
                    return true; // All runtimes support LINQ
                
                case "json":
                    return true; // All runtimes support JSON
                
                case "http":
                    return true; // All runtimes support HTTP
                
                case "threading":
                    return CurrentRuntime != RuntimeType.Unity; // Unity has limited threading support
                
                case "serialization":
                    return true; // All runtimes support basic serialization
                
                default:
                    return true; // Default to supported
            }
        }
    }
} 