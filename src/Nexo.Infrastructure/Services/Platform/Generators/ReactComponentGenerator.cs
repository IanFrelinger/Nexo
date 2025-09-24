using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Platform;

namespace Nexo.Infrastructure.Services.Platform.Generators
{
    /// <summary>
    /// Generates React components for web applications
    /// </summary>
    public class ReactComponentGenerator
    {
        private readonly ILogger<ReactComponentGenerator> _logger;

        public ReactComponentGenerator(ILogger<ReactComponentGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generates React components from application logic
        /// </summary>
        public async Task<ReactGenerationResult> GenerateComponentsAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting React component generation for application: {ApplicationName}", 
                applicationLogic.ApplicationName);

            var result = new ReactGenerationResult
            {
                Success = false,
                Components = new List<GeneratedComponent>()
            };

            try
            {
                // Generate main application component
                var mainComponent = await GenerateMainComponentAsync(applicationLogic, options, cancellationToken);
                result.Components.Add(mainComponent);

                // Generate feature components
                var featureComponents = await GenerateFeatureComponentsAsync(applicationLogic, options, cancellationToken);
                result.Components.AddRange(featureComponents);

                // Generate utility components
                var utilityComponents = await GenerateUtilityComponentsAsync(applicationLogic, options, cancellationToken);
                result.Components.AddRange(utilityComponents);

                result.Success = true;
                _logger.LogInformation("React component generation completed. Generated {Count} components", 
                    result.Components.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during React component generation");
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private async Task<GeneratedComponent> GenerateMainComponentAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken)
        {
            // Generate main application component
            var component = new GeneratedComponent
            {
                Name = "App",
                Type = ComponentType.Main,
                Content = GenerateAppComponentContent(applicationLogic, options),
                FilePath = "src/App.tsx"
            };

            await Task.Delay(100, cancellationToken); // Simulate work
            return component;
        }

        private async Task<List<GeneratedComponent>> GenerateFeatureComponentsAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var components = new List<GeneratedComponent>();

            // Generate components for each feature
            foreach (var feature in applicationLogic.Features)
            {
                var component = new GeneratedComponent
                {
                    Name = feature.Name,
                    Type = ComponentType.Feature,
                    Content = GenerateFeatureComponentContent(feature, options),
                    FilePath = $"src/components/{feature.Name}.tsx"
                };
                components.Add(component);
            }

            await Task.Delay(100, cancellationToken); // Simulate work
            return components;
        }

        private async Task<List<GeneratedComponent>> GenerateUtilityComponentsAsync(
            ApplicationLogic applicationLogic,
            WebGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var components = new List<GeneratedComponent>();

            // Generate common utility components
            var utilityComponents = new[] { "Header", "Footer", "Navigation", "Loading", "Error" };
            
            foreach (var componentName in utilityComponents)
            {
                var component = new GeneratedComponent
                {
                    Name = componentName,
                    Type = ComponentType.Utility,
                    Content = GenerateUtilityComponentContent(componentName, options),
                    FilePath = $"src/components/{componentName}.tsx"
                };
                components.Add(component);
            }

            await Task.Delay(100, cancellationToken); // Simulate work
            return components;
        }

        private string GenerateAppComponentContent(ApplicationLogic applicationLogic, WebGenerationOptions options)
        {
            return $@"
import React from 'react';
import {{ BrowserRouter as Router, Routes, Route }} from 'react-router-dom';
import {{ {string.Join(", ", applicationLogic.Features.Select(f => f.Name))} }} from './components';

const App: React.FC = () => {{
  return (
    <Router>
      <div className=""App"">
        <Routes>
          {string.Join("\n          ", applicationLogic.Features.Select(f => $"<Route path=\"/{f.Name.ToLower()}\" element={{<{f.Name} />}} />"))}
        </Routes>
      </div>
    </Router>
  );
}};

export default App;";
        }

        private string GenerateFeatureComponentContent(FeatureLogic feature, WebGenerationOptions options)
        {
            return $@"
import React, {{ useState, useEffect }} from 'react';

const {feature.Name}: React.FC = () => {{
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {{
    // Load {feature.Name} data
    setLoading(false);
  }}, []);

  if (loading) return <div>Loading...</div>;

  return (
    <div className=""{feature.Name.ToLower()}"">
      <h1>{feature.Name}</h1>
      <p>{feature.Description}</p>
    </div>
  );
}};

export default {feature.Name};";
        }

        private string GenerateUtilityComponentContent(string componentName, WebGenerationOptions options)
        {
            return $@"
import React from 'react';

const {componentName}: React.FC = () => {{
  return (
    <div className=""{componentName.ToLower()}"">
      <h2>{componentName}</h2>
    </div>
  );
}};

export default {componentName};";
        }
    }

    /// <summary>
    /// Result of React component generation
    /// </summary>
    public class ReactGenerationResult
    {
        public bool Success { get; set; }
        public List<GeneratedComponent> Components { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Generated React component
    /// </summary>
    public class GeneratedComponent
    {
        public string Name { get; set; } = string.Empty;
        public ComponentType Type { get; set; }
        public string Content { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Component types
    /// </summary>
    public enum ComponentType
    {
        Main,
        Feature,
        Utility
    }
}
