namespace Nexo.Feature.Container.Models
{
    /// <summary>
    /// Represents a mapping of ports between the host and a container.
    /// </summary>
    public sealed class PortMapping
    {
        public int HostPort { get; set; }
        public int ContainerPort { get; set; }
        public string Protocol { get; set; }

        public PortMapping()
        {
            Protocol = "tcp";
        }

        public PortMapping(int hostPort, int containerPort, string protocol = "tcp")
        {
            HostPort = hostPort;
            ContainerPort = containerPort;
            Protocol = protocol;
        }
    }
}