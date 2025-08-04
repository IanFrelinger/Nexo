namespace Nexo.Core.Application.Models
{
    /// <summary>
    /// Represents a runtime package/module.
    /// </summary>
    public sealed class RuntimePackage
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public bool IsInstalled { get; set; }

        public RuntimePackage()
        {
            Name = string.Empty;
            Version = string.Empty;
            Description = string.Empty;
            IsInstalled = false;
        }
    }
}