namespace Nexo.Core.Application.Enums
{
    /// <summary>
    /// Represents integration testing within the scope of test execution.
    /// </summary>
    public enum TestScope
    {
        /// <summary>
        /// Represents all scopes of test execution.
        /// </summary>
        All = 0,

        /// <summary>
        /// Represents unit testing within the scope of test execution.
        /// </summary>
        Unit = 1,

        /// <summary>
        /// Represents integration testing within the scope of test execution.
        /// </summary>
        Integration = 2,

        /// <summary>
        /// Represents end-to-end testing within the scope of test execution.
        /// </summary>
        EndToEnd = 3,

        /// <summary>
        /// Represents a custom, user-defined scope for test execution.
        /// </summary>
        Custom = 4
    }
}