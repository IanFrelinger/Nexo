namespace Nexo.Feature.Web.Enums
{
    /// <summary>
    /// Types of web components that can be generated.
    /// </summary>
    public enum WebComponentType
    {
        /// <summary>
        /// Functional component (React hooks, Vue composition API)
        /// </summary>
        Functional = 0,
        
        /// <summary>
        /// Class component (React class, Vue options API)
        /// </summary>
        Class = 1,
        
        /// <summary>
        /// Pure component for performance optimization
        /// </summary>
        Pure = 2,
        
        /// <summary>
        /// Higher-order component (HOC)
        /// </summary>
        HigherOrder = 3,
        
        /// <summary>
        /// Custom hook (React) or composable (Vue)
        /// </summary>
        Hook = 4,
        
        /// <summary>
        /// Context provider component
        /// </summary>
        Context = 5,
        
        /// <summary>
        /// Page component for routing
        /// </summary>
        Page = 6,
        
        /// <summary>
        /// Layout component
        /// </summary>
        Layout = 7
    }
} 