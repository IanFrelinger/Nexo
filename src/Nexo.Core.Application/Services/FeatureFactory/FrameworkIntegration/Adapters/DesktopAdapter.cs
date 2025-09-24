using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Domain.Entities.FeatureFactory;
using Nexo.Core.Domain.Entities.FeatureFactory.ApplicationLogic;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.FrameworkIntegration.Adapters;

/// <summary>
/// Adapter for desktop framework code generation (Console, WPF, WinForms)
/// </summary>
public class DesktopAdapter
{
    private readonly ILogger<DesktopAdapter> _logger;
    private readonly IAIRuntimeSelector _runtimeSelector;

    public DesktopAdapter(ILogger<DesktopAdapter> logger, IAIRuntimeSelector runtimeSelector)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtimeSelector = runtimeSelector ?? throw new ArgumentNullException(nameof(runtimeSelector));
    }

    public async Task<ConsoleResult> GenerateConsoleCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating Console code for application logic");

            var result = new ConsoleResult
            {
                Success = true,
                GeneratedAt = DateTime.UtcNow
            };

            // Generate commands
            var commands = new List<ConsoleCommand>();
            foreach (var controller in applicationLogic.Controllers)
            {
                var command = await GenerateConsoleCommandsAsync(controller, cancellationToken);
                if (command != null)
                {
                    commands.Add(command);
                }
            }
            result.Commands = commands;

            // Generate services
            var services = new List<ConsoleService>();
            foreach (var service in applicationLogic.Services)
            {
                var consoleService = await GenerateConsoleServiceAsync(service, cancellationToken);
                if (consoleService != null)
                {
                    services.Add(consoleService);
                }
            }
            result.Services = services;

            // Generate configuration
            result.Configuration = await GenerateConsoleConfigurationAsync(applicationLogic, cancellationToken);

            result.Success = true;
            result.Message = "Console code generation completed successfully";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Console code");
            return new ConsoleResult
            {
                Success = false,
                Message = $"Console code generation failed: {ex.Message}",
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<WpfResult> GenerateWpfCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating WPF code for application logic");

            var result = new WpfResult
            {
                Success = true,
                GeneratedAt = DateTime.UtcNow
            };

            // Generate windows
            var windows = new List<WpfWindow>();
            foreach (var controller in applicationLogic.Controllers)
            {
                var window = await GenerateWpfWindowsAsync(controller, cancellationToken);
                if (window != null)
                {
                    windows.Add(window);
                }
            }
            result.Windows = windows;

            // Generate user controls
            var userControls = new List<WpfUserControl>();
            foreach (var model in applicationLogic.Models)
            {
                var userControl = await GenerateWpfUserControlsAsync(model, cancellationToken);
                if (userControl != null)
                {
                    userControls.Add(userControl);
                }
            }
            result.UserControls = userControls;

            // Generate view models
            var viewModels = new List<WpfViewModel>();
            foreach (var service in applicationLogic.Services)
            {
                var viewModel = await GenerateWpfViewModelsAsync(service, cancellationToken);
                if (viewModel != null)
                {
                    viewModels.Add(viewModel);
                }
            }
            result.ViewModels = viewModels;

            // Generate configuration
            result.Configuration = await GenerateWpfConfigurationAsync(applicationLogic, cancellationToken);

            result.Success = true;
            result.Message = "WPF code generation completed successfully";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating WPF code");
            return new WpfResult
            {
                Success = false,
                Message = $"WPF code generation failed: {ex.Message}",
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<WinFormsResult> GenerateWinFormsCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating WinForms code for application logic");

            var result = new WinFormsResult
            {
                Success = true,
                GeneratedAt = DateTime.UtcNow
            };

            // Generate forms
            var forms = new List<WinFormsForm>();
            foreach (var controller in applicationLogic.Controllers)
            {
                var form = await GenerateWinFormsFormsAsync(controller, cancellationToken);
                if (form != null)
                {
                    forms.Add(form);
                }
            }
            result.Forms = forms;

            // Generate controls
            var controls = new List<WinFormsControl>();
            foreach (var model in applicationLogic.Models)
            {
                var control = await GenerateWinFormsControlsAsync(model, cancellationToken);
                if (control != null)
                {
                    controls.Add(control);
                }
            }
            result.Controls = controls;

            // Generate services
            var services = new List<WinFormsService>();
            foreach (var service in applicationLogic.Services)
            {
                var winFormsService = await GenerateWinFormsServiceAsync(service, cancellationToken);
                if (winFormsService != null)
                {
                    services.Add(winFormsService);
                }
            }
            result.Services = services;

            // Generate configuration
            result.Configuration = await GenerateWinFormsConfigurationAsync(applicationLogic, cancellationToken);

            result.Success = true;
            result.Message = "WinForms code generation completed successfully";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating WinForms code");
            return new WinFormsResult
            {
                Success = false,
                Message = $"WinForms code generation failed: {ex.Message}",
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<ConsoleCommand> GenerateConsoleCommandsAsync(ApplicationController controller, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating Console command: {ControllerName}", controller.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Desktop,
                Framework = FrameworkType.Console,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a Console command for the following application controller:
Name: {controller.Name}
Description: {controller.Description}
Actions: {string.Join(", ", controller.Actions.Select(a => a.Name))}

Generate:
1. Console command class
2. Command line argument parsing
3. Help text
4. Error handling
5. Logging
6. Documentation comments

Ensure the command follows Console application best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for Console command: {ControllerName}", controller.Name);
                return null;
            }

            return new ConsoleCommand
            {
                Name = controller.Name,
                Content = response,
                Actions = controller.Actions.Select(a => a.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Console command: {ControllerName}", controller.Name);
            return null;
        }
    }

    private async Task<ConsoleService> GenerateConsoleServiceAsync(ApplicationService service, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating Console service: {ServiceName}", service.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Desktop,
                Framework = FrameworkType.Console,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a Console service for the following application service:
Name: {service.Name}
Description: {service.Description}
Methods: {string.Join(", ", service.Methods.Select(m => m.Name))}

Generate:
1. Service interface
2. Service implementation
3. Dependency injection setup
4. Business logic methods
5. Error handling
6. Logging
7. Documentation comments

Ensure the service follows Console application best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for Console service: {ServiceName}", service.Name);
                return null;
            }

            return new ConsoleService
            {
                Name = service.Name,
                Content = response,
                Methods = service.Methods.Select(m => m.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Console service: {ServiceName}", service.Name);
            return null;
        }
    }

    private async Task<WpfWindow> GenerateWpfWindowsAsync(ApplicationController controller, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating WPF window: {ControllerName}", controller.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Desktop,
                Framework = FrameworkType.Wpf,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a WPF window for the following application controller:
Name: {controller.Name}
Description: {controller.Description}
Actions: {string.Join(", ", controller.Actions.Select(a => a.Name))}

Generate:
1. WPF window class
2. XAML markup
3. Code-behind logic
4. Event handling
5. Data binding
6. Styling
7. Documentation comments

Ensure the window follows WPF best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for WPF window: {ControllerName}", controller.Name);
                return null;
            }

            return new WpfWindow
            {
                Name = controller.Name,
                Content = response,
                Actions = controller.Actions.Select(a => a.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating WPF window: {ControllerName}", controller.Name);
            return null;
        }
    }

    private async Task<WpfUserControl> GenerateWpfUserControlsAsync(ApplicationModel model, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating WPF user control: {ModelName}", model.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Desktop,
                Framework = FrameworkType.Wpf,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a WPF user control for the following application model:
Name: {model.Name}
Description: {model.Description}
Properties: {string.Join(", ", model.Properties.Select(p => $"{p.Name}: {p.Type}"))}

Generate:
1. WPF user control class
2. XAML markup
3. Code-behind logic
4. Data binding
5. Styling
6. Documentation comments

Ensure the user control follows WPF best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for WPF user control: {ModelName}", model.Name);
                return null;
            }

            return new WpfUserControl
            {
                Name = model.Name,
                Content = response,
                Properties = model.Properties.Select(p => p.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating WPF user control: {ModelName}", model.Name);
            return null;
        }
    }

    private async Task<WpfViewModel> GenerateWpfViewModelsAsync(ApplicationService service, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating WPF view model: {ServiceName}", service.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Desktop,
                Framework = FrameworkType.Wpf,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a WPF view model for the following application service:
Name: {service.Name}
Description: {service.Description}
Methods: {string.Join(", ", service.Methods.Select(m => m.Name))}

Generate:
1. View model class
2. Properties with INotifyPropertyChanged
3. Commands with ICommand
4. Business logic methods
5. Error handling
6. Logging
7. Documentation comments

Ensure the view model follows WPF MVVM best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for WPF view model: {ServiceName}", service.Name);
                return null;
            }

            return new WpfViewModel
            {
                Name = service.Name,
                Content = response,
                Methods = service.Methods.Select(m => m.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating WPF view model: {ServiceName}", service.Name);
            return null;
        }
    }

    private async Task<WinFormsForm> GenerateWinFormsFormsAsync(ApplicationController controller, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating WinForms form: {ControllerName}", controller.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Desktop,
                Framework = FrameworkType.WinForms,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a WinForms form for the following application controller:
Name: {controller.Name}
Description: {controller.Description}
Actions: {string.Join(", ", controller.Actions.Select(a => a.Name))}

Generate:
1. WinForms form class
2. Designer code
3. Event handlers
4. Control initialization
5. Layout management
6. Documentation comments

Ensure the form follows WinForms best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for WinForms form: {ControllerName}", controller.Name);
                return null;
            }

            return new WinFormsForm
            {
                Name = controller.Name,
                Content = response,
                Actions = controller.Actions.Select(a => a.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating WinForms form: {ControllerName}", controller.Name);
            return null;
        }
    }

    private async Task<WinFormsControl> GenerateWinFormsControlsAsync(ApplicationModel model, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating WinForms control: {ModelName}", model.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Desktop,
                Framework = FrameworkType.WinForms,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a WinForms control for the following application model:
Name: {model.Name}
Description: {model.Description}
Properties: {string.Join(", ", model.Properties.Select(p => $"{p.Name}: {p.Type}"))}

Generate:
1. WinForms control class
2. Designer code
3. Event handlers
4. Control initialization
5. Layout management
6. Documentation comments

Ensure the control follows WinForms best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for WinForms control: {ModelName}", model.Name);
                return null;
            }

            return new WinFormsControl
            {
                Name = model.Name,
                Content = response,
                Properties = model.Properties.Select(p => p.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating WinForms control: {ModelName}", model.Name);
            return null;
        }
    }

    private async Task<WinFormsService> GenerateWinFormsServiceAsync(ApplicationService service, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating WinForms service: {ServiceName}", service.Name);

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Desktop,
                Framework = FrameworkType.WinForms,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate a WinForms service for the following application service:
Name: {service.Name}
Description: {service.Description}
Methods: {string.Join(", ", service.Methods.Select(m => m.Name))}

Generate:
1. Service interface
2. Service implementation
3. Dependency injection setup
4. Business logic methods
5. Error handling
6. Logging
7. Documentation comments

Ensure the service follows WinForms best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for WinForms service: {ServiceName}", service.Name);
                return null;
            }

            return new WinFormsService
            {
                Name = service.Name,
                Content = response,
                Methods = service.Methods.Select(m => m.Name).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating WinForms service: {ServiceName}", service.Name);
            return null;
        }
    }

    private async Task<ConsoleConfiguration> GenerateConsoleConfigurationAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating Console configuration");

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Desktop,
                Framework = FrameworkType.Console,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate Console configuration for the following application:
Name: {applicationLogic.ApplicationName}
Controllers: {string.Join(", ", applicationLogic.Controllers.Select(c => c.Name))}
Services: {string.Join(", ", applicationLogic.Services.Select(s => s.Name))}

Generate:
1. Startup configuration
2. Dependency injection setup
3. Logging configuration
4. Error handling
5. Console-specific configurations

Ensure the configuration follows Console application best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for Console configuration");
                return null;
            }

            return new ConsoleConfiguration
            {
                Content = response,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Console configuration");
            return null;
        }
    }

    private async Task<WpfConfiguration> GenerateWpfConfigurationAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating WPF configuration");

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Desktop,
                Framework = FrameworkType.Wpf,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate WPF configuration for the following application:
