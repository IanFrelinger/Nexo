namespace Ashlar.Core.Domain.Workflows;

/// <summary>Delivery channel for workflow output produced by an <see cref="OutputNode"/>.</summary>
public enum OutputType
{
    /// <summary>Render results inline in the composer or operator UI.</summary>
    Display,

    /// <summary>Write results to a filesystem path.</summary>
    File,

    /// <summary>POST results to an HTTP webhook endpoint.</summary>
    Webhook,

    /// <summary>Persist results to a database table.</summary>
    Database
}
