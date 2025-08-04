namespace Nexo.Core.Application.Enums
{
    /// <summary>
    /// Represents a template type for modules, typically used to define independently deployable or reusable units of functionality in an application.
    /// </summary>
    public enum TemplateType
    {
        /// <summary>
        /// Represents a class within a template, typically used to encapsulate data and behavior in object-oriented designs.
        /// </summary>
        Class = 0,

        /// <summary>
        /// Represents a placeholder within a template.
        /// </summary>
        Interface = 1,

        /// <summary>
        /// Represents a method within a template, typically used to define reusable operations or behaviors executable within code.
        /// </summary>
        Method = 2,

        /// <summary>
        /// Represents a template type for defining properties within a class or interface.
        /// </summary>
        Property = 3,

        /// <summary>
        /// Represents a test type in the template system.
        /// </summary>
        Constructor = 4,

        /// <summary>
        /// Represents a test template type, typically used for creating or validating
        /// test scenarios, cases, or related artifacts within an application.
        /// </summary>
        Test = 5,

        /// <summary>
        /// Represents a configuration-specific template type.
        /// </summary>
        Configuration = 6,

        /// <summary>
        /// Represents a template designed for documentation purposes.
        /// </summary>
        Documentation = 7,

        /// <summary>
        /// Represents a project-based template type.
        /// </summary>
        Project = 8,

        /// <summary>
        /// Represents a module within a template, typically used to define a cohesive and independently deployable unit of functionality in an application.
        /// </summary>
        Module = 9,

        /// <summary>
        /// Represents a component within a template, typically used to define a self-contained logical or functional unit of an application.
        /// </summary>
        Component = 10,

        /// <summary>
        /// Represents a service template type, commonly used to define components that encapsulate business logic or operations in an application.
        /// </summary>
        Service = 11,

        /// <summary>
        /// Represents a repository template, typically used to define a data access layer
        /// pattern for interacting with persistent storage in an application.
        /// </summary>
        Repository = 12,

        /// <summary>
        /// Represents a controller within a template, typically used to handle user input, process requests, and interact with services or data models in an application.
        /// </summary>
        Controller = 13,

        /// <summary>
        /// Represents a template type for defining data models.
        /// </summary>
        Model = 14,

        /// <summary>
        /// Represents a template specific to ViewModels, often used for defining logic and data binding
        /// between a View and its associated Model in an MVVM (Model-View-ViewModel) pattern.
        /// </summary>
        ViewModel = 15,

        /// <summary>
        /// Represents a middleware template type, typically used to define components that handle requests and responses in an application pipeline.
        /// </summary>
        Middleware = 16,

        /// <summary>
        /// Represents a custom template type defined by the user.
        /// </summary>
        Custom = 999
    }
}