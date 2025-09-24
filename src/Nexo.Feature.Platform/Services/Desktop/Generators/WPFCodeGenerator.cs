using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Platform.Models;

namespace Nexo.Feature.Platform.Services.Desktop.Generators
{
    public class WPFCodeGenerator
    {
        private readonly ILogger<WPFCodeGenerator> _logger;

        public WPFCodeGenerator(ILogger<WPFCodeGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DesktopCodeGenerationResult> GenerateWPFCodeAsync(DesktopCodeGenerationRequest request)
        {
            try
            {
                _logger.LogInformation("Generating WPF code for {ApplicationType}", request.ApplicationType);

                var result = new DesktopCodeGenerationResult
                {
                    Platform = request.Platform,
                    ApplicationType = request.ApplicationType,
                    UIFramework = "WPF",
                    Success = true
                };

                // Generate main window
                var mainWindow = GenerateMainWindowCode(request);
                result.GeneratedFiles.Add(new GeneratedFile
                {
                    FileName = "MainWindow.xaml",
                    Content = mainWindow,
                    FileType = "XAML"
                });

                // Generate main window code-behind
                var mainWindowCodeBehind = GenerateMainWindowCodeBehind(request);
                result.GeneratedFiles.Add(new GeneratedFile
                {
                    FileName = "MainWindow.xaml.cs",
                    Content = mainWindowCodeBehind,
                    FileType = "C#"
                });

                // Generate view model
                var viewModel = GenerateViewModelCode(request);
                result.GeneratedFiles.Add(new GeneratedFile
                {
                    FileName = "MainViewModel.cs",
                    Content = viewModel,
                    FileType = "C#"
                });

                // Generate app.xaml
                var appXaml = GenerateAppXamlCode(request);
                result.GeneratedFiles.Add(new GeneratedFile
                {
                    FileName = "App.xaml",
                    Content = appXaml,
                    FileType = "XAML"
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating WPF code");
                return new DesktopCodeGenerationResult
                {
                    Success = false,
                    Errors = { $"WPF code generation failed: {ex.Message}" }
                };
            }
        }

        private string GenerateMainWindowCode(DesktopCodeGenerationRequest request)
        {
            return $@"<Window x:Class=""{request.ApplicationType}.MainWindow""
        xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
        Title=""{request.ApplicationType}"" Height=""450"" Width=""800"">
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
            return $@"using System.Windows;

namespace {request.ApplicationType}
{{
    public partial class MainWindow : Window
    {{
        public MainWindow()
        {{
            InitializeComponent();
            DataContext = new MainViewModel();
        }}
    }}
}}";
        }

        private string GenerateViewModelCode(DesktopCodeGenerationRequest request)
        {
            return $@"using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace {request.ApplicationType}
{{
    public class MainViewModel : INotifyPropertyChanged
    {{
        private string _title = ""{request.ApplicationType}"";

        public string Title
        {{
            get => _title;
            set
            {{
                _title = value;
                OnPropertyChanged();
            }}
        }}

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {{
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }}
    }}
}}";
        }

        private string GenerateAppXamlCode(DesktopCodeGenerationRequest request)
        {
            return $@"<Application x:Class=""{request.ApplicationType}.App""
             xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
             xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
             StartupUri=""MainWindow.xaml"">
    <Application.Resources>
        <!-- Application resources -->
    </Application.Resources>
</Application>";
        }
    }
}
