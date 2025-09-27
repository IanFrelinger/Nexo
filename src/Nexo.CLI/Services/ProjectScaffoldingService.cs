using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Services
{
    public partial class ProjectScaffoldingService
    {
        private readonly ILogger<ProjectScaffoldingService> _logger;

        public ProjectScaffoldingService(ILogger<ProjectScaffoldingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProjectInfo> GenerateProjectAsync(ProjectAnalysis analysis, string outputDir)
        {
            _logger.LogInformation($"Generating {analysis.AppType} project structure...");

            // Create output directory
            Directory.CreateDirectory(outputDir);

            var projectInfo = new ProjectInfo
            {
                ProjectName = Path.GetFileName(outputDir),
                Platform = analysis.Platform,
                AppType = analysis.AppType,
                OutputDirectory = outputDir,
                Files = new List<string>()
            };

            // Generate platform-specific structure
            switch (analysis.Platform)
            {
                case "web":
                    await GenerateWebStructure(analysis, outputDir, projectInfo);
                    break;
                case "mobile":
                    await GenerateMobileStructure(analysis, outputDir, projectInfo);
                    break;
                case "api":
                    await GenerateApiStructure(analysis, outputDir, projectInfo);
                    break;
                case "game":
                    await GenerateGameStructure(analysis, outputDir, projectInfo);
                    break;
            }

            _logger.LogInformation($"✅ Project structure generated with {projectInfo.Files.Count} files");
            return projectInfo;
        }

        private async Task GenerateWebStructure(ProjectAnalysis analysis, string outputDir, ProjectInfo projectInfo)
        {
            var directories = new[]
            {
                "src",
                "public",
                "src/components",
                "src/pages",
                "src/services",
                "src/utils",
                "src/types"
            };

            foreach (var dir in directories)
            {
                var dirPath = Path.Combine(outputDir, dir);
                Directory.CreateDirectory(dirPath);
                projectInfo.Files.Add($"{dir}/");
            }

            // Create essential files
            var files = new Dictionary<string, string>
            {
                ["index.html"] = GenerateWebIndexHtml(analysis),
                ["vite.config.ts"] = GenerateViteConfig(),
                ["tsconfig.json"] = GenerateTypeScriptConfig(),
                [".gitignore"] = GenerateGitIgnore("web")
            };

            foreach (var file in files)
            {
                var filePath = Path.Combine(outputDir, file.Key);
                await File.WriteAllTextAsync(filePath, file.Value);
                projectInfo.Files.Add(file.Key);
            }
        }

        private async Task GenerateMobileStructure(ProjectAnalysis analysis, string outputDir, ProjectInfo projectInfo)
        {
            var directories = new[]
            {
                "src",
                "src/components",
                "src/screens",
                "src/services",
                "src/utils",
                "src/types",
                "assets",
                "assets/images"
            };

            foreach (var dir in directories)
            {
                var dirPath = Path.Combine(outputDir, dir);
                Directory.CreateDirectory(dirPath);
                projectInfo.Files.Add($"{dir}/");
            }

            var files = new Dictionary<string, string>
            {
                ["app.json"] = GenerateExpoConfig(analysis),
                ["babel.config.js"] = GenerateBabelConfig(),
                ["metro.config.js"] = GenerateMetroConfig(),
                [".gitignore"] = GenerateGitIgnore("mobile")
            };

            foreach (var file in files)
            {
                var filePath = Path.Combine(outputDir, file.Key);
                await File.WriteAllTextAsync(filePath, file.Value);
                projectInfo.Files.Add(file.Key);
            }
        }

        private async Task GenerateApiStructure(ProjectAnalysis analysis, string outputDir, ProjectInfo projectInfo)
        {
            var directories = new[]
            {
                "src",
                "src/routes",
                "src/controllers",
                "src/models",
                "src/middleware",
                "src/services",
                "src/utils",
                "src/types"
            };

            foreach (var dir in directories)
            {
                var dirPath = Path.Combine(outputDir, dir);
                Directory.CreateDirectory(dirPath);
                projectInfo.Files.Add($"{dir}/");
            }

            var files = new Dictionary<string, string>
            {
                ["tsconfig.json"] = GenerateTypeScriptConfig(),
                [".env.example"] = GenerateEnvExample(),
                [".gitignore"] = GenerateGitIgnore("api")
            };

            foreach (var file in files)
            {
                var filePath = Path.Combine(outputDir, file.Key);
                await File.WriteAllTextAsync(filePath, file.Value);
                projectInfo.Files.Add(file.Key);
            }
        }

        private async Task GenerateGameStructure(ProjectAnalysis analysis, string outputDir, ProjectInfo projectInfo)
        {
            var directories = new[]
            {
                "Assets",
                "Assets/Scripts",
                "Assets/Scenes",
                "Assets/Prefabs",
                "Assets/Materials",
                "Assets/Textures",
                "ProjectSettings"
            };

            foreach (var dir in directories)
            {
                var dirPath = Path.Combine(outputDir, dir);
                Directory.CreateDirectory(dirPath);
                projectInfo.Files.Add($"{dir}/");
            }

            var files = new Dictionary<string, string>
            {
                ["ProjectSettings/ProjectVersion.txt"] = "m_EditorVersion: 2022.3.0f1",
                ["Assets/Scenes/Main.unity"] = "# Unity scene file",
                [".gitignore"] = GenerateGitIgnore("game")
            };

            foreach (var file in files)
            {
                var filePath = Path.Combine(outputDir, file.Key);
                await File.WriteAllTextAsync(filePath, file.Value);
                projectInfo.Files.Add(file.Key);
            }
        }

        private string GenerateWebIndexHtml(ProjectAnalysis analysis)
        {
            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>{analysis.AppType}</title>
</head>
<body>
    <div id=""root""></div>
    <script type=""module"" src=""/src/main.tsx""></script>
</body>
</html>";
        }

        private string GenerateViteConfig()
        {
            return @"import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    open: true
  }
})";
        }

        private string GenerateTypeScriptConfig()
        {
            return @"{
  ""compilerOptions"": {
    ""target"": ""ES2020"",
    ""useDefineForClassFields"": true,
    ""lib"": [""ES2020"", ""DOM"", ""DOM.Iterable""],
    ""module"": ""ESNext"",
    ""skipLibCheck"": true,
    ""moduleResolution"": ""bundler"",
    ""allowImportingTsExtensions"": true,
    ""resolveJsonModule"": true,
    ""isolatedModules"": true,
    ""noEmit"": true,
    ""jsx"": ""react-jsx"",
    ""strict"": true,
    ""noUnusedLocals"": true,
    ""noUnusedParameters"": true,
    ""noFallthroughCasesInSwitch"": true
  },
  ""include"": [""src""],
  ""references"": [{ ""path"": ""./tsconfig.node.json"" }]
}";
        }

        private string GenerateExpoConfig(ProjectAnalysis analysis)
        {
            return $@"{{
  ""expo"": {{
    ""name"": ""{analysis.AppType}"",
    ""slug"": ""{analysis.AppType.ToLower().Replace(" ", "-")}"",
    ""version"": ""1.0.0"",
    ""orientation"": ""portrait"",
    ""icon"": """./assets/icon.png"",
    ""userInterfaceStyle"": ""light"",
    ""splash"": {{
      ""image"": ""./assets/splash.png"",
      ""resizeMode"": ""contain"",
      ""backgroundColor"": ""#ffffff""
    }},
    ""assetBundlePatterns"": [
      ""**/*""
    ],
    ""ios"": {{
      ""supportsTablet"": true
    }},
    ""android"": {{
      ""adaptiveIcon"": {{
        ""foregroundImage"": ""./assets/adaptive-icon.png"",
        ""backgroundColor"": ""#FFFFFF""
      }}
    }},
    ""web"": {{
      ""favicon"": ""./assets/favicon.png""
    }}
  }}
}}";
        }

        private string GenerateBabelConfig()
        {
            return @"module.exports = function(api) {
  api.cache(true);
  return {
    presets: ['babel-preset-expo'],
  };
};";
        }

        private string GenerateMetroConfig()
        {
            return @"const { getDefaultConfig } = require('expo/metro-config');

const config = getDefaultConfig(__dirname);

module.exports = config;";
        }

        private string GenerateEnvExample()
        {
            return @"# Environment variables
PORT=3000
NODE_ENV=development
DATABASE_URL=mongodb://localhost:27017/nexo-app
JWT_SECRET=your-secret-key-here
API_KEY=your-api-key-here";
        }

        private string GenerateGitIgnore(string platform)
        {
            var baseIgnore = @"# Dependencies
node_modules/
npm-debug.log*
yarn-debug.log*
yarn-error.log*

# Environment variables
.env
.env.local
.env.development.local
.env.test.local
.env.production.local

# IDE
.vscode/
.idea/
*.swp
*.swo

# OS
.DS_Store
Thumbs.db

# Logs
logs
*.log";

            return platform switch
            {
                "web" => baseIgnore + @"

# Build outputs
dist/
build/
.vite/

# TypeScript
*.tsbuildinfo",
                "mobile" => baseIgnore + @"

# Expo
.expo/
.expo-shared/

# iOS
ios/
*.ipa

# Android
android/
*.apk
*.aab",
                "api" => baseIgnore + @"

# Build outputs
dist/
build/

# TypeScript
*.tsbuildinfo",
                "game" => baseIgnore + @"

# Unity
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/

# Unity generated files
*.tmp
*.unity.meta
*.unity.meta.bak

# Unity crash logs
crashlytics-build.properties

# Unity packages
[Pp]ackages/
*.unitypackage",
                _ => baseIgnore
            };
        }
    }

    public partial class ProjectInfo
    {
        public string ProjectName { get; set; } = "";
        public string Platform { get; set; } = "";
        public string AppType { get; set; } = "";
        public string OutputDirectory { get; set; } = "";
        public List<string> Files { get; set; } = new();
    }

    public partial class ProjectAnalysis
    {
        public string Description { get; set; } = "";
        public string Platform { get; set; } = "";
        public string AppType { get; set; } = "";
        public string[] Features { get; set; } = Array.Empty<string>();
        public string[] Technologies { get; set; } = Array.Empty<string>();
        public string Complexity { get; set; } = "Low";
    }
}
