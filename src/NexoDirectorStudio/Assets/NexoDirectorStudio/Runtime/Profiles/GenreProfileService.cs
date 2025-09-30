using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NexoDirectorStudio.Profiles
{
    /// <summary>
    /// Service for managing genre profiles and auto-detection.
    /// Initializes and configures the genre registry with all available profiles.
    /// </summary>
    public sealed class GenreProfileService
    {
        private readonly GenreRegistry _registry;
        private readonly ILogger<GenreProfileService> _logger;
        
        public GenreProfileService(GenreRegistry registry, ILogger<GenreProfileService> logger)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Initializes the genre registry with all available profiles.
        /// </summary>
        public void InitializeProfiles()
        {
            try
            {
                _logger.LogInformation("Initializing genre profiles...");
                
                // Register FPS profile
                var fpsProfile = new FPSProfile();
                _registry.RegisterProfile(fpsProfile);
                _logger.LogInformation("Registered FPS profile: {ProfileName}", fpsProfile.Name);
                
                // Register Platformer profile
                var platformerProfile = new PlatformerProfile();
                _registry.RegisterProfile(platformerProfile);
                _logger.LogInformation("Registered Platformer profile: {ProfileName}", platformerProfile.Name);
                
                // Register RPG profile
                var rpgProfile = new RPGProfile();
                _registry.RegisterProfile(rpgProfile);
                _logger.LogInformation("Registered RPG profile: {ProfileName}", rpgProfile.Name);
                
                _logger.LogInformation("Genre profile initialization completed. Total profiles: {ProfileCount}", _registry.ProfileCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize genre profiles");
                throw;
            }
        }
        
        /// <summary>
        /// Gets the genre registry.
        /// </summary>
        public GenreRegistry Registry => _registry;
        
        /// <summary>
        /// Gets a profile by its ID.
        /// </summary>
        /// <param name="id">The profile ID</param>
        /// <returns>The profile, or null if not found</returns>
        public IGenreProfile? GetProfileById(string id)
        {
            return _registry.GetProfileById(id);
        }
        
        /// <summary>
        /// Gets a profile by its name.
        /// </summary>
        /// <param name="name">The profile name</param>
        /// <returns>The profile, or null if not found</returns>
        public IGenreProfile? GetProfileByName(string name)
        {
            return _registry.GetProfileByName(name);
        }
        
        /// <summary>
        /// Auto-detects the best genre profile for a design brief.
        /// </summary>
        /// <param name="brief">The design brief to analyze</param>
        /// <returns>The best matching profile, or null if no good match</returns>
        public IGenreProfile? AutoDetectGenre(DTO.DesignBrief brief)
        {
            if (brief == null)
                return null;
            
            var detectedProfile = _registry.AutoDetectGenre(brief);
            
            if (detectedProfile != null)
            {
                _logger.LogInformation("Auto-detected genre: {GenreName} for brief: {BriefDescription}", 
                    detectedProfile.Name, brief.Description);
            }
            else
            {
                _logger.LogWarning("No genre profile could be auto-detected for brief: {BriefDescription}", 
                    brief.Description);
            }
            
            return detectedProfile;
        }
        
        /// <summary>
        /// Gets all available genre profiles.
        /// </summary>
        /// <returns>List of all registered profiles</returns>
        public IReadOnlyList<IGenreProfile> GetAllProfiles()
        {
            return _registry.AllProfiles;
        }
        
        /// <summary>
        /// Gets profiles that match specific keywords.
        /// </summary>
        /// <param name="keywords">Keywords to search for</param>
        /// <returns>List of matching profiles</returns>
        public IReadOnlyList<IGenreProfile> GetProfilesByKeywords(IReadOnlyList<string> keywords)
        {
            return _registry.GetProfilesByKeywords(keywords);
        }
        
        /// <summary>
        /// Gets profiles that support specific mechanics.
        /// </summary>
        /// <param name="mechanics">Mechanics to search for</param>
        /// <returns>List of matching profiles</returns>
        public IReadOnlyList<IGenreProfile> GetProfilesByMechanics(IReadOnlyList<string> mechanics)
        {
            return _registry.GetProfilesByMechanics(mechanics);
        }
        
        /// <summary>
        /// Gets a summary of all registered profiles.
        /// </summary>
        /// <returns>Summary information about all profiles</returns>
        public string GetSummary()
        {
            return _registry.GetSummary();
        }
    }
}
