using System.Drawing;
using System.Runtime.CompilerServices;
using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.WinAutomation.CommonUtility;
using Xunit;

namespace II.RPA.Plugin.GUI.Tests
{
    public class JavaCaptureModeTests
    {
        [Theory]
        [InlineData("agent", 390)]
        [InlineData("jab", -390)]
        public void Initialization_AppliesJavaAlterCategoryToSwing(string mode, int expected)
        {
            var swing = new TestOperator(eGuiType.Swing);
            var java = new TestOperator(eGuiType.Java);
            PluginPackage.ConfigureOperatorPriorities(new[] { swing, java }, new[]
            {
                new GUICategory { Name = "GUIJavaAlter", Sort = 390 },
                new GUICategory { Name = "GUIJava", Sort = 400 }
            }, mode);
            Assert.Equal(expected, swing.Sort);
            Assert.Equal(400, java.Sort);
        }

        [Fact]
        public void Toggle_InvalidatesBothJavaCaches_AndCanSwitchBack()
        {
            var swing = new TestOperator(eGuiType.Swing) { Sort = 390 };
            var java = new TestOperator(eGuiType.Java) { Sort = 400 };
            var windows = new TestOperator(eGuiType.Windows) { Sort = 10000 };
            var manager = (GuiManager)RuntimeHelpers.GetUninitializedObject(typeof(GuiManager));
            manager.GuiOperators = new List<IGuiOperator> { swing, java, windows };
            manager.GuiOperatorsDic = new Dictionary<IntPtr, IGuiOperator>();
            manager.CacheWndGuiType((IntPtr)1, swing);
            manager.CacheWndGuiType((IntPtr)2, windows);
            manager.ToggleJavaCaptureMode();
            Assert.Equal(-390, swing.Sort);
            Assert.Null(manager.GetGuiType((IntPtr)1));
            Assert.Same(windows, manager.GetGuiType((IntPtr)2));
            manager.CacheWndGuiType((IntPtr)1, java);
            manager.ToggleJavaCaptureMode();
            Assert.Equal(390, swing.Sort);
            Assert.Null(manager.GetGuiType((IntPtr)1));
        }

        private sealed class TestOperator : IGuiOperator
        {
            public TestOperator(eGuiType type) { GuiType = type; }
            public int Sort { get; set; }
            public eGuiType GuiType { get; }
            public void ReadyToCapture() { }
            public void Release() { }
            public bool IsCanUse(Point point, AutomationElement root, AutomationElement element) => true;
            public bool InspectNode(Point point, AutomationElement root, AutomationElement element, string selector, out IGuiNode node) { node = null; return false; }
            public IGuiNode GetElementFromSelector(GetElementFromSelectorParams args) => null;
            public string GetElementFromSelectorAll(GetElementFromSelectorParams args) => null;
        }
    }
}
