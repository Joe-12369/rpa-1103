using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Owin;
using System;
using System.IO;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.ZBrainProxyProvider
{
    public static class Extensions
    {
        public static async Task<string> ReadFromRequest(this IOwinRequest request)
        {
            string body = string.Empty;
            using (var reader = new StreamReader(request.Body))
            {
                body = await reader.ReadToEndAsync();
            }
            return body;
        }

        public static async Task<string> GetFromRequest(this IOwinRequest request, string key)
        {
            if (request.Method == "GET")
            {
                return request.Query.Get(key);
            }
            else if ("application/json" == request.ContentType)
            {
                var body = await request.ReadFromRequest();
                var json = JObject.Parse(body);
                json.TryGetValue(key, out var value);
                return value.ToString();
            }
            return default;
        }

        public static async Task WriteResponse(
            this IOwinResponse response,
            object body,
            bool isSuccess = true)
        {
            response.StatusCode = isSuccess ? 200 : 500;
            response.ContentType = "application/json;charset=UTF-8";
            await response.WriteAsync(JsonConvert.SerializeObject(body,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                }));
        }

        public static Task WriteErrorResponse(
            this IOwinResponse response,
            string message)
        {
            return response.WriteResponse(new ZBrainResponse<string>
            {
                Message = message,
            }, false);
        }

        public static void SafeRun(this IAppBuilder app, Func<IOwinContext, Task> handler)
        {
            Func<IOwinContext, Task> wrapper = async (context) =>
            {
                try
                {
                    await handler(context);
                }
                catch (Exception ex)
                {
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(ex,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        }));
                }
            };
            app.Run(wrapper);
        }

        //public static IOwinContext ToOwinContext(this HttpContext httpContext)
        //{
        //    var environment = new Dictionary<string, object>
        //    {
        //        ["owin.Version"] = "1.8",
        //        ["owin.RequestScheme"] = httpContext.Request.Scheme,
        //        ["owin.RequestMethod"] = httpContext.Request.Method,
        //        ["owin.RequestPath"] = httpContext.Request.Path.Value,
        //        ["owin.RequestPathBase"] = httpContext.Request.PathBase.Value,
        //        ["owin.RequestQueryString"] = httpContext.Request.QueryString.Value,
        //        ["owin.RequestProtocol"] = httpContext.Request.Protocol,
        //        ["owin.RequestHeaders"] = new Dictionary<string, StringValues>(httpContext.Request.Headers.ToDictionary(h => h.Key, h => (StringValues)h.Value)),
        //        ["owin.RequestBody"] = httpContext.Request.Body,
        //        ["owin.ResponseBody"] = httpContext.Response.Body,
        //        ["owin.ResponseHeaders"] = new Dictionary<string, StringValues>(httpContext.Response.Headers.ToDictionary(h => h.Key, h => (StringValues)h.Value)),
        //        ["owin.CallCancelled"] = httpContext.RequestAborted,
        //        ["owin.RequestId"] = httpContext.TraceIdentifier,
        //        ["server.RemoteIpAddress"] = httpContext.Connection.RemoteIpAddress,
        //        ["server.LocalIpAddress"] = httpContext.Connection.LocalIpAddress
        //    };

        //    return new OwinContext(environment);
        //}

        //public static async Task WriteResponseAsync(this HttpContext context, object value, Encoding encoding = null)
        //{
        //    if (value == null)
        //    {
        //        throw new ArgumentNullException(nameof(value), "Value cannot be null.");
        //    }
        //    encoding ??= Encoding.UTF8;
        //    context.Response.ContentType = "application/json; charset=utf-8";
        //    string json = JsonConvert.SerializeObject(value);
        //    await context.Response.WriteAsync(json, encoding);
        //}
    }
}
