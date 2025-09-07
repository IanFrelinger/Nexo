namespace Nexo.Feature.Platform.Enums
{
    /// <summary>
    /// Application pattern types
    /// </summary>
    public enum PatternType
    {
        Repository,
        UnitOfWork,
        Command,
        Query,
        Mediator
    }

    /// <summary>
    /// Security pattern types
    /// </summary>
    public enum SecurityPatternType
    {
        Jwt,
        Authorization,
        InputValidation,
        DataEncryption
    }

    /// <summary>
    /// State management types
    /// </summary>
    public enum StateManagementType
    {
        GlobalState,
        LocalState,
        Redux,
        MobX
    }

    /// <summary>
    /// HTTP method types
    /// </summary>
    public enum HttpMethod
    {
        Get,
        Post,
        Put,
        Delete,
        Patch
    }

    /// <summary>
    /// Data flow types
    /// </summary>
    public enum DataFlowType
    {
        Unidirectional,
        Bidirectional,
        EventDriven
    }

    /// <summary>
    /// Caching strategy types
    /// </summary>
    public enum CachingStrategyType
    {
        MemoryCache,
        DistributedCache,
        ResponseCache
    }
}