Name: {applicationLogic.ApplicationName}
Controllers: {string.Join(", ", applicationLogic.Controllers.Select(c => c.Name))}
Models: {string.Join(", ", applicationLogic.Models.Select(m => m.Name))}
Services: {string.Join(", ", applicationLogic.Services.Select(s => s.Name))}

Generate:
1. Startup configuration
2. Dependency injection setup
3. Logging configuration
4. Error handling
5. WPF-specific configurations

Ensure the configuration follows WPF best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for WPF configuration");
                return null;
            }

            return new WpfConfiguration
            {
                Content = response,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating WPF configuration");
            return null;
        }
    }

    private async Task<WinFormsConfiguration> GenerateWinFormsConfigurationAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating WinForms configuration");

            var aiContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Desktop,
                Framework = FrameworkType.WinForms,
                GeneratedAt = DateTime.UtcNow
            };

            var prompt = $@"
Generate WinForms configuration for the following application:
Name: {applicationLogic.ApplicationName}
Controllers: {string.Join(", ", applicationLogic.Controllers.Select(c => c.Name))}
Models: {string.Join(", ", applicationLogic.Models.Select(m => m.Name))}
Services: {string.Join(", ", applicationLogic.Services.Select(s => s.Name))}

Generate:
1. Startup configuration
2. Dependency injection setup
3. Logging configuration
4. Error handling
5. WinForms-specific configurations

Ensure the configuration follows WinForms best practices and conventions.";

            var response = await _runtimeSelector.GenerateResponseAsync(prompt, aiContext, cancellationToken);
            
            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("No response generated for WinForms configuration");
                return null;
            }

            return new WinFormsConfiguration
            {
                Content = response,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating WinForms configuration");
            return null;
        }
    }
}
