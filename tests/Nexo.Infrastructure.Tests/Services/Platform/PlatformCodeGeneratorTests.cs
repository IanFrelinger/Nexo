// This file has been refactored into specialized test classes:
// - Success/SuccessCaseTests.cs - Successful scenarios for all generators
// - ErrorHandling/ErrorHandlingTests.cs - Error handling scenarios
// - Cancellation/CancellationTests.cs - Cancellation behavior

// Re-export for backward compatibility
using Nexo.Infrastructure.Tests.Services.Platform.Success;
using Nexo.Infrastructure.Tests.Services.Platform.ErrorHandling;
using Nexo.Infrastructure.Tests.Services.Platform.Cancellation;

namespace Nexo.Infrastructure.Tests.Services.Platform
{
    // Tests are organized into specialized classes under the namespaces above.
}

        [Fact]
        public async Task AndroidCodeGenerator_GenerateCodeAsync_ShouldReturnSuccessResult()
        {
            // Arrange
            var generator = new AndroidCodeGenerator(_mockAndroidLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new AndroidGenerationOptions
            {
                GenerateComposeUI = true,
                GenerateRoomDatabase = true,
                GenerateViewModels = true,
                GenerateRepositories = true,
                GenerateServices = true,
                GenerateTests = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated Android code" });

            // Act
            var result = await generator.GenerateCodeAsync(applicationLogic, options);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(applicationLogic.ApplicationName, result.ApplicationName);
            Assert.NotNull(result.ComposeUI);
            Assert.NotNull(result.RoomDatabase);
            Assert.NotNull(result.ViewModels);
            Assert.NotNull(result.Repositories);
            Assert.NotNull(result.Services);
            Assert.NotNull(result.Tests);
        }

        [Fact]
        public async Task WebCodeGenerator_GenerateCodeAsync_ShouldReturnSuccessResult()
        {
            // Arrange
            var generator = new WebCodeGenerator(_mockWebLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new WebGenerationOptions
            {
                Framework = "React",
                GenerateComponents = true,
                GenerateStateManagement = true,
                GenerateApiLayer = true,
                GenerateWebAssembly = true,
                GeneratePWA = true,
                GenerateTests = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated Web code" });

            // Act
            var result = await generator.GenerateCodeAsync(applicationLogic, options);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(applicationLogic.ApplicationName, result.ApplicationName);
            Assert.NotNull(result.Components);
            Assert.NotNull(result.StateManagement);
            Assert.NotNull(result.ApiLayer);
            Assert.NotNull(result.WebAssemblyModules);
            Assert.NotNull(result.PWAConfiguration);
            Assert.NotNull(result.Tests);
        }

        [Fact]
        public async Task DesktopCodeGenerator_GenerateCodeAsync_ShouldReturnSuccessResult()
        {
            // Arrange
            var generator = new DesktopCodeGenerator(_mockDesktopLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new DesktopGenerationOptions
            {
                Platform = "WPF",
                GenerateUIComponents = true,
                GenerateViewModels = true,
                GenerateServices = true,
                GenerateDataAccess = true,
                GenerateConfiguration = true,
                GenerateTests = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated Desktop code" });

            // Act
            var result = await generator.GenerateCodeAsync(applicationLogic, options);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(applicationLogic.ApplicationName, result.ApplicationName);
            Assert.NotNull(result.UIComponents);
            Assert.NotNull(result.ViewModels);
            Assert.NotNull(result.Services);
            Assert.NotNull(result.DataAccess);
            Assert.NotNull(result.Configuration);
            Assert.NotNull(result.Tests);
        }

        [Fact]
        public async Task iOSCodeGenerator_GenerateCodeAsync_ShouldHandleErrors()
        {
            // Arrange
            var generator = new iOSCodeGenerator(_mockIOSLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new iOSGenerationOptions();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("AI service error"));

            // Act
            var result = await generator.GenerateCodeAsync(applicationLogic, options);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("AI service error", result.ErrorMessage);
        }

        [Fact]
        public async Task AndroidCodeGenerator_GenerateCodeAsync_ShouldHandleErrors()
        {
            // Arrange
            var generator = new AndroidCodeGenerator(_mockAndroidLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new AndroidGenerationOptions();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("AI service error"));

            // Act
            var result = await generator.GenerateCodeAsync(applicationLogic, options);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("AI service error", result.ErrorMessage);
        }

        [Fact]
        public async Task WebCodeGenerator_GenerateCodeAsync_ShouldHandleErrors()
        {
            // Arrange
            var generator = new WebCodeGenerator(_mockWebLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new WebGenerationOptions();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("AI service error"));

            // Act
            var result = await generator.GenerateCodeAsync(applicationLogic, options);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("AI service error", result.ErrorMessage);
        }

        [Fact]
        public async Task DesktopCodeGenerator_GenerateCodeAsync_ShouldHandleErrors()
        {
            // Arrange
            var generator = new DesktopCodeGenerator(_mockDesktopLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new DesktopGenerationOptions();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("AI service error"));

            // Act
            var result = await generator.GenerateCodeAsync(applicationLogic, options);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("AI service error", result.ErrorMessage);
        }

        [Fact]
        public async Task iOSCodeGenerator_GenerateSwiftUIAsync_ShouldReturnScreens()
        {
            // Arrange
            var generator = new iOSCodeGenerator(_mockIOSLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new iOSGenerationOptions();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated SwiftUI screen" });

            // Act
            var result = await generator.GenerateSwiftUIAsync(applicationLogic, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.All(result, screen => Assert.True(screen.Success));
        }

        [Fact]
        public async Task AndroidCodeGenerator_GenerateJetpackComposeUIAsync_ShouldReturnScreens()
        {
            // Arrange
            var generator = new AndroidCodeGenerator(_mockAndroidLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new AndroidGenerationOptions();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated Compose screen" });

            // Act
            var result = await generator.GenerateJetpackComposeUIAsync(applicationLogic, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.All(result, screen => Assert.True(screen.Success));
        }

        [Fact]
        public async Task WebCodeGenerator_GenerateComponentsAsync_ShouldReturnComponents()
        {
            // Arrange
            var generator = new WebCodeGenerator(_mockWebLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new WebGenerationOptions { Framework = "React" };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated React component" });

            // Act
            var result = await generator.GenerateComponentsAsync(applicationLogic, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.All(result, component => Assert.True(component.Success));
        }

        [Fact]
        public async Task DesktopCodeGenerator_GenerateUIComponentsAsync_ShouldReturnComponents()
        {
            // Arrange
            var generator = new DesktopCodeGenerator(_mockDesktopLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new DesktopGenerationOptions { Platform = "WPF" };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated WPF component" });

            // Act
            var result = await generator.GenerateUIComponentsAsync(applicationLogic, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.All(result, component => Assert.True(component.Success));
        }

        [Fact]
        public async Task iOSCodeGenerator_GenerateCoreDataAsync_ShouldReturnDatabase()
        {
            // Arrange
            var generator = new iOSCodeGenerator(_mockIOSLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new iOSGenerationOptions();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated Core Data model" });

            // Act
            var result = await generator.GenerateCoreDataAsync(applicationLogic, options);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(applicationLogic.ApplicationName, result.ApplicationName);
        }

        [Fact]
        public async Task AndroidCodeGenerator_GenerateRoomDatabaseAsync_ShouldReturnDatabase()
        {
            // Arrange
            var generator = new AndroidCodeGenerator(_mockAndroidLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new AndroidGenerationOptions();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated Room database" });

            // Act
            var result = await generator.GenerateRoomDatabaseAsync(applicationLogic, options);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(applicationLogic.ApplicationName, result.ApplicationName);
        }

        [Fact]
        public async Task WebCodeGenerator_GenerateStateManagementAsync_ShouldReturnStateManagement()
        {
            // Arrange
            var generator = new WebCodeGenerator(_mockWebLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new WebGenerationOptions { Framework = "React" };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated state management" });

            // Act
            var result = await generator.GenerateStateManagementAsync(applicationLogic, options);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(applicationLogic.ApplicationName, result.ApplicationName);
        }

        [Fact]
        public async Task DesktopCodeGenerator_GenerateDataAccessAsync_ShouldReturnDataAccess()
        {
            // Arrange
            var generator = new DesktopCodeGenerator(_mockDesktopLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new DesktopGenerationOptions { Platform = "WPF" };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated data access" });

            // Act
            var result = await generator.GenerateDataAccessAsync(applicationLogic, options);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(applicationLogic.ApplicationName, result.ApplicationName);
        }

        [Fact]
        public async Task iOSCodeGenerator_GenerateTestsAsync_ShouldReturnTests()
        {
            // Arrange
            var generator = new iOSCodeGenerator(_mockIOSLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new iOSGenerationOptions();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated iOS tests" });

            // Act
            var result = await generator.GenerateTestsAsync(applicationLogic, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.All(result, test => Assert.True(test.Success));
        }

        [Fact]
        public async Task AndroidCodeGenerator_GenerateTestsAsync_ShouldReturnTests()
        {
            // Arrange
            var generator = new AndroidCodeGenerator(_mockAndroidLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new AndroidGenerationOptions();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated Android tests" });

            // Act
            var result = await generator.GenerateTestsAsync(applicationLogic, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.All(result, test => Assert.True(test.Success));
        }

        [Fact]
        public async Task WebCodeGenerator_GenerateTestsAsync_ShouldReturnTests()
        {
            // Arrange
            var generator = new WebCodeGenerator(_mockWebLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new WebGenerationOptions { Framework = "React" };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated Web tests" });

            // Act
            var result = await generator.GenerateTestsAsync(applicationLogic, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.All(result, test => Assert.True(test.Success));
        }

        [Fact]
        public async Task DesktopCodeGenerator_GenerateTestsAsync_ShouldReturnTests()
        {
            // Arrange
            var generator = new DesktopCodeGenerator(_mockDesktopLogger.Object, _mockModelOrchestrator.Object);
            var applicationLogic = CreateSampleApplicationLogic();
            var options = new DesktopGenerationOptions { Platform = "WPF" };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Generated Desktop tests" });

            // Act
            var result = await generator.GenerateTestsAsync(applicationLogic, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.All(result, test => Assert.True(test.Success));
        }

        private ApplicationLogic CreateSampleApplicationLogic()
        {
            return new ApplicationLogic
            {
                ApplicationName = "TestApp",
                Description = "Test application",
                Features = new List<Feature>
                {
                    new Feature
                    {
                        Name = "UserManagement",
                        Description = "User management feature",
                        Requirements = new List<string> { "Authentication", "Authorization" }
                    }
                },
                Entities = new List<Entity>
                {
                    new Entity
                    {
                        Name = "User",
                        Description = "User entity",
                        Properties = new List<Property>
                        {
                            new Property { Name = "Id", Type = "int" },
                            new Property { Name = "Name", Type = "string" }
                        }
                    }
                },
                Services = new List<Service>
                {
                    new Service
                    {
                        Name = "UserService",
                        Description = "User service",
                        Methods = new List<Method>
                        {
                            new Method { Name = "GetUser", ReturnType = "User" },
                            new Method { Name = "CreateUser", ReturnType = "void" }
                        }
                    }
                }
            };
        }
    }
}
