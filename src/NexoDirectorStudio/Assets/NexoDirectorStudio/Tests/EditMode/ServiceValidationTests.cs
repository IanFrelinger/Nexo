using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Adapters;
using NexoDirectorStudio.Profiles;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Tests for service initialization and resolution validation
    /// </summary>
    public class ServiceValidationTests : IDisposable
    {
        private readonly DirectorStudioService _service;
        
        public ServiceValidationTests()
        {
            _service = new DirectorStudioService();
        }
        
        public void Dispose()
        {
            _service?.Dispose();
        }
        
        [Test]
        public void Service_ShouldBeInitialized()
        {
            // Assert
            Assert.IsNotNull(_service);
        }
        
        [Test]
        public void Service_ShouldProvideAllRequiredServices()
        {
            // Act & Assert
            Assert.IsNotNull(_service.GetService<IPlanGameSliceCommand>());
            Assert.IsNotNull(_service.GetService<IBuildWorldLayoutCommand>());
            Assert.IsNotNull(_service.GetService<IPlaceInteractionsCommand>());
            Assert.IsNotNull(_service.GetService<ICreateContentBundleCommand>());
            Assert.IsNotNull(_service.GetService<IProposeAutoFixesCommand>());
            Assert.IsNotNull(_service.GetService<IApplyAutoFixesCommand>());
        }
        
        [Test]
        public void Service_ShouldProvideValidators()
        {
            // Act
            var validators = _service.GetService<IEnumerable<IValidator<GamePlan>>>();
            
            // Assert
            Assert.IsNotNull(validators, "Should provide validators");
            Assert.IsTrue(validators.Any(), "Should have at least one validator");
            Assert.IsTrue(validators.Any(v => v is PlayabilityValidator), "Should include PlayabilityValidator");
            Assert.IsTrue(validators.Any(v => v is MechanicsValidator), "Should include MechanicsValidator");
        }
        
        [Test]
        public void Service_ShouldProvideAdapters()
        {
            // Act & Assert
            Assert.IsNotNull(_service.GetService<IOllamaAdapter>(), "Should provide IOllamaAdapter");
            Assert.IsNotNull(_service.GetService<ITextureGenAdapter>(), "Should provide ITextureGenAdapter");
            Assert.IsNotNull(_service.GetService<ITtsAdapter>(), "Should provide ITtsAdapter");
        }
        
        [Test]
        public void Service_ShouldProvideGenreProfiles()
        {
            // Act
            var genreRegistry = _service.GetService<GenreRegistry>();
            var genreProfileService = _service.GetService<GenreProfileService>();
            
            // Assert
            Assert.IsNotNull(genreRegistry, "Should provide GenreRegistry");
            Assert.IsNotNull(genreProfileService, "Should provide GenreProfileService");
            Assert.IsTrue(genreRegistry.GetAllProfiles().Count >= 3, "Should have at least 3 genre profiles");
        }
    }
}
