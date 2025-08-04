namespace Nexo.Shared.Models
{
    /// <summary>
    /// Configuration for build execution.
    /// </summary>
    public class BuildConfiguration
    {
        public string Configuration { get; set; }
        public string TargetFramework { get; set; }
        public string Platform { get; set; }
        public bool Clean { get; set; }
        public bool Restore { get; set; }
        public bool RestorePackages { get; set; }
        public bool RunCodeAnalysis { get; set; }
        public bool TreatWarningsAsErrors { get; set; }
    }
}