using System.Text.Json;
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
    /// Mock model provider for testing and development.
    /// </summary>
    public class MockModelProvider : IModelProvider
    {
        private readonly ILogger<MockModelProvider> _logger;
        private readonly Dictionary<string, ModelInfo> _mockModels;

        public MockModelProvider(ILogger<MockModelProvider> logger)
        {
            _logger = logger;
            _mockModels = InitializeMockModels();
        }

        public string ProviderId => "mock";
        public string DisplayName => "Mock Provider";
        public string Description => "Mock AI model provider for testing and development";
        public bool IsAvailable => true;
        
        // IModelProvider interface properties
        public string Name => DisplayName;
        public string ProviderType => "Mock";
        public bool IsEnabled => IsAvailable;
        public bool IsPrimary => false;

        public IEnumerable<ModelType> SupportedModelTypes => new[]
        {
            ModelType.TextGeneration,
            ModelType.CodeGeneration,
            ModelType.TextEmbedding
        };

        public ModelCapabilities Capabilities => new ModelCapabilities
        {
            SupportsStreaming = true,
            SupportsFunctionCalling = true,
            SupportsTextEmbedding = true,
            MaxInputLength = 8192,
            MaxOutputLength = 8192,
            SupportedLanguages = new List<string> { "en", "es", "fr", "de" }
        };

        public async Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting available mock models");
            await Task.CompletedTask;
            return _mockModels.Values;
        }

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

        public async Task<ModelInfo> GetModelInfoAsync(string modelName, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting mock model info: {ModelName}", modelName);
            await Task.CompletedTask;
            
            ModelInfo modelInfo;
            if (_mockModels.TryGetValue(modelName, out modelInfo))
                return modelInfo;
            return null;
        }

        public async Task<ModelValidationResult> ValidateModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Validating mock model: {ModelName}", modelName);
            await Task.CompletedTask;
            
            var result = new ModelValidationResult { IsValid = true };
            
            if (!_mockModels.ContainsKey(modelName))
            {
                result.IsValid = false;
                result.Errors.Add($"Mock model '{modelName}' not found");
            }
            
            return result;
        }

        public async Task<ModelHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting mock provider health status");
            await Task.CompletedTask;
            
            return new ModelHealthStatus
            {
                ProviderName = DisplayName,
                IsHealthy = true,
                ResponseTimeMs = 10,
                ErrorRate = 0.0,
                LastError = "",
                LastCheckTime = DateTime.UtcNow
            };
        }

        public async Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Executing mock model request");
            var startTime = DateTime.UtcNow;
            
            await Task.Delay(100, cancellationToken); // Simulate processing time
            
            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            
            return new ModelResponse
            {
                Content = $"Mock response to: {request.Input}",
                Model = "mock-model",
                TotalTokens = request.Input.Length / 4, // Rough token estimation
                ProcessingTimeMs = (long)executionTime,
                Metadata = new Dictionary<string, object>
                {
                    ["provider"] = ProviderId,
                    ["mock"] = true
                }
            };
        }

        public async Task<IEnumerable<ModelResponseChunk>> ExecuteStreamAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Executing mock model stream request");
            
            return GenerateMockStream(request, cancellationToken);
        }

        public async Task<ModelValidationResult> ValidateRequestAsync(ModelRequest request)
        {
            _logger.LogInformation("Validating mock model request");
            await Task.CompletedTask;
            
            var result = new ModelValidationResult { IsValid = true };
            
            if (string.IsNullOrEmpty(request.Input))
            {
                result.IsValid = false;
                result.Errors.Add("Input is required");
            }
            
            if (request.Input?.Length > 10000)
            {
                result.IsValid = false;
                result.Errors.Add("Input too long (max 10000 characters)");
            }
            
            return result;
        }

        private Dictionary<string, ModelInfo> InitializeMockModels()
        {
            return new Dictionary<string, ModelInfo>
            {
                ["mock-text"] = new ModelInfo
                {
                    Id = "mock-text",
                    Name = "Mock Text Model",
                    Description = "Mock model for text generation",
                    Version = "1.0",
                    Type = ModelType.TextGeneration,
                    Provider = ProviderId,
                    IsAvailable = true,
                    LastUpdated = DateTime.UtcNow,
                    Capabilities = new ModelCapabilities
                    {
                        SupportsStreaming = true,
                        SupportsFunctionCalling = false,
                        SupportsTextEmbedding = false,
                        MaxInputLength = 4096,
                        MaxOutputLength = 4096,
                        SupportedLanguages = new List<string> { "en" }
                    }
                },
                ["mock-code"] = new ModelInfo
                {
                    Id = "mock-code",
                    Name = "Mock Code Model",
                    Description = "Mock model for code generation",
                    Version = "1.0",
                    Type = ModelType.CodeGeneration,
                    Provider = ProviderId,
                    IsAvailable = true,
                    LastUpdated = DateTime.UtcNow,
                    Capabilities = new ModelCapabilities
                    {
                        SupportsStreaming = true,
                        SupportsFunctionCalling = true,
                        SupportsTextEmbedding = false,
                        MaxInputLength = 8192,
                        MaxOutputLength = 8192,
                        SupportedLanguages = new List<string> { "csharp", "javascript", "python" }
                    }
                }
            };
        }

        private IEnumerable<ModelResponseChunk> GenerateMockStream(ModelRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var words = (request.Input != null) ? request.Input.Split(' ') : new string[0];
            var response = "Mock streaming response to: " + request.Input;
            var chunks = response.Split(' ');
            
            for (int i = 0; i < chunks.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                yield return new ModelResponseChunk
                {
                    Content = chunks[i] + (i < chunks.Length - 1 ? " " : ""),
                    IsFinal = i == chunks.Length - 1,
                    Index = i,
                    FinishReason = i == chunks.Length - 1 ? "stop" : null,
                    Metadata = new Dictionary<string, object>
                    {
                        ["provider"] = ProviderId,
                        ["mock"] = true
                    }
                };
                
                Task.Delay(50, cancellationToken).Wait(cancellationToken); // Simulate streaming delay
            }
        }
    }

    /// <summary>
    /// Mock model implementation.
    /// </summary>
    public class MockModel : IModel
    {
        private readonly string _modelName;
        private readonly ModelInfo _modelInfo;
        private readonly ILogger _logger;

        public MockModel(string modelName, ModelInfo modelInfo, ILogger logger)
        {
            _modelName = modelName;
            _modelInfo = modelInfo;
            _logger = logger;
        }

        public ModelInfo Info => _modelInfo;
        public bool IsLoaded => true;

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
                Content = content,
                Model = _modelName,
                TotalTokens = request.Input?.Length / 4 ?? 0,
                ProcessingTimeMs = (long)executionTime,
                Metadata = new Dictionary<string, object>
                {
                    ["mock"] = true,
                    ["model_type"] = _modelInfo.Type.ToString()
                }
            };
        }

        public IEnumerable<ModelResponseChunk> ProcessStreamAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing mock model stream request: {ModelName}", _modelName);
            
            return GenerateMockStream(request, cancellationToken);
        }

        public ModelCapabilities GetCapabilities()
        {
            return _modelInfo.Capabilities;
        }

        public Task UnloadAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Unloading mock model: {ModelName}", _modelName);
            return Task.CompletedTask;
        }

        private IEnumerable<ModelResponseChunk> GenerateMockStream(ModelRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var response = $"Mock {_modelName} streaming response to: {request.Input}";
            var chunks = response.Split(' ').ToArray();
            
            for (int i = 0; i < chunks.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                yield return new ModelResponseChunk
                {
                    Content = chunks[i] + (i < chunks.Length - 1 ? " " : ""),
                    IsFinal = i == chunks.Length - 1,
                    Index = i,
                    FinishReason = i == chunks.Length - 1 ? "stop" : null,
                    Metadata = new Dictionary<string, object>
                    {
                        ["mock"] = true,
                        ["model_name"] = _modelName
                    }
                };
                
                Task.Delay(50, cancellationToken).Wait(cancellationToken);
            }
        }

        private string GenerateTaskSpecificResponse(ModelRequest request)
        {
            var input = request.Input?.ToLowerInvariant() ?? "";
            
            // Template generation - check for the exact prompt pattern
            if (input.Contains("generate a comprehensive template based on the following description:"))
            {
                return @"using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route(""api/[controller]"")]
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
                _logger.LogError(ex, ""Error retrieving users"");
                return StatusCode(500, ""Internal server error"");
            }
        }

        [HttpGet(""{id}"")]
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
                _logger.LogError(ex, ""Error retrieving user with id {UserId}"", id);
                return StatusCode(500, ""Internal server error"");
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
                _logger.LogError(ex, ""Error creating user"");
                return StatusCode(500, ""Internal server error"");
            }
        }

        [HttpPut(""{id}"")]
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
                _logger.LogError(ex, ""Error updating user with id {UserId}"", id);
                return StatusCode(500, ""Internal server error"");
            }
        }

        [HttpDelete(""{id}"")]
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
                _logger.LogError(ex, ""Error deleting user with id {UserId}"", id);
                return StatusCode(500, ""Internal server error"");
            }
        }
    }
}";
            }
            
            // Project structure generation
            if (input.Contains("project structure") || input.Contains("webapi") || input.Contains("project type"))
            {
                return @"# WebAPI Project Structure

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
  - Integration/";
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
                return @"[Fact]
public async Task TestMethod_ShouldReturnExpectedResult()
{
    // Arrange
    var service = new TestService();
    var expected = ""expected result"";

    // Act
    var result = await service.DoSomethingAsync();

    // Assert
    Assert.Equal(expected, result);
}

[Theory]
[InlineData(""test1"")]
[InlineData(""test2"")]
public void TestMethod_WithDifferentInputs_ShouldHandleCorrectly(string input)
{
    // Arrange
    var service = new TestService();

    // Act
    var result = service.Process(input);

    // Assert
    Assert.NotNull(result);
}";
            }
            
            // Default response for unrecognized tasks
            return $@"Mock mock-text response to: {request.Input}";
        }
    }
} 