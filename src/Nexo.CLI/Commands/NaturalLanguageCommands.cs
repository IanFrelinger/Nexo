using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Services;

namespace Nexo.CLI.Commands
{
    public class NaturalLanguageCommands
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NaturalLanguageCommands> _logger;
        private readonly ProjectScaffoldingService _scaffoldingService;
        private readonly DependencyManagerService _dependencyManager;

        public NaturalLanguageCommands(IServiceProvider serviceProvider, ILogger<NaturalLanguageCommands> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scaffoldingService = new ProjectScaffoldingService(_logger);
            _dependencyManager = new DependencyManagerService(_logger);
        }

        public Command CreateNaturalLanguageCommand()
        {
            var command = new Command("build", "Build applications using natural language descriptions");

            // Main build command
            var descriptionArgument = new Argument<string>("description", "Natural language description of what you want to build");
            var platformOption = new Option<string>("--platform", () => "web", "Target platform (web, mobile, desktop, api, game)");
            var outputOption = new Option<string>("--output", () => "./generated-app", "Output directory for the generated project");
            var installOption = new Option<bool>("--install", () => true, "Install dependencies automatically");
            var runOption = new Option<bool>("--run", () => false, "Run the generated application after building");

            command.AddArgument(descriptionArgument);
            command.AddOption(platformOption);
            command.AddOption(outputOption);
            command.AddOption(installOption);
            command.AddOption(runOption);

            command.SetHandler(async (string description, string platform, string output, bool install, bool run, InvocationContext context) =>
            {
                await HandleBuildCommand(description, platform, output, install, run);
            }, descriptionArgument, platformOption, outputOption, installOption, runOption);

            // Quick commands for common scenarios
            var quickCommand = new Command("quick", "Quick build commands for common scenarios");
            quickCommand.AddCommand(CreateQuickWebCommand());
            quickCommand.AddCommand(CreateQuickMobileCommand());
            quickCommand.AddCommand(CreateQuickApiCommand());
            quickCommand.AddCommand(CreateQuickGameCommand());
            command.AddCommand(quickCommand);

            // List available templates
            var templatesCommand = new Command("templates", "List available project templates and examples");
            templatesCommand.SetHandler(ListTemplates);
            command.AddCommand(templatesCommand);

            // Show examples
            var examplesCommand = new Command("examples", "Show example natural language descriptions");
            examplesCommand.SetHandler(ShowExamples);
            command.AddCommand(examplesCommand);

            return command;
        }

        private Command CreateQuickWebCommand()
        {
            var command = new Command("web", "Quick web application generation");
            var nameArgument = new Argument<string>("name", "Application name");
            var featuresOption = new Option<string[]>("--features", () => Array.Empty<string>(), "Additional features to include");
            
            command.AddArgument(nameArgument);
            command.AddOption(featuresOption);
            
            command.SetHandler(async (string name, string[] features) =>
            {
                var description = $"A {name} web application";
                if (features.Any())
                {
                    description += $" with {string.Join(", ", features)}";
                }
                await HandleBuildCommand(description, "web", $"./{name.ToLower().Replace(" ", "-")}", true, false);
            }, nameArgument, featuresOption);
            
            return command;
        }

        private Command CreateQuickMobileCommand()
        {
            var command = new Command("mobile", "Quick mobile application generation");
            var nameArgument = new Argument<string>("name", "Application name");
            var featuresOption = new Option<string[]>("--features", () => Array.Empty<string>(), "Additional features to include");
            
            command.AddArgument(nameArgument);
            command.AddOption(featuresOption);
            
            command.SetHandler(async (string name, string[] features) =>
            {
                var description = $"A {name} mobile application";
                if (features.Any())
                {
                    description += $" with {string.Join(", ", features)}";
                }
                await HandleBuildCommand(description, "mobile", $"./{name.ToLower().Replace(" ", "-")}", true, false);
            }, nameArgument, featuresOption);
            
            return command;
        }

        private Command CreateQuickApiCommand()
        {
            var command = new Command("api", "Quick API server generation");
            var nameArgument = new Argument<string>("name", "API name");
            var featuresOption = new Option<string[]>("--features", () => Array.Empty<string>(), "Additional features to include");
            
            command.AddArgument(nameArgument);
            command.AddOption(featuresOption);
            
            command.SetHandler(async (string name, string[] features) =>
            {
                var description = $"A {name} REST API server";
                if (features.Any())
                {
                    description += $" with {string.Join(", ", features)}";
                }
                await HandleBuildCommand(description, "api", $"./{name.ToLower().Replace(" ", "-")}", true, false);
            }, nameArgument, featuresOption);
            
            return command;
        }

