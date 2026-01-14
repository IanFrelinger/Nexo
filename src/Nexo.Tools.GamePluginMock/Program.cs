using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

var services = new ServiceCollection()
    .AddLogging(b => b.AddSimpleConsole(o =>
    {
        o.SingleLine = true;
        o.TimestampFormat = "HH:mm:ss ";
    }))
    .BuildServiceProvider();

var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("GamePluginMock");

var port = 9999;
if (args.Length > 0 && int.TryParse(args[0], out var parsed) && parsed > 0) port = parsed;

var listener = new TcpListener(IPAddress.Loopback, port);
listener.Start();
logger.LogInformation("Mock Nexo Unity plugin listening on 127.0.0.1:{Port}", port);

while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = Task.Run(async () =>
    {
        try
        {
            logger.LogInformation("Client connected");
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };

            while (client.Connected)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;

                var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var cmd = parts.Length > 0 ? parts[0].Trim().ToUpperInvariant() : "";
                var arg = parts.Length > 1 ? parts[1] : "";

                switch (cmd)
                {
                    case "HELLO":
                        await writer.WriteLineAsync("NEXO_PLUGIN 1.0");
                        break;
                    case "PING":
                        await writer.WriteLineAsync("PONG");
                        break;
                    case "SCREENSHOT":
                        await writer.WriteLineAsync("DATA:" + Convert.ToBase64String(CreatePng()));
                        break;
                    case "GAMESTATE":
                        await writer.WriteLineAsync(JsonSerializer.Serialize(new
                        {
                            SceneName = "MockScene",
                            IsPaused = false,
                            TimeScale = 1.0,
                            Score = 0,
                            Level = 1
                        }));
                        break;
                    case "PLAYERSTATE":
                        await writer.WriteLineAsync(JsonSerializer.Serialize(new
                        {
                            Position = new { X = 0, Y = 0, Z = 0 },
                            Rotation = new { X = 0, Y = 0, Z = 0 },
                            Health = 100,
                            Mana = 50,
                            Inventory = Array.Empty<string>()
                        }));
                        break;
                    case "INTERACTABLES":
                        await writer.WriteLineAsync(JsonSerializer.Serialize(new[]
                        {
                            new {
                                Id = "mock-door-1",
                                Type = "Door",
                                Label = "Mock Door",
                                Description = "A door you can interact with",
                                Bounds = (object?)null,
                                IsVisible = true,
                                IsEnabled = true,
                                CurrentValue = (string?)null,
                                PossibleActions = new[]{"interact"}
                            }
                        }));
                        break;
                    case "INTERACT":
                        await writer.WriteLineAsync($"OK INTERACT {arg}");
                        break;
                    case "CLICK":
                        await writer.WriteLineAsync($"OK CLICK {arg}");
                        break;
                    case "MOVE":
                    case "LOOK":
                    case "INPUT":
                    case "KEY":
                    case "USEITEM":
                        await writer.WriteLineAsync($"OK {line}");
                        break;
                    case "PERFORMANCE":
                        await writer.WriteLineAsync(JsonSerializer.Serialize(new
                        {
                            Fps = 60,
                            CpuPercent = 5.0,
                            MemoryUsageMb = 128,
                            NetworkLatencyMs = 0
                        }));
                        break;
                    default:
                        await writer.WriteLineAsync($"ERR UnknownCommand {cmd}");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Client handler crashed");
        }
        finally
        {
            try { client.Dispose(); } catch { /* ignore */ }
            logger.LogInformation("Client disconnected");
        }
    });
}

static byte[] CreatePng()
{
    using var img = new Image<Rgba32>(2, 2);
    img[0, 0] = new Rgba32(255, 0, 0);
    img[1, 0] = new Rgba32(0, 255, 0);
    img[0, 1] = new Rgba32(0, 0, 255);
    img[1, 1] = new Rgba32(255, 255, 255);

    using var ms = new MemoryStream();
    img.Save(ms, new PngEncoder());
    return ms.ToArray();
}

