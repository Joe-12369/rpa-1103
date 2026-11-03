using II.RPA.Plugin.Core.Commons;
using Xunit.Abstractions;

namespace Test.II.RPA
{
    public class Win32ServiceTest
    {
        private const string TestText = "实在智能`1234567890-=~!@#$%^&*()_+qazwsx[];'./实在智能";
        private readonly ITestOutputHelper _testOutput;
        public Win32ServiceTest(ITestOutputHelper testOutputHelper)
        {
            _testOutput = testOutputHelper;
        }
        [Theory]
        [InlineData("实在智能")]
        [InlineData("AI-Indeed")]
        [InlineData("1234567890")]
        [InlineData("abcdefghijklmnopqrstuvwxyz")]
        [InlineData("~!@#$%^&*()_+`-=][{}|/';.,:.,?'\\")]
        [InlineData("~！@#￥%……&*（）——+-=·【】、；‘，。、’）|？》《“：「」”》")]
        public async Task ClipboardTestAync(string targetText)
        {
            try
            {
                _testOutput.WriteLine($"Start ClipboardTest:{targetText}");
                await ClipBoardInvoker.SetTextAsync(targetText);
                await ClipBoardInvoker.SleepAsync(1);
                var actualText = await ClipBoardInvoker.GetTextAsync();
                Assert.Equal(targetText, actualText);
                await ClipBoardInvoker.ClearAsync();
                await ClipBoardInvoker.SleepAsync(1);
                Assert.Null(await ClipBoardInvoker.GetTextAsync());
                await ClipBoardInvoker.SleepAsync(1);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }
        [Theory]
        [InlineData("实在智能")]
        [InlineData("AI-Indeed")]
        [InlineData("1234567890")]
        [InlineData("abcdefghijklmnopqrstuvwxyz")]
        [InlineData("~!@#$%^&*()_+`-=][{}|/';.,:.,?'\\")]
        [InlineData("~！@#￥%……&*（）——+-=·【】、；‘，。、’）|？》《“：「」”》")]
        public void ClipboardTest(string targetText)
        {
            try
            {
                _testOutput.WriteLine($"Start ClipboardTest:{targetText}");
                ClipBoardInvoker.SetText(targetText);
                var actualText = ClipBoardInvoker.GetText();
                Assert.Equal(targetText, actualText);
                ClipBoardInvoker.Clear();
                Assert.Null(ClipBoardInvoker.GetText());
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }
        [Theory]
        //1次重试 运行3+后 就大概率失败
        //[InlineData(TestText, 1, 100, 1)]
        [InlineData(TestText, 10, 100, 1, 100)]
        [InlineData(TestText, 30, 100, 1, 100)]
        [InlineData(TestText, 50, 100, 10, 100)]
        //[InlineData(TestText, 100, 100, 1)]
        public async Task ClipboardRetryTest(string targetText,
            int retryCount,
            int retryDelay,
            int sleepDelay,
            int loopCount)
        {
            try
            {
                _testOutput.WriteLine($"Start ClipboardTest:{targetText};{retryCount};{retryDelay};{sleepDelay}");
                var looping = Enumerable.Range(1, loopCount);
                foreach (var loop in looping)
                {
                    ClipBoardInvoker.SetupEnvironment(retryCount, TimeSpan.FromMilliseconds(retryDelay));
                    await ClipBoardInvoker.SetTextAsync(targetText);
                    await ClipBoardInvoker.SleepAsync(TimeSpan.FromMilliseconds(sleepDelay));
                    var actualText = await ClipBoardInvoker.GetTextAsync();
                    Assert.Equal(targetText, actualText);
                    await ClipBoardInvoker.ClearAsync();
                    await ClipBoardInvoker.SleepAsync(TimeSpan.FromMilliseconds(sleepDelay));
                    Assert.Null(await ClipBoardInvoker.GetTextAsync());
                    await ClipBoardInvoker.SleepAsync(TimeSpan.FromMilliseconds(sleepDelay));
                    _testOutput.WriteLine($"Loop count:{loop}/{loopCount}");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

    }
}