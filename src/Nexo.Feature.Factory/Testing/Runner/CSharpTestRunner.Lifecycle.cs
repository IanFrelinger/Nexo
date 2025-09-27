using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Attributes;

namespace Nexo.Feature.Factory.Testing.Runner
{
    /// <summary>
    /// Test lifecycle management functionality
    /// </summary>
    public sealed partial class CSharpTestRunner : ITestRunner
    {
        private async Task RunSetupMethodsAsync(object testInstance, Type testClass, CancellationToken cancellationToken)
        {
            var setupMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestSetupAttribute>() != null)
                .ToList();

            foreach (var method in setupMethods)
            {
                try
                {
                    var result = method.Invoke(testInstance, null);
                    if (result is Task task)
                    {
                        await task;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Setup method {MethodName} failed", method.Name);
                }
            }
        }

        private async Task RunCleanupMethodsAsync(object testInstance, Type testClass, CancellationToken cancellationToken)
        {
            var cleanupMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestCleanupAttribute>() != null)
                .ToList();

            foreach (var method in cleanupMethods)
            {
                try
                {
                    var result = method.Invoke(testInstance, null);
                    if (result is Task task)
                    {
                        await task;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cleanup method {MethodName} failed", method.Name);
                }
            }
        }
    }
}
