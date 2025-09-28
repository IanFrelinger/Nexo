using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nexo.Shared.Factories
{
    /// <summary>
    /// Centralized factory system
    /// </summary>
    public static class FactoryManager
    {
        private static readonly Dictionary<Type, Func<object>> _factories = new();
        private static readonly Dictionary<Type, Type> _implementations = new();
        private static readonly Dictionary<string, object> _singletons = new();

        /// <summary>
        /// Registers a factory function for a type
        /// </summary>
        public static void RegisterFactory<T>(Func<T> factory)
        {
            _factories[typeof(T)] = () => factory();
        }

        /// <summary>
        /// Registers an implementation type for an interface
        /// </summary>
        public static void RegisterImplementation<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            _implementations[typeof(TInterface)] = typeof(TImplementation);
        }

        /// <summary>
        /// Registers a singleton instance
        /// </summary>
        public static void RegisterSingleton<T>(T instance)
        {
            _singletons[typeof(T).FullName!] = instance!;
        }

        /// <summary>
        /// Creates an instance of the specified type
        /// </summary>
        public static T Create<T>()
        {
            var type = typeof(T);
            
            // Check for singleton first
            if (_singletons.TryGetValue(type.FullName!, out var singleton))
            {
                return (T)singleton;
            }

            // Check for registered factory
            if (_factories.TryGetValue(type, out var factory))
            {
                return (T)factory();
            }

            // Check for registered implementation
            if (_implementations.TryGetValue(type, out var implementationType))
            {
                return (T)Activator.CreateInstance(implementationType)!;
            }

            // Try to create with default constructor
            try
            {
                return Activator.CreateInstance<T>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot create instance of {type.Name}. Register a factory or implementation.", ex);
            }
        }

        /// <summary>
        /// Creates an instance with parameters
        /// </summary>
        public static T Create<T>(params object[] parameters)
        {
            var type = typeof(T);
            
            // Check for registered implementation
            if (_implementations.TryGetValue(type, out var implementationType))
            {
                return (T)Activator.CreateInstance(implementationType, parameters)!;
            }

            // Try to create with parameters
            try
            {
                return (T)Activator.CreateInstance(type, parameters)!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot create instance of {type.Name} with parameters.", ex);
            }
        }

        /// <summary>
        /// Creates an instance using dependency injection
        /// </summary>
        public static T CreateWithDependencies<T>()
        {
            var type = typeof(T);
            var constructor = type.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (constructor == null)
            {
                return Create<T>();
            }

            var parameters = constructor.GetParameters();
            var args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                args[i] = CreateByType(paramType);
            }

            return (T)Activator.CreateInstance(type, args)!;
        }

        /// <summary>
        /// Creates an instance by type
        /// </summary>
        public static object CreateByType(Type type)
        {
            // Check for singleton first
            if (_singletons.TryGetValue(type.FullName!, out var singleton))
            {
                return singleton;
            }

            // Check for registered factory
            if (_factories.TryGetValue(type, out var factory))
            {
                return factory();
            }

            // Check for registered implementation
            if (_implementations.TryGetValue(type, out var implementationType))
            {
                return Activator.CreateInstance(implementationType)!;
            }

            // Try to create with default constructor
            try
            {
                return Activator.CreateInstance(type)!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot create instance of {type.Name}. Register a factory or implementation.", ex);
            }
        }

        /// <summary>
        /// Gets all registered types
        /// </summary>
        public static IEnumerable<Type> GetRegisteredTypes()
        {
            return _factories.Keys.Concat(_implementations.Keys).Distinct();
        }

        /// <summary>
        /// Clears all registrations
        /// </summary>
        public static void Clear()
        {
            _factories.Clear();
            _implementations.Clear();
            _singletons.Clear();
        }
    }
}
