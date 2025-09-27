using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Commands.AI
{
    /// <summary>
    /// Command creation and configuration for AI documentation commands.
    /// </summary>
    public partial class AIDocumentationCommands
    {
        /// <summary>
        /// Creates API documentation command.
        /// </summary>
        private Command CreateApiDocumentationCommand()
        {
            var apiCommand = new Command("api", "Generate API documentation");
            var inputOption = new Option<string>("--input", "Input directory or file");
            var outputOption = new Option<string>("--output", "Output directory");
            var formatOption = new Option<string>("--format", "Output format (markdown, html, pdf)");
            var includePrivateOption = new Option<bool>("--include-private", "Include private members");

            apiCommand.AddOption(inputOption);
            apiCommand.AddOption(outputOption);
            apiCommand.AddOption(formatOption);
            apiCommand.AddOption(includePrivateOption);

            apiCommand.SetHandler(async (string input, string output, string format, bool includePrivate) =>
            {
                try
                {
                    Console.WriteLine("📚 Generating API Documentation");
                    Console.WriteLine(new string('=', 35));
                    Console.WriteLine($"Input: {input ?? "Current directory"}");
                    Console.WriteLine($"Output: {output ?? "docs/api"}");
                    Console.WriteLine($"Format: {format ?? "markdown"}");
                    Console.WriteLine($"Include Private: {includePrivate}");
                    Console.WriteLine();

                    await GenerateApiDocumentationAsync(input, output, format, includePrivate);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to generate API documentation: {ex.Message}");
                    _logger.LogError(ex, "Failed to generate API documentation");
                }
            }, inputOption, outputOption, formatOption, includePrivateOption);

            return apiCommand;
        }

        /// <summary>
        /// Creates README generation command.
        /// </summary>
        private Command CreateReadmeCommand()
        {
            var readmeCommand = new Command("readme", "Generate README.md");
            var projectOption = new Option<string>("--project", "Project directory");
            var outputOption = new Option<string>("--output", "Output file path");
            var templateOption = new Option<string>("--template", "Template to use");

            readmeCommand.AddOption(projectOption);
            readmeCommand.AddOption(outputOption);
            readmeCommand.AddOption(templateOption);

            readmeCommand.SetHandler(async (string project, string output, string template) =>
            {
                try
                {
                    Console.WriteLine("📄 Generating README.md");
                    Console.WriteLine(new string('=', 30));
                    Console.WriteLine($"Project: {project ?? "Current directory"}");
                    Console.WriteLine($"Output: {output ?? "README.md"}");
                    Console.WriteLine($"Template: {template ?? "default"}");
                    Console.WriteLine();

                    await GenerateReadmeAsync(project, output, template);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to generate README: {ex.Message}");
                    _logger.LogError(ex, "Failed to generate README");
                }
            }, projectOption, outputOption, templateOption);

            return readmeCommand;
        }

        /// <summary>
        /// Creates code comments command.
        /// </summary>
        private Command CreateCodeCommentsCommand()
        {
            var commentsCommand = new Command("comments", "Generate code comments");
            var inputOption = new Option<string>("--input", "Input directory or file");
            var outputOption = new Option<string>("--output", "Output directory");
            var languageOption = new Option<string>("--language", "Programming language");
            var styleOption = new Option<string>("--style", "Comment style (xml, javadoc, doxygen)");

            commentsCommand.AddOption(inputOption);
            commentsCommand.AddOption(outputOption);
            commentsCommand.AddOption(languageOption);
            commentsCommand.AddOption(styleOption);

            commentsCommand.SetHandler(async (string input, string output, string language, string style) =>
            {
                try
                {
                    Console.WriteLine("💬 Generating Code Comments");
                    Console.WriteLine(new string('=', 30));
                    Console.WriteLine($"Input: {input ?? "Current directory"}");
                    Console.WriteLine($"Output: {output ?? "Same as input"}");
                    Console.WriteLine($"Language: {language ?? "Auto-detect"}");
                    Console.WriteLine($"Style: {style ?? "xml"}");
                    Console.WriteLine();

                    await GenerateCodeCommentsAsync(input, output, language, style);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to generate code comments: {ex.Message}");
                    _logger.LogError(ex, "Failed to generate code comments");
                }
            }, inputOption, outputOption, languageOption, styleOption);

            return commentsCommand;
        }

        /// <summary>
        /// Creates architecture documentation command.
        /// </summary>
        private Command CreateArchitectureCommand()
        {
            var archCommand = new Command("architecture", "Generate architecture documentation");
            var projectOption = new Option<string>("--project", "Project directory");
            var outputOption = new Option<string>("--output", "Output directory");
            var includeDiagramsOption = new Option<bool>("--include-diagrams", "Include architecture diagrams");

            archCommand.AddOption(projectOption);
            archCommand.AddOption(outputOption);
            archCommand.AddOption(includeDiagramsOption);

            archCommand.SetHandler(async (string project, string output, bool includeDiagrams) =>
            {
                try
                {
                    Console.WriteLine("Building Generating Architecture Documentation");
                    Console.WriteLine(new string('=', 40));
                    Console.WriteLine($"Project: {project ?? "Current directory"}");
                    Console.WriteLine($"Output: {output ?? "docs/architecture"}");
                    Console.WriteLine($"Include Diagrams: {includeDiagrams}");
                    Console.WriteLine();

                    await GenerateArchitectureDocumentationAsync(project, output, includeDiagrams);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to generate architecture documentation: {ex.Message}");
                    _logger.LogError(ex, "Failed to generate architecture documentation");
                }
            }, projectOption, outputOption, includeDiagramsOption);

            return archCommand;
        }

        /// <summary>
        /// Creates changelog generation command.
        /// </summary>
        private Command CreateChangelogCommand()
        {
            var changelogCommand = new Command("changelog", "Generate CHANGELOG.md");
            var projectOption = new Option<string>("--project", "Project directory");
            var outputOption = new Option<string>("--output", "Output file path");
            var versionOption = new Option<string>("--version", "Version to generate changelog for");
            var sinceOption = new Option<string>("--since", "Generate changelog since this version");

            changelogCommand.AddOption(projectOption);
            changelogCommand.AddOption(outputOption);
            changelogCommand.AddOption(versionOption);
            changelogCommand.AddOption(sinceOption);

            changelogCommand.SetHandler(async (string project, string output, string version, string since) =>
            {
                try
                {
                    Console.WriteLine("Document Generating CHANGELOG.md");
                    Console.WriteLine(new string('=', 30));
                    Console.WriteLine($"Project: {project ?? "Current directory"}");
                    Console.WriteLine($"Output: {output ?? "CHANGELOG.md"}");
                    Console.WriteLine($"Version: {version ?? "Latest"}");
                    Console.WriteLine($"Since: {since ?? "Previous version"}");
                    Console.WriteLine();

                    await GenerateChangelogAsync(project, output, version, since);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to generate changelog: {ex.Message}");
                    _logger.LogError(ex, "Failed to generate changelog");
                }
            }, projectOption, outputOption, versionOption, sinceOption);

            return changelogCommand;
        }

        /// <summary>
        /// Creates user guide generation command.
        /// </summary>
        private Command CreateUserGuideCommand()
        {
            var guideCommand = new Command("guide", "Generate user guide");
            var projectOption = new Option<string>("--project", "Project directory");
            var outputOption = new Option<string>("--output", "Output directory");
            var audienceOption = new Option<string>("--audience", "Target audience (beginner, intermediate, advanced)");

            guideCommand.AddOption(projectOption);
            guideCommand.AddOption(outputOption);
            guideCommand.AddOption(audienceOption);

            guideCommand.SetHandler(async (string project, string output, string audience) =>
            {
                try
                {
                    Console.WriteLine("📖 Generating User Guide");
                    Console.WriteLine(new string('=', 30));
                    Console.WriteLine($"Project: {project ?? "Current directory"}");
                    Console.WriteLine($"Output: {output ?? "docs/user-guide"}");
                    Console.WriteLine($"Audience: {audience ?? "intermediate"}");
                    Console.WriteLine();

                    await GenerateUserGuideAsync(project, output, audience);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to generate user guide: {ex.Message}");
                    _logger.LogError(ex, "Failed to generate user guide");
                }
            }, projectOption, outputOption, audienceOption);

            return guideCommand;
        }

        /// <summary>
        /// Creates comprehensive documentation command.
        /// </summary>
        private Command CreateComprehensiveCommand()
        {
            var comprehensiveCommand = new Command("all", "Generate comprehensive documentation");
            var projectOption = new Option<string>("--project", "Project directory");
            var outputOption = new Option<string>("--output", "Output directory");
            var includeOption = new Option<string[]>("--include", "Documentation types to include");

            comprehensiveCommand.AddOption(projectOption);
            comprehensiveCommand.AddOption(outputOption);
            comprehensiveCommand.AddOption(includeOption);

            comprehensiveCommand.SetHandler(async (string project, string[] include) =>
            {
                try
                {
                    var output = "docs";
                    var types = include.Length > 0 ? include : new[] { "api", "readme", "architecture", "changelog", "guide" };

                    Console.WriteLine("Documentation Generating Comprehensive Documentation");
                    Console.WriteLine(new string('=', 45));
                    Console.WriteLine($"Project: {project ?? "Current directory"}");
                    Console.WriteLine($"Output: {output}");
                    Console.WriteLine($"Types: {string.Join(", ", types)}");
                    Console.WriteLine();

                    await GenerateComprehensiveDocumentationAsync(project, output, types);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to generate comprehensive documentation: {ex.Message}");
                    _logger.LogError(ex, "Failed to generate comprehensive documentation");
                }
            }, projectOption, includeOption);

            return comprehensiveCommand;
        }
    }
}
