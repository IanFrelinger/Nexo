using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Platform.Models;

namespace Nexo.Feature.Platform.Services.Desktop.Generators
{
    public partial class ConsoleCodeGenerator
    {
        private readonly ILogger<ConsoleCodeGenerator> _logger;

        public ConsoleCodeGenerator(ILogger<ConsoleCodeGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DesktopCodeGenerationResult> GenerateConsoleCodeAsync(DesktopCodeGenerationRequest request)
        {
            try
            {
                _logger.LogInformation("Generating Console code for {ApplicationType}", request.ApplicationType);

                var result = new DesktopCodeGenerationResult
                {
                    Platform = request.Platform,
                    ApplicationType = request.ApplicationType,
                    UIFramework = "Console",
                    Success = true
                };

                // Generate main program
                var program = GenerateProgramCode(request);
                result.GeneratedFiles.Add(new GeneratedFile
                {
                    FileName = "Program.cs",
                    Content = program,
                    FileType = "C#"
                });

                // Generate service class
                var service = GenerateServiceCode(request);
                result.GeneratedFiles.Add(new GeneratedFile
                {
                    FileName = "ApplicationService.cs",
                    Content = service,
                    FileType = "C#"
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Console code");
                return new DesktopCodeGenerationResult
                {
                    Success = false,
                    Errors = { $"Console code generation failed: {ex.Message}" }
                };
            }
        }

        private string GenerateProgramCode(DesktopCodeGenerationRequest request)
        {
            return $@"using System;
using System.Threading.Tasks;

namespace {request.ApplicationType}
{{
    class Program
    {{
        static async Task Main(string[] args)
        {{
            Console.WriteLine(""Welcome to {request.ApplicationType}!"");
            Console.WriteLine(""Platform: {request.Platform}"");
            Console.WriteLine(""Application Type: {request.ApplicationType}"");
            
            var service = new ApplicationService();
            await service.RunAsync();
            
            Console.WriteLine(""Press any key to exit..."");
            Console.ReadKey();
        }}
    }}
}}";
        }

        private string GenerateServiceCode(DesktopCodeGenerationRequest request)
        {
            return $@"using System;
using System.Threading.Tasks;

namespace {request.ApplicationType}
{{
    public partial class ApplicationService
    {{
        public async Task RunAsync()
        {{
            Console.WriteLine(""Starting {request.ApplicationType} service..."");
            
            // Main application logic
            await ProcessDataAsync();
            
            Console.WriteLine(""{request.ApplicationType} service completed."");
        }}
        
        private async Task ProcessDataAsync()
        {{
            // Simulate some work
            await Task.Delay(1000);
            Console.WriteLine(""Data processing completed."");
        }}
    }}
}}";
        }
    }
}
