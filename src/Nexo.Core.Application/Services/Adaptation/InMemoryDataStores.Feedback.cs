using Nexo.Core.Application.Services.Learning;
using Nexo.Core.Domain.Entities.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Adaptation
{
    /// <summary>
    /// In-memory implementation of feedback store
    /// </summary>
    public partial class InMemoryFeedbackStore : IFeedbackStore
    {
        private readonly List<UserFeedback> _feedback = new();
        private readonly object _lock = new();
        
        public Task StoreFeedbackAsync(UserFeedback feedback)
        {
            lock (_lock)
            {
                _feedback.Add(feedback);
            }
            return Task.CompletedTask;
        }
        
        public Task<IEnumerable<UserFeedback>> GetFeedbackSinceAsync(DateTime since)
        {
            lock (_lock)
            {
                return Task.FromResult(_feedback.Where(f => f.Timestamp >= since));
            }
        }
        
        public Task<IEnumerable<UserFeedback>> GetFeedbackInTimeRangeAsync(DateTime start, DateTime end)
        {
            lock (_lock)
            {
                return Task.FromResult(_feedback.Where(f => f.Timestamp >= start && f.Timestamp <= end));
            }
        }
        
        public Task<UserFeedback?> GetFeedbackByIdAsync(string feedbackId)
        {
            lock (_lock)
            {
                return Task.FromResult(_feedback.FirstOrDefault(f => f.Id == feedbackId));
            }
        }
        
        public Task DeleteFeedbackAsync(string feedbackId)
        {
            lock (_lock)
            {
                _feedback.RemoveAll(f => f.Id == feedbackId);
            }
            return Task.CompletedTask;
        }
        
        public Task<UserFeedback?> GetFeedbackAsync(string id)
        {
            return GetFeedbackByIdAsync(id);
        }
        
        public Task<IEnumerable<UserFeedback>> GetFeedbackAsync(DateTime startTime, DateTime endTime)
        {
            return GetFeedbackInTimeRangeAsync(startTime, endTime);
        }
        
        public Task DeleteOldFeedbackAsync(DateTime cutoffTime)
        {
            lock (_lock)
            {
                _feedback.RemoveAll(f => f.Timestamp < cutoffTime);
            }
            return Task.CompletedTask;
        }
    }
}
