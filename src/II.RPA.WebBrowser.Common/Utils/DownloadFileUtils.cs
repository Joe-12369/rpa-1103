using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Utils
{
    public class DownloadFileUtils
    {
        protected static ILogger logger = Logger.OfContext();

        public static bool FindSaveAsFrom(string fromName)
        {
            for (int i = 0; i < 4; i++)
            {
                Thread.Sleep(1000);
                IntPtr notarize = NativeMethods.FindWindowEx(new IntPtr(), new IntPtr(), null, fromName);
                if (notarize != IntPtr.Zero)
                    return true;
            }
            return false;
        }
        public static void NotarizeSaveAs()
        {
            Thread.Sleep(200);
            IntPtr notarize = NativeMethods.FindWindowEx(new IntPtr(), new IntPtr(), null, "确认另存为");
            if (notarize != IntPtr.Zero)
            {
                var notarizeSaveAs = UiCommon.UIAAutomation.FromHandle(notarize);
                var btn = GetSaveAsElement(notarizeSaveAs, "是(Y)", "CCPushButton");
                var rect = btn.BoundingRectangle;
                Mouse.LeftClick(new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2));
            }
        }
        public static bool SaveAs(string saveFile, string windowName, ref string fileName)
        {
            try
            {
                Thread.Sleep(1000);
                IntPtr saveAsintPtr = NativeMethods.FindWindowEx(new IntPtr(), new IntPtr(), null, windowName);
                if (saveAsintPtr != IntPtr.Zero)
                {
                    Thread.Sleep(200);
                    var saveAs = UiCommon.UIAAutomation.FromHandle(saveAsintPtr);
                    var com = GetSaveAsElement(saveAs, "文件名:", "AppControlHost");
                    if (string.IsNullOrEmpty(fileName))
                        fileName = com.AsComboBox().EditableText;
                    //com.AsComboBox().EditableText = Path.Combine(saveFile, fileName);

                    //此处将给comboBox赋值方式改为通过剪切板方式给输入框赋值，
                    //comboBox赋值方式在部分电脑会存在保存还是拿到的弹框首次进来的默认值
                    try
                    {
                        UiCommon.ClipboardPaste(Path.Combine(saveFile, fileName), 0);
                        Thread.Sleep(200);
                    }
                    catch
                    {
                    }
                    var fName = com.AsComboBox().EditableText;
                    if (string.IsNullOrEmpty(fName) || fName != Path.Combine(saveFile, fileName))
                    {
                        //剪切板粘贴失效或结果不对时，进行原方式增强
                        com.AsComboBox().EditableText = Path.Combine(saveFile, fileName);
                        Thread.Sleep(200);
                    }
                    var btn = GetSaveAsElement(saveAs, "保存(S)", "Button");
                    var rect = btn.BoundingRectangle;
                    Mouse.LeftClick(new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2));
                    if (com.AsComboBox().EditableText != saveFile)
                    {
                        Thread.Sleep(200);
                        Mouse.LeftClick(new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2));
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                logger.LogDebug(e);
                return false;
            }
        }
        public static AutomationElement GetSaveAsElement(AutomationElement automationElement, string name, string className)
        {
            /*******插件化拆分时  调整uia3Automation 对象获取的来源*********/
            //var uia3Automation = new UIA3Automation();
            List<ConditionBase> combobox = new List<ConditionBase>();
            //combobox.Add(new PropertyCondition(uia3Automation.PropertyLibrary.Element.Name, name));
            //combobox.Add(new PropertyCondition(uia3Automation.PropertyLibrary.Element.ClassName, className));
            combobox.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.Name, name));
            combobox.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.ClassName, className));
            AndCondition ad = new AndCondition(combobox);
            return automationElement.FindAll(TreeScope.Descendants, ad).FirstOrDefault();
        }

        public static bool SelectedReserver(IntPtr browserHWnd)
        {
            try
            {
                if (browserHWnd != IntPtr.Zero)
                {
                    Thread.Sleep(200);
                    var save = UiCommon.UIAAutomation.FromHandle(browserHWnd);
                    //var uia3Automation = new UIA3Automation();
                    List<ConditionBase> button = new List<ConditionBase>();
                    /*******插件化拆分时  调整uia3Automation 对象获取的来源*********/
                    //button.Add(new PropertyCondition(uia3Automation.PropertyLibrary.Element.Name, "保留"));
                    button.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.Name, "保留"));
                    AndCondition ad = new AndCondition(button);
                    var btn = save.FindAll(TreeScope.Descendants, ad).FirstOrDefault();
                    if (btn == null)
                        return false;
                    var rect = btn.BoundingRectangle;
                    Mouse.LeftClick(new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2));
                }
                return true;
            }
            catch (Exception e)
            {
                logger.LogDebug("点击保存按钮报错", e);
                return false;
            }
        }
    }
}
