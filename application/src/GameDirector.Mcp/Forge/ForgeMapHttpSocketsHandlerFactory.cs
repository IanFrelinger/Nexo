using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nexo.API.Forge;

/// <summary>
/// Factory for <see cref="SocketsHttpHandler"/> used by the forge-map <see cref="HttpClient"/>,
/// including optional connect-time IP validation against private ranges.
/// </summary>
internal static class ForgeMapHttpSocketsHandlerFactory
{
    public static SocketsHttpHandler Create(
        IOptionsMonitor<ForgeSessionOptions> optionsMonitor,
        ILoggerFactory loggerFactory)
    {
        var log = loggerFactory.CreateLogger("ForgeMapHttp");
        var opts = optionsMonitor.CurrentValue;

        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = opts.AllowHttpRedirectsForMapFetch,
            ConnectCallback = async (context, cancellationToken) =>
            {
                var optsNow = optionsMonitor.CurrentValue;
                var uri = context.InitialRequestMessage.RequestUri;
                if (uri is null)
                    throw new InvalidOperationException("Request URI is required.");

                if (!ForgeMapFetchUrlValidator.TryValidate(uri.ToString(), optsNow, out var err))
                    throw new HttpRequestException(err ?? "Invalid map fetch URL.");

                var host = context.DnsEndPoint.Host;
                var port = context.DnsEndPoint.Port;

                IPAddress[] addresses;
                try
                {
                    addresses = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new HttpRequestException($"DNS resolution failed for '{host}'.", ex);
                }

                Exception? last = null;
                foreach (var address in addresses.OrderBy(a => a.AddressFamily == AddressFamily.InterNetwork ? 0 : 1))
                {
                    if (ForgeMapFetchUrlValidator.IsBlockedIp(address))
                        continue;

                    try
                    {
                        var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                        {
                            NoDelay = true
                        };
                        await socket.ConnectAsync(address, port, cancellationToken).ConfigureAwait(false);
                        log.LogDebug("Forge map fetch connected to {Host} via {Address}", host, address);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch (Exception ex)
                    {
                        last = ex;
                    }
                }

                throw new HttpRequestException(
                    $"Could not connect to '{host}:{port}' with a permitted address." +
                    (last is not null ? " " + last.Message : string.Empty),
                    last);
            }
        };

        return handler;
    }
}
