using System;

namespace Nexo.Core.Domain.Entities.Infrastructure
{
    /// <summary>
    /// Basic operating system information
    /// </summary>
    public partial class OSVersion
    {
        /// <summary>
        /// Operating system service pack
        /// </summary>
        public string ServicePack { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system edition
        /// </summary>
        public string Edition { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system language
        /// </summary>
        public string Language { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system region
        /// </summary>
        public string Region { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system time zone
        /// </summary>
        public string TimeZone { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system locale
        /// </summary>
        public string Locale { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system culture
        /// </summary>
        public string Culture { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system display name
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system description
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system manufacturer
        /// </summary>
        public string Manufacturer { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system product name
        /// </summary>
        public string ProductName { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system product type
        /// </summary>
        public string ProductType { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system product version
        /// </summary>
        public string ProductVersion { get; set; } = string.Empty;
        
        /// <summary>
        /// Operating system serial number
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;
    }
}
