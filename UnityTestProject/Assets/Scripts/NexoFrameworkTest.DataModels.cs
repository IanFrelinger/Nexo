using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NexoDoomGame
{
    /// <summary>
    /// Test result classes and data structures
    /// </summary>
    public partial class NexoFrameworkTest
    {
        // This partial class contains all the data model classes
    }
    
    /// <summary>
    /// Test results container
    /// </summary>
    [System.Serializable]
    public class TestResults
    {
        public List<TestResult> Results = new List<TestResult>();
        
        public int SuccessCount => Results.Count(r => r.Success);
        public int FailureCount => Results.Count(r => !r.Success);
        
        public void AddSuccess(string category, string message)
        {
            Results.Add(new TestResult { Category = category, Message = message, Success = true });
        }
        
        public void AddFailure(string category, string message)
        {
            Results.Add(new TestResult { Category = category, Message = message, Success = false });
        }
    }
    
    /// <summary>
    /// Individual test result
    /// </summary>
    [System.Serializable]
    public class TestResult
    {
        public string Category;
        public string Message;
        public bool Success;
        public DateTime Timestamp = DateTime.Now;
    }
}
