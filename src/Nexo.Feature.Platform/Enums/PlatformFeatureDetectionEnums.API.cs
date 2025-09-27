namespace Nexo.Feature.Platform.Enums
{
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
}
