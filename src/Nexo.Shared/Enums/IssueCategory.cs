namespace Nexo.Core.Application.Enums
{
    /// <summary>
    /// Represents code issues related to security vulnerabilities or risks.
    /// </summary>
    public enum IssueCategory
    {
        /// <summary>
        /// Represents code issues related to visual or stylistic aspects of code, such as formatting or naming conventions.
        /// </summary>
        Style = 0,

        /// <summary>
        /// Represents code issues that affect the performance of an application, such as inefficiencies or resource mismanagement.
        /// </summary>
        Performance = 1,

        /// <summary>
        /// Represents code issues related to security vulnerabilities or risks.
        /// </summary
        Security = 2,

        /// <summary>
        /// Represents code issues related to the design or structural aspects of the application,
        /// such as architecture patterns, modularity, or system-level organization.
        /// </summary>
        Architecture = 3,

        /// <summary>
        /// Represents code issues that affect the ease of maintaining, understanding, or modifying the codebase over time.
        /// </summary>
        Maintainability = 4,

        /// <summary>
        /// Represents code issues related to missing, incorrect, or inadequate documentation within the codebase.
        /// </summary>
        Documentation = 5
    }
}