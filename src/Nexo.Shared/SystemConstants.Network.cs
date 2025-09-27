using System;
using System.Collections.Generic;

namespace Nexo.Shared
{
    /// <summary>
    /// Network protocol names and identifiers with case-insensitive matching.
    /// </summary>
    public static partial class SystemConstants
    {
        public static class Protocols
        {
            public const string HTTP = "http";
            public const string HTTPS = "https";
            public const string FTP = "ftp";
            public const string SFTP = "sftp";
            public const string SSH = "ssh";
            public const string TCP = "tcp";
            public const string UDP = "udp";
            public const string WebSocket = "ws";
            public const string WebSocketSecure = "wss";
            public const string GRPC = "grpc";
            public const string GRPCS = "grpcs";

            /// <summary>
            /// Gets all protocol variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> AllVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                HTTP, HTTPS, FTP, SFTP, SSH, TCP, UDP, WebSocket, WebSocketSecure, GRPC, GRPCS,
                "HTTP", "HTTPS", "FTP", "SFTP", "SSH", "TCP", "UDP", "WS", "WSS", "GRPC", "GRPCS"
            };

            /// <summary>
            /// Tries to match a protocol name case-insensitively.
            /// </summary>
            /// <param name="protocolName">The protocol name to match.</param>
            /// <returns>The standardized protocol name or empty string if not found.</returns>
            public static string MatchProtocol(string protocolName)
            {
                if (string.IsNullOrWhiteSpace(protocolName))
                    return string.Empty;

                var normalizedName = protocolName.Trim();
                
                if (AllVariations.Contains(normalizedName))
                    return normalizedName.ToLowerInvariant();
                
                return string.Empty;
            }
        }
    }
}