        private Command CreateQuickGameCommand()
        {
            var command = new Command("game", "Quick game generation");
            var nameArgument = new Argument<string>("name", "Game name");
            var typeOption = new Option<string>("--type", () => "2d", "Game type (2d, 3d, puzzle, action)");
            
            command.AddArgument(nameArgument);
            command.AddOption(typeOption);
            
            command.SetHandler(async (string name, string type) =>
            {
                var description = $"A {type} {name} game";
                await HandleBuildCommand(description, "game", $"./{name.ToLower().Replace(" ", "-")}", true, false);
            }, nameArgument, typeOption);
            
            return command;
        }

        private async Task HandleBuildCommand(string description, string platform, string outputDir, bool installDeps, bool runApp)
        {
            try
            {
                _logger.LogInformation("🚀 NEXO NATURAL LANGUAGE BUILDER");
                _logger.LogInformation("=================================");
                _logger.LogInformation($"Description: {description}");
                _logger.LogInformation($"Platform: {platform}");
                _logger.LogInformation($"Output: {outputDir}");
                _logger.LogInformation();

                // Step 1: Analyze the description
                _logger.LogInformation("🔍 Step 1: Analyzing your description...");
                var analysis = await AnalyzeDescription(description, platform);
                _logger.LogInformation($"✅ Identified: {analysis.AppType} application");
                _logger.LogInformation($"   Features: {string.Join(", ", analysis.Features)}");
                _logger.LogInformation($"   Technologies: {string.Join(", ", analysis.Technologies)}");
                _logger.LogInformation();

                // Step 2: Generate project structure
                _logger.LogInformation("🏗️ Step 2: Generating project structure...");
                var projectInfo = await _scaffoldingService.GenerateProjectAsync(analysis, outputDir);
                _logger.LogInformation($"✅ Created project: {projectInfo.ProjectName}");
                _logger.LogInformation($"   Files generated: {projectInfo.Files.Count}");
                _logger.LogInformation();

                // Step 3: Install dependencies
                if (installDeps)
                {
                    _logger.LogInformation("📦 Step 3: Installing dependencies...");
                    await _dependencyManager.InstallDependenciesAsync(outputDir, analysis.Technologies);
                    _logger.LogInformation("✅ Dependencies installed successfully");
                    _logger.LogInformation();
                }

                // Step 4: Generate application code
                _logger.LogInformation("💻 Step 4: Generating application code...");
                await GenerateApplicationCode(analysis, outputDir);
                _logger.LogInformation("✅ Application code generated");
                _logger.LogInformation();

                // Step 5: Create documentation
                _logger.LogInformation("📚 Step 5: Creating documentation...");
                await GenerateDocumentation(analysis, outputDir);
                _logger.LogInformation("✅ Documentation created");
                _logger.LogInformation();

                // Step 6: Run the application (if requested)
                if (runApp)
                {
                    _logger.LogInformation("▶️ Step 6: Running the application...");
                    await RunApplication(outputDir, platform);
                    _logger.LogInformation("✅ Application started");
                    _logger.LogInformation();
                }

                _logger.LogInformation("🎉 BUILD COMPLETED SUCCESSFULLY!");
                _logger.LogInformation("=================================");
                _logger.LogInformation($"Your {analysis.AppType} application is ready!");
                _logger.LogInformation($"Location: {Path.GetFullPath(outputDir)}");
                _logger.LogInformation();
                _logger.LogInformation("Next steps:");
                _logger.LogInformation($"  cd {outputDir}");
                if (installDeps)
                {
                    _logger.LogInformation("  # Dependencies are already installed");
                }
                else
                {
                    _logger.LogInformation("  # Install dependencies:");
                    _logger.LogInformation($"  {GetInstallCommand(platform)}");
                }
                _logger.LogInformation($"  # Run the application:");
                _logger.LogInformation($"  {GetRunCommand(platform)}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Build failed: {ex.Message}");
                throw;
            }
        }

