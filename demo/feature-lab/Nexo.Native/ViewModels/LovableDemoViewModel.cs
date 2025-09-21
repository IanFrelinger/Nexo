using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace Nexo.Native.ViewModels
{
    public class LovableDemoViewModel : INotifyPropertyChanged
    {
        private string _userPrompt = "";
        private AppTypeInfo _selectedAppType;
        private bool _isGenerating = false;
        private bool _showResults = false;
        private double _generationProgress = 0;
        private string _currentStep = "";
        private string _generatedAppName = "";
        private string _generatedDescription = "";
        private bool _darkModeEnabled = false;
        private bool _authenticationEnabled = false;
        private bool _databaseEnabled = false;
        private bool _responsiveEnabled = false;
        private bool _realTimeEnabled = false;
        private bool _pwaEnabled = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public LovableDemoViewModel()
        {
            InitializeAppTypes();
            InitializeQuickExamples();
            GenerateCommand = new Command(async () => await GenerateApplicationAsync(), () => CanGenerate);
            OpenProjectCommand = new Command(async () => await OpenProjectAsync());
            RunAppCommand = new Command(async () => await RunAppAsync());
            UseExampleCommand = new Command<QuickExample>(async (example) => await UseExampleAsync(example));
        }

        public string UserPrompt
        {
            get => _userPrompt;
            set
            {
                _userPrompt = value;
                OnPropertyChanged();
                ((Command)GenerateCommand).ChangeCanExecute();
            }
        }

        public AppTypeInfo SelectedAppType
        {
            get => _selectedAppType;
            set
            {
                _selectedAppType = value;
                OnPropertyChanged();
                ((Command)GenerateCommand).ChangeCanExecute();
            }
        }

        public bool IsGenerating
        {
            get => _isGenerating;
            set
            {
                _isGenerating = value;
                OnPropertyChanged();
                ((Command)GenerateCommand).ChangeCanExecute();
            }
        }

        public bool ShowResults
        {
            get => _showResults;
            set
            {
                _showResults = value;
                OnPropertyChanged();
            }
        }

        public double GenerationProgress
        {
            get => _generationProgress;
            set
            {
                _generationProgress = value;
                OnPropertyChanged();
            }
        }

        public string CurrentStep
        {
            get => _currentStep;
            set
            {
                _currentStep = value;
                OnPropertyChanged();
            }
        }

        public string GeneratedAppName
        {
            get => _generatedAppName;
            set
            {
                _generatedAppName = value;
                OnPropertyChanged();
            }
        }

        public string GeneratedDescription
        {
            get => _generatedDescription;
            set
            {
                _generatedDescription = value;
                OnPropertyChanged();
            }
        }

        public bool DarkModeEnabled
        {
            get => _darkModeEnabled;
            set
            {
                _darkModeEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool AuthenticationEnabled
        {
            get => _authenticationEnabled;
            set
            {
                _authenticationEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool DatabaseEnabled
        {
            get => _databaseEnabled;
            set
            {
                _databaseEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool ResponsiveEnabled
        {
            get => _responsiveEnabled;
            set
            {
                _responsiveEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool RealTimeEnabled
        {
            get => _realTimeEnabled;
            set
            {
                _realTimeEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool PWAEnabled
        {
            get => _pwaEnabled;
            set
            {
                _pwaEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool CanGenerate => !string.IsNullOrWhiteSpace(UserPrompt) && SelectedAppType != null && !IsGenerating;

        public ObservableCollection<AppTypeInfo> AppTypes { get; } = new();
        public ObservableCollection<QuickExample> QuickExamples { get; } = new();

        public ICommand GenerateCommand { get; }
        public ICommand OpenProjectCommand { get; }
        public ICommand RunAppCommand { get; }
        public ICommand UseExampleCommand { get; }

        private void InitializeAppTypes()
        {
            AppTypes.Add(new AppTypeInfo
            {
                Name = "Web App",
                Description = "React, Vue, Angular",
                Icon = "🌐",
                Platform = "web"
            });

            AppTypes.Add(new AppTypeInfo
            {
                Name = "Mobile App",
                Description = "iOS, Android",
                Icon = "📱",
                Platform = "mobile"
            });

            AppTypes.Add(new AppTypeInfo
            {
                Name = "Desktop App",
                Description = "Windows, macOS, Linux",
                Icon = "🖥️",
                Platform = "desktop"
            });

            AppTypes.Add(new AppTypeInfo
            {
                Name = "API Server",
                Description = "REST, GraphQL",
                Icon = "🔌",
                Platform = "api"
            });

            AppTypes.Add(new AppTypeInfo
            {
                Name = "Game",
                Description = "Unity, Unreal",
                Icon = "🎮",
                Platform = "game"
            });

            AppTypes.Add(new AppTypeInfo
            {
                Name = "Console App",
                Description = "CLI, Terminal",
                Icon = "💻",
                Platform = "console"
            });
        }

        private void InitializeQuickExamples()
        {
            QuickExamples.Add(new QuickExample
            {
                Title = "Todo App",
                Description = "A modern todo app with dark mode, drag-and-drop reordering, and real-time sync",
                Platform = "web"
            });

            QuickExamples.Add(new QuickExample
            {
                Title = "Fitness Tracker",
                Description = "A mobile fitness tracker with workout plans, progress charts, and social features",
                Platform = "mobile"
            });

            QuickExamples.Add(new QuickExample
            {
                Title = "E-commerce Store",
                Description = "A full-featured online store with product listings, shopping cart, and payment processing",
                Platform = "web"
            });

            QuickExamples.Add(new QuickExample
            {
                Title = "Chat API",
                Description = "A real-time chat API with user management, messaging, and presence features",
                Platform = "api"
            });

            QuickExamples.Add(new QuickExample
            {
                Title = "Photo Editor",
                Description = "A desktop photo editing application with filters, cropping, and batch processing",
                Platform = "desktop"
            });

            QuickExamples.Add(new QuickExample
            {
                Title = "2D Platformer",
                Description = "A simple 2D platformer game with character movement, jumping, and collectibles",
                Platform = "game"
            });
        }

        private async Task GenerateApplicationAsync()
        {
            if (!CanGenerate) return;

            IsGenerating = true;
            ShowResults = false;
            GenerationProgress = 0;

            try
            {
                // Step 1: Analyze description
                CurrentStep = "🔍 Analyzing your description...";
                GenerationProgress = 0.1;
                await Task.Delay(1000);

                // Step 2: Generate project structure
                CurrentStep = "🏗️ Generating project structure...";
                GenerationProgress = 0.3;
                await Task.Delay(1500);

                // Step 3: Install dependencies
                CurrentStep = "📦 Installing dependencies...";
                GenerationProgress = 0.5;
                await Task.Delay(2000);

                // Step 4: Generate code
                CurrentStep = "💻 Generating application code...";
                GenerationProgress = 0.7;
                await Task.Delay(2000);

                // Step 5: Create documentation
                CurrentStep = "📚 Creating documentation...";
                GenerationProgress = 0.9;
                await Task.Delay(1000);

                // Complete
                CurrentStep = "✅ Application generated successfully!";
                GenerationProgress = 1.0;
                await Task.Delay(500);

                // Set results
                GeneratedAppName = DetermineAppName(UserPrompt);
                GeneratedDescription = $"A {SelectedAppType.Name.ToLower()} built with modern technologies";
                ShowResults = true;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Generation failed: {ex.Message}", "OK");
            }
            finally
            {
                IsGenerating = false;
            }
        }

        private async Task OpenProjectAsync()
        {
            await Application.Current.MainPage.DisplayAlert("Open Project", 
                $"Opening project: {GeneratedAppName}\n\nLocation: ./{GeneratedAppName.ToLower().Replace(" ", "-")}", "OK");
        }

        private async Task RunAppAsync()
        {
            await Application.Current.MainPage.DisplayAlert("Run Application", 
                $"Starting {GeneratedAppName}...\n\nThis would launch your generated application!", "OK");
        }

        private async Task UseExampleAsync(QuickExample example)
        {
            UserPrompt = example.Description;
            
            // Find matching app type
            foreach (var appType in AppTypes)
            {
                if (appType.Platform == example.Platform)
                {
                    SelectedAppType = appType;
                    break;
                }
            }

            await Application.Current.MainPage.DisplayAlert("Example Loaded", 
                $"Loaded: {example.Title}\n\nYou can now customize the features and generate your app!", "OK");
        }

        private string DetermineAppName(string description)
        {
            var words = description.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 0)
            {
                var firstWord = words[0].ToLower();
                if (firstWord == "a" || firstWord == "an" || firstWord == "the")
                {
                    return words.Length > 1 ? words[1] : "My App";
                }
                return firstWord;
            }
            return "My App";
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AppTypeInfo
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Platform { get; set; } = "";
        public bool IsSelected { get; set; } = false;
    }

    public class QuickExample
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Platform { get; set; } = "";
    }
}
