using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Domain.Models.Extensions;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// CLI commands for AI-generated extension management
    /// </summary>
    public static class ExtensionCommands
    {
        /// <summary>
        /// Creates the main extension command with all subcommands
        /// </summary>
        public static Command CreateExtensionCommand(IServiceProvider serviceProvider)
        {
            var extensionCommand = new Command("extension", "AI-generated extension management and generation");
            
            // Add subcommands
            extensionCommand.AddCommand(CreateGenerateCommand(serviceProvider));
            extensionCommand.AddCommand(CreateListCommand(serviceProvider));
            extensionCommand.AddCommand(CreateValidateCommand(serviceProvider));
            
            return extensionCommand;
        }
        
        /// <summary>
        /// Creates the generate command for generating new extensions
        /// </summary>
        private static Command CreateGenerateCommand(IServiceProvider serviceProvider)
        {
            var generateCommand = new Command("generate", "Generate a new AI-powered extension");
            
            var nameOption = new Option<string>(
                "--name",
                "Name of the extension to generate");
            nameOption.IsRequired = true;
            
            var descriptionOption = new Option<string>(
                "--description",
                "Description of what the extension should do");
            descriptionOption.IsRequired = true;
            
            var typeOption = new Option<ExtensionType>(
                "--type",
                () => ExtensionType.Analyzer,
                "Type of extension to generate (Analyzer, Generator, Processor)");
            
            var authorOption = new Option<string>(
                "--author",
                () => "System",
                "Author of the extension");
            
            var versionOption = new Option<string>(
                "--version",
                () => "1.0.0",
                "Version of the extension");
            
            var outputOption = new Option<string>(
                "--output",
                () => ".",
                "Output directory for the generated extension file");
            
            var tagsOption = new Option<string[]>(
                "--tags",
                () => Array.Empty<string>(),
                "Tags to categorize the extension");
            
            generateCommand.AddOption(nameOption);
            generateCommand.AddOption(descriptionOption);
            generateCommand.AddOption(typeOption);
            generateCommand.AddOption(authorOption);
            generateCommand.AddOption(versionOption);
            generateCommand.AddOption(outputOption);
            generateCommand.AddOption(tagsOption);
            
            generateCommand.SetHandler(async (name, description, type, author, version, output, tags) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<ExtensionCommands>>();
                var extensionGenerator = serviceProvider.GetRequiredService<IExtensionGenerator>();
                
                await GenerateExtension(extensionGenerator, logger, name, description, type, author, version, output, tags);
            }, nameOption, descriptionOption, typeOption, authorOption, versionOption, outputOption, tagsOption);
            
            return generateCommand;
        }
        
        /// <summary>
        /// Creates the list command for listing generated extensions
        /// </summary>
        private static Command CreateListCommand(IServiceProvider serviceProvider)
        {
            var listCommand = new Command("list", "List generated extensions in the output directory");
            
            var outputOption = new Option<string>(
                "--output",
                () => ".",
                "Directory to search for extension files");
            
            listCommand.AddOption(outputOption);
            
            listCommand.SetHandler(async (output) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<ExtensionCommands>>();
                await ListExtensions(logger, output);
            }, outputOption);
            
            return listCommand;
        }
        
        /// <summary>
        /// Creates the validate command for validating extension files
        /// </summary>
        private static Command CreateValidateCommand(IServiceProvider serviceProvider)
        {
            var validateCommand = new Command("validate", "Validate an extension file for syntax and structure");
            
            var fileOption = new Option<string>(
                "--file",
                "Path to the extension file to validate");
            fileOption.IsRequired = true;
            
            validateCommand.AddOption(fileOption);
            
            validateCommand.SetHandler(async (file) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<ExtensionCommands>>();
                await ValidateExtension(logger, file);
            }, fileOption);
            
            return validateCommand;
        }
        
        /// <summary>
        /// Generates a new extension using the AI generator
        /// </summary>
        private static async Task GenerateExtension(
            IExtensionGenerator extensionGenerator,
            ILogger logger,
            string name,
            string description,
            ExtensionType type,
            string author,
            string version,
            string output,
            string[] tags)
        {
            try
            {
                logger.LogInformation("Generating extension: {ExtensionName}", name);
                
                // Create the extension request
                var request = new ExtensionRequest
                {
                    Name = name,
                    Description = description,
                    Type = type,
                    Author = author,
                    Version = version,
                    Tags = tags,
                    Dependencies = Array.Empty<string>(),
                    Configuration = new Dictionary<string, object>()
                };
                
                // Generate the extension
                var result = await extensionGenerator.GenerateAsync(request);
                
                if (!result.IsSuccess)
                {
                    logger.LogError("Failed to generate extension: {ExtensionName}", name);
                    Console.WriteLine($"❌ Failed to generate extension '{name}':");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"  - {error}");
                    }
                    return;
                }
                
                // Ensure output directory exists
                var outputDir = Path.GetFullPath(output);
                Directory.CreateDirectory(outputDir);
                
                // Write the generated code to file
                var filePath = Path.Combine(outputDir, result.FileName);
                await File.WriteAllTextAsync(filePath, result.Code);
                
                logger.LogInformation("Successfully generated extension: {ExtensionName} at {FilePath}", name, filePath);
                
                Console.WriteLine($"✅ Successfully generated extension '{name}'");
                Console.WriteLine($"📁 File: {filePath}");
                Console.WriteLine($"📊 Code length: {result.Code.Length} characters");
                Console.WriteLine($"🏷️  Type: {type}");
                Console.WriteLine($"👤 Author: {author}");
                Console.WriteLine($"📝 Version: {version}");
                
                if (result.Warnings.Any())
                {
                    Console.WriteLine("⚠️  Warnings:");
                    foreach (var warning in result.Warnings)
                    {
                        Console.WriteLine($"  - {warning}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating extension: {ExtensionName}", name);
                Console.WriteLine($"❌ Error generating extension '{name}': {ex.Message}");
            }
        }
        
        /// <summary>
        /// Lists all extension files in the specified directory
        /// </summary>
        private static async Task ListExtensions(ILogger logger, string output)
        {
            try
            {
                var outputDir = Path.GetFullPath(output);
                
                if (!Directory.Exists(outputDir))
                {
                    Console.WriteLine($"❌ Output directory does not exist: {outputDir}");
                    return;
                }
                
                var extensionFiles = Directory.GetFiles(outputDir, "*.cs", SearchOption.TopDirectoryOnly)
                    .Where(f => Path.GetFileName(f).EndsWith(".cs"))
                    .OrderBy(f => Path.GetFileName(f))
                    .ToArray();
                
                if (extensionFiles.Length == 0)
                {
                    Console.WriteLine($"📁 No extension files found in: {outputDir}");
                    return;
                }
                
                Console.WriteLine($"📁 Found {extensionFiles.Length} extension file(s) in: {outputDir}");
                Console.WriteLine();
                
                foreach (var file in extensionFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var fileInfo = new FileInfo(file);
                    var fileSize = fileInfo.Length;
                    var lastModified = fileInfo.LastWriteTime;
                    
                    Console.WriteLine($"  📄 {fileName}");
                    Console.WriteLine($"     Size: {fileSize:N0} bytes");
                    Console.WriteLine($"     Modified: {lastModified:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error listing extensions in directory: {OutputDir}", output);
                Console.WriteLine($"❌ Error listing extensions: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Validates an extension file for basic syntax and structure
        /// </summary>
        private static async Task ValidateExtension(ILogger logger, string file)
        {
            try
            {
                var filePath = Path.GetFullPath(file);
                
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"❌ Extension file does not exist: {filePath}");
                    return;
                }
                
                var content = await File.ReadAllTextAsync(filePath);
                
                // Basic validation checks
                var isValid = true;
                var errors = new List<string>();
                
                // Check if file is empty
                if (string.IsNullOrWhiteSpace(content))
                {
                    errors.Add("File is empty or contains only whitespace");
                    isValid = false;
                }
                
                // Check for basic C# structure
                if (!content.Contains("class") && !content.Contains("interface") && !content.Contains("enum"))
                {
                    errors.Add("File does not contain any class, interface, or enum definitions");
                    isValid = false;
                }
                
                // Check for basic syntax (simple checks)
                var openBraces = content.Count(c => c == '{');
                var closeBraces = content.Count(c => c == '}');
                if (openBraces != closeBraces)
                {
                    errors.Add($"Mismatched braces: {openBraces} opening, {closeBraces} closing");
                    isValid = false;
                }
                
                var openParens = content.Count(c => c == '(');
                var closeParens = content.Count(c => c == ')');
                if (openParens != closeParens)
                {
                    errors.Add($"Mismatched parentheses: {openParens} opening, {closeParens} closing");
                    isValid = false;
                }
                
                // Display results
                if (isValid)
                {
                    Console.WriteLine($"✅ Extension file is valid: {filePath}");
                    Console.WriteLine($"📊 File size: {content.Length:N0} characters");
                    Console.WriteLine($"📄 Lines: {content.Split('\n').Length:N0}");
                }
                else
                {
                    Console.WriteLine($"❌ Extension file has validation errors: {filePath}");
                    Console.WriteLine();
                    Console.WriteLine("Errors found:");
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"  - {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error validating extension file: {FilePath}", file);
                Console.WriteLine($"❌ Error validating extension file: {ex.Message}");
            }
        }
    }
}