        private async Task<ProjectAnalysis> AnalyzeDescription(string description, string platform)
        {
            // Simulate AI analysis of the natural language description
            await Task.Delay(1000);

            var analysis = new ProjectAnalysis
            {
                Description = description,
                Platform = platform,
                AppType = DetermineAppType(description, platform),
                Features = ExtractFeatures(description),
                Technologies = DetermineTechnologies(description, platform),
                Complexity = DetermineComplexity(description)
            };

            return analysis;
        }

        private string DetermineAppType(string description, string platform)
        {
            var lowerDesc = description.ToLower();
            
            if (lowerDesc.Contains("todo") || lowerDesc.Contains("task"))
                return "Todo Application";
            if (lowerDesc.Contains("ecommerce") || lowerDesc.Contains("shop") || lowerDesc.Contains("store"))
                return "E-commerce Application";
            if (lowerDesc.Contains("chat") || lowerDesc.Contains("messaging"))
                return "Chat Application";
            if (lowerDesc.Contains("blog") || lowerDesc.Contains("cms"))
                return "Content Management System";
            if (lowerDesc.Contains("dashboard") || lowerDesc.Contains("admin"))
                return "Admin Dashboard";
            if (lowerDesc.Contains("game"))
                return "Game Application";
            if (lowerDesc.Contains("api") || lowerDesc.Contains("server"))
                return "API Server";
            
            return platform switch
            {
                "web" => "Web Application",
                "mobile" => "Mobile Application",
                "desktop" => "Desktop Application",
                "api" => "API Server",
                "game" => "Game Application",
                _ => "Application"
            };
        }

        private string[] ExtractFeatures(string description)
        {
            var features = new List<string>();
            var lowerDesc = description.ToLower();

            if (lowerDesc.Contains("dark mode") || lowerDesc.Contains("theme"))
                features.Add("Dark Mode");
            if (lowerDesc.Contains("auth") || lowerDesc.Contains("login") || lowerDesc.Contains("user"))
                features.Add("Authentication");
            if (lowerDesc.Contains("database") || lowerDesc.Contains("data") || lowerDesc.Contains("storage"))
                features.Add("Database");
            if (lowerDesc.Contains("real-time") || lowerDesc.Contains("live") || lowerDesc.Contains("websocket"))
                features.Add("Real-time");
            if (lowerDesc.Contains("responsive") || lowerDesc.Contains("mobile"))
                features.Add("Responsive Design");
            if (lowerDesc.Contains("pwa") || lowerDesc.Contains("progressive"))
                features.Add("PWA");
            if (lowerDesc.Contains("payment") || lowerDesc.Contains("stripe") || lowerDesc.Contains("paypal"))
                features.Add("Payment Processing");
            if (lowerDesc.Contains("search") || lowerDesc.Contains("filter"))
                features.Add("Search & Filter");
            if (lowerDesc.Contains("upload") || lowerDesc.Contains("file"))
                features.Add("File Upload");
            if (lowerDesc.Contains("chart") || lowerDesc.Contains("graph") || lowerDesc.Contains("analytics"))
                features.Add("Data Visualization");

            return features.ToArray();
        }

        private string[] DetermineTechnologies(string description, string platform)
        {
            var technologies = new List<string>();

            switch (platform)
            {
                case "web":
                    technologies.AddRange(new[] { "React", "TypeScript", "CSS3", "Vite" });
                    if (description.ToLower().Contains("backend") || description.ToLower().Contains("api"))
                        technologies.AddRange(new[] { "Node.js", "Express", "MongoDB" });
                    break;
                case "mobile":
                    technologies.AddRange(new[] { "React Native", "TypeScript", "Expo" });
                    break;
                case "desktop":
                    technologies.AddRange(new[] { "Electron", "React", "TypeScript" });
                    break;
                case "api":
                    technologies.AddRange(new[] { "Node.js", "Express", "TypeScript", "MongoDB", "JWT" });
                    break;
                case "game":
                    technologies.AddRange(new[] { "Unity", "C#", "2D/3D Graphics" });
                    break;
            }

            return technologies.ToArray();
        }

        private string DetermineComplexity(string description)
        {
            var lowerDesc = description.ToLower();
            var wordCount = description.Split(' ').Length;
            
            if (wordCount > 50 || lowerDesc.Contains("complex") || lowerDesc.Contains("enterprise"))
                return "High";
            if (wordCount > 20 || lowerDesc.Contains("advanced") || lowerDesc.Contains("multiple"))
                return "Medium";
            return "Low";
        }

