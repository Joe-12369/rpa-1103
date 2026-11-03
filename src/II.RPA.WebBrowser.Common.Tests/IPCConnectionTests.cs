using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.connection;
using II.RPA.WebBrowser.Common.Basic.connection.ClientTransfer;
using System;
using System.Collections.Generic;
using Xunit;

namespace II.RPA.WebBrowser.Common.Tests
{
    public class IPCConnectionTests
    {
        [Fact]
        public void ConstructionAndIsConnectedReadPerformNoWcfIo()
        {
            var probes = new ScriptedProbeFactory();
            var clock = new FakeWaitClock();
            var connection = new IPCConnection("net.pipe://nmh", probes, clock);

            Assert.False(connection.IsConnected);
            Assert.False(connection.IsConnected);
            Assert.Equal(0, probes.CreateCount);
        }

        [Fact]
        public void StartRetriesPendingUntilReadyAndDisposesEveryProbe()
        {
            var probes = new ScriptedProbeFactory(
                new InvalidOperationException("not ready"),
                new InvalidOperationException("still starting"),
                null);
            var clock = new FakeWaitClock();
            var connection = new IPCConnection("net.pipe://nmh", probes, clock)
            {
                ConnectTimeout = 500
            };

            connection.Start();

            Assert.True(connection.IsConnected);
            Assert.Equal(3, probes.CreateCount);
            Assert.Equal(3, probes.DisposeCount);
            Assert.Equal(new[] { 500, 450, 400 }, probes.PendingTimeouts);
        }

        [Fact]
        public void StartUsesOneTotalBudgetAndFailsClosedAtDeadline()
        {
            var probes = new ScriptedProbeFactory(
                new InvalidOperationException("not ready"),
                new InvalidOperationException("not ready"),
                new InvalidOperationException("not ready"));
            var clock = new FakeWaitClock();
            var connection = new IPCConnection("net.pipe://nmh", probes, clock)
            {
                ConnectTimeout = 120
            };

            BrowserConnectionException error =
                Assert.Throws<BrowserConnectionException>(() => connection.Start());

            Assert.False(connection.IsConnected);
            Assert.Equal(ClientTransferCompletionReason.ConnectionClosed, error.Reason);
            Assert.Equal(3, probes.CreateCount);
            Assert.Equal(3, probes.DisposeCount);
            Assert.Equal(new[] { 120, 70, 20 }, probes.PendingTimeouts);
            Assert.Equal(120, clock.ElapsedMilliseconds);
        }

        [Fact]
        public void TestConnectionPerformsExactlyOneShortProbe()
        {
            var probes = new ScriptedProbeFactory();
            var connection = new IPCConnection(
                "net.pipe://nmh",
                probes,
                new FakeWaitClock());

            bool result = connection.TestConnection();

            Assert.True(result);
            Assert.Equal(1, probes.CreateCount);
            Assert.Equal(1, probes.DisposeCount);
            Assert.Equal(new[] { 500 }, probes.PendingTimeouts);
            Assert.False(connection.IsConnected);
        }

        [Fact]
        public void StopClearsReadyStateWithoutWcfIo()
        {
            var probes = new ScriptedProbeFactory();
            var connection = new IPCConnection(
                "net.pipe://nmh",
                probes,
                new FakeWaitClock())
            {
                ConnectTimeout = 500
            };
            connection.Start();
            int createCountAfterStart = probes.CreateCount;

            connection.Stop();

            Assert.False(connection.IsConnected);
            Assert.Equal(createCountAfterStart, probes.CreateCount);
        }

        [Theory]
        [InlineData("getVersion")]
        [InlineData("getWindowMap")]
        public void BuiltInCommandDoesNotRequireInitializedScript(string command)
        {
            Assert.False(IPCConnection.RequiresInitializedScript(command));
        }

        [Fact]
        public void PageCommandStillRequiresInitializedScript()
        {
            Assert.True(IPCConnection.RequiresInitializedScript("elements.click"));
        }

        private sealed class ScriptedProbeFactory : IWcfConnectionProbeFactory
        {
            private readonly Queue<Exception> _results;

            internal ScriptedProbeFactory(params Exception[] results)
            {
                _results = new Queue<Exception>(results ?? Array.Empty<Exception>());
            }

            internal int CreateCount { get; private set; }

            internal int DisposeCount { get; private set; }

            internal List<int> PendingTimeouts { get; } = new List<int>();

            public IWcfConnectionProbe Create(string address)
            {
                CreateCount++;
                Exception result = _results.Count == 0 ? null : _results.Dequeue();
                return new Probe(this, result);
            }

            private sealed class Probe : IWcfConnectionProbe
            {
                private readonly ScriptedProbeFactory _owner;
                private readonly Exception _result;

                internal Probe(ScriptedProbeFactory owner, Exception result)
                {
                    _owner = owner;
                    _result = result;
                }

                public void Pending(int timeoutMilliseconds)
                {
                    _owner.PendingTimeouts.Add(timeoutMilliseconds);
                    if (_result != null)
                    {
                        throw _result;
                    }
                }

                public void Dispose()
                {
                    _owner.DisposeCount++;
                }
            }
        }

        private sealed class FakeWaitClock : IConnectionWaitClock
        {
            public long ElapsedMilliseconds { get; private set; }

            public void Restart()
            {
                ElapsedMilliseconds = 0;
            }

            public void Wait(int milliseconds)
            {
                ElapsedMilliseconds += milliseconds;
            }
        }
    }
}
