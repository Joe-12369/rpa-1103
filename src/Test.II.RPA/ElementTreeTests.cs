using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Deeps;
using II.RPA.Plugin.GUI.Deeps.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Drawing;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Xunit.Abstractions;

namespace Test.II.RPA
{
    public class ElementTreeTests
    {
        private readonly AutomationElement _targetElement;
        private readonly ElementService _elementService;
        private readonly ITestOutputHelper _testOutputHelper;
        private const int count = 5;
        public ElementTreeTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _elementService = Providers.ElementService;
            _targetElement = UiCommon.UIAAutomation.FromHandle(new IntPtr(2039330));
        }

        [Fact]
        public async Task BuildElementTree_PeerElement_Rx()
        {
            for (int i = 0; i < count; i++)
            {
                await Task.Delay(200);

                var targetAutomation = _targetElement;
                var sw = Stopwatch.StartNew();

                try
                {
                    _testOutputHelper.WriteLine($"开始构建 #{i}: {sw.ElapsedMilliseconds}ms");

                    // 先准备监听，再启动构建
                    var resultTask = _elementService.PeerStream
                        .FirstOrDefaultAsync()
                        .Timeout(TimeSpan.FromSeconds(5))
                        .ToTask();

                    // 启动异步构建
                    await _elementService.StartBuildTreeAsync(targetAutomation);

                    // 等待结果
                    var result = await resultTask;

                    _testOutputHelper.WriteLine($"完成构建 #{i}: {sw.ElapsedMilliseconds}ms");

                    if (result != null)
                    {
                        _testOutputHelper.WriteLine(JsonConvert.SerializeObject(result));
                    }
                    else
                    {
                        _testOutputHelper.WriteLine($"结果为空 #{i}");
                    }
                }
                catch (TimeoutException)
                {
                    _testOutputHelper.WriteLine($"等待结果超时 #{i}: {sw.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    _testOutputHelper.WriteLine($"异常 #{i}: {ex.Message}");
                }
                finally
                {
                    sw.Stop();
                    _testOutputHelper.WriteLine($"总耗时 #{i}: {sw.ElapsedMilliseconds}ms");
                }
            }
        }
        [Fact]
        public async Task BuildElementTree_PeerElement_Rx_Cancel()
        {
            for (int i = 0; i < count; i++)
            {
                await Task.Delay(200);

                var targetAutomation = _targetElement;
                var sw = Stopwatch.StartNew();
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

                try
                {
                    _testOutputHelper.WriteLine($"开始构建 #{i}: {sw.ElapsedMilliseconds}ms");

                    // 准备监听结果
                    var resultTask = _elementService.PeerStream
                        .FirstOrDefaultAsync()
                        .ToTask(cts.Token);

                    // 启动构建
                    await _elementService.StartBuildTreeAsync(targetAutomation, cts.Token);
                    _testOutputHelper.WriteLine($"构建完成 #{i}: {sw.ElapsedMilliseconds}ms");

                    // 等待结果
                    var result = await resultTask;
                    _testOutputHelper.WriteLine($"收到结果 #{i}: {sw.ElapsedMilliseconds}ms");

                    if (result != null)
                    {
                        _testOutputHelper.WriteLine(JsonConvert.SerializeObject(result));
                    }
                    else
                    {
                        _testOutputHelper.WriteLine($"结果为空 #{i}");
                    }
                }
                catch (OperationCanceledException)
                {
                    _testOutputHelper.WriteLine($"操作被取消或超时 #{i}: {sw.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    _testOutputHelper.WriteLine($"异常 #{i}: {ex.Message}");
                }
                finally
                {
                    sw.Stop();
                    _testOutputHelper.WriteLine($"总耗时 #{i}: {sw.ElapsedMilliseconds}ms");
                }
            }
        }
        [Fact]
        public async Task BuildElementTree_PeerElement_Track()
        {
            for (int i = 0; i < count; i++)
            {
                await Task.Delay(200);
                var sw = Stopwatch.StartNew();
                var results = new List<UiaTreeResultDto>();
                var subscription = _elementService.PeerStream.Subscribe(result =>
                {
                    results.Add(result);
                    _testOutputHelper.WriteLine($"收到结果 #{results.Count} 在 #{i}: {sw.ElapsedMilliseconds}ms");
                    _testOutputHelper.WriteLine(JsonConvert.SerializeObject(result));
                });

                try
                {
                    _testOutputHelper.WriteLine($"开始构建 #{i}: {sw.ElapsedMilliseconds}ms");

                    var element = Providers.Automation.FromPoint(new System.Drawing.Point { X = 1369, Y = 554 });
                    var task1 = _elementService.StartPeerElementAsync(element);
                    _testOutputHelper.WriteLine($"启动第一次构建 #{i}: {sw.ElapsedMilliseconds}ms");

                    var task2 = _elementService.StartPeerElementAsync(element);
                    _testOutputHelper.WriteLine($"启动第二次构建 #{i}: {sw.ElapsedMilliseconds}ms");

                    // 等待所有构建完成
                    try
                    {
                        await Task.WhenAll(task1, task2);
                    }
                    catch (OperationCanceledException)
                    {
                        _testOutputHelper.WriteLine($"某个构建被取消 #{i}: {sw.ElapsedMilliseconds}ms");
                    }

                    // 等待一段时间以接收所有结果
                    await Task.Delay(1000);

                    _testOutputHelper.WriteLine($"完成构建 #{i}: {sw.ElapsedMilliseconds}ms，收到 {results.Count} 个结果");

                    if (results.Count == 0)
                    {
                        _testOutputHelper.WriteLine($"未收到任何结果 #{i}");
                    }
                }
                catch (TimeoutException)
                {
                    _testOutputHelper.WriteLine($"等待结果超时 #{i}: {sw.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    _testOutputHelper.WriteLine($"异常 #{i}: {ex.Message}");
                }
                finally
                {
                    subscription.Dispose();
                    sw.Stop();
                    _testOutputHelper.WriteLine($"总耗时 #{i}: {sw.ElapsedMilliseconds}ms");
                }
            }
        }

        [Fact]
        public async Task StartPeerElementAsync_ShouldReplaceOldRequest_WhenSequentialCalls()
        {


            var track1 = new Point { X = 1369, Y = 554 };
            var track2 = new Point { X = 1369, Y = 554 };


            await _elementService.StartPeerElementAsync(Providers.Automation.FromPoint(track1));
            await _elementService.StartPeerElementAsync(Providers.Automation.FromPoint(track2));


            Assert.Equal(2, _elementService.StartedCount);
            Assert.Equal(2, _elementService.CompletedCount);
        }


        [Fact]
        public async Task StartPeerElementAsync_ShouldCancelFirst_WhenConcurrentCalls()
        {


            var track1 = new Point { X = 10, Y = 20 };
            var track2 = new Point { X = 30, Y = 40 };


            var t1 = _elementService.StartPeerElementAsync(Providers.Automation.FromPoint(track1));
            var t2 = _elementService.StartPeerElementAsync(Providers.Automation.FromPoint(track2));


            await Task.WhenAll(t1, t2);


            Assert.Equal(2, _elementService.StartedCount); // 两次都启动
            Assert.Equal(1, _elementService.CanceledCount); // 第一次被取消
            Assert.Equal(1, _elementService.CompletedCount); // 第二次完成
        }
    }
}
