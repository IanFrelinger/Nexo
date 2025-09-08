using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Nexo.Infrastructure.Services.AI
{
    /// <summary>
    /// Provides a mock implementation of the IModelProvider interface, intended for use in testing and development scenarios.
    /// </summary>
    public class MockModelProvider : IModelProvider
    {
        /// <summary>
        /// Logger instance for capturing and logging operational details, errors, and debug information within the <see cref="MockModelProvider"/> class.
        /// </summary>
        /// <remarks>
        /// Used to record information such as method operations, error handling, and process updates
        /// for better traceability and debugging.
        /// </remarks>
        private readonly ILogger<MockModelProvider> _logger;

        /// <summary>
        /// Holds a dictionary of mock models identified by their names.
        /// This dictionary is used to simulate the availability and metadata of AI models
        /// for testing and development purposes in the mock provider implementation.
        /// </summary>
        private readonly Dictionary<string, ModelInfo> _mockModels;

        /// <summary>
        /// Represents a mock implementation of the IModelProvider interface.
        /// Provides mock AI model handling for development and testing purposes.
        /// </summary>
        public MockModelProvider(ILogger<MockModelProvider> logger)
        {
            _logger = logger;
            _mockModels = InitializeMockModels();
        }

        /// <summary>
        /// Gets the unique identifier of the provider.
        /// </summary>
        /// <remarks>
        /// This property represents a string identifier used to uniquely distinguish the specific model provider.
        /// This identifier is used for logging, metadata, and other purposes where provider distinction is needed.
        /// </remarks>
        public string ProviderId => "mock";

        /// <summary>
        /// Gets the display name of the model provider.
        /// This is a user-friendly string that identifies the provider
        /// and can be used for logging, UI representation, or debugging purposes.
        /// </summary>
        public string DisplayName => "Mock Provider";

        /// <summary>
        /// Gets a brief description of the mock model provider,
        /// indicating its usage for testing and development purposes.
        /// </summary>
        public string Description => "Mock AI model provider for testing and development";

        /// <summary>
        /// Indicates whether the provider is currently available for use.
        /// </summary>
        public bool IsAvailable => true;
        
        // IModelProvider interface properties
        /// <summary>
        /// Gets the name of the model provider, derived from the provider's display name.
        /// </summary>
        /// <remarks>
        /// This property is intended to provide the name of the AI model provider
        /// dynamically, based on the specific implementation. In this case, it
        /// corresponds to the value of the <c>DisplayName</c> property in the <c>MockModelProvider</c>.
        /// </remarks>
        public string Name => DisplayName;

        /// <summary>
        /// Gets the type of the provider represented as a string.
        /// This property is used to distinguish the specific type of
        /// model provider implementation. In the mock implementation,
        /// it returns a hardcoded value corresponding to "Mock".
        /// </summary>
        public string ProviderType => "Mock";

        /// <summary>
        /// Indicates whether the MockModelProvider is currently enabled.
        /// </summary>
        /// <remarks>
        /// This property evaluates to true if the provider is available (`IsAvailable` is true).
        /// Used to determine if the provider can be utilized for operations.
        /// </remarks>
        public bool IsEnabled => IsAvailable;

        /// <summary>
        /// Indicates whether the model provider is designated as the primary provider.
        /// </summary>
        /// <remarks>
        /// This property returns a boolean value indicating the primary status of the provider.
        /// It is commonly used when selecting or prioritizing providers within systems
        /// that support multiple model providers.
        /// </remarks>
        public bool IsPrimary => false;

        /// <summary>
        /// Gets the set of model types that are supported by the current model provider.
        /// </summary>
        /// <remarks>
        /// The property provides a collection of <see cref="ModelType"/> values that determine
        /// the capabilities of the model provider. These model types describe the types of
        /// AI tasks that the model provider can handle, such as text generation, code generation,
        /// and text embedding.
        /// </remarks>
        public IEnumerable<ModelType> SupportedModelTypes =>
        [
            ModelType.TextGeneration,
            ModelType.CodeGeneration,
            ModelType.TextEmbedding
        ];

        /// <summary>
        /// Represents the capabilities of the AI model within the MockModelProvider.
        /// </summary>
        /// <remarks>
        /// This property provides details about the supported functionalities and limitations
        /// of the AI model, such as streaming capabilities, function calling support,
        /// text embedding support, input and output length limits, and supported languages.
        /// </remarks>
        public ModelCapabilities Capabilities
        {
            get
            {
                var capabilities = new ModelCapabilities
                {
                    SupportsTextGeneration = true,
                    SupportsCodeGeneration = true,
                    SupportsAnalysis = true,
                    SupportsOptimization = false,
                    SupportsStreaming = true
                };
                return capabilities;
            }
        }

        /// <summary>
        /// Retrieves a collection of available models from the mock model provider.
        /// This method is asynchronous and may be used to simulate the retrieval
        /// of model information for AI-related services.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests during the operation.
        /// If not provided, the default CancellationToken is used.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains
        /// an enumerable collection of <see cref="ModelInfo"/> objects representing the mock models.
        /// </returns>
        public async Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting available mock models");
            await Task.CompletedTask;
            return _mockModels.Values;
        }

        /// <summary>
        /// Asynchronously loads a mock model identified by the specified model name.
        /// </summary>
        /// <param name="modelName">The name of the model to load.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete. Optional.</param>
        /// <returns>An instance of <see cref="IModel"/> representing the loaded mock model.</returns>
        /// <exception cref="ArgumentException">Thrown when the specified model name is not found in the mock models.</exception>
        public async Task<IModel> LoadModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Loading mock model: {ModelName}", modelName);
            await Task.CompletedTask;
            
            if (_mockModels.TryGetValue(modelName, out var modelInfo))
            {
                return new MockModel(modelName, modelInfo, _logger);
            }
            
            throw new ArgumentException($"Mock model '{modelName}' not found");
        }

        /// <summary>
        /// Retrieves the information of a specific model by its name asynchronously.
        /// </summary>
        /// <param name="modelName">The name of the model for which information is to be retrieved.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the <see cref="ModelInfo"/> for the specified model if found, or null if the model does not exist.</returns>
        public async Task<ModelInfo> GetModelInfoAsync(string modelName)
        {
            _logger.LogInformation("Getting mock model info: {ModelName}", modelName);
            await Task.CompletedTask;
            
            ModelInfo modelInfo;
            return _mockModels.TryGetValue(modelName, out modelInfo) ? modelInfo : new ModelInfo { Name = modelName, IsAvailable = false };
        }

        /// <summary>
        /// Validates the specified model and checks its existence within the mock model provider.
        /// </summary>
        /// <param name="modelName">The name of the model to validate.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A <see cref="ModelValidationResult"/> object containing validation results, including errors and warnings if the model is invalid.</returns>
        public async Task<ModelValidationResult> ValidateModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Validating mock model: {ModelName}", modelName);
            await Task.CompletedTask;
            
            if (_mockModels.ContainsKey(modelName))
            {
                return new ModelValidationResult { IsValid = true };
            }

            return new ModelValidationResult
            {
                IsValid = false,
                Errors = new List<string> { $"Mock model '{modelName}' not found" }
            };
        }

        /// <summary>
        /// Retrieves the health status of the mock model provider asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to propagate notification that operations should be canceled.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the health status of the mock model provider.</returns>
        public async Task<ModelHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting mock provider health status");
            await Task.CompletedTask;
            
            return new ModelHealthStatus
            {
                IsHealthy = true,
                Status = "Healthy",
                LastChecked = DateTime.UtcNow,
                ResponseTimeMs = 10,
                ErrorRate = 0.0
            };
        }

        /// <summary>
        /// Executes a mocked model request asynchronously and returns a model response.
        /// </summary>
        /// <param name="request">The model request containing input and required parameters for execution.</param>
        /// <param name="cancellationToken">An optional token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the model response generated from the mocked execution.</returns>
        public async Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Executing mock model request");
            var startTime = DateTime.UtcNow;
            
            await Task.Delay(100, cancellationToken); // Simulate processing time
            
            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            
            return new ModelResponse
            {
                Response = $"Mock response to: {request.Input}",
                InputTokens = request.Input.Length / 4,
                OutputTokens = request.Input.Length / 4,
                ProcessingTimeMs = (long)executionTime,
                Metadata = new Dictionary<string, object>
                {
                    ["provider"] = ProviderId,
                    ["mock"] = true
                }
            };
        }

        /// <summary>
        /// Executes the provided model request as a streaming operation, returning a stream of response chunks.
        /// </summary>
        /// <param name="request">The model request to be processed.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation, which contains a stream of <see cref="ModelResponseChunk"/> objects.</returns>
        public Task<IEnumerable<ModelResponseChunk>> ExecuteStreamAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Executing mock model stream request");
            
            return Task.FromResult(GenerateMockStream(request, cancellationToken));
        }

        /// <summary>
        /// Validates a model request asynchronously and returns the validation result.
        /// </summary>
        /// <param name="request">The model request to validate.</param>
        /// <returns>A <see cref="ModelValidationResult"/> indicating whether the request is valid and providing error messages if any.</returns>
        public async Task<ModelValidationResult> ValidateRequestAsync(ModelRequest request)
        {
            _logger.LogInformation("Validating mock model request");
            await Task.CompletedTask;
            
            var errors = new List<string>();
            
            if (string.IsNullOrEmpty(request.Input))
            {
                errors.Add("Input is required");
            }

            if (request.Input?.Length > 10000)
            {
                errors.Add("Input too long (max 10000 characters)");
            }

            var result = new ModelValidationResult 
            { 
                IsValid = errors.Count == 0,
                Errors = errors
            };

            return result;
        }

        /// <summary>
        /// Initializes and returns a dictionary of mock models with predefined configurations.
        /// Each mock model contains metadata and capabilities relevant to its type, such as
        /// supported languages, maximum input and output lengths, and streaming support.
        /// </summary>
        /// <returns>
        /// A dictionary where the key is the model identifier and the value is the model's detailed information.
        /// </returns>
        private Dictionary<string, ModelInfo> InitializeMockModels()
        {
            return new Dictionary<string, ModelInfo>
            {
                ["mock-text"] = new ModelInfo
                {
                    Name = "Mock Text Model",
                    DisplayName = "Mock Text Model",
                    ModelType = ModelType.TextGeneration,
                    IsAvailable = true,
                    SizeBytes = 1024 * 1024 * 1024,
                    MaxContextLength = 4096,
                    Capabilities = new ModelCapabilities
                    {
                        SupportsTextGeneration = true,
                        SupportsCodeGeneration = true,
                        SupportsAnalysis = true,
                        SupportsOptimization = false,
                        SupportsStreaming = true
                    }
                },
                ["mock-code"] = new ModelInfo
                {
                    Name = "Mock Code Model",
                    DisplayName = "Mock Code Model",
                    ModelType = ModelType.CodeGeneration,
                    IsAvailable = true,
                    SizeBytes = 1024 * 1024 * 1024,
                    MaxContextLength = 4096,
                    Capabilities = new ModelCapabilities
                    {
                        SupportsTextGeneration = true,
                        SupportsCodeGeneration = true,
                        SupportsAnalysis = true,
                        SupportsOptimization = false,
                        SupportsStreaming = true
                    }
                }
            };
        }

        /// <summary>
        /// Generates a mock stream of model response chunks based on the provided model request.
        /// </summary>
        /// <param name="request">The model request containing the input data.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An enumerable collection of <see cref="ModelResponseChunk"/> representing the mock streamed response.</returns>
        private IEnumerable<ModelResponseChunk> GenerateMockStream(ModelRequest request, CancellationToken cancellationToken)
        {
            var words = (request.Input != null) ? request.Input.Split(' ') : [];
            var response = "Mock streaming response to: " + request.Input;
            var chunks = response.Split(' ');
            
            for (var i = 0; i < chunks.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                yield return new ModelResponseChunk
                {
                    Content = chunks[i] + (i < chunks.Length - 1 ? " " : ""),
                    IsFinal = i == chunks.Length - 1,
                    Index = i
                };
                
                Task.Delay(50, cancellationToken).Wait(cancellationToken); // Simulate streaming delay
            }
        }
    }

    /// <summary>
    /// Represents a mock implementation of the IModel interface for testing purposes.
    /// </summary>
    public class MockModel : IModel
    {
        /// <summary>
        /// Represents the name of the model associated with the instance.
        /// </summary>
        /// <remarks>
        /// Typically used to identify the model being processed and logged in operations.
        /// </remarks>
        private readonly string _modelName;

        /// <summary>
        /// Represents the metadata and detailed information about the AI model being used
        /// by the MockModel class, such as its type, capabilities, size, and other attributes.
        /// </summary>
        private readonly ModelInfo _modelInfo;

        /// <summary>
        /// An instance of <see cref="ILogger"/> used for logging messages and events
        /// related to operations within the <see cref="MockModel"/> class.
        /// Facilitates structured and contextual logging for model processing,
        /// unloading, and other activities.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Represents a mock implementation of an AI model for testing or development purposes.
        /// </summary>
        public MockModel(string modelName, ModelInfo modelInfo, ILogger logger)
        {
            _modelName = modelName;
            _modelInfo = modelInfo;
            _logger = logger;
        }

        /// <summary>
        /// Provides information about the underlying model, including properties such as
        /// name, version, type, provider, size, and other metadata.
        /// </summary>
        public ModelInfo Info => _modelInfo;

        /// <summary>
        /// Gets a value indicating whether the model is currently loaded and ready to process requests.
        /// </summary>
        public bool IsLoaded => true;

        /// <summary>
        /// Unique identifier for this model instance
        /// </summary>
        public string ModelId => _modelName;

        /// <summary>
        /// Human-readable name of the model
        /// </summary>
        public string Name => _modelName;

        /// <summary>
        /// Type of model
        /// </summary>
        public Nexo.Feature.AI.Enums.ModelType ModelType => _modelInfo.ModelType;

        /// <summary>
        /// Processes a model request asynchronously and generates a response based on the input data.
        /// </summary>
        /// <param name="request">The model request containing input data to be processed.</param>
        /// <param name="cancellationToken">An optional token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="ModelResponse"/> with the processed response data.</returns>
        public async Task<ModelResponse> ProcessAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing mock model request: {ModelName}", _modelName);
            
            var startTime = DateTime.UtcNow;
            await Task.Delay(200, cancellationToken); // Simulate processing time
            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            
            // Generate task-specific responses based on the request content
            var content = GenerateTaskSpecificResponse(request);
            
            return new ModelResponse
            {
                Response = content,
                InputTokens = request.Input?.Length / 4 ?? 0,
                OutputTokens = request.Input?.Length / 4 ?? 0,
                ProcessingTimeMs = (long)executionTime,
                Metadata = new Dictionary<string, object>
                {
                    ["mock"] = true,
                    ["model_type"] = _modelInfo.ModelType.ToString()
                }
            };
        }

        /// <summary>
        /// Processes a model request and returns a stream of model response chunks.
        /// </summary>
        /// <param name="request">The request object containing the input data and other parameters for processing.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>An enumerable stream of <c>ModelResponseChunk</c> representing pieces of the model's response.</returns>
        public IEnumerable<ModelResponseChunk> ProcessStreamAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing mock model stream request: {ModelName}", _modelName);
            
            return GenerateMockStream(request, cancellationToken);
        }

        /// <summary>
        /// Retrieves the capabilities of the model.
        /// </summary>
        /// <returns>
        /// An object of type <see cref="ModelCapabilities"/> containing details about the functionality
        /// and limitations supported by the model.
        /// </returns>
        public ModelCapabilities GetCapabilities()
        {
            return _modelInfo.Capabilities;
        }

        /// <summary>
        /// Loads the model into memory asynchronously.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Loading mock model: {ModelName}", _modelName);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously unloads the model, releasing any resources held by it.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        public Task UnloadAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Unloading mock model: {ModelName}", _modelName);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Generates a mock streaming response based on the provided model request and returns
        /// a sequence of <see cref="ModelResponseChunk"/> objects.
        /// </summary>
        /// <param name="request">The model request containing input and configuration details.</param>
        /// <param name="cancellationToken">A token used to monitor for cancellation requests.</param>
        /// <returns>
        /// A sequence of <see cref="ModelResponseChunk"/> objects simulating a streaming response.
        /// </returns>
        private IEnumerable<ModelResponseChunk> GenerateMockStream(ModelRequest request, CancellationToken cancellationToken)
        {
            var response = $"Mock {_modelName} streaming response to: {request.Input}";
            var chunks = response.Split(' ').ToArray();
            
            for (var i = 0; i < chunks.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                yield return new ModelResponseChunk
                {
                    Content = chunks[i] + (i < chunks.Length - 1 ? " " : ""),
                    IsFinal = i == chunks.Length - 1,
                    Index = i
                };
                
                Task.Delay(50, cancellationToken).Wait(cancellationToken);
            }
        }

        /// <summary>
        /// Generates a task-specific response based on the input contained in the provided model request.
        /// </summary>
        /// <param name="request">The request containing input data based on which the response will be generated.</param>
        /// <returns>A string containing the task-specific response tailored to the input in the request.</returns>
        private string GenerateTaskSpecificResponse(ModelRequest request)
        {
            var input = request.Input?.ToLowerInvariant() ?? "";
            
            // Template generation - check for the exact prompt pattern
            if (input.Contains("generate a comprehensive template based on the following description:"))
            {
                return """
                       using Microsoft.AspNetCore.Mvc;
                       using System.Threading.Tasks;
                       using System.Collections.Generic;

                       namespace WebAPI.Controllers
                       {
                           [ApiController]
                           [Route("api/[controller]")]
                           public class UserController : ControllerBase
                           {
                               private readonly IUserService _userService;
                               private readonly ILogger<UserController> _logger;

                               public UserController(IUserService userService, ILogger<UserController> logger)
                               {
                                   _userService = userService;
                                   _logger = logger;
                               }

                               [HttpGet]
                               public async Task<ActionResult<IEnumerable<User>>> GetUsers()
                               {
                                   try
                                   {
                                       var users = await _userService.GetAllUsersAsync();
                                       return Ok(users);
                                   }
                                   catch (Exception ex)
                                   {
                                       _logger.LogError(ex, "Error retrieving users");
                                       return StatusCode(500, "Internal server error");
                                   }
                               }

                               [HttpGet("{id}")]
                               public async Task<ActionResult<User>> GetUser(int id)
                               {
                                   try
                                   {
                                       var user = await _userService.GetUserByIdAsync(id);
                                       if (user == null)
                                           return NotFound();

                                       return Ok(user);
                                   }
                                   catch (Exception ex)
                                   {
                                       _logger.LogError(ex, "Error retrieving user with id {UserId}", id);
                                       return StatusCode(500, "Internal server error");
                                   }
                               }

                               [HttpPost]
                               public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserRequest request)
                               {
                                   try
                                   {
                                       if (!ModelState.IsValid)
                                           return BadRequest(ModelState);

                                       var user = await _userService.CreateUserAsync(request);
                                       return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
                                   }
                                   catch (Exception ex)
                                   {
                                       _logger.LogError(ex, "Error creating user");
                                       return StatusCode(500, "Internal server error");
                                   }
                               }

                               [HttpPut("{id}")]
                               public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
                               {
                                   try
                                   {
                                       if (!ModelState.IsValid)
                                           return BadRequest(ModelState);

                                       var success = await _userService.UpdateUserAsync(id, request);
                                       if (!success)
                                           return NotFound();

                                       return NoContent();
                                   }
                                   catch (Exception ex)
                                   {
                                       _logger.LogError(ex, "Error updating user with id {UserId}", id);
                                       return StatusCode(500, "Internal server error");
                                   }
                               }

                               [HttpDelete("{id}")]
                               public async Task<IActionResult> DeleteUser(int id)
                               {
                                   try
                                   {
                                       var success = await _userService.DeleteUserAsync(id);
                                       if (!success)
                                           return NotFound();

                                       return NoContent();
                                   }
                                   catch (Exception ex)
                                   {
                                       _logger.LogError(ex, "Error deleting user with id {UserId}", id);
                                       return StatusCode(500, "Internal server error");
                                   }
                               }
                           }
                       }
                       """;
            }
            
            // Project structure generation
            if (input.Contains("project structure") || input.Contains("webapi") || input.Contains("project type"))
            {
                return """
                       # WebAPI Project Structure

                       ## Controllers
                       - Controllers/
                         - UserController.cs
                         - ProductController.cs
                         - OrderController.cs

                       ## Models
                       - Models/
                         - User.cs
                         - Product.cs
                         - Order.cs
                         - DTOs/
                           - UserDto.cs
                           - ProductDto.cs

                       ## Services
                       - Services/
                         - IUserService.cs
                         - UserService.cs
                         - IProductService.cs
                         - ProductService.cs

                       ## Data
                       - Data/
                         - ApplicationDbContext.cs
                         - Migrations/

                       ## Configuration
                       - appsettings.json
                       - Program.cs
                       - Startup.cs

                       ## Tests
                       - Tests/
                         - Controllers/
                         - Services/
                         - Integration/
                       """;
            }
            
            // Analysis suggestions
            if (input.Contains("analyze") || input.Contains("suggestion") || input.Contains("improve") || input.Contains("review") || input.Contains("console.writeline"))
            {
                return @"Here are some suggestions for improving your code:

1. **Add proper logging**: Consider using ILogger<T> for structured logging instead of Console.WriteLine
2. **Implement error handling**: Add try-catch blocks around critical operations
3. **Follow naming conventions**: Use PascalCase for public methods and properties
4. **Add XML documentation**: Document public APIs with XML comments
5. **Consider async/await**: Use asynchronous programming patterns where appropriate
6. **Implement validation**: Add input validation and error checking
7. **Use dependency injection**: Follow SOLID principles and use DI containers
8. **Add unit tests**: Ensure code coverage with comprehensive testing
9. **Optimize performance**: Consider caching and efficient algorithms
10. **Security best practices**: Implement proper authentication and authorization";
            }
            
            // Test generation
            if (input.Contains("generate") && input.Contains("test") || input.Contains("unit test") || input.Contains("test method"))
            {
                return """
                       [Fact]
                       public async Task TestMethod_ShouldReturnExpectedResult()
                       {
                           // Arrange
                           var service = new TestService();
                           var expected = "expected result";

                           // Act
                           var result = await service.DoSomethingAsync();

                           // Assert
                           Assert.Equal(expected, result);
                       }

                       [Theory]
                       [InlineData("test1")]
                       [InlineData("test2")]
                       public void TestMethod_WithDifferentInputs_ShouldHandleCorrectly(string input)
                       {
                           // Arrange
                           var service = new TestService();

                           // Act
                           var result = service.Process(input);

                           // Assert
                           Assert.NotNull(result);
                       }
                       """;
            }
            
            // Default response for unrecognized tasks
            return $"Mock mock-text response to: {request.Input}";
        }
    }
} 