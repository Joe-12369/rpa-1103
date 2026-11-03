using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;
using II.RPA.IPC;
using II.RPA.IPC.Config;
using Xunit;
using Xunit.Abstractions;

namespace II.RPA.NativeMessagingHost.IntegrationTests
{
    public sealed class NmhBlackBoxTests
    {
        private readonly ITestOutputHelper output;
        public NmhBlackBoxTests(ITestOutputHelper value) { output = value; }

        [Fact]
        public async Task RealNmhSupportsFourClientsConcurrentOutOfOrderRequests()
        {
            using (NmhProcess host = NmhProcess.Start())
            {
                await Task.Delay(500);
                host.EnsureRunning();
                PipeSession[] clients = Enumerable.Range(0, 4).Select(_ => new PipeSession(host.PipeName)).ToArray();
                try
                {
                    await Task.WhenAll(clients.Select(x => x.ConnectAsync()));
                    await Task.WhenAll(clients.Select(x => x.PingAsync()));
                    var requests = new List<Task<Tuple<JObject, double>>>();
                    for (int c = 0; c < 4; c++) for (int n = 0; n < 500; n++) requests.Add(clients[c].RequestAsync(c, n));
                    Tuple<JObject, double>[] results = await Task.WhenAll(requests);
                    Assert.Equal(2000, results.Length);
                    Assert.All(results, result => Assert.Equal(200, result.Item1["CallbackReslut"]?["code"]?.Value<int>()));
                    Assert.InRange(host.MaxActiveResponses, 2, 32);
                    Report("concurrent-2000", results.Select(x => x.Item2).ToArray());
                }
                finally { foreach (PipeSession item in clients) item.Dispose(); }
            }
        }

        [Fact]
        public async Task RealNmhReportsLowContentionRoundTripLatency()
        {
            using (NmhProcess host = NmhProcess.Start())
            {
                await Task.Delay(500); host.EnsureRunning();
                using (var client = new PipeSession(host.PipeName))
                {
                    await client.ConnectAsync();
                    var values = new List<double>();
                    for (int i = 0; i < 500; i++) values.Add((await client.RequestAsync(0, i, 5000, 0)).Item2);
                    Report("low-contention-500", values.ToArray());
                }
            }
        }

        [Fact]
        public async Task RealNmhLimitsOneConnectionToEightActiveRequests()
        {
            using (NmhProcess host = NmhProcess.Start())
            {
                await Task.Delay(500); host.EnsureRunning();
                using (var client = new PipeSession(host.PipeName))
                {
                    await client.ConnectAsync();
                    Task<Tuple<JObject, double>>[] requests = Enumerable.Range(0, 40).Select(i => client.RequestAsync(0, i, 5000, 100)).ToArray();
                    await Task.WhenAll(requests);
                    Assert.InRange(host.MaxActiveResponses, 2, 8);
                }
            }
        }

        [Fact]
        public async Task RealNmhReturnsSameConnectionResponsesOutOfOrderByMessageId()
        {
            using (NmhProcess host = NmhProcess.Start())
            {
                await Task.Delay(500); host.EnsureRunning();
                using (var client = new PipeSession(host.PipeName))
                {
                    await client.ConnectAsync();
                    Task<Tuple<JObject, double>> slow = client.RequestAsync(0, 1, 5000, 300);
                    await Task.Delay(20);
                    Task<Tuple<JObject, double>> fast = client.RequestAsync(0, 2, 5000, 0);
                    Assert.Same(fast, await Task.WhenAny(slow, fast));
                    Assert.Equal(2, (await fast).Item1["SendData"]?["sequence"]?.Value<int>());
                    Assert.Equal(1, (await slow).Item1["SendData"]?["sequence"]?.Value<int>());
                }
            }
        }

        [Fact]
        public async Task RealNmhSupportsRapidReconnectAndDirectExit()
        {
            using (NmhProcess host = NmhProcess.Start())
            {
                await Task.Delay(500);
                host.EnsureRunning();
                for (int cycle = 0; cycle < 100; cycle++)
                    using (var client = new PipeSession(host.PipeName)) { await client.ConnectAsync(); Assert.Equal(cycle, (await client.RequestAsync(0, cycle)).Item1["SendData"]?["sequence"]?.Value<int>()); }
                await host.SendExitFrameAsync();
                Assert.Equal(1, await host.WaitForExitAsync(TimeSpan.FromSeconds(10)));
            }
        }

