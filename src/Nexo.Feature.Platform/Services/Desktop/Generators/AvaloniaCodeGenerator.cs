using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Platform.Models;

namespace Nexo.Feature.Platform.Services.Desktop.Generators
{
    public class AvaloniaCodeGenerator
    {
        private readonly ILogger<AvaloniaCodeGenerator> _logger;

        public AvaloniaCodeGenerator(ILogger<AvaloniaCodeGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DesktopCodeGenerationResult> GenerateAvaloniaCodeAsync(DesktopCodeGenerationRequest request)
        {
            try
            {
                _logger.LogInformation("Generating Avalonia code for {ApplicationType}", request.ApplicationType);

                var result = new DesktopCodeGenerationResult
                {
                    Platform = request.Platform,
                    ApplicationType = request.ApplicationType,
                    UIFramework = "Avalonia",
                    Success = true
                };

                // Generate main window
                var mainWindow = GenerateMainWindowCode(request);
                result.GeneratedFiles.Add(new GeneratedFile
                {
                    FileName = "MainWindow.axaml",
                    Content = mainWindow,
                    FileType = "AXAML"
                });

                // Generate main window code-behind
                var mainWindowCodeBehind = GenerateMainWindowCodeBehind(request);
                result.GeneratedFiles.Add(new GeneratedFile
                {
                    FileName = "MainWindow.axaml.cs",
                    Content = mainWindowCodeBehind,
                    FileType = "C#"
                });

                // Generate app.axaml
                var appAxaml = GenerateAppAxamlCode(request);
                result.GeneratedFiles.Add(new GeneratedFile
                {
                    FileName = "App.axaml",
                    Content = appAxaml,
                    FileType = "AXAML"
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Avalonia code");
                return new DesktopCodeGenerationResult
                {
                    Success = false,
                    Errors = { $"Avalonia code generation failed: {ex.Message}" }
                };
            }
        }

        private string GenerateMainWindowCode(DesktopCodeGenerationRequest request)
        {
            return $@"<Window xmlns=""https://github.com/avaloniaui""
        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
        x:Class=""{request.ApplicationType}.MainWindow""
        Title=""{request.ApplicationType}"" Width=""800"" Height=""450"">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto""/>
            <RowDefinition Height=""*""/>
            <RowDefinition Height=""Auto""/>
        </Grid.RowDefinitions>
        
        <Menu Grid.Row=""0"">
            <MenuItem Header=""_File"">
                <MenuItem Header=""_New""/>
                <MenuItem Header=""_Open""/>
                <MenuItem Header=""_Save""/>
                <Separator/>
                <MenuItem Header=""E_xit""/>
            </MenuItem>
            <MenuItem Header=""_Edit"">
                <MenuItem Header=""_Undo""/>
                <MenuItem Header=""_Redo""/>
            </MenuItem>
            <MenuItem Header=""_Help"">
                <MenuItem Header=""_About""/>
            </MenuItem>
        </Menu>
        
        <TextBlock Grid.Row=""1"" 
                   Text=""Welcome to {request.ApplicationType}"" 
                   HorizontalAlignment=""Center"" 
                   VerticalAlignment=""Center"" 
                   FontSize=""24""/>
        
        <StatusBar Grid.Row=""2"">
            <StatusBarItem>
                <TextBlock Text=""Ready""/>
            </StatusBarItem>
        </StatusBar>
    </Grid>
</Window>";
        }

        private string GenerateMainWindowCodeBehind(DesktopCodeGenerationRequest request)
        {
            return $@"using Avalonia.Controls;

namespace {request.ApplicationType}
{{
    public partial class MainWindow : Window
    {{
        public MainWindow()
        {{
            InitializeComponent();
        }}
    }}
}}";
        }

        private string GenerateAppAxamlCode(DesktopCodeGenerationRequest request)
        {
            return $@"<Application xmlns=""https://github.com/avaloniaui""
             xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
             x:Class=""{request.ApplicationType}.App""
             RequestedThemeVariant=""Default"">
    <Application.Styles>
        <FluentTheme />
    </Application.Styles>
</Application>";
        }
    }
}