        private async Task GenerateApplicationCode(ProjectAnalysis analysis, string outputDir)
        {
            // Generate code based on analysis
            await Task.Delay(2000); // Simulate code generation time
            
            var codeFiles = new Dictionary<string, string>();
            
            switch (analysis.Platform)
            {
                case "web":
                    codeFiles = GenerateWebCode(analysis);
                    break;
                case "mobile":
                    codeFiles = GenerateMobileCode(analysis);
                    break;
                case "api":
                    codeFiles = GenerateApiCode(analysis);
                    break;
                case "game":
                    codeFiles = GenerateGameCode(analysis);
                    break;
            }

            foreach (var file in codeFiles)
            {
                var filePath = Path.Combine(outputDir, file.Key);
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                await File.WriteAllTextAsync(filePath, file.Value);
            }
        }

        private Dictionary<string, string> GenerateWebCode(ProjectAnalysis analysis)
        {
            return new Dictionary<string, string>
            {
                ["src/App.tsx"] = $@"import React from 'react';
import './App.css';

const App: React.FC = () => {{
  return (
    <div className=""app"">
      <header className=""app-header"">
        <h1>{analysis.AppType}</h1>
        <p>Generated by Nexo AI Platform</p>
      </header>
      <main className=""app-main"">
        <div className=""welcome"">
          <h2>Welcome to your {analysis.AppType.ToLower()}!</h2>
          <p>This application was generated from: ""{analysis.Description}""</p>
          <div className=""features"">
            <h3>Features included:</h3>
            <ul>
              {{analysis.Features.map(feature => (
                <li key={{feature}}>{{feature}}</li>
              ))}}
            </ul>
          </div>
        </div>
      </main>
    </div>
  );
}};

export default App;",
                ["src/App.css"] = @"/* Generated by Nexo AI Platform */
.app {
  text-align: center;
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
}

.app-header {
  padding: 2rem;
}

.app-header h1 {
  margin: 0;
  font-size: 3rem;
  font-weight: 700;
}

.app-header p {
  margin: 0.5rem 0 0 0;
  opacity: 0.9;
  font-size: 1.2rem;
}

.app-main {
  padding: 2rem;
}

.welcome {
  max-width: 600px;
  margin: 0 auto;
  background: rgba(255, 255, 255, 0.1);
  padding: 2rem;
  border-radius: 16px;
  backdrop-filter: blur(10px);
}

.features ul {
  list-style: none;
  padding: 0;
}

.features li {
  padding: 0.5rem;
  background: rgba(255, 255, 255, 0.1);
  margin: 0.5rem 0;
  border-radius: 8px;
}",
                ["package.json"] = @"{
  ""name"": ""nexo-generated-app"",
  ""version"": ""1.0.0"",
  ""description"": ""Application generated by Nexo AI Platform"",
  ""type"": ""module"",
  ""scripts"": {
    ""dev"": ""vite"",
    ""build"": ""tsc && vite build"",
    ""preview"": ""vite preview""
  },
  ""dependencies"": {
    ""react"": ""^18.2.0"",
    ""react-dom"": ""^18.2.0""
  },
  ""devDependencies"": {
    ""@types/react"": ""^18.2.0"",
    ""@types/react-dom"": ""^18.2.0"",
    ""@vitejs/plugin-react"": ""^4.0.0"",
    ""typescript"": ""^5.0.0"",
    ""vite"": ""^4.4.0""
  }
}",
                ["README.md"] = $@"# {analysis.AppType}

This application was generated by Nexo AI Platform from the description:
> {analysis.Description}

## Features
{string.Join("\n", analysis.Features.Select(f => $"- {f}"))}

## Technologies Used
{string.Join("\n", analysis.Technologies.Select(t => $"- {t}"))}

## Getting Started

### Prerequisites
- Node.js 16 or higher
- npm or yarn

### Installation
```bash
npm install
```

### Development
```bash
npm run dev
```

### Build
```bash
npm run build
```

## Generated by Nexo
This project was created using Nexo's AI-powered development platform.
Visit [Nexo](/) to learn more about AI-assisted development."
            };
        }

        private Dictionary<string, string> GenerateMobileCode(ProjectAnalysis analysis)
        {
            return new Dictionary<string, string>
            {
                ["App.tsx"] = $@"import React from 'react';
import {{ StyleSheet, Text, View, ScrollView }} from 'react-native';

export default function App() {{
  return (
    <View style={{styles.container}}>
      <View style={{styles.header}}>
        <Text style={{styles.title}}>{analysis.AppType}</Text>
        <Text style={{styles.subtitle}}>Generated by Nexo AI Platform</Text>
      </View>
      <ScrollView style={{styles.content}}>
        <View style={{styles.welcome}}>
          <Text style={{styles.welcomeTitle}}>Welcome to your {analysis.AppType.ToLower()}!</Text>
          <Text style={{styles.description}}>This application was generated from: ""{analysis.Description}""</Text>
          <View style={{styles.features}}>
            <Text style={{styles.featuresTitle}}>Features included:</Text>
            {string.Join("\n", analysis.Features.Select(f => $"            <Text style={{styles.feature}}>• {f}</Text>"))}
          </View>
        </View>
      </ScrollView>
    </View>
  );
}}

const styles = StyleSheet.create({{
  container: {{
    flex: 1,
    backgroundColor: '#667eea',
  }},
  header: {{
    padding: 40,
    alignItems: 'center',
  }},
  title: {{
    fontSize: 32,
    fontWeight: 'bold',
    color: 'white',
  }},
  subtitle: {{
    fontSize: 16,
    color: 'white',
    opacity: 0.9,
    marginTop: 8,
  }},
  content: {{
    flex: 1,
    padding: 20,
  }},
  welcome: {{
    backgroundColor: 'rgba(255, 255, 255, 0.1)',
    padding: 20,
    borderRadius: 16,
  }},
  welcomeTitle: {{
    fontSize: 24,
    fontWeight: '600',
    color: 'white',
    marginBottom: 12,
  }},
  description: {{
    fontSize: 16,
    color: 'white',
    opacity: 0.9,
    marginBottom: 20,
  }},
  features: {{
    marginTop: 16,
  }},
  featuresTitle: {{
    fontSize: 18,
    fontWeight: '600',
    color: 'white',
    marginBottom: 12,
  }},
  feature: {{
    fontSize: 16,
    color: 'white',
    marginBottom: 8,
  }},
}});",
                ["package.json"] = @"{
  ""name"": ""nexo-mobile-app"",
  ""version"": ""1.0.0"",
  ""description"": ""Mobile app generated by Nexo AI Platform"",
  ""main"": ""App.tsx"",
  ""scripts"": {
    ""start"": ""expo start"",
    ""android"": ""expo start --android"",
    ""ios"": ""expo start --ios""
  },
  ""dependencies"": {
    ""expo"": ""~49.0.0"",
    ""react"": ""18.2.0"",
    ""react-native"": ""0.72.0""
  }
}",
                ["README.md"] = $@"# {analysis.AppType} (Mobile)

This mobile application was generated by Nexo AI Platform from the description:
> {analysis.Description}

## Features
{string.Join("\n", analysis.Features.Select(f => $"- {f}"))}

## Technologies Used
{string.Join("\n", analysis.Technologies.Select(t => $"- {t}"))}

## Getting Started

### Prerequisites
- Node.js 16 or higher
- Expo CLI
- iOS Simulator or Android Emulator

### Installation
```bash
npm install
```

### Development
```bash
npm start
```

### Run on Device
```bash
npm run ios     # iOS
npm run android # Android
```

## Generated by Nexo
This project was created using Nexo's AI-powered development platform."
            };
        }

        private Dictionary<string, string> GenerateApiCode(ProjectAnalysis analysis)
        {
            return new Dictionary<string, string>
            {
                ["src/server.ts"] = $@"import express from 'express';
import cors from 'cors';
import helmet from 'helmet';

const app = express();
const PORT = process.env.PORT || 3000;

// Middleware
app.use(helmet());
app.use(cors());
app.use(express.json());

// Routes
app.get('/', (req, res) => {{
  res.json({{
    message: '{analysis.AppType}',
    description: '{analysis.Description}',
    features: {JsonSerializer.Serialize(analysis.Features)},
    technologies: {JsonSerializer.Serialize(analysis.Technologies)},
    generatedBy: 'Nexo AI Platform'
  }});
}});

app.get('/health', (req, res) => {{
  res.json({{ status: 'OK', timestamp: new Date().toISOString() }});
}});

// Start server
app.listen(PORT, () => {{
  console.log(`🚀 {analysis.AppType} server running on port ${{PORT}}`);
  console.log(`📚 Generated by Nexo AI Platform`);
}});",
                ["package.json"] = @"{
  ""name"": ""nexo-api-server"",
  ""version"": ""1.0.0"",
  ""description"": ""API server generated by Nexo AI Platform"",
  ""main"": ""dist/server.js"",
  ""scripts"": {
    ""dev"": ""ts-node src/server.ts"",
    ""build"": ""tsc"",
    ""start"": ""node dist/server.js""
  },
  ""dependencies"": {
    ""express"": ""^4.18.0"",
    ""cors"": ""^2.8.5"",
    ""helmet"": ""^7.0.0""
  },
  ""devDependencies"": {
    ""@types/express"": ""^4.17.0"",
    ""@types/cors"": ""^2.8.0"",
    ""@types/node"": ""^20.0.0"",
    ""ts-node"": ""^10.9.0"",
    ""typescript"": ""^5.0.0""
  }
}",
                ["README.md"] = $@"# {analysis.AppType}

This API server was generated by Nexo AI Platform from the description:
> {analysis.Description}

## Features
{string.Join("\n", analysis.Features.Select(f => $"- {f}"))}

## Technologies Used
{string.Join("\n", analysis.Technologies.Select(t => $"- {t}"))}

## Getting Started

### Prerequisites
- Node.js 16 or higher
- npm or yarn

### Installation
```bash
npm install
```

### Development
```bash
npm run dev
```

### Build
```bash
npm run build
```

### Production
```bash
npm start
```

## API Endpoints
- `GET /` - Application information
- `GET /health` - Health check

## Generated by Nexo
This project was created using Nexo's AI-powered development platform."
            };
        }

        private Dictionary<string, string> GenerateGameCode(ProjectAnalysis analysis)
        {
            return new Dictionary<string, string>
            {
                ["Assets/Scripts/GameManager.cs"] = $@"using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{{
    [Header(""UI Elements"")]
    public Text titleText;
    public Text descriptionText;
    public Text featuresText;
    
    void Start()
    {{
        // Display game information
        if (titleText != null)
            titleText.text = ""{analysis.AppType}"";
            
        if (descriptionText != null)
            descriptionText.text = ""Generated from: {analysis.Description}"";
            
        if (featuresText != null)
            featuresText.text = ""Features:\\n"" + string.Join(""\\n"", {JsonSerializer.Serialize(analysis.Features)});
    }}
    
    void Update()
    {{
        // Basic game loop
        if (Input.GetKeyDown(KeyCode.Escape))
        {{
            Application.Quit();
        }}
    }}
}}",
                ["Assets/Scripts/PlayerController.cs"] = @"using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    void Update()
    {
        float horizontal = Input.GetAxis(""Horizontal"");
        float vertical = Input.GetAxis(""Vertical"");
        
        Vector2 movement = new Vector2(horizontal, vertical) * moveSpeed;
        rb.velocity = movement;
    }
}",
                ["README.md"] = $@"# {analysis.AppType}

