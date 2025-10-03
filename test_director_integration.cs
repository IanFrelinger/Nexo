using UnityEngine;
using UnityEditor;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Director.Test
{
    /// <summary>
    /// Test script to validate Director Server functionality
    /// This can be run in Unity Editor to test the IPC bridge
    /// </summary>
    public class DirectorIntegrationTest : MonoBehaviour
    {
        [Header("Director Server Test")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private int port = 5088;
        
        private TcpListener _tcpListener;
        private bool _isRunning = false;
        private string _currentToken = "";

        void Start()
        {
            if (autoStart)
            {
                StartDirectorServer();
            }
        }

        [ContextMenu("Start Director Server")]
        public void StartDirectorServer()
        {
            if (_isRunning)
            {
                Debug.Log("Director Server already running");
                return;
            }

            try
            {
                _tcpListener = new TcpListener(IPAddress.Loopback, port);
                _tcpListener.Start();
                _isRunning = true;
                
                // Generate a test token
                _currentToken = Guid.NewGuid().ToString();
                
                Debug.Log($"Director Server started on port {port}");
                Debug.Log($"Test Token: {_currentToken}");
                Debug.Log("Avalonia app can now connect using this token");
                
                // Start accepting connections
                _ = Task.Run(AcceptConnectionsAsync);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start Director Server: {ex.Message}");
            }
        }

        [ContextMenu("Stop Director Server")]
        public void StopDirectorServer()
        {
            if (!_isRunning)
            {
                Debug.Log("Director Server not running");
                return;
            }

            try
            {
                _tcpListener?.Stop();
                _isRunning = false;
                Debug.Log("Director Server stopped");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error stopping Director Server: {ex.Message}");
            }
        }

        private async Task AcceptConnectionsAsync()
        {
            while (_isRunning)
            {
                try
                {
                    var tcpClient = await _tcpListener.AcceptTcpClientAsync();
                    Debug.Log("Client connected to Director Server");
                    
                    // Handle client in background
                    _ = Task.Run(() => HandleClientAsync(tcpClient));
                }
                catch (ObjectDisposedException)
                {
                    // Server was stopped
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error accepting connection: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                var stream = client.GetStream();
                var reader = new System.IO.StreamReader(stream, Encoding.UTF8);
                var writer = new System.IO.StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

                // Read authentication token
                var token = await reader.ReadLineAsync();
                if (token == _currentToken)
                {
                    Debug.Log("Client authenticated successfully");
                    
                    // Send welcome message
                    var welcomeMessage = "{\"Type\":\"ConnectionEvent\",\"Payload\":{\"Connected\":true,\"Message\":\"Connected to Unity Editor\"}}";
                    await writer.WriteLineAsync(welcomeMessage);
                    
                    // Keep connection alive and handle commands
                    await HandleClientCommandsAsync(reader, writer);
                }
                else
                {
                    Debug.LogWarning($"Invalid token received: {token}");
                    var errorMessage = "{\"Type\":\"ConnectionEvent\",\"Payload\":{\"Connected\":false,\"Message\":\"Invalid token\"}}";
                    await writer.WriteLineAsync(errorMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
                Debug.Log("Client disconnected");
            }
        }

        private async Task HandleClientCommandsAsync(System.IO.StreamReader reader, System.IO.StreamWriter writer)
        {
            try
            {
                while (true)
                {
                    var command = await reader.ReadLineAsync();
                    if (command == null) break;

                    Debug.Log($"Received command: {command}");
                    
                    // Echo back the command as a test
                    var response = $"{{\"Type\":\"LogLine\",\"Payload\":{{\"Line\":\"Echo: {command}\",\"Level\":0}}}}";
                    await writer.WriteLineAsync(response);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling commands: {ex.Message}");
            }
        }

        void OnDestroy()
        {
            StopDirectorServer();
        }

        void OnApplicationQuit()
        {
            StopDirectorServer();
        }
    }
}
