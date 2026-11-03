using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Test.II.RPA
{
    public class WindowElementScreenshotTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _tempDirectory;

        public WindowElementScreenshotTests(ITestOutputHelper output)
        {
            _output = output;
            _tempDirectory = Path.Combine(Path.GetTempPath(), "WindowScreenshotTests");
            Directory.CreateDirectory(_tempDirectory);
        }

        #region 基础功能测试

        [Fact]
        public void ScreenToClient_ValidWindowHandle_ShouldConvertCoordinatesCorrectly()
        {
            // Arrange
            IntPtr desktopHandle = GetDesktopWindow();
            Point screenPoint = new Point(100, 100);

            // Act
            Point clientPoint = WindowElementScreenshot.ScreenToClient(desktopHandle, screenPoint);

            // Assert
            Assert.True(clientPoint.X >= 0);
            Assert.True(clientPoint.Y >= 0);
            _output.WriteLine($"Screen point: {screenPoint}, Client point: {clientPoint}");
        }

        [Fact]
        public void CaptureWindow_InvalidHandle_ShouldThrowException()
        {
            // Arrange
            IntPtr invalidHandle = IntPtr.Zero;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                WindowElementScreenshot.CaptureWindow(invalidHandle));
        }

        [Fact]
        public void CaptureElement_ElementOutsideWindow_ShouldThrowException()
        {
            // Arrange
            IntPtr desktopHandle = GetDesktopWindow();
            Rectangle elementRect = new Rectangle(-100, -100, 50, 50); // 超出范围的坐标

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                WindowElementScreenshot.CaptureElement(desktopHandle, elementRect));
        }

        #endregion

        #region 集成测试（需要实际窗口）

        [Fact]
        public void CaptureWindow_DesktopWindow_ShouldReturnValidBitmap()
        {
            // Arrange
            IntPtr desktopHandle = GetDesktopWindow();

            // Act
            using (Bitmap bitmap = WindowElementScreenshot.CaptureWindow(desktopHandle, false))
            {
                // Assert
                Assert.NotNull(bitmap);
                Assert.True(bitmap.Width > 0);
                Assert.True(bitmap.Height > 0);
                Assert.Equal(PixelFormat.Format32bppRgb, bitmap.PixelFormat);

                // 保存测试图片
                string testImagePath = Path.Combine(_tempDirectory, "desktop_capture_test.png");
                bitmap.Save(testImagePath, ImageFormat.Png);
                _output.WriteLine($"Desktop capture saved to: {testImagePath}");
                _output.WriteLine($"Bitmap size: {bitmap.Width}x{bitmap.Height}");
            }
        }

        [Fact]
        public void CaptureWindow_WithAndWithoutFrame_ShouldHaveDifferentSizes()
        {
            // Arrange
            IntPtr windowHandle = FindAnyVisibleWindow();

            if (windowHandle == IntPtr.Zero)
            {
                _output.WriteLine("No visible window found, skipping test");
                return;
            }

            // Act
            using (Bitmap withFrame = WindowElementScreenshot.CaptureWindow(windowHandle, true))
            using (Bitmap withoutFrame = WindowElementScreenshot.CaptureWindow(windowHandle, false))
            {
                // Assert
                Assert.NotNull(withFrame);
                Assert.NotNull(withoutFrame);

                // 包含框架的截图通常会更大（除非窗口没有标题栏）
                _output.WriteLine($"With frame: {withFrame.Width}x{withFrame.Height}");
                _output.WriteLine($"Without frame: {withoutFrame.Width}x{withoutFrame.Height}");

                // 保存测试图片
                string withFramePath = Path.Combine(_tempDirectory, "window_with_frame.png");
                string withoutFramePath = Path.Combine(_tempDirectory, "window_without_frame.png");

                withFrame.Save(withFramePath, ImageFormat.Png);
                withoutFrame.Save(withoutFramePath, ImageFormat.Png);

                _output.WriteLine($"Window captures saved to: {_tempDirectory}");
            }
        }

        [Fact]
        public void CaptureElement_ValidCoordinates_ShouldReturnElementBitmap()
        {
            // Arrange
            IntPtr windowHandle = FindAnyVisibleWindow();

            if (windowHandle == IntPtr.Zero)
            {
                _output.WriteLine("No visible window found, skipping test");
                return;
            }

            // 获取窗口信息
            var windowRect = GetWindowRectangle(windowHandle);

            // 定义一个小的测试区域（窗口左上角的 100x100 区域）
            int elementX = windowRect.X + 10;
            int elementY = windowRect.Y + 10;
            int elementWidth = Math.Min(100, windowRect.Width - 20);
            int elementHeight = Math.Min(100, windowRect.Height - 20);

            // Act
            using (Bitmap elementBitmap = WindowElementScreenshot.CaptureElement(
                windowHandle, elementX, elementY, elementWidth, elementHeight, true))
            {
                // Assert
                Assert.NotNull(elementBitmap);
                Assert.Equal(elementWidth, elementBitmap.Width);
                Assert.Equal(elementHeight, elementBitmap.Height);

                // 保存测试图片
                string elementImagePath = Path.Combine(_tempDirectory, "element_capture_test.png");
                elementBitmap.Save(elementImagePath, ImageFormat.Png);

                _output.WriteLine($"Element capture saved to: {elementImagePath}");
                _output.WriteLine($"Element size: {elementBitmap.Width}x{elementBitmap.Height}");
                _output.WriteLine($"Element coordinates: ({elementX}, {elementY})");
            }
        }

        #endregion

        #region 边界条件测试

        [Theory]
        [InlineData(0, 0, 10, 10)]
        [InlineData(50, 50, 100, 100)]
        public void CaptureElement_DifferentSizes_ShouldReturnCorrectDimensions(
            int offsetX, int offsetY, int width, int height)
        {
            // Arrange
            IntPtr windowHandle = FindAnyVisibleWindow();

            if (windowHandle == IntPtr.Zero)
            {
                _output.WriteLine("No visible window found, skipping test");
                return;
            }

            var windowRect = GetWindowRectangle(windowHandle);

            // 确保坐标在窗口范围内
            int elementX = windowRect.X + offsetX;
            int elementY = windowRect.Y + offsetY;
            int maxWidth = Math.Min(width, windowRect.Width - offsetX);
            int maxHeight = Math.Min(height, windowRect.Height - offsetY);

            if (maxWidth <= 0 || maxHeight <= 0)
            {
                _output.WriteLine($"Skipping test - invalid dimensions: {maxWidth}x{maxHeight}");
                return;
            }

            // Act
            using (Bitmap bitmap = WindowElementScreenshot.CaptureElement(
                windowHandle, elementX, elementY, maxWidth, maxHeight, true))
            {
                // Assert
                Assert.Equal(maxWidth, bitmap.Width);
                Assert.Equal(maxHeight, bitmap.Height);

                _output.WriteLine($"Test passed for size: {maxWidth}x{maxHeight}");
            }
        }

        #endregion

        #region 性能测试

        [Fact]
        public void CaptureElement_Performance_ShouldCompleteWithinReasonableTime()
        {
            // Arrange
            IntPtr windowHandle = FindAnyVisibleWindow();

            if (windowHandle == IntPtr.Zero)
            {
                _output.WriteLine("No visible window found, skipping test");
                return;
            }

            var windowRect = GetWindowRectangle(windowHandle);
            var stopwatch = Stopwatch.StartNew();

            // Act
            using (Bitmap bitmap = WindowElementScreenshot.CaptureElement(
                windowHandle, windowRect.X + 10, windowRect.Y + 10, 200, 150, false))
            {
                stopwatch.Stop();

                // Assert
                Assert.True(stopwatch.ElapsedMilliseconds < 1000,
                    $"Capture took too long: {stopwatch.ElapsedMilliseconds}ms");

                _output.WriteLine($"Capture completed in {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        #endregion

        #region 辅助方法

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private IntPtr FindAnyVisibleWindow()
        {
            IntPtr foundWindow = IntPtr.Zero;

            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd) && GetWindowRectangle(hWnd).Width > 100 && GetWindowRectangle(hWnd).Height > 100)
                {
                    foundWindow = hWnd;
                    return false; // 停止枚举
                }
                return true; // 继续枚举
            }, IntPtr.Zero);

            return foundWindow;
        }

        private Rectangle GetWindowRectangle(IntPtr hWnd)
        {
            RECT rect;
            GetWindowRect(hWnd, out rect);
            return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        #endregion

        #region 清理资源

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDirectory))
                {
                    Directory.Delete(_tempDirectory, true);
                    _output.WriteLine($"Cleaned up temp directory: {_tempDirectory}");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Failed to clean up temp directory: {ex.Message}");
            }
        }

        #endregion
    }

    #region 测试配置

    public class WindowScreenshotTestCollection : ICollectionFixture<WindowScreenshotTestCollection>
    {
        // 这个类用于共享测试上下文，如果需要的话
    }

    #endregion
}