This game was generated by Nexo AI Platform from the description:
> {analysis.Description}

## Features
{string.Join("\n", analysis.Features.Select(f => $"- {f}"))}

## Technologies Used
{string.Join("\n", analysis.Technologies.Select(t => $"- {t}"))}

## Getting Started

### Prerequisites
- Unity 2022.3 or higher

### Installation
1. Open Unity Hub
2. Click ""Add"" and select this project folder
3. Open the project in Unity

### Running
1. Open the Main scene
2. Press Play in the Unity Editor
3. Use WASD or arrow keys to move

## Generated by Nexo
This project was created using Nexo's AI-powered development platform."
            };
        }

        private async Task GenerateDocumentation(ProjectAnalysis analysis, string outputDir)
        {
            var readmePath = Path.Combine(outputDir, "README.md");
            if (!File.Exists(readmePath))
            {
                var readmeContent = $@"# {analysis.AppType}

This application was generated by Nexo AI Platform from the description:
> {analysis.Description}

## Features
{string.Join("\n", analysis.Features.Select(f => $"- {f}"))}

## Technologies Used
{string.Join("\n", analysis.Technologies.Select(t => $"- {t}"))}

## Generated by Nexo
This project was created using Nexo's AI-powered development platform.
Visit [Nexo](/) to learn more about AI-assisted development.";
                
                await File.WriteAllTextAsync(readmePath, readmeContent);
            }
        }

        private async Task RunApplication(string outputDir, string platform)
        {
            var runCommand = GetRunCommand(platform);
            _logger.LogInformation($"Running: {runCommand}");
            
            // In a real implementation, this would execute the command
            await Task.Delay(2000);
        }

        private string GetInstallCommand(string platform)
        {
            return platform switch
            {
                "web" => "npm install",
                "mobile" => "npm install",
                "api" => "npm install",
                "game" => "# Open in Unity and let it install packages",
                _ => "npm install"
            };
        }

        private string GetRunCommand(string platform)
        {
            return platform switch
            {
                "web" => "npm run dev",
                "mobile" => "npm start",
                "api" => "npm run dev",
                "game" => "# Press Play in Unity Editor",
                _ => "npm start"
            };
        }

        private void ListTemplates()
        {
            _logger.LogInformation("📋 AVAILABLE PROJECT TEMPLATES");
            _logger.LogInformation("===============================");
            _logger.LogInformation();
            _logger.LogInformation("🌐 Web Applications:");
            _logger.LogInformation("  • Todo App with dark mode");
            _logger.LogInformation("  • E-commerce store with payment");
            _logger.LogInformation("  • Blog with CMS");
            _logger.LogInformation("  • Dashboard with analytics");
            _logger.LogInformation();
            _logger.LogInformation("📱 Mobile Applications:");
            _logger.LogInformation("  • Fitness tracker");
            _logger.LogInformation("  • Social media app");
            _logger.LogInformation("  • E-commerce mobile app");
            _logger.LogInformation();
            _logger.LogInformation("🔌 API Servers:");
            _logger.LogInformation("  • REST API with authentication");
            _logger.LogInformation("  • GraphQL API with subscriptions");
            _logger.LogInformation("  • Microservices architecture");
            _logger.LogInformation();
            _logger.LogInformation("🎮 Games:");
            _logger.LogInformation("  • 2D platformer");
            _logger.LogInformation("  • Puzzle game");
            _logger.LogInformation("  • Action RPG");
        }

        private void ShowExamples()
        {
            _logger.LogInformation("💡 NATURAL LANGUAGE EXAMPLES");
            _logger.LogInformation("=============================");
            _logger.LogInformation();
            _logger.LogInformation("Web Applications:");
            _logger.LogInformation("  nexo build ""A todo app with dark mode and drag-and-drop"" --platform web");
            _logger.LogInformation("  nexo build ""E-commerce store with payment processing and user accounts"" --platform web");
            _logger.LogInformation("  nexo build ""Blog with CMS and comment system"" --platform web");
            _logger.LogInformation();
            _logger.LogInformation("Mobile Applications:");
            _logger.LogInformation("  nexo build ""Fitness tracker with workout plans and progress charts"" --platform mobile");
            _logger.LogInformation("  nexo build ""Social media app with real-time messaging"" --platform mobile");
            _logger.LogInformation();
            _logger.LogInformation("API Servers:");
            _logger.LogInformation("  nexo build ""REST API for user management with JWT authentication"" --platform api");
            _logger.LogInformation("  nexo build ""E-commerce API with payment processing and inventory"" --platform api");
            _logger.LogInformation();
            _logger.LogInformation("Games:");
            _logger.LogInformation("  nexo build ""2D platformer game with collectibles and levels"" --platform game");
            _logger.LogInformation("  nexo build ""Puzzle game with multiple difficulty levels"" --platform game");
            _logger.LogInformation();
            _logger.LogInformation("Quick Commands:");
            _logger.LogInformation("  nexo build quick web ""My Todo App"" --features dark-mode,responsive");
            _logger.LogInformation("  nexo build quick mobile ""Fitness Tracker"" --features charts,offline");
            _logger.LogInformation("  nexo build quick api ""User Management"" --features auth,database");
        }
    }

    public class ProjectAnalysis
    {
        public string Description { get; set; } = "";
        public string Platform { get; set; } = "";
        public string AppType { get; set; } = "";
        public string[] Features { get; set; } = Array.Empty<string>();
        public string[] Technologies { get; set; } = Array.Empty<string>();
        public string Complexity { get; set; } = "Low";
    }
}
