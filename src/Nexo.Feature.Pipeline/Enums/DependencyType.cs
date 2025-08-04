namespace Nexo.Feature.Pipeline.Enums
{
    /// <summary>
    /// Defines the types of dependencies between pipeline components.
    /// </summary>
    public enum DependencyType
    {
        /// <summary>
        /// Required dependency - the dependent component cannot execute without this dependency.
        /// </summary>
        Required,

        /// <summary>
        /// Optional dependency - the dependent component can execute without this dependency.
        /// </summary>
        Optional,

        /// <summary>
        /// Soft dependency - the dependent component can execute but may have reduced functionality.
        /// </summary>
        Soft,

        /// <summary>
        /// Hard dependency - the dependent component must wait for this dependency to complete successfully.
        /// </summary>
        Hard,

        /// <summary>
        /// Phase order dependency - ensures phases execute in the correct order.
        /// </summary>
        PhaseOrder
    }
} 