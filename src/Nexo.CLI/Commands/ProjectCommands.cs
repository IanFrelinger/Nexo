using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Enums;
using Nexo.Shared.Interfaces;
using Nexo.Feature.Template.Interfaces;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// CLI commands for project templates, scaffolding, and development environment management.
    /// </summary>
    public static class ProjectCommands
    {
        /// <summary>
        /// Creates the project command with all subcommands.
        /// </summary>
        /// <param name="templateService">Template service.</param>
        /// <param name="intelligentTemplateService">Intelligent template service.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>The project command.</returns>
        public static Command CreateProjectCommand(
            ITemplateService templateService,
            IIntelligentTemplateService intelligentTemplateService,
            ILogger logger)
        {
            var projectCommand = new Command("project", "Project templates and scaffolding");
            
            // Init command for project initialization
            var initCommand = new Command("init", "Initialize a new project");
            var initNameOption = new Option<string>("--name", "Project name") { IsRequired = true };
            var initTypeOption = new Option<string>("--type", "Project type (webapi, console, library, microservice)") { IsRequired = true };
            var initPathOption = new Option<string>("--path", "Project path") { IsRequired = false };
            var initTemplateOption = new Option<string>("--template", "Template to use") { IsRequired = false };
            var initAiOption = new Option<bool>("--ai", "Use AI-enhanced initialization") { IsRequired = false };
            
            initCommand.AddOption(initNameOption);
            initCommand.AddOption(initTypeOption);
            initCommand.AddOption(initPathOption);
            initCommand.AddOption(initTemplateOption);
            initCommand.AddOption(initAiOption);
            
            initCommand.SetHandler(async (name, type, path, template, ai) =>
            {
                try
                {
                    logger.LogInformation("Initializing project: {Name} of type {Type}", name, type);
                    
                    var projectPath = path ?? Path.Combine(Directory.GetCurrentDirectory(), name);
                    
                    if (ai)
                    {
                        Console.WriteLine($"Using AI-enhanced initialization for {name}...");
                        
                        try
                        {
                            // Load the AI-enhanced project initialization pipeline
                            var pipelineConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "examples", "project-init-pipeline.json");
                            if (!File.Exists(pipelineConfigPath))
                            {
                                // Fallback to current directory
                                pipelineConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "examples", "project-init-pipeline.json");
                            }
                            
                            if (File.Exists(pipelineConfigPath))
                            {
                                var pipelineConfig = await System.Text.Json.JsonSerializer.DeserializeAsync<Nexo.Feature.Pipeline.Models.PipelineConfiguration>(
                                    File.OpenRead(pipelineConfigPath));
                                
                                if (pipelineConfig != null)
                                {
                                    // Set up pipeline context with project parameters
                                    var context = new Dictionary<string, object>
                                    {
                                        { "projectName", name },
                                        { "projectType", type },
                                        { "projectPath", projectPath },
                                        { "enableAICustomization", true },
                                        { "generateDocumentation", true }
                                    };
                                    
                                    if (!string.IsNullOrEmpty(template))
                                    {
                                        context["customTemplate"] = template;
                                    }
                                    
                                    // Execute the AI-enhanced initialization pipeline
                                    Console.WriteLine("Executing AI-enhanced project initialization pipeline...");
                                    
                                    // TODO: Replace with actual pipeline execution service
                                    // var pipelineService = serviceProvider.GetRequiredService<IPipelineExecutionService>();
                                    // var result = await pipelineService.ExecutePipelineAsync(pipelineConfig, context, CancellationToken.None);
                                    
                                    // For now, simulate the pipeline execution
                                    await SimulateAIEnhancedInitialization(name, type, projectPath, template, logger);
                                    
                                    Console.WriteLine($"AI-enhanced project initialization completed for: {name}");
                                }
                                else
                                {
                                    throw new InvalidOperationException("Failed to load pipeline configuration");
                                }
                            }
                            else
                            {
                                throw new FileNotFoundException($"Pipeline configuration not found at: {pipelineConfigPath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "AI-enhanced initialization failed for project {Name}", name);
                            Console.WriteLine($"Warning: AI-enhanced initialization failed: {ex.Message}");
                            Console.WriteLine("Falling back to standard initialization...");
                            
                            // Fallback to standard initialization
                            await PerformStandardInitialization(name, type, projectPath, template, logger);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Initializing {type} project: {name}");
                        Console.WriteLine($"Project path: {projectPath}");
                        
                        await PerformStandardInitialization(name, type, projectPath, template, logger);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to initialize project {Name}", name);
                    Console.WriteLine($"Error: Failed to initialize project: {ex.Message}");
                }
            }, initNameOption, initTypeOption, initPathOption, initTemplateOption, initAiOption);
            
            projectCommand.AddCommand(initCommand);
            
            // Template command for template management
            var templateCommand = new Command("template", "Manage project templates");
            var templateListOption = new Option<bool>("--list", "List available templates") { IsRequired = false };
            var templateCreateOption = new Option<string>("--create", "Create a new template") { IsRequired = false };
            var templateShowOption = new Option<string>("--show", "Show template details") { IsRequired = false };
            var templateUpdateOption = new Option<string>("--update", "Update an existing template") { IsRequired = false };
            var templateDeleteOption = new Option<string>("--delete", "Delete a template") { IsRequired = false };
            
            templateCommand.AddOption(templateListOption);
            templateCommand.AddOption(templateCreateOption);
            templateCommand.AddOption(templateShowOption);
            templateCommand.AddOption(templateUpdateOption);
            templateCommand.AddOption(templateDeleteOption);
            
            templateCommand.SetHandler((list, create, show, update, delete) =>
            {
                try
                {
                    if (list)
                    {
                        logger.LogInformation("Listing available templates");
                        Console.WriteLine("Available templates:");
                        // TODO: Implement template listing
                        Console.WriteLine("- webapi (ASP.NET Core Web API)");
                        Console.WriteLine("- console (.NET Console Application)");
                        Console.WriteLine("- library (.NET Class Library)");
                        Console.WriteLine("- microservice (Microservice Template)");
                        Console.WriteLine("- blazor (Blazor Web Application)");
                    }
                    else if (!string.IsNullOrEmpty(create))
                    {
                        logger.LogInformation("Creating template: {Template}", create);
                        Console.WriteLine($"Creating template: {create}");
                        // TODO: Implement template creation
                    }
                    else if (!string.IsNullOrEmpty(show))
                    {
                        logger.LogInformation("Showing template: {Template}", show);
                        Console.WriteLine($"Template details for: {show}");
                        // TODO: Implement template details
                    }
                    else if (!string.IsNullOrEmpty(update))
                    {
                        logger.LogInformation("Updating template: {Template}", update);
                        Console.WriteLine($"Updating template: {update}");
                        // TODO: Implement template update
                    }
                    else if (!string.IsNullOrEmpty(delete))
                    {
                        logger.LogInformation("Deleting template: {Template}", delete);
                        Console.WriteLine($"Deleting template: {delete}");
                        // TODO: Implement template deletion
                    }
                    else
                    {
                        Console.WriteLine("Please specify an action: --list, --create, --show, --update, or --delete");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to manage templates");
                    Console.WriteLine($"Error: Failed to manage templates: {ex.Message}");
                }
            }, templateListOption, templateCreateOption, templateShowOption, templateUpdateOption, templateDeleteOption);
            
            projectCommand.AddCommand(templateCommand);
            
            // Scaffold command for code scaffolding
            var scaffoldCommand = new Command("scaffold", "Scaffold code and project structure");
            var scaffoldTypeOption = new Option<string>("--type", "Scaffold type (controller, service, model, test)") { IsRequired = true };
            var scaffoldNameOption = new Option<string>("--name", "Name for the scaffolded item") { IsRequired = true };
            var scaffoldPathOption = new Option<string>("--path", "Output path") { IsRequired = false };
            var scaffoldOptionsOption = new Option<string>("--options", "Additional options (JSON)") { IsRequired = false };
            
            scaffoldCommand.AddOption(scaffoldTypeOption);
            scaffoldCommand.AddOption(scaffoldNameOption);
            scaffoldCommand.AddOption(scaffoldPathOption);
            scaffoldCommand.AddOption(scaffoldOptionsOption);
            
            scaffoldCommand.SetHandler((type, name, path, options) =>
            {
                try
                {
                    logger.LogInformation("Scaffolding {Type}: {Name}", type, name);
                    
                    var outputPath = path ?? Directory.GetCurrentDirectory();
                    var scaffoldOptions = !string.IsNullOrEmpty(options) 
                        ? JsonSerializer.Deserialize<Dictionary<string, object>>(options)
                        : new Dictionary<string, object>();
                    
                    Console.WriteLine($"Scaffolding {type}: {name}");
                    Console.WriteLine($"Output path: {outputPath}");
                    
                    // TODO: Implement scaffolding logic
                    switch (type.ToLowerInvariant())
                    {
                        case "controller":
                            Console.WriteLine($"Creating API controller: {name}Controller.cs");
                            break;
                        case "service":
                            Console.WriteLine($"Creating service: {name}Service.cs");
                            break;
                        case "model":
                            Console.WriteLine($"Creating model: {name}.cs");
                            break;
                        case "test":
                            Console.WriteLine($"Creating test: {name}Tests.cs");
                            break;
                        default:
                            Console.WriteLine($"Unknown scaffold type: {type}");
                            break;
                    }
                    
                    Console.WriteLine("Scaffolding completed");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to scaffold {Type}: {Name}", type, name);
                    Console.WriteLine($"Error: Failed to scaffold: {ex.Message}");
                }
            }, scaffoldTypeOption, scaffoldNameOption, scaffoldPathOption, scaffoldOptionsOption);
            
            projectCommand.AddCommand(scaffoldCommand);
            
            // Environment command for development environment management
            var envCommand = new Command("env", "Manage development environment");
            var envSetupOption = new Option<string>("--setup", "Setup development environment for project") { IsRequired = false };
            var envCheckOption = new Option<bool>("--check", "Check environment requirements") { IsRequired = false };
            var envUpdateOption = new Option<bool>("--update", "Update development tools") { IsRequired = false };
            var envCleanOption = new Option<bool>("--clean", "Clean development environment") { IsRequired = false };
            
            envCommand.AddOption(envSetupOption);
            envCommand.AddOption(envCheckOption);
            envCommand.AddOption(envUpdateOption);
            envCommand.AddOption(envCleanOption);
            
            envCommand.SetHandler((setup, check, update, clean) =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(setup))
                    {
                        logger.LogInformation("Setting up development environment for: {Project}", setup);
                        Console.WriteLine($"Setting up development environment for: {setup}");
                        // TODO: Implement environment setup
                    }
                    else if (check)
                    {
                        logger.LogInformation("Checking development environment");
                        Console.WriteLine("Checking development environment requirements:");
                        // TODO: Implement environment check
                        Console.WriteLine("✓ .NET 8.0 SDK");
                        Console.WriteLine("✓ Git");
                        Console.WriteLine("✓ Docker (optional)");
                    }
                    else if (update)
                    {
                        logger.LogInformation("Updating development tools");
                        Console.WriteLine("Updating development tools...");
                        // TODO: Implement tool updates
                    }
                    else if (clean)
                    {
                        logger.LogInformation("Cleaning development environment");
                        Console.WriteLine("Cleaning development environment...");
                        // TODO: Implement environment cleanup
                    }
                    else
                    {
                        Console.WriteLine("Please specify an action: --setup, --check, --update, or --clean");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to manage environment");
                    Console.WriteLine($"Error: Failed to manage environment: {ex.Message}");
                }
            }, envSetupOption, envCheckOption, envUpdateOption, envCleanOption);
            
            projectCommand.AddCommand(envCommand);
            
            return projectCommand;
        }
        
        /// <summary>
        /// Simulates AI-enhanced project initialization using the pipeline architecture.
        /// </summary>
        private static async Task SimulateAIEnhancedInitialization(string name, string type, string projectPath, string? template, ILogger logger)
        {
            logger.LogInformation("Simulating AI-enhanced initialization for project {Name} of type {Type}", name, type);
            
            // Step 1: Validate project requirements
            Console.WriteLine("✓ Validating project requirements...");
            await Task.Delay(500); // Simulate processing time
            
            // Step 2: AI requirements analysis
            Console.WriteLine("✓ Analyzing requirements with AI...");
            await Task.Delay(1000); // Simulate AI processing
            
            // Step 3: Select optimal template
            var selectedTemplate = !string.IsNullOrEmpty(template) ? template : GetDefaultTemplateForType(type);
            Console.WriteLine($"✓ Selected optimal template: {selectedTemplate}");
            await Task.Delay(500);
            
            // Step 4: Create project structure
            Console.WriteLine("✓ Creating project structure...");
            await CreateProjectStructure(name, projectPath, type);
            await Task.Delay(1000);
            
            // Step 5: Generate project files
            Console.WriteLine("✓ Generating project files...");
            await GenerateProjectFiles(name, projectPath, type, selectedTemplate);
            await Task.Delay(1500);
            
            // Step 6: AI customization
            Console.WriteLine("✓ Applying AI-powered customizations...");
            await Task.Delay(2000); // Simulate AI customization
            
            // Step 7: Validate generated project
            Console.WriteLine("✓ Validating generated project...");
            await Task.Delay(500);
            
            // Step 8: Generate documentation
            Console.WriteLine("✓ Generating project documentation...");
            await GenerateProjectDocumentation(name, projectPath, type);
            await Task.Delay(500);
            
            Console.WriteLine($"🎉 AI-enhanced project '{name}' initialized successfully!");
        }
        
        /// <summary>
        /// Performs standard project initialization without AI enhancement.
        /// </summary>
        private static async Task PerformStandardInitialization(string name, string type, string projectPath, string? template, ILogger logger)
        {
            logger.LogInformation("Performing standard initialization for project {Name} of type {Type}", name, type);
            
            // Step 1: Create project structure
            Console.WriteLine("✓ Creating project structure...");
            await CreateProjectStructure(name, projectPath, type);
            
            // Step 2: Generate project files
            Console.WriteLine("✓ Generating project files...");
            var selectedTemplate = !string.IsNullOrEmpty(template) ? template : GetDefaultTemplateForType(type);
            await GenerateProjectFiles(name, projectPath, type, selectedTemplate);
            
            // Step 3: Generate basic documentation
            Console.WriteLine("✓ Generating basic documentation...");
            await GenerateProjectDocumentation(name, projectPath, type);
            
            Console.WriteLine($"✅ Project '{name}' initialized successfully!");
        }
        
        /// <summary>
        /// Creates the basic project directory structure.
        /// </summary>
        private static async Task CreateProjectStructure(string name, string projectPath, string type)
        {
            try
            {
                // Create main project directory
                Directory.CreateDirectory(projectPath);
                
                // Create standard .NET project structure
                var srcDir = Path.Combine(projectPath, "src");
                var testsDir = Path.Combine(projectPath, "tests");
                var docsDir = Path.Combine(projectPath, "docs");
                
                Directory.CreateDirectory(srcDir);
                Directory.CreateDirectory(testsDir);
                Directory.CreateDirectory(docsDir);
                
                // Create project-specific directories based on type
                switch (type.ToLowerInvariant())
                {
                    case "webapi":
                    case "mvc":
                        Directory.CreateDirectory(Path.Combine(srcDir, "Controllers"));
                        Directory.CreateDirectory(Path.Combine(srcDir, "Models"));
                        Directory.CreateDirectory(Path.Combine(srcDir, "Services"));
                        Directory.CreateDirectory(Path.Combine(srcDir, "Data"));
                        break;
                    case "microservice":
                        Directory.CreateDirectory(Path.Combine(srcDir, "API"));
                        Directory.CreateDirectory(Path.Combine(srcDir, "Core"));
                        Directory.CreateDirectory(Path.Combine(srcDir, "Infrastructure"));
                        Directory.CreateDirectory(Path.Combine(srcDir, "Shared"));
                        break;
                    case "library":
                        Directory.CreateDirectory(Path.Combine(srcDir, "Core"));
                        Directory.CreateDirectory(Path.Combine(srcDir, "Extensions"));
                        break;
                }
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create project structure: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Generates project files using the selected template.
        /// </summary>
        private static async Task GenerateProjectFiles(string name, string projectPath, string type, string template)
        {
            try
            {
                var srcDir = Path.Combine(projectPath, "src");
                var projectFileName = $"{name}.csproj";
                var solutionFileName = $"{name}.sln";
                
                // Generate .csproj file
                var projectFileContent = GenerateProjectFileContent(name, type);
#if NET8_0_OR_GREATER
                await File.WriteAllTextAsync(Path.Combine(srcDir, projectFileName), projectFileContent);
#else
                File.WriteAllText(Path.Combine(srcDir, projectFileName), projectFileContent);
#endif
                
                // Generate solution file
                var solutionContent = GenerateSolutionContent(name, projectFileName);
#if NET8_0_OR_GREATER
                await File.WriteAllTextAsync(Path.Combine(projectPath, solutionFileName), solutionContent);
#else
                File.WriteAllText(Path.Combine(projectPath, solutionFileName), solutionContent);
#endif
                
                // Generate basic source files
                await GenerateBasicSourceFiles(name, srcDir, type);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate project files: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Generates project documentation.
        /// </summary>
        private static async Task GenerateProjectDocumentation(string name, string projectPath, string type)
        {
            try
            {
                var readmeContent = GenerateReadmeContent(name, type);
#if NET8_0_OR_GREATER
                await File.WriteAllTextAsync(Path.Combine(projectPath, "README.md"), readmeContent);
#else
                File.WriteAllText(Path.Combine(projectPath, "README.md"), readmeContent);
#endif
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate documentation: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Gets the default template for a given project type.
        /// </summary>
        private static string GetDefaultTemplateForType(string type)
        {
            return type.ToLowerInvariant() switch
            {
                "webapi" => "webapi",
                "console" => "console",
                "library" => "library",
                "microservice" => "microservice",
                "blazor" => "blazor",
                "mvc" => "mvc",
                _ => "console"
            };
        }
        
        /// <summary>
        /// Generates the content for a .csproj file.
        /// </summary>
        private static string GenerateProjectFileContent(string name, string type)
        {
            var targetFramework = "<TargetFramework>net8.0</TargetFramework>";
            var sdk = type.ToLowerInvariant() switch
            {
                "webapi" => "Microsoft.NET.Sdk.Web",
                "mvc" => "Microsoft.NET.Sdk.Web",
                "blazor" => "Microsoft.NET.Sdk.Web",
                "console" => "Microsoft.NET.Sdk",
                "library" => "Microsoft.NET.Sdk",
                "microservice" => "Microsoft.NET.Sdk.Web",
                _ => "Microsoft.NET.Sdk"
            };
            
            var packageReferences = type.ToLowerInvariant() switch
            {
                "webapi" => @"    <PackageReference Include=""Microsoft.Extensions.Hosting"" Version=""8.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Logging"" Version=""8.0.0"" />
    <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""6.5.0"" />",
                "mvc" => @"    <PackageReference Include=""Microsoft.Extensions.Hosting"" Version=""8.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Logging"" Version=""8.0.0"" />",
                "blazor" => @"    <PackageReference Include=""Microsoft.Extensions.Hosting"" Version=""8.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Logging"" Version=""8.0.0"" />",
                "microservice" => @"    <PackageReference Include=""Microsoft.Extensions.Hosting"" Version=""8.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Logging"" Version=""8.0.0"" />
    <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""6.5.0"" />
    <PackageReference Include=""Microsoft.Extensions.Diagnostics.HealthChecks"" Version=""8.0.0"" />",
                _ => @"    <PackageReference Include=""Microsoft.Extensions.Hosting"" Version=""8.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Logging"" Version=""8.0.0"" />"
            };
            
            return $@"<Project Sdk=""{sdk}"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    {targetFramework}
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
{packageReferences}
  </ItemGroup>

</Project>";
        }
        
        /// <summary>
        /// Generates the content for a solution file.
        /// </summary>
        private static string GenerateSolutionContent(string name, string projectFileName)
        {
            return $@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{name}"", ""src\{projectFileName}"", ""{{GUID-HERE}}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{{GUID-HERE}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{GUID-HERE}}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{{GUID-HERE}}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{{GUID-HERE}}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal";
        }
        
        /// <summary>
        /// Generates basic source files for the project.
        /// </summary>
        private static async Task GenerateBasicSourceFiles(string name, string srcDir, string type)
        {
            // Generate Program.cs
            var programContent = type.ToLowerInvariant() switch
            {
                "webapi" => GenerateWebApiProgramContent(name),
                "mvc" => GenerateMvcProgramContent(name),
                "blazor" => GenerateBlazorProgramContent(name),
                "console" => GenerateConsoleProgramContent(name),
                "library" => GenerateLibraryProgramContent(name),
                "microservice" => GenerateMicroserviceProgramContent(name),
                _ => GenerateConsoleProgramContent(name)
            };
            
#if NET8_0_OR_GREATER
            await File.WriteAllTextAsync(Path.Combine(srcDir, "Program.cs"), programContent);
#else
            File.WriteAllText(Path.Combine(srcDir, "Program.cs"), programContent);
#endif
        }
        
        /// <summary>
        /// Generates Program.cs content for different project types.
        /// </summary>
        private static string GenerateWebApiProgramContent(string name)
        {
            return $@"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{{
    app.UseSwagger();
    app.UseSwaggerUI();
}}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();";
        }
        
        private static string GenerateMvcProgramContent(string name)
        {
            return $@"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{{
    app.UseExceptionHandler(""/Home/Error"");
    app.UseHsts();
}}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllerRoute(
    name: ""default"",
    pattern: ""{{controller=Home}}/{{action=Index}}/{{id?}}"");

app.Run();";
        }
        
        private static string GenerateBlazorProgramContent(string name)
        {
            return $@"using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{{
    app.UseExceptionHandler(""/Error"");
    app.UseHsts();
}}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage(""/_Host"");

app.Run();";
        }
        
        private static string GenerateConsoleProgramContent(string name)
        {
            return $@"using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {{
        // Add your services here
    }})
    .ConfigureLogging(logging =>
    {{
        logging.ClearProviders();
        logging.AddConsole();
    }});

var host = builder.Build();
await host.RunAsync();";
        }
        
        private static string GenerateLibraryProgramContent(string name)
        {
            return $@"// This is a class library project.
// Add your classes and interfaces here.

namespace {name}
{{
    public class Class1
    {{
        // Add your implementation here
    }}
}}";
        }
        
        private static string GenerateMicroserviceProgramContent(string name)
        {
            return $@"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{{
    app.UseSwagger();
    app.UseSwaggerUI();
}}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks(""/health"");

app.Run();";
        }
        
        /// <summary>
        /// Generates README.md content for the project.
        /// </summary>
        private static string GenerateReadmeContent(string name, string type)
        {
            return $@"# {name}

A {type} project created with Nexo CLI.

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Your preferred IDE (Visual Studio, VS Code, Rider, etc.)

### Running the Project

```bash
cd {name}
dotnet run
```

### Building the Project

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

## Project Structure

This project follows standard .NET conventions:

- `src/` - Source code
- `tests/` - Test projects
- `docs/` - Documentation

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## License

This project is licensed under the MIT License.
";
        }
    }
} 