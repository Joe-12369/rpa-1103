using System.Reflection;
using System.Runtime.CompilerServices;
using Grpc.Core;
using Grpc.Core.Interceptors;
using II.RPA.Core.GRPCService.Service;
using Rpa.Core.Plugin;
using Xunit;

namespace II.RPA.Core.Talker.Tests;

public sealed class PluginGRPCServiceBindingTests
{
    [Fact]
    public async Task BindingToServiceDoesNotApplyUnaryInterceptor()
    {
        var interceptor = new RecordingInterceptor();
        var service = (PluginGRPCService)RuntimeHelpers.GetUninitializedObject(typeof(PluginGRPCService));
        typeof(PluginGRPCService)
            .GetField("m_ServicePollyInterceptor", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(service, interceptor);

        var definition = service.BindingToService();
        var binder = new CapturingServiceBinder();
        typeof(ServerServiceDefinition)
            .GetMethod("BindService", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(definition, new object[] { binder });

        var handler = Assert.IsType<UnaryServerMethod<EnvironmentDiagnosticRequest, EnvironmentDiagnosticResponse>>(
            binder.UnaryHandlers["EnvironmentDiagnosticUnary"]);
        await handler(new EnvironmentDiagnosticRequest(), null!);

        Assert.Equal(0, interceptor.UnaryCalls);
    }

    private sealed class RecordingInterceptor : Interceptor
    {
        public int UnaryCalls { get; private set; }

        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
            where TRequest : class
            where TResponse : class
        {
            UnaryCalls++;
            return continuation(request, context);
        }
    }

    private sealed class CapturingServiceBinder : ServiceBinderBase
    {
        public Dictionary<string, Delegate?> UnaryHandlers { get; } = new();

        public override void AddMethod<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            UnaryServerMethod<TRequest, TResponse>? handler)
            where TRequest : class
            where TResponse : class
        {
            UnaryHandlers[method.Name] = handler;
        }

        public override void AddMethod<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            ClientStreamingServerMethod<TRequest, TResponse>? handler)
            where TRequest : class
            where TResponse : class
        {
        }

        public override void AddMethod<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            ServerStreamingServerMethod<TRequest, TResponse>? handler)
            where TRequest : class
            where TResponse : class
        {
        }

        public override void AddMethod<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            DuplexStreamingServerMethod<TRequest, TResponse>? handler)
            where TRequest : class
            where TResponse : class
        {
        }
    }
}
