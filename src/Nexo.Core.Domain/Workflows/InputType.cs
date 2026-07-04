namespace Nexo.Core.Domain.Workflows;

/// <summary>Source of initial data supplied by an <see cref="InputNode"/>.</summary>
public enum InputType
{
    /// <summary>Read input from a filesystem path.</summary>
    File,

    /// <summary>Inline string content supplied in the workflow definition.</summary>
    Content,

    /// <summary>Receive input from an HTTP webhook callback.</summary>
    Webhook,

    /// <summary>Query input from a database connection.</summary>
    Database
}
