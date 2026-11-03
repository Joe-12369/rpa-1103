using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Microsoft.Owin.Builder;
using Owin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.ZBrainProxyProvider
{
    /// <summary>
    /// Hosts the existing transport-neutral OWIN route pipeline on Kestrel.
    /// Microsoft.Owin.Host.HttpListener registers its HTTP.sys prefix but does not
    /// dispatch requests reliably when Core runs on the current .NET runtime.
    /// </summary>
    internal sealed class CoreTalkerKestrelOwinServer : IDisposable
    {
        private readonly IHost m_Host;
        private readonly Func<IDictionary<string, object>, Task> m_OwinApplication;
        private bool m_Disposed;

        public CoreTalkerKestrelOwinServer(int port, Action<IAppBuilder> configure)
        {
            if (port <= 0 || port > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var owinBuilder = new AppBuilder();
            configure(owinBuilder);
            m_OwinApplication =
                owinBuilder.Build<Func<IDictionary<string, object>, Task>>();

            m_Host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webHost => webHost
                    .UseKestrel(options => options.Listen(IPAddress.Loopback, port))
                    .Configure(application => application.Run(InvokeAsync)))
                .Build();
            m_Host.Start();
        }

        private async Task InvokeAsync(HttpContext context)
        {
            using (var responseBody = new MemoryStream())
            {
                var responseHeaders =
                    new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                var environment = CreateOwinEnvironment(
                    context,
                    responseBody,
                    responseHeaders);

                await m_OwinApplication(environment).ConfigureAwait(false);

                context.Response.StatusCode = GetEnvironmentValue(
                    environment,
                    "owin.ResponseStatusCode",
                    StatusCodes.Status200OK);
                if (environment.TryGetValue("owin.ResponseReasonPhrase", out var reason) &&
                    reason is string reasonPhrase &&
                    !string.IsNullOrWhiteSpace(reasonPhrase))
                {
                    context.Features
                        .Get<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>()
                        .ReasonPhrase = reasonPhrase;
                }

                foreach (var header in responseHeaders)
                {
                    if (!string.Equals(
                            header.Key,
                            "Transfer-Encoding",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.Headers[header.Key] =
                            new StringValues(header.Value);
                    }
                }

                responseBody.Position = 0;
                if (!context.Response.Headers.ContainsKey("Content-Length"))
                {
                    context.Response.ContentLength = responseBody.Length;
                }
                await responseBody
                    .CopyToAsync(context.Response.Body, context.RequestAborted)
                    .ConfigureAwait(false);
            }
        }

        private static IDictionary<string, object> CreateOwinEnvironment(
            HttpContext context,
            Stream responseBody,
            IDictionary<string, string[]> responseHeaders)
        {
            var request = context.Request;
            return new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["owin.Version"] = "1.0",
                ["owin.RequestScheme"] = request.Scheme,
                ["owin.RequestMethod"] = request.Method,
                ["owin.RequestPathBase"] = request.PathBase.Value ?? string.Empty,
                ["owin.RequestPath"] = request.Path.Value ?? string.Empty,
                ["owin.RequestQueryString"] =
                    request.QueryString.Value?.TrimStart('?') ?? string.Empty,
                ["owin.RequestProtocol"] = request.Protocol,
                ["owin.RequestHeaders"] = request.Headers.ToDictionary(
                    header => header.Key,
                    header => header.Value.ToArray(),
                    StringComparer.OrdinalIgnoreCase),
                ["owin.RequestBody"] = request.Body,
                ["owin.RequestUser"] = context.User ?? new ClaimsPrincipal(),
                ["owin.ResponseStatusCode"] = StatusCodes.Status200OK,
                ["owin.ResponseHeaders"] = responseHeaders,
                ["owin.ResponseBody"] = responseBody,
                ["owin.CallCancelled"] = context.RequestAborted,
                ["server.RemoteIpAddress"] =
                    context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                ["server.RemotePort"] = context.Connection.RemotePort.ToString(),
                ["server.LocalIpAddress"] =
                    context.Connection.LocalIpAddress?.ToString() ?? string.Empty,
                ["server.LocalPort"] = context.Connection.LocalPort.ToString(),
                ["server.IsLocal"] = context.Connection.RemoteIpAddress != null &&
                    IPAddress.IsLoopback(context.Connection.RemoteIpAddress)
            };
        }

        private static int GetEnvironmentValue(
            IDictionary<string, object> environment,
            string key,
            int defaultValue)
        {
            return environment.TryGetValue(key, out var value) && value is int intValue
                ? intValue
                : defaultValue;
        }

        public void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }
            m_Disposed = true;

            try
            {
                m_Host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            }
            finally
            {
                m_Host.Dispose();
            }
        }
    }
}
