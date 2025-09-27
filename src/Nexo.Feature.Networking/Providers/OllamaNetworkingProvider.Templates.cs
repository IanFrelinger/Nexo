using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Feature.Networking.Models;

namespace Nexo.Feature.Networking.Providers;

/// <summary>
/// Code template generation for OllamaNetworkingProvider.
/// </summary>
public partial class OllamaNetworkingProvider
{
    /// <summary>
    /// Generates server code template
    /// </summary>
    private string GenerateServerCodeTemplate(NetworkingConfiguration configuration)
    {
        return $@"
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Networking.Server
{{
    public class {configuration.Name}Server
    {{
        private TcpListener _listener;
        private bool _isRunning;
        private readonly int _port;
        private readonly int _maxPlayers;

        public {configuration.Name}Server(int port = {configuration.Server.Port}, int maxPlayers = {configuration.Server.MaxPlayers})
        {{
            _port = port;
            _maxPlayers = maxPlayers;
        }}

        public async Task StartAsync()
        {{
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _isRunning = true;

            Console.WriteLine($""Server started on port {{_port}} with max {{_maxPlayers}} players"");

            while (_isRunning)
            {{
                try
                {{
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client));
                }}
                catch (Exception ex)
                {{
                    Console.WriteLine($""Error accepting client: {{ex.Message}}"");
                }}
            }}
        }}

        private async Task HandleClientAsync(TcpClient client)
        {{
            // Handle client connection
            Console.WriteLine($""Client connected: {{client.Client.RemoteEndPoint}}"");
            
            // TODO: Implement client handling logic
            
            client.Close();
        }}

        public void Stop()
        {{
            _isRunning = false;
            _listener?.Stop();
        }}
    }}
}}";
    }

    /// <summary>
    /// Generates client code template
    /// </summary>
    private string GenerateClientCodeTemplate(NetworkingConfiguration configuration)
    {
        return $@"
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Networking.Client
{{
    public class {configuration.Name}Client
    {{
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isConnected;

        public async Task<bool> ConnectAsync(string serverIP = ""{configuration.Client.DefaultServerIP}"", int port = {configuration.Client.DefaultServerPort})
        {{
            try
            {{
                _client = new TcpClient();
                await _client.ConnectAsync(serverIP, port);
                _stream = _client.GetStream();
                _isConnected = true;

                Console.WriteLine($""Connected to server {{serverIP}}:{{port}}"");
                return true;
            }}
            catch (Exception ex)
            {{
                Console.WriteLine($""Failed to connect: {{ex.Message}}"");
                return false;
            }}
        }}

        public async Task SendMessageAsync(byte[] data)
        {{
            if (!_isConnected) return;

            try
            {{
                await _stream.WriteAsync(data, 0, data.Length);
            }}
            catch (Exception ex)
            {{
                Console.WriteLine($""Error sending message: {{ex.Message}}"");
                _isConnected = false;
            }}
        }}

        public void Disconnect()
        {{
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
        }}
    }}
}}";
    }
}
