using System;
using System.Collections.Generic;

namespace Nexo.Feature.Container.Models
{
    /// <summary>
    /// Defines configurable parameters to manage the behavior and execution constraints of a session, such as
    /// resource usage limitations, networking permissions, and automatic persistence of session state.
    /// </summary>
    public sealed class SessionOptions
    {
        public bool AutoSave { get; set; }
        public bool PersistEnvironment { get; set; }
        public bool EnableNetworking { get; set; }
        public bool EnableGpuAccess { get; set; }
        public TimeSpan IdleTimeout { get; set; }
        public int MaxMemoryMb { get; set; }
        public int MaxCpuCores { get; set; }

        public SessionOptions()
        {
            AutoSave = true;
            PersistEnvironment = true;
            EnableNetworking = true;
            EnableGpuAccess = false;
            IdleTimeout = TimeSpan.FromMinutes(30);
            MaxMemoryMb = 2048;
            MaxCpuCores = 2;
        }
    }
}