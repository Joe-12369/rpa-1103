using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Deeps;
using System.Drawing;
using Xunit.Abstractions;

namespace Test.II.RPA
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    public class SelectorBuilderTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly AutomationBase _automationBase;
        private readonly PluginLoaderService _pluginLoaderService;

        public SelectorBuilderTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _automationBase = UiCommon.UIAAutomation;
            _pluginLoaderService = new PluginLoaderService();
        }

        /// <summary>
        /// 测试 SelectorBuilder 的异步构建方法
        /// 通过坐标点 (x,y) 获取 UI 元素，再构建 selector
        /// </summary>
        /// <param name="x">屏幕坐标 X</param>
        /// <param name="y">屏幕坐标 Y</param>
        [Theory]
        [InlineData(250, 270)]
        public void GetElementRootWindow_Test(int x, int y)
        {
            var pluginloadPath = PathHelper.GetPluginLoaderPath(AppDomain.CurrentDomain.BaseDirectory);
            var cost = _pluginLoaderService.LoadAndInitialize(pluginloadPath);
            _testOutputHelper.WriteLine($"开始测试 GetElementRootWindow_Test，输入坐标: ({x}, {y})");

            // 1. 从坐标点获取 UI 元素
            var element = _automationBase.FromPoint(new Point(x, y));
            Assert.NotNull(element); // 断言不为空，否则测试失败
            _testOutputHelper.WriteLine("成功获取 UI 元素");

            var window = SelectorBuilder.GetElementRootWindowHandle(element);

            _testOutputHelper.WriteLine(window.ToString());
            _testOutputHelper.WriteLine($"获取完成，window: {window}");
        }
    }

}
