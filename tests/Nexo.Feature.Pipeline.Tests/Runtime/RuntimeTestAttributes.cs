using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Nexo.Feature.Pipeline.Tests.Runtime
{
    /// <summary>
    /// Attribute to mark tests that should only run on specific runtimes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RuntimeFactAttribute : FactAttribute
    {
        public RuntimeFactAttribute(params RuntimeDetection.RuntimeType[] supportedRuntimes)
        {
            SupportedRuntimes = supportedRuntimes;
            Skip = GetSkipReason();
        }

        public RuntimeDetection.RuntimeType[] SupportedRuntimes { get; }

        private string GetSkipReason()
        {
            var currentRuntime = RuntimeDetection.CurrentRuntime;
            
            if (SupportedRuntimes.Length > 0 && !SupportedRuntimes.Contains(currentRuntime))
            {
                return $"Test is not supported on runtime {currentRuntime}. Supported runtimes: {string.Join(", ", SupportedRuntimes)}";
            }

            return null;
        }
    }

    /// <summary>
    /// Attribute to mark tests that should only run on specific runtimes with theory data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RuntimeTheoryAttribute : TheoryAttribute
    {
        public RuntimeTheoryAttribute(params RuntimeDetection.RuntimeType[] supportedRuntimes)
        {
            SupportedRuntimes = supportedRuntimes;
            Skip = GetSkipReason();
        }

        public RuntimeDetection.RuntimeType[] SupportedRuntimes { get; }

        private string GetSkipReason()
        {
            var currentRuntime = RuntimeDetection.CurrentRuntime;
            
            if (SupportedRuntimes.Length > 0 && !SupportedRuntimes.Contains(currentRuntime))
            {
                return $"Test is not supported on runtime {currentRuntime}. Supported runtimes: {string.Join(", ", SupportedRuntimes)}";
            }

            return null;
        }
    }

    /// <summary>
    /// Attribute to mark tests that require specific runtime features.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequiresFeatureAttribute : FactAttribute
    {
        public RequiresFeatureAttribute(string requiredFeature)
        {
            RequiredFeature = requiredFeature;
            Skip = GetSkipReason();
        }

        public string RequiredFeature { get; }

        private string GetSkipReason()
        {
            if (!RuntimeDetection.SupportsFeature(RequiredFeature))
            {
                return $"Test requires feature '{RequiredFeature}' which is not supported on runtime {RuntimeDetection.CurrentRuntime}";
            }

            return null;
        }
    }

    /// <summary>
    /// Attribute to mark tests that require specific runtime features with theory data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequiresFeatureTheoryAttribute : TheoryAttribute
    {
        public RequiresFeatureTheoryAttribute(string requiredFeature)
        {
            RequiredFeature = requiredFeature;
            Skip = GetSkipReason();
        }

        public string RequiredFeature { get; }

        private string GetSkipReason()
        {
            if (!RuntimeDetection.SupportsFeature(RequiredFeature))
            {
                return $"Test requires feature '{RequiredFeature}' which is not supported on runtime {RuntimeDetection.CurrentRuntime}";
            }

            return null;
        }
    }

    /// <summary>
    /// Attribute to mark tests that should be skipped on specific runtimes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class SkipOnRuntimeAttribute : FactAttribute
    {
        public SkipOnRuntimeAttribute(params RuntimeDetection.RuntimeType[] skipRuntimes)
        {
            SkipRuntimes = skipRuntimes;
            Skip = GetSkipReason();
        }

        public RuntimeDetection.RuntimeType[] SkipRuntimes { get; }

        private string GetSkipReason()
        {
            var currentRuntime = RuntimeDetection.CurrentRuntime;
            
            if (SkipRuntimes.Contains(currentRuntime))
            {
                return $"Test is skipped on runtime {currentRuntime}";
            }

            return null;
        }
    }

    /// <summary>
    /// Attribute to mark tests that should be skipped on specific runtimes with theory data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class SkipOnRuntimeTheoryAttribute : TheoryAttribute
    {
        public SkipOnRuntimeTheoryAttribute(params RuntimeDetection.RuntimeType[] skipRuntimes)
        {
            SkipRuntimes = skipRuntimes;
            Skip = GetSkipReason();
        }

        public RuntimeDetection.RuntimeType[] SkipRuntimes { get; }

        private string GetSkipReason()
        {
            var currentRuntime = RuntimeDetection.CurrentRuntime;
            
            if (SkipRuntimes.Contains(currentRuntime))
            {
                return $"Test is skipped on runtime {currentRuntime}";
            }

            return null;
        }
    }

    /// <summary>
    /// Attribute to mark tests that have runtime-specific timeouts.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RuntimeTimeoutAttribute : FactAttribute
    {
        public RuntimeTimeoutAttribute(int baseTimeoutMs = 10000)
        {
            BaseTimeoutMs = baseTimeoutMs;
            Timeout = GetRuntimeAdjustedTimeout(baseTimeoutMs);
        }

        public int BaseTimeoutMs { get; }

        private int GetRuntimeAdjustedTimeout(int baseTimeoutMs)
        {
            var currentRuntime = RuntimeDetection.CurrentRuntime;
            
            switch (currentRuntime)
            {
                case RuntimeDetection.RuntimeType.Unity:
                    return baseTimeoutMs * 2; // Unity can be slower
                case RuntimeDetection.RuntimeType.Mono:
                    return baseTimeoutMs * 3; // Mono can be significantly slower
                case RuntimeDetection.RuntimeType.CoreCLR:
                    return baseTimeoutMs; // CoreCLR is typically fast
                case RuntimeDetection.RuntimeType.DotNet:
                    return baseTimeoutMs; // .NET is typically fast
                default:
                    return baseTimeoutMs * 2; // Conservative default
            }
        }
    }

    /// <summary>
    /// Attribute to mark tests that have runtime-specific timeouts with theory data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RuntimeTimeoutTheoryAttribute : TheoryAttribute
    {
        public RuntimeTimeoutTheoryAttribute(int baseTimeoutMs = 10000)
        {
            BaseTimeoutMs = baseTimeoutMs;
            Timeout = GetRuntimeAdjustedTimeout(baseTimeoutMs);
        }

        public int BaseTimeoutMs { get; }

        private int GetRuntimeAdjustedTimeout(int baseTimeoutMs)
        {
            var currentRuntime = RuntimeDetection.CurrentRuntime;
            
            switch (currentRuntime)
            {
                case RuntimeDetection.RuntimeType.Unity:
                    return baseTimeoutMs * 2; // Unity can be slower
                case RuntimeDetection.RuntimeType.Mono:
                    return baseTimeoutMs * 3; // Mono can be significantly slower
                case RuntimeDetection.RuntimeType.CoreCLR:
                    return baseTimeoutMs; // CoreCLR is typically fast
                case RuntimeDetection.RuntimeType.DotNet:
                    return baseTimeoutMs; // .NET is typically fast
                default:
                    return baseTimeoutMs * 2; // Conservative default
            }
        }
    }
} 