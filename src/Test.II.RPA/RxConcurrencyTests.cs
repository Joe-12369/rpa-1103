using System.Reactive.Linq;
using System.Reactive.Subjects;
using Xunit.Abstractions;
using Xunit.Sdk;


namespace Test.II.RPA
{

    public class RxConcurrencyTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        public RxConcurrencyTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }
        [Fact]
        public async Task SelectMerge_Should_Limit_MaxConcurrency()
        {
            var incoming = new Subject<int>();
            var processed = new List<int>();
            var maxObservedConcurrency = 0;
            int currentConcurrency = 0;

            int maxConcurrency = 3;
            int totalMessages = 10;

            var subscription = incoming
                .Select(item =>
                    Observable.FromAsync(async () =>
                    {
                        Interlocked.Increment(ref currentConcurrency);
                        maxObservedConcurrency = Math.Max(maxObservedConcurrency, currentConcurrency);

                        _testOutputHelper.WriteLine($"开始执行:{item}");
                        _testOutputHelper.WriteLine($"当前并发数:{currentConcurrency};最大并发数:{maxObservedConcurrency}");
                        await Task.Delay(100);
                        _testOutputHelper.WriteLine($"已完成:{item}-{Thread.CurrentThread.ManagedThreadId}");

                        lock (processed)
                        {
                            processed.Add(item);
                        }

                        Interlocked.Decrement(ref currentConcurrency);
                    })
                )
                .Merge(maxConcurrency)
                .Subscribe();

            for (int i = 0; i < totalMessages; i++)
            {
                incoming.OnNext(i);
                _testOutputHelper.WriteLine($"已发送:{i}");
            }

            await Task.Delay(1500);

            Assert.InRange(maxObservedConcurrency, 1, maxConcurrency);

            Assert.Equal(totalMessages, processed.Count);

            subscription.Dispose();
            incoming.Dispose();
        }
    }
}
