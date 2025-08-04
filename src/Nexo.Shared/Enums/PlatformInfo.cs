namespace Nexo.Core.Application.Enums
{
    /// <summary>
    /// Represents platform information.
    /// </summary>
    public sealed class PlatformInfo
    {
        public PlatformType Type { get; private set; }
        public string Name { get; private set; }
        public string Version { get; private set; }
        public string Architecture { get; private set; }
        public bool SupportsContainers { get; private set; }
        public bool SupportsGui { get; private set; }

        public PlatformInfo(PlatformType type, string name, string version, string architecture, bool supportsContainers, bool supportsGui)
        {
            Type = type;
            Name = name;
            Version = version;
            Architecture = architecture;
            SupportsContainers = supportsContainers;
            SupportsGui = supportsGui;
        }
    }
}