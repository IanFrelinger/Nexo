using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Testing request
    /// </summary>
    public class TestingRequest
    {
        /// <summary>
        /// Request ID
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Code to test
        /// </summary>
        public string Code { get; set; } = string.Empty;
        
        /// <summary>
        /// Programming language
        /// </summary>
        public string Language { get; set; } = string.Empty;
        
        /// <summary>
        /// Test type
        /// </summary>
        public string TestType { get; set; } = string.Empty;
        
        /// <summary>
        /// Test criteria
        /// </summary>
        public List<string> TestCriteria { get; set; } = new();
        
        /// <summary>
        /// Additional context
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new();
        
        /// <summary>
        /// Additional data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
        
        /// <summary>
        /// Timestamp when request was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
