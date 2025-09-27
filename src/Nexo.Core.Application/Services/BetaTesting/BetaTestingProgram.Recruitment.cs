using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Domain.Entities.BetaTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Domain.Enums.BetaTesting;

namespace Nexo.Core.Application.Services.BetaTesting
{
    /// <summary>
    /// User recruitment functionality
    /// </summary>
    public partial class BetaTestingProgram
    {
        /// <summary>
        /// Recruits users for the beta testing program
        /// </summary>
        public async Task<RecruitmentResult> RecruitUsersAsync(string programId, RecruitmentRequest request)
        {
            _logger.LogInformation("Recruiting users for program: {ProgramId}", programId);

            var recruitedUsers = new List<BetaUser>();
            var recruitmentErrors = new List<string>();

            foreach (var segmentId in request.SegmentIds)
            {
                try
                {
                    var segment = await GetSegmentAsync(programId, segmentId);
                    if (segment == null)
                    {
                        recruitmentErrors.Add($"Segment {segmentId} not found");
                        continue;
                    }

                    var userIds = await _userRecruitment.RecruitUsersForSegmentAsync(segmentId, programId);
                    var users = userIds.Select(userId => new BetaUser 
                    { 
                        Id = userId, 
                        ProgramId = programId, 
                        SegmentId = segmentId,
                        Status = BetaUserStatus.Active,
                        JoinedAt = DateTime.UtcNow
                    }).ToList();
                    recruitedUsers.AddRange(users);

                    // Update segment status
                    segment.CurrentSize = users.Count();
                    segment.Status = users.Count() >= segment.TargetSize ? BetaSegmentStatus.Full : BetaSegmentStatus.Recruiting;

                    _logger.LogInformation("Recruited {UserCount} users for segment {SegmentId}", users.Count(), segmentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to recruit users for segment {SegmentId}", segmentId);
                    recruitmentErrors.Add($"Failed to recruit for segment {segmentId}: {ex.Message}");
                }
            }

            var result = new RecruitmentResult
            {
                ProgramId = programId,
                RecruitedUsers = recruitedUsers,
                TotalRecruited = recruitedUsers.Count,
                Errors = recruitmentErrors,
                Success = !recruitmentErrors.Any(),
                Timestamp = DateTime.UtcNow
            };

            // Track recruitment
            await _analytics.TrackEventAsync("UsersRecruited", new Dictionary<string, object>
            {
                ["ProgramId"] = programId,
                ["RecruitedCount"] = recruitedUsers.Count(),
                ["Errors"] = recruitmentErrors
            });

            return result;
        }
    }
}
