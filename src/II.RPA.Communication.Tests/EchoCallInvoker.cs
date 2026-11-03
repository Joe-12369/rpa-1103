using System;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;

namespace II.RPA.Communication.Tests
{
    /// <summary>
    /// 测试替身：充当底层传输，记录是否被调用并原样返回 "echo:{request}"。
    /// </summary>
    internal sealed class EchoCallInvoker : CallInvoker
    {
        public static readonly Method<string, string> EchoMethod = new Method<string, string>(
            MethodType.Unary,
            "ii.rpa.tests.Echo",
            "Echo",
            Marshallers.Create(s => Encoding.UTF8.GetBytes(s), b => Encoding.UTF8.GetString(b)),
            Marshallers.Create(s => Encoding.UTF8.GetBytes(s), b => Encoding.UTF8.GetString(b)));

        public bool WasInvoked { get; private set; }

        public CallOptions LastOptions { get; private set; }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
        {
            Record(options);
            return (TResponse)(object)$"echo:{request}";
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
        {
            Record(options);
            var response = (TResponse)(object)$"echo:{request}";
            return new AsyncUnaryCall<TResponse>(
                Task.FromResult(response),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
        {
            throw new NotSupportedException();
        }

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options)
        {
            throw new NotSupportedException();
        }

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options)
        {
            throw new NotSupportedException();
        }

        private void Record(CallOptions options)
        {
            WasInvoked = true;
            LastOptions = options;
        }
    }
}
