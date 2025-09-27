using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Contracts;
using Nexo.Core.Contracts.Capabilities;
using Nexo.PluginHost.Schemas;

namespace Nexo.PluginHost
{
    /// <summary>
    /// Plugin validation functionality
    /// </summary>
    public partial class PluginHost
    {
        /// <summary>
        /// Validates that plugin constructors only request allowed interfaces.
        /// </summary>
        private bool ValidateConstructorDependencies(List<Type> capabilityTypes)
        {
            var allowedInterfaces = new HashSet<Type>
            {
                typeof(INexoFileSystem),
                typeof(INexoProcessRunner),
                typeof(ILogger<>)
            };

            foreach (var type in capabilityTypes)
            {
                var constructors = type.GetConstructors();
                foreach (var constructor in constructors)
                {
                    var parameters = constructor.GetParameters();
                    foreach (var parameter in parameters)
                    {
                        var parameterType = parameter.ParameterType;
                        
                        // Check if it's a generic type (like ILogger<T>)
                        if (parameterType.IsGenericType)
                        {
                            var genericTypeDefinition = parameterType.GetGenericTypeDefinition();
                            if (!allowedInterfaces.Contains(genericTypeDefinition))
                            {
                                _logger.LogWarning("Constructor parameter {ParameterType} is not in allowed interfaces", parameterType.Name);
                                return false;
                            }
                        }
                        else if (!allowedInterfaces.Contains(parameterType))
                        {
                            _logger.LogWarning("Constructor parameter {ParameterType} is not in allowed interfaces", parameterType.Name);
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private List<Type> ValidatePluginCapabilities(Assembly assembly, PluginManifest manifest)
        {
            var capabilityTypes = new List<Type>();
            var capabilityInterfaces = new[]
            {
                typeof(ISense),
                typeof(IDecide),
                typeof(IAct),
                typeof(IGuard)
            };

            var allTypes = assembly.GetTypes()
                .Where(t => !t.IsInterface && !t.IsAbstract && t.IsClass)
                .ToList();

            foreach (var type in allTypes)
            {
                var implementedCapabilities = capabilityInterfaces
                    .Where(ci => ci.IsAssignableFrom(type))
                    .ToList();

                if (implementedCapabilities.Count > 0)
                {
                    // Verify that the declared capabilities match the implemented ones
                    var declaredCapabilities = manifest.Capabilities.ToHashSet();
                    var implementedCapabilityNames = implementedCapabilities
                        .Select(ci => ci.Name)
                        .ToHashSet();

                    if (declaredCapabilities.SetEquals(implementedCapabilityNames))
                    {
                        capabilityTypes.Add(type);
                        _logger.LogInformation("Validated capability implementation: {TypeName} implements {Capabilities}", 
                            type.Name, string.Join(", ", implementedCapabilityNames));
                    }
                    else
                    {
                        _logger.LogWarning("Capability mismatch for type {TypeName}. Declared: [{Declared}], Implemented: [{Implemented}]", 
                            type.Name, string.Join(", ", declaredCapabilities), string.Join(", ", implementedCapabilityNames));
                    }
                }
            }

            return capabilityTypes;
        }
    }
}
