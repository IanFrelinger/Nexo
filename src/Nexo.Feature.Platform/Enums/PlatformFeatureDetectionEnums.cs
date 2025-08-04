namespace Nexo.Feature.Platform.Enums
{
    /// <summary>
    /// Types of platform features.
    /// </summary>
    public enum FeatureType
    {
        /// <summary>
        /// UI/UX features
        /// </summary>
        UserInterface,
        
        /// <summary>
        /// Hardware integration features
        /// </summary>
        HardwareIntegration,
        
        /// <summary>
        /// Network and communication features
        /// </summary>
        Networking,
        
        /// <summary>
        /// Security and authentication features
        /// </summary>
        Security,
        
        /// <summary>
        /// Performance optimization features
        /// </summary>
        Performance,
        
        /// <summary>
        /// Storage and data management features
        /// </summary>
        Storage,
        
        /// <summary>
        /// System integration features
        /// </summary>
        SystemIntegration,
        
        /// <summary>
        /// Multimedia features
        /// </summary>
        Multimedia,
        
        /// <summary>
        /// Accessibility features
        /// </summary>
        Accessibility,
        
        /// <summary>
        /// Development and debugging features
        /// </summary>
        Development,
        
        /// <summary>
        /// Other miscellaneous features
        /// </summary>
        Other
    }

    /// <summary>
    /// Availability status of a feature.
    /// </summary>
    public enum FeatureAvailability
    {
        /// <summary>
        /// Feature is fully available and supported
        /// </summary>
        Available,
        
        /// <summary>
        /// Feature is available but with limitations
        /// </summary>
        Limited,
        
        /// <summary>
        /// Feature is available but experimental
        /// </summary>
        Experimental,
        
        /// <summary>
        /// Feature is not available
        /// </summary>
        NotAvailable,
        
        /// <summary>
        /// Feature availability is unknown
        /// </summary>
        Unknown,
        
        /// <summary>
        /// Feature is deprecated
        /// </summary>
        Deprecated
    }



    /// <summary>
    /// Types of platform capabilities.
    /// </summary>
    public enum CapabilityType
    {
        /// <summary>
        /// Processing capabilities
        /// </summary>
        Processing,
        
        /// <summary>
        /// Memory capabilities
        /// </summary>
        Memory,
        
        /// <summary>
        /// Storage capabilities
        /// </summary>
        Storage,
        
        /// <summary>
        /// Network capabilities
        /// </summary>
        Network,
        
        /// <summary>
        /// Graphics capabilities
        /// </summary>
        Graphics,
        
        /// <summary>
        /// Audio capabilities
        /// </summary>
        Audio,
        
        /// <summary>
        /// Input device capabilities
        /// </summary>
        Input,
        
        /// <summary>
        /// Sensor capabilities
        /// </summary>
        Sensors,
        
        /// <summary>
        /// Security capabilities
        /// </summary>
        Security,
        
        /// <summary>
        /// Battery and power capabilities
        /// </summary>
        Power,
        
        /// <summary>
        /// Other capabilities
        /// </summary>
        Other
    }

    /// <summary>
    /// Types of platform limitations.
    /// </summary>
    public enum LimitationType
    {
        /// <summary>
        /// Performance limitations
        /// </summary>
        Performance,
        
        /// <summary>
        /// Memory limitations
        /// </summary>
        Memory,
        
        /// <summary>
        /// Storage limitations
        /// </summary>
        Storage,
        
        /// <summary>
        /// Network limitations
        /// </summary>
        Network,
        
        /// <summary>
        /// Hardware limitations
        /// </summary>
        Hardware,
        
        /// <summary>
        /// Software limitations
        /// </summary>
        Software,
        
        /// <summary>
        /// Security limitations
        /// </summary>
        Security,
        
        /// <summary>
        /// Compatibility limitations
        /// </summary>
        Compatibility,
        
        /// <summary>
        /// Other limitations
        /// </summary>
        Other
    }

    /// <summary>
    /// Types of fallback strategies.
    /// </summary>
    public enum FallbackType
    {
        /// <summary>
        /// Use an alternative feature
        /// </summary>
        AlternativeFeature,
        
        /// <summary>
        /// Use a different implementation
        /// </summary>
        AlternativeImplementation,
        
        /// <summary>
        /// Use a polyfill or shim
        /// </summary>
        Polyfill,
        
        /// <summary>
        /// Graceful degradation
        /// </summary>
        GracefulDegradation,
        
        /// <summary>
        /// Feature detection and conditional loading
        /// </summary>
        ConditionalLoading,
        
        /// <summary>
        /// Use a different platform
        /// </summary>
        PlatformSwitch,
        
        /// <summary>
        /// Disable the feature
        /// </summary>
        Disable,
        
        /// <summary>
        /// Other fallback strategy
        /// </summary>
        Other
    }

    /// <summary>
    /// Types of compatibility issues.
    /// </summary>
    public enum IssueType
    {
        /// <summary>
        /// Feature not supported
        /// </summary>
        NotSupported,
        
        /// <summary>
        /// Feature partially supported
        /// </summary>
        PartiallySupported,
        
        /// <summary>
        /// Feature deprecated
        /// </summary>
        Deprecated,
        
        /// <summary>
        /// Feature experimental
        /// </summary>
        Experimental,
        
        /// <summary>
        /// Performance issue
        /// </summary>
        Performance,
        
        /// <summary>
        /// Security issue
        /// </summary>
        Security,
        
        /// <summary>
        /// Compatibility issue
        /// </summary>
        Compatibility,
        
        /// <summary>
        /// Other issue
        /// </summary>
        Other
    }

    /// <summary>
    /// Types of feature changes.
    /// </summary>
    public enum ChangeType
    {
        /// <summary>
        /// Feature added
        /// </summary>
        Added,
        
        /// <summary>
        /// Feature removed
        /// </summary>
        Removed,
        
        /// <summary>
        /// Feature updated
        /// </summary>
        Updated,
        
        /// <summary>
        /// Feature deprecated
        /// </summary>
        Deprecated,
        
        /// <summary>
        /// Feature availability changed
        /// </summary>
        AvailabilityChanged,
        
        /// <summary>
        /// Feature configuration changed
        /// </summary>
        ConfigurationChanged,
        
        /// <summary>
        /// Other change
        /// </summary>
        Other
    }

    #region Native API Integration Enums

    /// <summary>
    /// Types of native APIs.
    /// </summary>
    public enum APIType
    {
        /// <summary>
        /// System API
        /// </summary>
        System,
        
        /// <summary>
        /// Hardware API
        /// </summary>
        Hardware,
        
        /// <summary>
        /// Network API
        /// </summary>
        Network,
        
        /// <summary>
        /// Security API
        /// </summary>
        Security,
        
        /// <summary>
        /// Multimedia API
        /// </summary>
        Multimedia,
        
        /// <summary>
        /// Storage API
        /// </summary>
        Storage,
        
        /// <summary>
        /// Sensor API
        /// </summary>
        Sensor,
        
        /// <summary>
        /// UI/UX API
        /// </summary>
        UserInterface,
        
        /// <summary>
        /// Other API type
        /// </summary>
        Other
    }

    /// <summary>
    /// Types of permissions.
    /// </summary>
    public enum PermissionType
    {
        /// <summary>
        /// Camera permission
        /// </summary>
        Camera,
        
        /// <summary>
        /// Microphone permission
        /// </summary>
        Microphone,
        
        /// <summary>
        /// Location permission
        /// </summary>
        Location,
        
        /// <summary>
        /// Storage permission
        /// </summary>
        Storage,
        
        /// <summary>
        /// Network permission
        /// </summary>
        Network,
        
        /// <summary>
        /// Contact permission
        /// </summary>
        Contacts,
        
        /// <summary>
        /// Calendar permission
        /// </summary>
        Calendar,
        
        /// <summary>
        /// Notification permission
        /// </summary>
        Notifications,
        
        /// <summary>
        /// Bluetooth permission
        /// </summary>
        Bluetooth,
        
        /// <summary>
        /// Biometric permission
        /// </summary>
        Biometric,
        
        /// <summary>
        /// Other permission type
        /// </summary>
        Other
    }

    /// <summary>
    /// Permission status.
    /// </summary>
    public enum PermissionStatus
    {
        /// <summary>
        /// Permission granted
        /// </summary>
        Granted,
        
        /// <summary>
        /// Permission denied
        /// </summary>
        Denied,
        
        /// <summary>
        /// Permission not determined
        /// </summary>
        NotDetermined,
        
        /// <summary>
        /// Permission restricted
        /// </summary>
        Restricted,
        
        /// <summary>
        /// Permission unavailable
        /// </summary>
        Unavailable
    }

    /// <summary>
    /// Types of API compatibility issues.
    /// </summary>
    public enum APICompatibilityIssueType
    {
        /// <summary>
        /// API not supported
        /// </summary>
        NotSupported,
        
        /// <summary>
        /// API partially supported
        /// </summary>
        PartiallySupported,
        
        /// <summary>
        /// API deprecated
        /// </summary>
        Deprecated,
        
        /// <summary>
        /// API experimental
        /// </summary>
        Experimental,
        
        /// <summary>
        /// Performance issue
        /// </summary>
        Performance,
        
        /// <summary>
        /// Security issue
        /// </summary>
        Security,
        
        /// <summary>
        /// Compatibility issue
        /// </summary>
        Compatibility,
        
        /// <summary>
        /// Permission issue
        /// </summary>
        Permission,
        
        /// <summary>
        /// Other issue
        /// </summary>
        Other
    }

    #endregion

    #region Performance Optimization Enums

    /// <summary>
    /// Types of performance tuning profiles.
    /// </summary>
    public enum TuningProfileType
    {
        /// <summary>
        /// Balanced performance profile
        /// </summary>
        Balanced,
        
        /// <summary>
        /// High performance profile
        /// </summary>
        HighPerformance,
        
        /// <summary>
        /// Power saving profile
        /// </summary>
        PowerSaving,
        
        /// <summary>
        /// Custom tuning profile
        /// </summary>
        Custom,
        
        /// <summary>
        /// Gaming profile
        /// </summary>
        Gaming,
        
        /// <summary>
        /// Development profile
        /// </summary>
        Development
    }

    /// <summary>
    /// Types of memory optimization.
    /// </summary>
    public enum MemoryOptimizationType
    {
        /// <summary>
        /// Garbage collection optimization
        /// </summary>
        GarbageCollection,
        
        /// <summary>
        /// Memory pooling
        /// </summary>
        MemoryPooling,
        
        /// <summary>
        /// Cache optimization
        /// </summary>
        CacheOptimization,
        
        /// <summary>
        /// Memory compression
        /// </summary>
        MemoryCompression,
        
        /// <summary>
        /// Memory defragmentation
        /// </summary>
        MemoryDefragmentation,
        
        /// <summary>
        /// Other memory optimization
        /// </summary>
        Other
    }

    /// <summary>
    /// Types of battery optimization.
    /// </summary>
    public enum BatteryOptimizationType
    {
        /// <summary>
        /// CPU frequency scaling
        /// </summary>
        CPUFrequencyScaling,
        
        /// <summary>
        /// Display brightness optimization
        /// </summary>
        DisplayOptimization,
        
        /// <summary>
        /// Network power management
        /// </summary>
        NetworkPowerManagement,
        
        /// <summary>
        /// Background process optimization
        /// </summary>
        BackgroundProcessOptimization,
        
        /// <summary>
        /// Wake lock management
        /// </summary>
        WakeLockManagement,
        
        /// <summary>
        /// Other battery optimization
        /// </summary>
        Other
    }

    /// <summary>
    /// Types of performance bottlenecks.
    /// </summary>
    public enum BottleneckType
    {
        /// <summary>
        /// CPU bottleneck
        /// </summary>
        CPU,
        
        /// <summary>
        /// Memory bottleneck
        /// </summary>
        Memory,
        
        /// <summary>
        /// Disk I/O bottleneck
        /// </summary>
        DiskIO,
        
        /// <summary>
        /// Network bottleneck
        /// </summary>
        Network,
        
        /// <summary>
        /// GPU bottleneck
        /// </summary>
        GPU,
        
        /// <summary>
        /// Battery bottleneck
        /// </summary>
        Battery,
        
        /// <summary>
        /// Other bottleneck
        /// </summary>
        Other
    }

    /// <summary>
    /// Types of performance recommendations.
    /// </summary>
    public enum RecommendationType
    {
        /// <summary>
        /// Memory optimization recommendation
        /// </summary>
        MemoryOptimization,
        
        /// <summary>
        /// CPU optimization recommendation
        /// </summary>
        CPUOptimization,
        
        /// <summary>
        /// Battery optimization recommendation
        /// </summary>
        BatteryOptimization,
        
        /// <summary>
        /// Network optimization recommendation
        /// </summary>
        NetworkOptimization,
        
        /// <summary>
        /// Code optimization recommendation
        /// </summary>
        CodeOptimization,
        
        /// <summary>
        /// Configuration optimization recommendation
        /// </summary>
        ConfigurationOptimization,
        
        /// <summary>
        /// Other recommendation
        /// </summary>
        Other
    }

    #endregion
} 