using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Domain.Entities.BetaTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Domain.Enums.BetaTesting;

namespace Nexo.Core.Application.Services.BetaTesting
{
    /// <summary>
    /// Core beta testing program management
    /// </summary>
    public partial class BetaTestingProgram
    {
        /// <summary>
        /// Initializes the beta testing program
        /// </summary>
        public async Task<BetaProgramResult> InitializeProgramAsync(BetaProgramConfiguration config)
        {
            _logger.LogInformation("Initializing beta testing program: {ProgramName}", config.Name);

            var program = new BetaProgram
            {
                Id = Guid.NewGuid().ToString(),
                Name = config.Name,
                Description = config.Description,
                Status = BetaProgramStatus.Active,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(config.DurationDays),
                Configuration = config,
                CreatedAt = DateTime.UtcNow
            };

            // Initialize program segments
            foreach (var segmentConfig in config.UserSegments)
            {
                var segment = new BetaUserSegment
                {
                    Id = Guid.NewGuid().ToString(),
                    ProgramId = program.Id,
                    Name = segmentConfig.Name,
                    Description = segmentConfig.Description,
                    TargetSize = segmentConfig.TargetSize,
                    FocusAreas = segmentConfig.FocusAreas,
                    Status = BetaSegmentStatus.Recruiting,
                    CreatedAt = DateTime.UtcNow
                };

                program.Segments.Add(segment);
            }

            // Track program initialization
            await _analytics.TrackEventAsync("BetaProgramInitialized", new Dictionary<string, object>
            {
                ["EventType"] = BetaAnalyticsEventType.ProgramInitialized.ToString(),
                ["ProgramId"] = program.Id,
                ["Timestamp"] = DateTime.UtcNow,
                ["Configuration"] = config
            });

            _logger.LogInformation("Beta testing program initialized: {ProgramId}", program.Id);
            return new BetaProgramResult
            {
                ProgramId = program.Id,
                Success = true,
                Message = "Beta testing program initialized successfully"
            };
        }
    }
}
