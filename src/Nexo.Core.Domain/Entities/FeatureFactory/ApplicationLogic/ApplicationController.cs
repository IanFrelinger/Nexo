using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexo.Core.Domain.Entities.FeatureFactory.ApplicationLogic
{
    /// <summary>
    /// Represents an application controller in the generated application logic
    /// </summary>
    public class ApplicationController
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string BaseController { get; set; } = string.Empty;
        public List<ControllerAction> Actions { get; set; } = new();
        public List<ControllerAttribute> Attributes { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public List<string> UsingStatements { get; set; } = new();
        public ControllerType Type { get; set; } = ControllerType.WebApi;
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a controller action
    /// </summary>
    public class ControllerAction
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public HttpMethod HttpMethod { get; set; } = HttpMethod.Get;
        public string Route { get; set; } = string.Empty;
        public List<ActionParameter> Parameters { get; set; } = new();
        public List<ActionAttribute> Attributes { get; set; } = new();
        public string Implementation { get; set; } = string.Empty;
        public ActionVisibility Visibility { get; set; } = ActionVisibility.Public;
        public bool IsAsync { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsOverride { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a controller action parameter
    /// </summary>
    public class ActionParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ParameterSource Source { get; set; } = ParameterSource.Query;
        public bool IsRequired { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
        public List<ValidationAttribute> ValidationAttributes { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a controller attribute
    /// </summary>
    public class ControllerAttribute
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents an action attribute
    /// </summary>
    public class ActionAttribute
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Types of application controllers
    /// </summary>
    public enum ControllerType
    {
        WebApi,
        Mvc,
        Blazor,
        SignalR,
        GraphQL,
        gRPC
    }

    /// <summary>
    /// HTTP methods for controller actions
    /// </summary>
    public enum HttpMethod
    {
        Get,
        Post,
        Put,
        Delete,
        Patch,
        Head,
        Options
    }

    /// <summary>
    /// Parameter sources for controller actions
    /// </summary>
    public enum ParameterSource
    {
        Query,
        Route,
        Body,
        Header,
        Form
    }

    /// <summary>
    /// Action visibility levels
    /// </summary>
    public enum ActionVisibility
    {
        Public,
        Private,
        Protected,
        Internal
    }
}
