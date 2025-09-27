using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Platform.Models;

namespace Nexo.Feature.Platform.Services.Desktop.Generators
{
    public partial class EtoCodeGenerator
    {
        private readonly ILogger<EtoCodeGenerator> _logger;

        public EtoCodeGenerator(ILogger<EtoCodeGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DesktopCodeGenerationResult> GenerateEtoCodeAsync(DesktopCodeGenerationRequest request)
        {
            try
            {
                _logger.LogInformation("Generating Eto code for {ApplicationType}", request.ApplicationType);

                var result = new DesktopCodeGenerationResult
                {
                    Platform = request.Platform,
                    ApplicationType = request.ApplicationType,
                    UIFramework = "Eto",
                    Success = true
                };

                // Generate main form
                var mainForm = GenerateMainFormCode(request);
                result.GeneratedFiles.Add(new GeneratedFile
                {
                    FileName = "MainForm.cs",
                    Content = mainForm,
                    FileType = "C#"
                });

                // Generate program
                var program = GenerateProgramCode(request);
                result.GeneratedFiles.Add(new GeneratedFile
                {
                    FileName = "Program.cs",
                    Content = program,
                    FileType = "C#"
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Eto code");
                return new DesktopCodeGenerationResult
                {
                    Success = false,
                    Errors = { $"Eto code generation failed: {ex.Message}" }
                };
            }
        }

        private string GenerateMainFormCode(DesktopCodeGenerationRequest request)
        {
            return $@"using Eto.Forms;
using Eto.Drawing;

namespace {request.ApplicationType}
{{
    public partial class MainForm : Form
    {{
        public MainForm()
        {{
            Title = ""{request.ApplicationType}"";
            ClientSize = new Size(800, 450);
            
            var layout = new TableLayout
            {{
                Spacing = new Size(5, 5),
                Padding = new Padding(10)
            }};
            
            // Add controls
            var label = new Label
            {{
                Text = ""Welcome to {request.ApplicationType}!"",
                TextAlignment = TextAlignment.Center
            }};
            
            var button = new Button
            {{
                Text = ""Click Me""
            }};
            button.Click += (sender, e) => MessageBox.Show(""Hello from {request.ApplicationType}!"", ""Information"");
            
            layout.Add(label, 0, 0);
            layout.Add(button, 0, 1);
            
            Content = layout;
        }}
    }}
}}";
        }

        private string GenerateProgramCode(DesktopCodeGenerationRequest request)
        {
            return $@"using Eto.Forms;

namespace {request.ApplicationType}
{{
    class Program
    {{
        [STAThread]
        static void Main(string[] args)
        {{
            var app = new Application();
            app.Run(new MainForm());
        }}
    }}
}}";
        }
    }
}
