using System.Collections.Concurrent;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Profiles
{
    /// <summary>
    /// Registry for managing genre profiles and auto-detection.
    /// Provides centralized access to all available genre profiles.
    /// </summary>
    public sealed class GenreRegistry
    {
        private readonly ConcurrentDictionary<string, IGenreProfile> _profiles = new();
        private readonly ConcurrentDictionary<string, IGenreProfile> _profilesByName = new();
        
        /// <summary>
        /// Gets all registered genre profiles.
        /// </summary>
        public IReadOnlyList<IGenreProfile> AllProfiles => _profiles.Values.ToList();
        
        /// <summary>
        /// Gets the number of registered profiles.
        /// </summary>
        public int ProfileCount => _profiles.Count;
        
        /// <summary>
        /// Registers a genre profile.
        /// </summary>
        /// <param name="profile">The profile to register</param>
        public void RegisterProfile(IGenreProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            
            if (string.IsNullOrWhiteSpace(profile.Id))
                throw new ArgumentException("Profile ID cannot be null or empty", nameof(profile));
            
            if (string.IsNullOrWhiteSpace(profile.Name))
                throw new ArgumentException("Profile name cannot be null or empty", nameof(profile));
            
            _profiles[profile.Id] = profile;
            _profilesByName[profile.Name.ToLowerInvariant()] = profile;
        }
        
        /// <summary>
        /// Gets a profile by its ID.
        /// </summary>
        /// <param name="id">The profile ID</param>
        /// <returns>The profile, or null if not found</returns>
        public IGenreProfile? GetProfileById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            
            return _profiles.TryGetValue(id, out var profile) ? profile : null;
        }
        
        /// <summary>
        /// Gets a profile by its name.
        /// </summary>
        /// <param name="name">The profile name</param>
        /// <returns>The profile, or null if not found</returns>
        public IGenreProfile? GetProfileByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            
            return _profilesByName.TryGetValue(name.ToLowerInvariant(), out var profile) ? profile : null;
        }
        
        /// <summary>
        /// Auto-detects the best genre profile for a design brief.
        /// </summary>
        /// <param name="brief">The design brief to analyze</param>
        /// <returns>The best matching profile, or null if no good match</returns>
        public IGenreProfile? AutoDetectGenre(DesignBrief brief)
        {
            if (brief == null)
                return null;
            
            var bestMatch = (Profile: (IGenreProfile?)null, Score: 0);
            
            foreach (var profile in _profiles.Values)
            {
                var score = profile.DetectGenre(brief);
                if (score > bestMatch.Score)
                {
                    bestMatch = (profile, score);
                }
            }
            
            // Only return if confidence is above threshold
            return bestMatch.Score >= 60 ? bestMatch.Profile : null;
        }
        
        /// <summary>
        /// Gets the best matching profiles for a design brief, ordered by confidence.
        /// </summary>
        /// <param name="brief">The design brief to analyze</param>
        /// <param name="minConfidence">Minimum confidence score to include (0-100)</param>
        /// <returns>List of matching profiles ordered by confidence</returns>
        public IReadOnlyList<(IGenreProfile Profile, int Confidence)> GetMatchingProfiles(DesignBrief brief, int minConfidence = 30)
        {
            if (brief == null)
                return Array.Empty<(IGenreProfile, int)>();
            
            var matches = new List<(IGenreProfile Profile, int Confidence)>();
            
            foreach (var profile in _profiles.Values)
            {
                var confidence = profile.DetectGenre(brief);
                if (confidence >= minConfidence)
                {
                    matches.Add((profile, confidence));
                }
            }
            
            return matches.OrderByDescending(m => m.Confidence).ToList();
        }
        
        /// <summary>
        /// Gets profiles that match specific keywords.
        /// </summary>
        /// <param name="keywords">Keywords to search for</param>
        /// <returns>List of profiles that match the keywords</returns>
        public IReadOnlyList<IGenreProfile> GetProfilesByKeywords(IReadOnlyList<string> keywords)
        {
            if (keywords == null || keywords.Count == 0)
                return Array.Empty<IGenreProfile>();
            
            var matches = new List<IGenreProfile>();
            
            foreach (var profile in _profiles.Values)
            {
                var profileKeywords = profile.Keywords.Select(k => k.ToLowerInvariant()).ToList();
                var searchKeywords = keywords.Select(k => k.ToLowerInvariant()).ToList();
                
                var matchCount = searchKeywords.Count(k => profileKeywords.Any(pk => pk.Contains(k) || k.Contains(pk)));
                var matchRatio = (float)matchCount / searchKeywords.Count;
                
                if (matchRatio >= 0.3f) // At least 30% keyword match
                {
                    matches.Add(profile);
                }
            }
            
            return matches;
        }
        
        /// <summary>
        /// Gets profiles that support specific mechanics.
        /// </summary>
        /// <param name="mechanics">Mechanics to search for</param>
        /// <returns>List of profiles that support the mechanics</returns>
        public IReadOnlyList<IGenreProfile> GetProfilesByMechanics(IReadOnlyList<string> mechanics)
        {
            if (mechanics == null || mechanics.Count == 0)
                return Array.Empty<IGenreProfile>();
            
            var matches = new List<IGenreProfile>();
            
            foreach (var profile in _profiles.Values)
            {
                var profileMechanics = profile.CoreMechanics.Select(m => m.ToLowerInvariant()).ToList();
                var searchMechanics = mechanics.Select(m => m.ToLowerInvariant()).ToList();
                
                var matchCount = searchMechanics.Count(m => profileMechanics.Any(pm => pm.Contains(m) || m.Contains(pm)));
                var matchRatio = (float)matchCount / searchMechanics.Count;
                
                if (matchRatio >= 0.5f) // At least 50% mechanic match
                {
                    matches.Add(profile);
                }
            }
            
            return matches;
        }
        
        /// <summary>
        /// Clears all registered profiles.
        /// </summary>
        public void Clear()
        {
            _profiles.Clear();
            _profilesByName.Clear();
        }
        
        /// <summary>
        /// Removes a profile by its ID.
        /// </summary>
        /// <param name="id">The profile ID to remove</param>
        /// <returns>True if the profile was removed, false if not found</returns>
        public bool RemoveProfile(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;
            
            if (_profiles.TryRemove(id, out var profile))
            {
                _profilesByName.TryRemove(profile.Name.ToLowerInvariant(), out _);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets a summary of all registered profiles.
        /// </summary>
        /// <returns>Summary information about all profiles</returns>
        public string GetSummary()
        {
            var summary = new List<string>
            {
                $"Total Profiles: {_profiles.Count}",
                $"Profile IDs: {string.Join(", ", _profiles.Keys)}",
                $"Profile Names: {string.Join(", ", _profiles.Values.Select(p => p.Name))}"
            };
            
            return string.Join("\n", summary);
        }
    }
}
