using System;
using Grpc.Core;
using II.RPA.Communication.Exceptions;
using II.RPA.Communication.Options;
using II.RPA.Communication.Proto;
using II.RPA.Communication.Timeout;
using Xunit;

namespace II.RPA.Communication.Tests
{
    public class TimeoutAwareCallInvokerTests
    {
        [Fact]
        public void BlockingUnaryCall_WithoutDeadline_DoesNotAddDeadline()
        {
            var inner = new EchoCallInvoker();
            var response = CreateInvoker(inner).BlockingUnaryCall(
                EchoCallInvoker.EchoMethod, null, new CallOptions(), "ping");

            Assert.Equal("echo:ping", response);
            Assert.Null(inner.LastOptions.Deadline);
        }

        [Fact]
        public async Task AsyncUnaryCall_WithoutDeadline_DoesNotAddDeadline()
        {
            var inner = new EchoCallInvoker();
            using var call = CreateInvoker(inner).AsyncUnaryCall(
                EchoCallInvoker.EchoMethod, null, new CallOptions(), "ping");

            Assert.Equal("echo:ping", await call.ResponseAsync);
            Assert.Null(inner.LastOptions.Deadline);
        }

        [Fact]
        public async Task AsyncUnaryCall_WithConfiguredDefaultTimeout_StillTimesOut()
        {
            var invoker = new TimeoutAwareCallInvoker(new CancelledOnCallCancellationInvoker(),
                new TimeoutResolver(new TimeoutOptions { DefaultRequestTimeout = TimeSpan.FromMilliseconds(50) }));
            using var call = invoker.AsyncUnaryCall(
                EchoCallInvoker.EchoMethod, null, new CallOptions(), "ping");

            var exception = await Assert.ThrowsAsync<RpcException>(async () =>
                await call.ResponseAsync.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.Equal(StatusCode.DeadlineExceeded, exception.StatusCode);
        }

        [Fact]
        public void BlockingUnaryCall_WithInfiniteDeadline_ReturnsInnerResponse()
        {
            var inner = new EchoCallInvoker();
            var invoker = CreateInvoker(inner);

            var response = invoker.BlockingUnaryCall(
                EchoCallInvoker.EchoMethod,
                null,
                new CallOptions(deadline: DateTime.MaxValue),
                "ping");

            Assert.Equal("echo:ping", response);
            Assert.True(inner.WasInvoked);
        }

        [Fact]
        public void BlockingUnaryCall_WithDeadlineBeyondCancelAfterLimit_ReturnsInnerResponse()
        {
            var inner = new EchoCallInvoker();
            var invoker = CreateInvoker(inner);

            var response = invoker.BlockingUnaryCall(
                EchoCallInvoker.EchoMethod,
                null,
                new CallOptions(deadline: DateTime.UtcNow.AddDays(60)),
                "ping");

            Assert.Equal("echo:ping", response);
            Assert.True(inner.WasInvoked);
        }

        [Fact]
        public async Task AsyncUnaryCall_WithInfiniteDeadline_ReturnsInnerResponse()
        {
            var inner = new EchoCallInvoker();
            var invoker = CreateInvoker(inner);

            var call = invoker.AsyncUnaryCall(
                EchoCallInvoker.EchoMethod,
                null,
                new CallOptions(deadline: DateTime.MaxValue),
                "ping");

            var response = await call.ResponseAsync;

            Assert.Equal("echo:ping", response);
            Assert.True(inner.WasInvoked);
            call.Dispose();
        }

        private static TimeoutAwareCallInvoker CreateInvoker(CallInvoker inner)
        {
            return new TimeoutAwareCallInvoker(inner, new TimeoutResolver(new TimeoutOptions()));
        }

        [Fact]
        public async Task AsyncUnaryCall_WithExpiredDeadline_ReportsTimeoutWithDuration()
        {
            var invoker = CreateInvoker(new CancelledOnCallCancellationInvoker());
            var call = invoker.AsyncUnaryCall(
                EchoCallInvoker.EchoMethod,
                null,
                new CallOptions(deadline: DateTime.UtcNow.AddMilliseconds(50)),
                "ping");

            try
            {
                var exception = await Record.ExceptionAsync(async () => await call.ResponseAsync);

                var rpcException = Assert.IsType<RpcException>(exception);
                Assert.Equal(StatusCode.DeadlineExceeded, rpcException.StatusCode);
                Assert.Contains("timed out after", rpcException.Message, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                call.Dispose();
            }
        }

        [Fact]
        public async Task AsyncUnaryCall_WhenCallerCancels_IsNotReportedAsTimeout()
        {
            var invoker = CreateInvoker(new CancelledOnCallCancellationInvoker());
            using var callerCancellation = new CancellationTokenSource();
            var call = invoker.AsyncUnaryCall(
                EchoCallInvoker.EchoMethod,
                null,
                new CallOptions(
                    cancellationToken: callerCancellation.Token),
                "ping");

            try
            {
                callerCancellation.CancelAfter(50);
                var exception = await Record.ExceptionAsync(async () => await call.ResponseAsync);

                Assert.NotNull(exception);
                Assert.False(
                    exception is RpcException { StatusCode: StatusCode.DeadlineExceeded },
                    "a caller cancellation must not be converted into a deadline exceeded timeout");
                Assert.False(
                    GRPCExceptionHelper.IsTimeout(exception!),
                    "a caller cancellation must not be classified as a timeout");
            }
            finally
            {
                call.Dispose();
            }
        }

        /// <summary>
        /// Test double that reproduces the named-pipe transport behaviour: the call only completes,
        /// with a Cancelled status, once the effective cancellation token fires. That is what real runs
        /// surface as "Status(StatusCode="Cancelled", Detail="")" in the logs.
        /// </summary>
        private sealed class CancelledOnCallCancellationInvoker : CallInvoker
        {
            public override TResponse BlockingUnaryCall<TRequest, TResponse>(
                Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
            {
                throw new NotSupportedException();
            }

            public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
                Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
            {
                var response = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
                options.CancellationToken.Register(
                    () => response.TrySetException(new RpcException(new Status(StatusCode.Cancelled, string.Empty))));

                return new AsyncUnaryCall<TResponse>(
                    response.Task,
                    Task.FromResult(new Metadata()),
                    () => new Status(StatusCode.Cancelled, string.Empty),
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
        }
    }
}
