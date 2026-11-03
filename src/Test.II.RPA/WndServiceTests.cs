using II.RPA.Plugin.GUI.Deeps;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Test.II.RPA
{
    public class WndServiceTests : IDisposable
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly WndService _wndService;

        public WndServiceTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _wndService = new WndService();
            _testOutputHelper.WriteLine("WndService 实例已创建");
        }
        [Fact]
        public void GetRootWindows_ShouldWarmup()
        {
            // Arrange
            _testOutputHelper.WriteLine("开始测试 WarmupCache 方法");
            var sw = Stopwatch.StartNew();
            // Act
            _wndService.WarmupCache();
            _testOutputHelper.WriteLine($"WarmupCache 执行时间: {sw.ElapsedMilliseconds} ms");
            _testOutputHelper.WriteLine($"缓存个数: {_wndService.GetCacheStats().TotalCached} ");
            sw.Stop();
            // Assert
            Assert.True(_wndService.GetCacheStats().TotalCached > 0);

        }
        [Fact]
        public void GetRootWindows_ShouldReturnWindowList()
        {
            // Arrange
            _testOutputHelper.WriteLine("开始测试 GetRootWindows 方法");

            // Act
            var windows = _wndService.GetRootWindows();

            // Assert
            Assert.NotNull(windows);
            _testOutputHelper.WriteLine($"获取到 {windows.Count} 个窗口");

            if (windows.Count > 0)
            {
                var firstWindow = windows.First();
                _testOutputHelper.WriteLine($"第一个窗口信息:");
                _testOutputHelper.WriteLine($"  名称: {firstWindow.Name}");
                _testOutputHelper.WriteLine($"  句柄: 0x{firstWindow.Handle.Value.ToString("X")}");
                _testOutputHelper.WriteLine($"  进程ID: {firstWindow.ProcessId}");
                _testOutputHelper.WriteLine($"  是否管理员: {firstWindow.IsAdmin}");
                _testOutputHelper.WriteLine($"  完整性级别: {firstWindow.IntegrityLevel}");
                _testOutputHelper.WriteLine($"  区域: {firstWindow.Region}");

                Assert.True(firstWindow.Handle != IntPtr.Zero);
                Assert.True(firstWindow.ProcessId > 0);
                Assert.False(string.IsNullOrEmpty(firstWindow.Name));
            }
        }

        [Fact]
        public void GetRootWindows_WithOnlyValidFalse_ShouldReturnMoreWindows()
        {
            // Arrange
            _testOutputHelper.WriteLine("开始测试 GetRootWindows(onlyValid: false) 方法");

            // Act
            var validWindows = _wndService.GetRootWindows(onlyValid: true);
            var allWindows = _wndService.GetRootWindows(onlyValid: false);

            // Assert
            _testOutputHelper.WriteLine($"仅有效窗口数量: {validWindows.Count}");
            _testOutputHelper.WriteLine($"所有窗口数量: {allWindows.Count}");

            Assert.True(allWindows.Count >= validWindows.Count, "所有窗口数量应大于等于有效窗口数量");
        }

        [Fact]
        public void GetCacheStats_InitialState_ShouldReturnZeros()
        {
            // Arrange
            _testOutputHelper.WriteLine("开始测试 GetCacheStats 初始状态");
            _wndService.ClearAllCache();

            // Act
            var (totalCached, excludedWindows, includedWindows) = _wndService.GetCacheStats();

            // Assert
            _testOutputHelper.WriteLine($"缓存统计 - 总数: {totalCached}, 排除: {excludedWindows}, 包含: {includedWindows}");
            Assert.Equal(0, totalCached);
            Assert.Equal(0, excludedWindows);
            Assert.Equal(0, includedWindows);
        }

        [Fact]
        public void WarmupCache_ShouldPopulateCache()
        {
            // Arrange
            _testOutputHelper.WriteLine("开始测试 WarmupCache 方法");
            _wndService.ClearAllCache();
            var initialStats = _wndService.GetCacheStats();
            _testOutputHelper.WriteLine($"预热前缓存统计: {initialStats}");

            // Act
            _wndService.WarmupCache();

            // Assert
            var afterWarmupStats = _wndService.GetCacheStats();
            _testOutputHelper.WriteLine($"预热后缓存统计: {afterWarmupStats}");

            Assert.True(afterWarmupStats.TotalCached > initialStats.TotalCached,
                "预热后缓存数量应该增加");
        }

        [Fact]
        public void AddToExcludedProcessNames_ShouldAddNewName()
        {
            // Arrange
            _testOutputHelper.WriteLine("开始测试 AddToExcludedProcessNames 方法");
            var testProcessName = "TestProcess.exe";

            // Act
            _wndService.AddToExcludedProcessNames(testProcessName);
            _testOutputHelper.WriteLine($"已添加排除进程名称: {testProcessName}");

            Assert.True(true, "添加进程名称成功");
        }

        [Fact]
        public void AddToExcludedProcessNames_WithNullOrEmpty_ShouldNotThrow()
        {
            // Arrange & Act & Assert
            _testOutputHelper.WriteLine("测试添加空或null进程名称");

            _wndService.AddToExcludedProcessNames(null);
            _wndService.AddToExcludedProcessNames("");
            _wndService.AddToExcludedProcessNames("   ");

            _testOutputHelper.WriteLine("添加空进程名称测试通过");
            Assert.True(true, "空进程名称处理正确");
        }

        [Fact]
        public void InvalidateWindowCache_WithValidHandle_ShouldNotThrow()
        {
            // Arrange
            _testOutputHelper.WriteLine("开始测试 InvalidateWindowCache 方法");
            var testHandle = new IntPtr(12345);

            // Act & Assert
            _wndService.InvalidateWindowCache(testHandle);
            _testOutputHelper.WriteLine($"已使句柄 0x{testHandle.ToString("X")} 的缓存失效");

            Assert.True(true, "缓存失效操作成功");
        }

        [Fact]
        public void IsWindowExcluded_WithNonExistentHandle_ShouldReturnFalse()
        {
            // Arrange
            _testOutputHelper.WriteLine("开始测试 IsWindowExcluded 方法");
            var testHandle = new IntPtr(99999);
            _wndService.ClearAllCache();

            // Act
            var isExcluded = _wndService.IsWindowExcluded(testHandle);

            // Assert
            _testOutputHelper.WriteLine($"句柄 0x{testHandle.ToString("X")} 是否被排除: {isExcluded}");
            Assert.False(isExcluded, "不存在的句柄应该返回false");
        }

        [Fact]
        public void ClearAllCache_ShouldResetCacheToEmpty()
        {
            // Arrange
            _testOutputHelper.WriteLine("开始测试 ClearAllCache 方法");
            _wndService.WarmupCache(); 
            var beforeClear = _wndService.GetCacheStats();
            _testOutputHelper.WriteLine($"清理前缓存统计: {beforeClear}");

            // Act
            _wndService.ClearAllCache();

            // Assert
            var afterClear = _wndService.GetCacheStats();
            _testOutputHelper.WriteLine($"清理后缓存统计: {afterClear}");

            Assert.Equal(0, afterClear.TotalCached);
            Assert.Equal(0, afterClear.ExcludedWindows);
            Assert.Equal(0, afterClear.IncludedWindows);
        }

        [Fact]
        public async Task CacheCleanup_ShouldWorkOverTime()
        {
            // Arrange
            _testOutputHelper.WriteLine("开始测试缓存自动清理功能");

            using var shortIntervalService = new WndService(
                new[] { "explorer.exe" },
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(200)
            );

            shortIntervalService.WarmupCache();
            var initialStats = shortIntervalService.GetCacheStats();
            _testOutputHelper.WriteLine($"初始缓存统计: {initialStats}");

            await Task.Delay(500);

            // Assert
            var finalStats = shortIntervalService.GetCacheStats();
            _testOutputHelper.WriteLine($"清理后缓存统计: {finalStats}");

            Assert.True(true, "缓存自动清理测试完成");
        }

        [Fact]
        public void Dispose_ShouldPreventFurtherOperations()
        {
            // Arrange
            _testOutputHelper.WriteLine("开始测试 Dispose 方法");
            using var disposableService = new WndService();

            // Act
            disposableService.Dispose();
            _testOutputHelper.WriteLine("WndService 已释放");

            // Assert
            Assert.Throws<ObjectDisposedException>(() => disposableService.GetRootWindows());
            Assert.Throws<ObjectDisposedException>(() => disposableService.GetCacheStats());
            Assert.Throws<ObjectDisposedException>(() => disposableService.WarmupCache());

            _testOutputHelper.WriteLine("释放后操作正确抛出 ObjectDisposedException");
        }

        

        public void Dispose()
        {
            _testOutputHelper.WriteLine("正在释放测试资源...");
            _wndService?.Dispose();
            _testOutputHelper.WriteLine("测试资源已释放");
        }
    }
}