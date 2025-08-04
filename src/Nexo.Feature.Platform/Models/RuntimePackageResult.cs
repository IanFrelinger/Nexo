namespace Nexo.Core.Application.Models
{
    /// <summary>
    /// Represents the result of a package operation.
    /// </summary>
    public sealed class RuntimePackageResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public RuntimePackage Package { get; set; }

        public RuntimePackageResult()
        {
            IsSuccess = false;
            Message = string.Empty;
            Package = new RuntimePackage { Name = string.Empty, Version = string.Empty, IsInstalled = false };
        }
    }
}