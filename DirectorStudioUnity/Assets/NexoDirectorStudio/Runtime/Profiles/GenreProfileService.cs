using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NexoDirectorStudio.Profiles
{
    /// <summary>
    /// Service for managing genre profiles and auto-detection.
    /// Initializes and configures the genre registry with all available profiles.
    /// </summary>
    public sealed class GenreProfileService
    {
        private readonly GenreRegistry _registry;
        
        public GenreProfileService() {
            _registry = new GenreRegistry();
        }
        
        /// <summary>
        /// Initializes the genre registry with all available profiles.
        /// </summary>
        public void InitializeProfiles()
        {
            try
            {
                
                
                // Register FPS profile
                var fpsProfile = new FPSProfile();
                _registry.RegisterProfile(fpsProfile);
                
                
                // Register Platformer profile
                var platformerProfile = new PlatformerProfile();
                _registry.RegisterProfile(platformerProfile);
                
                
                // Register RPG profile
                var rpgProfile = new RPGProfile();
                _registry.RegisterProfile(rpgProfile);
                
                
                
            }
            catch (Exception ex)
            {
                
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
                
            }
            else
            {
                
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
