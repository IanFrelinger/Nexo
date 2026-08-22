using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ashlar.Tests.Transport;

/// <summary>Grpc server call context factory.</summary>
internal static class GrpcServerCallContextFactory
{
    private static readonly Type HttpContextServerCallContextType =
        Assembly.Load("Grpc.AspNetCore.Server")
            .GetType("Grpc.AspNetCore.Server.Internal.HttpContextServerCallContext", throwOnError: true)!;

    private static readonly FieldInfo RequestHeadersField =
        HttpContextServerCallContextType.GetField("_requestHeaders", BindingFlags.Instance | BindingFlags.NonPublic)
        /// <summary>Invalid operation exception.</summary>
        /// <param name="field."">Field.".</param>
        ?? throw new InvalidOperationException("Could not locate gRPC request headers field.");

    public static ServerCallContext Create(Metadata requestHeaders, X509Certificate2? clientCertificate = null)
    {
        var httpContext = new DefaultHttpContext();
        if (clientCertificate is not null)
            httpContext.Connection.ClientCertificate = clientCertificate;

        var context = (ServerCallContext)Activator.CreateInstance(
            HttpContextServerCallContextType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { httpContext, null, typeof(object), typeof(object), NullLogger.Instance },
            culture: null)!;

        RequestHeadersField.SetValue(context, requestHeaders);
        return context;
    }
}