        [Fact]
        public async Task RealNmhReleasesDisconnectedRequestsAndReportsCrxTimeout()
        {
            using (NmhProcess host = NmhProcess.Start())
            {
                await Task.Delay(500); host.EnsureRunning();
                var abandoned = new PipeSession(host.PipeName);
                await abandoned.ConnectAsync();
                Task<Tuple<JObject, double>> abandonedRequest = abandoned.RequestAsync(0, 1, 5000, 1000);
                await Task.Delay(50);
                abandoned.Dispose();
                await Assert.ThrowsAnyAsync<Exception>(() => abandonedRequest);

                using (var replacement = new PipeSession(host.PipeName))
                {
                    await replacement.ConnectAsync();
                    JObject timeout = (await replacement.RequestAsync(1, 2, 100, 500)).Item1;
                    Assert.Equal(-110005, timeout["CallbackReslut"]?["result"]?["error"]?["code"]?.Value<int>());
                    Assert.Equal(200, (await replacement.RequestAsync(1, 3)).Item1["CallbackReslut"]?["code"]?.Value<int>());
                }
            }
        }

        [Fact]
        public async Task RealNmhKeepsWcfIpcAndNativePipeAvailableTogether()
        {
            using (NmhProcess host = NmhProcess.Start())
            {
                await Task.Delay(500); host.EnsureRunning();
                string address = "net.pipe://localhost/SZ_RPAService/" + NmhProcess.ServiceKey + "_" + host.Id;
                using (IClient<IMessageExchange> ipc = ClientGenerator.Instance.Generate<IMessageExchange>(PipeConfig.DefaultPipe, address))
                {
                    ipc.Service.Pending();
                    var request = new JObject { ["MessageId"] = Guid.NewGuid().ToString("N"), ["Action"] = "SendTransferMessage", ["SendData"] = new JObject { ["source"] = "ipc" }, ["Timeout"] = 5000, ["Timestamp"] = System.Diagnostics.Stopwatch.GetTimestamp() };
                    IResponseResult response = ipc.Service.Request(new RequestActionParams { Action = "Request", Parameters = request.ToString() });
                    Assert.Contains("\"code\":200", response.Result.Replace(" ", string.Empty));
                }
                using (var pipe = new PipeSession(host.PipeName)) { await pipe.ConnectAsync(); Assert.Equal(200, (await pipe.RequestAsync(2, 4)).Item1["CallbackReslut"]?["code"]?.Value<int>()); }
            }
        }

        [Fact]
        public async Task RealNmhRemainsNamedPipeOnlyWhenWcfStartupCannotSucceed()
        {
            string serviceKey = "nmh_ipc_failure_#_" + Guid.NewGuid().ToString("N");
            using (NmhProcess host = NmhProcess.Start(serviceKey))
            {
                await Task.Delay(500); host.EnsureRunning();
                using (var client = new PipeSession(host.PipeName))
                {
                    await client.ConnectAsync();
                    Assert.Equal(200, (await client.RequestAsync(3, 1)).Item1["CallbackReslut"]?["code"]?.Value<int>());
                    await Task.Delay(4000);
                    host.EnsureRunning();
                    Assert.Equal(200, (await client.RequestAsync(3, 2)).Item1["CallbackReslut"]?["code"]?.Value<int>());
                }
            }
        }

        [Fact]
        public async Task RealNmhCanRestartFiftyTimesWithoutStaleEndpoint()
        {
            for (int cycle = 0; cycle < 50; cycle++)
            {
                using (NmhProcess host = NmhProcess.Start())
                {
                    await Task.Delay(100); host.EnsureRunning();
                    using (var client = new PipeSession(host.PipeName)) { await client.ConnectAsync(); Assert.Equal(cycle, (await client.RequestAsync(0, cycle)).Item1["SendData"]?["sequence"]?.Value<int>()); }
                    await host.SendExitFrameAsync();
                    Assert.Equal(1, await host.WaitForExitAsync(TimeSpan.FromSeconds(10)));
                }
            }
        }

        private void Report(string name, double[] values)
        {
            Array.Sort(values);
            Func<double, double> p = value => values[Math.Min(values.Length - 1, (int)Math.Ceiling(values.Length * value) - 1)];
            string report = $"name={name} count={values.Length} mean={values.Average():F3}ms p50={p(.5):F3}ms p95={p(.95):F3}ms p99={p(.99):F3}ms max={values.Last():F3}ms";
            output.WriteLine(report);
            Console.WriteLine(report);
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "ii-rpa-nmh-blackbox-latency-" + name + ".txt"), report);
        }
    }
}
