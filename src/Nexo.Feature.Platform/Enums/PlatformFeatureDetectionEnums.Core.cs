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
}
