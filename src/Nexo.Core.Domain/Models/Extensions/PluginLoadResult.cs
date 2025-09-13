using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Common;
using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Models.Extensions
{
    public class PluginLoadResult : BaseResult
    {
        public IPlugin? Plugin { get; set; }
        public string PluginName { get; set; } = string.Empty;
        public string PluginVersion { get; set; } = string.Empty;
        public TimeSpan LoadTime { get; set; }
        public List<PluginLoadError> Errors { get; set; } = new();
        public new List<PluginLoadWarning> Warnings { get; set; } = new();
        public int ErrorCount => Errors.Count;
        public int WarningCount => Warnings.Count;
        public bool HasErrors => ErrorCount > 0;
        public bool HasWarnings => WarningCount > 0;

        public new bool IsSuccess 
        { 
            get => !HasErrors && base.IsSuccess; 
            set => base.IsSuccess = value; 
        }

        public void AddPluginError(string message, string property = "", string code = "")
        {
            Errors.Add(new PluginLoadError
            {
                Id = code,
                Message = message,
                Property = property,
                Timestamp = DateTime.UtcNow
            });
        }

        public void AddPluginWarning(string message, string property = "", string code = "")
        {
            Warnings.Add(new PluginLoadWarning
            {
                Id = code,
                Message = message,
                Property = property,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public class PluginLoadError
    {
        public string Id { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Property { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class PluginLoadWarning
    {
        public string Id { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Property { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
