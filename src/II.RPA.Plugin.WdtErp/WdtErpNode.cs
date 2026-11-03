using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.WdtErp.Models;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WdtErp
{
    partial class WdtErpNode : Selector, IGuiNode_6_6_0
    {
        internal string userDefinedID;
        internal string className;

        public string UserDefinedID { get; set; }

        public string Idx { get; }

        /// <summary>
        /// 元素所在进程的ID
        /// </summary>
        public int ProcessID
        {
            get; set;
        }

        public string AutomationId { get; }
        public string ClassName { get; set; }

        public string ControlType
        {
            get; set;
        }
        public string Name
        {
            get; set;
        }

        public string Role { get; }

        public string Description { get; }

        public string ProcessName { get { return GuiOperator.ProcessName; } }

        public string ProcessFullPath
        {
            get
            {
                return Process.GetProcessesByName(ProcessName)?.FirstOrDefault()?.MainModule?.FileName ?? "";
            }
        }

        public string SimilarElementsSelector { get; set; }
        public string NodeSelector
        {
            get
            {
                return GetSelector(false);
            }

        }
        public string GetSelector(bool innerTextFirst)
        {
            XPath = WdtErpProxy.Instance.GetElementPath(WindowHandle, ElementPtr);
            var elementText = GetText();
            var selector = WdtErpProxy.Instance.ConvertPathToSelector(Name, ClassName, XPath, elementText);
            RPACommon common = new RPACommon();

            common.Selector = string.IsNullOrEmpty(selector.ToString()) ? new JArray() : JArray.Parse(selector);

            if (!string.IsNullOrWhiteSpace(SimilarElementsSelector) && IsTable)
            {
                WdtErpProxy.Instance.ProcessSimilarSelector(common.Selector, SimilarElementsSelector);
            }

            CommonProps commonProps = new CommonProps();
            commonProps.GUIType = eGuiType.WdtErp.ToString();
            commonProps.ProcessName = ProcessName;

            ElementDetail elementDetail = new ElementDetail();
            elementDetail.CategoryIconFile = ProcessFullPath;
            elementDetail.CategoryName = ProcessName;
            elementDetail.WindowName = elementDetail.CategoryName;
            elementDetail.ElementName = $"{ClassName}_{elementText ?? ""}";
            common.ElementDetail = elementDetail;
            common.CommonProps = commonProps;

            return JsonConvert.SerializeObject(common);
        }
        public IntPtr WindowHandle { get; set; }
        public IntPtr ElementPtr { get; set; }
        public string XPath { get; set; }
        public IGuiNode Parent { get; }

        public IGuiNode AutomationElementParent { get; }

        public Rectangle BoundingRectangle { get; set; }

        public List<IGuiNode> Children { get; }

        public bool IsTopLevelWindow { get; }

        public bool IsShow
        {
            get
            {
                return true;
            }
        }

        public eGuiType GuiType => eGuiType.WdtErp;

        public bool IsTable
        {
            get
            {
                XPath = XPath ?? WdtErpProxy.Instance.GetElementPath(WindowHandle, ElementPtr);
                return XPath.Contains("/table");
            }
        }

        public string TableNodeSelector
        {
            get
            {
                if (IsTable)
                {
                    return GetSelector(false);
                }
                else
                {
                    return null;
                }
            }
        }


        public System.Drawing.Point GetClickablePoint()
        {
            var point = new System.Drawing.Point();
            var rect = GetRegion(3);
            point.X = rect.X + rect.Width / 2;
            point.Y = rect.Y + rect.Height / 2;
            return point;
        }

        public string ClickTableCell(string input)
        {
            throw new NotImplementedException();
        }

        public Bitmap ElementToBitmap()
        {
            throw new NotImplementedException();
        }


        public List<IGuiNode> FindAll(IGuiNode fromElement, int maxResult, int milliseconds)
        {
            throw new NotImplementedException();
        }

        public string FindElementWithPicPath(string strPicPath)
        {
            throw new NotImplementedException();
        }

        public string FindRelativeElementsInfo(string strRelativeSelector, ref JArray context)
        {
            return null;
        }

        public IGuiNode FindRelativeNode(int position, int offsetX, int offsetY)
        {
            throw new NotImplementedException();
        }

        public void Focus()
        {
            throw new NotImplementedException();
        }

        public IGuiNode GetChildByIdx(int idx)
        {
            throw new NotImplementedException();
        }

        public string GetElementAttribute(string name, string apiVersion)
        {
            if (name == "state")
            {
                var postData = JsonHelper.JsonByNewtonJson(new { element = (int)ElementPtr });
                var response = WdtErpProxy.Instance.HttpRequest("/GetElementState", postData);
                if (response != null && response.ErrNo == 0)
                {
                    var state = (ElementStateBits)Convert.ToUInt16(response.Data);
                    return state.ToString();
                }
                else
                {
                    return "";
                }
            }
            else
            {
                return "";
            }

        }

        public string GetElementCheck()
        {
            var postData = JsonHelper.JsonByNewtonJson(new { element = (int)ElementPtr });
            var response = WdtErpProxy.Instance.HttpRequest("/GetElementState", postData);
            if (response != null && response.ErrNo == 0)
            {
                var state = (ElementStateBits)Convert.ToUInt16(response.Data);
                var isChecked = (state & ElementStateBits.STATE_CHECKED) == ElementStateBits.STATE_CHECKED;
                return isChecked ? "true" : "false";
            }
            else
            {
                return "false";
            }
        }
        public void SetElementCheck(int check)
        {
            var isCheck = GetElementCheck();
            if (check == 1)
            {
                if (isCheck.Equals("false"))
                {
                    MouseClick(0, 0, new UiElementClickParams
                    {
                        RPAMouseButtons = RPAMouseButtons.Left,
                        RPAMousePresses = RPAMousePresses.Click,
                        cursorPos = 0,
                        isUsePerson = false,
                        moveMouse = false,
                        operateType = 1
                    });
                }
            }
            else
            {
                if (isCheck.Equals("true"))
                {
                    MouseClick(0, 0, new UiElementClickParams
                    {
                        RPAMouseButtons = RPAMouseButtons.Left,
                        RPAMousePresses = RPAMousePresses.Click,
                        cursorPos = 0,
                        isUsePerson = false,
                        moveMouse = false,
                        operateType = 1
                    });
                }
            }

        }

        public string GetElementSelect(int selectType)
        {
            throw new NotImplementedException();
        }

        public string GetElementSelectEx(string strJson)
        {
            throw new NotImplementedException();
        }
        public string GetEnableClick()
        {
            throw new NotImplementedException();
        }

        public Rectangle GetRegion(int positionType)
        {
            return BoundingRectangle;
        }
        public string GetSubElements()
        {
            throw new NotImplementedException();
        }

        public string GetTableData(string input)
        {
            var postData = JsonHelper.JsonByNewtonJson(new { selector = NodeSelector, dataIndex = 1, count = "", template = "", type = 1 });
            var response = WdtErpProxy.Instance.HttpRequest("/GetTableData", postData);
            if (response != null && response.ErrNo == 0)
            {
                //移除Html标签
                response.Data = Regex.Replace(response.Data, "<.*?>", " ")?.Replace("\r\n", " ")?.Replace("\t", "")?.Trim();
                return response.Data;
            }
            return JsonConvert.SerializeObject(new List<List<string>>());
        }

        public string GetText()
        {
            var postData = JsonHelper.JsonByNewtonJson(new { element = (int)ElementPtr });
            var response = WdtErpProxy.Instance.HttpRequest("/GetElementInnerText", postData);
            return response?.Data;
        }

        public void IntoView()
        {
            throw new NotImplementedException();
        }

        public void MouseClick(int offsetX, int offsetY, UiElementClickParams clickParams)
        {
            Thread.Sleep(100);
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            Thread.Sleep(200);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Left));
        }

        public void MouseConterClick(int offsetX, int offsetY, UiElementClickParams clickParams)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Middle));
        }
        public void MouseMove(int cursorPos, int offsetX = 0, int offsetY = 0, bool isUsePerson = false)
        {
            var rect = GetRegion(3);
            System.Drawing.Point point = new System.Drawing.Point(0, 0);
            if (cursorPos == 0)
            {
                point = GetClickablePoint();
                point = new System.Drawing.Point(point.X + offsetX, point.Y + offsetY);
            }
            else if (cursorPos == 1)//左上
            {
                point = new System.Drawing.Point(rect.X + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == 2)//右上
            {
                point = new System.Drawing.Point(rect.X + rect.Width + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == 3)//左下
            {
                point = new System.Drawing.Point(rect.X + offsetX, rect.Y + rect.Height + offsetY);
            }
            else if (cursorPos == 4)//右下
            {
                point = new System.Drawing.Point(rect.X + rect.Width + offsetX, rect.Y + rect.Height + offsetY);
            }
            else if (cursorPos == 5)//随机
            {
                point = rect.GetRandomPoint();
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_TOP)//上中
            {
                point = new System.Drawing.Point(rect.X + rect.Width / 2 + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_LEFT)//左中
            {
                point = new System.Drawing.Point(rect.X + offsetX, rect.Y + rect.Height / 2 + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_RIGHT)//右中
            {
                point = new System.Drawing.Point(rect.X + rect.Width + offsetX, rect.Y + rect.Height / 2 + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_BOTTOM)//下中
            {
                point = new System.Drawing.Point(rect.X + rect.Width / 2 + offsetX, rect.Y + rect.Height + offsetY);
            }
            GUI.Commons.MouseMotions.MouseMotionHelper.PersonMoveMouse(isUsePerson, point);
        }

        public void MouseConterDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => Mouse.DoubleClick(MouseButton.Middle));
        }

        public void MouseDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => Mouse.DoubleClick());
        }

        public void MouseHover(UiElementHoverParams hoverParams)
        {
            if (hoverParams == null)
                hoverParams = new UiElementHoverParams();
            MouseMove(hoverParams.cursorPos, hoverParams.offsetX, hoverParams.offsetY, hoverParams.isUsePerson);
        }

        public void MouseRightClick(int offsetX, int offsetY, UiElementClickParams clickParams)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Right));
        }

        public void MouseRightDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => Mouse.RightDoubleClick());
        }

        public void SetElementAttribute(string name, string value)
        {
            throw new NotImplementedException();
        }



        public void SetElementMultipleSelect(int selectType, string setValue)
        {
            throw new NotImplementedException();
        }

        public void SetElementSelect(int selectType, string setValue)
        {
            throw new NotImplementedException();
        }

        public void SetForeground()
        {
            throw new NotImplementedException();
        }

        public void SetText(string text, int isReplace, bool isSimulate)
        {
            var replace = isReplace == 0 ? true : false;
            UiElementClickParams clickParams = new UiElementClickParams();
            clickParams.operateType = 1;
            clickParams.cursorPos = 0;
            MouseClick(0, 0, clickParams);
            if (replace)
            {
                AutoItHelper.ClearWords(100);
            }
            AutoItHelper.Send(text, false, 100);
        }

        public void MouseThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
            {
                Mouse.DoubleClick();
                Mouse.Click();
            });
        }

        public void MouseRightThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
            {
                Mouse.RightDoubleClick();
                Mouse.RightClick();
            });
        }

        public void MouseConterThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
            {
                Mouse.DoubleClick(MouseButton.Middle);
                Mouse.Click(MouseButton.Middle);
            });
        }
        public void SetText(string text, int isReplace, int inputType, int interval = 0)
        {
            var replace = isReplace == 0 ? true : false;
            UiElementClickParams clickParams = new UiElementClickParams();
            clickParams.operateType = 1;
            clickParams.cursorPos = 0;
            MouseClick(0, 0, clickParams);
            SetTextEx(replace, interval, text);
        }


        #region 新方式处理模拟输入
        public void SetTextEx(bool replace, int interval, string text)
        {
            if (replace)
            {
                Keyboard.Press(VirtualKeyShort.CONTROL);
                Keyboard.Press(VirtualKeyShort.KEY_A);
                Keyboard.Press(VirtualKeyShort.DELETE);

                Keyboard.Release(VirtualKeyShort.DELETE);
                Keyboard.Release(VirtualKeyShort.KEY_A);
                Keyboard.Release(VirtualKeyShort.CONTROL);
                Thread.Sleep(200);
            }
            else
            {
                Keyboard.Press(VirtualKeyShort.CONTROL);
                Keyboard.Press(VirtualKeyShort.KEY_A);

                Keyboard.Release(VirtualKeyShort.KEY_A);
                Keyboard.Release(VirtualKeyShort.CONTROL);
                Thread.Sleep(200);

                Keyboard.Press(VirtualKeyShort.RIGHT);
                Keyboard.Release(VirtualKeyShort.RIGHT);
                Thread.Sleep(200);
            }

            if (interval > 0)
            {
                List<string> _ButtinList = BreakString(text);
                foreach (var item in _ButtinList)
                {
                    KeyboardSimulate.KeyboardSimulateInput(item);
                    Thread.Sleep(interval);
                }
            }
            else
            {
                KeyboardSimulate.KeyboardSimulateInput(text);
            }
        }

        public List<string> BreakString(string _str)
        {
            List<string> RetList = new List<string>();
            foreach (var item in _str.ToCharArray())
            {
                RetList.Add(item.ToString());
            }
            return RetList;
        }

        public List<SelectorProps> MatchNextButtonSelector(ParamDoElementEditorModel paramModel)
        {
            throw new NotImplementedException();
        }

        public List<SelectorProps> AutoBulidCollectDataSelector(ParamBulidCollectDataSelectorModel paramModel)
        {
            throw new NotImplementedException();
        }

        public List<SelectorProps> CompareBulidCollectDataSelector(ParamBulidCollectDataSelectorModel paramModel)
        {
            throw new NotImplementedException();
        }

        public string GetNextElementBuildSelector(List<string> cssSelectorList)
        {
            throw new NotImplementedException();
        }

        public string GetHtml()
        {
            throw new NotImplementedException();
        }

        #endregion

        public string GetSubElements(ParamGetSubElements param)
        {
            //未单独实现的类型，按照老的逻辑返回子元素
            return this.GetSubElements();
        }

        public string GetParent(ParamGetParent param)
        {
            throw new NotImplementedException();
        }

        public string GetSiblings(ParamGetSiblings param)
        {
            throw new NotImplementedException();
        }
        public JObject GetInputMethod()
        {
            string result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "GetState"), "{}");
            var info = JObject.Parse(result)["info"];
            var state = JObject.Parse(info.ToString());
            return state;
        }
        public void SetInputMethod(string model = "en_us")
        {
            JObject j = new JObject();
            j.Add("state", model);
            var result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "SetState"), j.ToString());
        }
        /// <summary>
        /// 输入组合按键，被组合的按键在输入的时候指定一个延迟，避免组合不生效
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="keys"></param>
        private void TypeCombine(int interval, params VirtualKeyShort[] keys)
        {
            foreach (var key in keys)
            {
                Keyboard.Press(key);
                Thread.Sleep(interval);
            }

            foreach (var key in keys.Reverse())
            {
                Keyboard.Release(key);
            }
        }
        /// <summary>
        /// 向剪贴板中添加文本
        /// </summary>
        /// <param name="text">文本</param>
        public static void ClipboardSetText(string text)
        {
            if (!NativeMethods.OpenClipboard(IntPtr.Zero))
            {
                ClipboardSetText(text);
                return;
            }
            NativeMethods.EmptyClipboard();
            NativeMethods.SetClipboardData(13, Marshal.StringToHGlobalUni(text));
            NativeMethods.CloseClipboard();
        }
        public bool SetText(ParamSetTextModel param)
        {
            UiElementClickParams clickParams = new UiElementClickParams();
            clickParams.operateType = 1;
            clickParams.cursorPos = param.ElementPosition; ;
            MouseClick(param.OffsetX, param.OffsetY, clickParams);
            Thread.Sleep(param.FocusTimeout);
            //Thread.Sleep(50);
            string oldValue = string.Empty;

            var replace = param.isReplace == 0 ? true : false;
            if (param.inputType == 0)
            {

            }
            else
            {
                if (replace)
                {
                    AutoItHelper.ClearWords(param.Interval);
                }
                else
                {
                    AutoItHelper.MoveCursorToEnd(param.Interval);
                }
                Thread.Sleep(200);
                if (param.inputType == 1)
                {
                    if (param.Trigger == 1)
                    {
                        SetInputMethod();
                    }
                    AutoItHelper.Send(param.text, false, param.Interval);
                }
                else
                {
                    ClipboardSetText(param.text);
                    //Ctrl + V
                    TypeCombine(20, VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
                }
            }
            var verify = true;
            if (param.isVerify == 1)
            {
                if (replace)
                    verify = GetText().Equals(param.text);
                else
                    verify = GetText().EndsWith(param.text);
            }
            //try
            //{
            //    if (replace && param.isVerify == 1 && !verify && param.inputType == 1)
            //    {
            //        param.inputType = 0;
            //        return SetText(param);
            //    }
            //}
            //catch (Exception)
            //{
            //}

            return verify;
        }

        public IGuiNode[] QueryTargetSelectorAll(ParamQueryTargetSelectorAllModel param)
        {
            throw new NotImplementedException();
        }

        public string GetElementAttribute_V1(string name, bool isIntact, string apiVersion)
        {
            throw new NotImplementedException();
        }

        public List<string> GetSelectorAll(List<IGuiNode> guiNodes, string sourceSelector)
        {
            List<string> result = new List<string>();
            foreach (var item in guiNodes)
            {
                result.Add(item.NodeSelector);
            }
            return result;
        }

        public List<Rectangle> GetRegionAll(List<IGuiNode> guiNodes)
        {
            List<Rectangle> regions = new List<Rectangle>();
            foreach (var item in guiNodes)
            {
                regions.Add(item.BoundingRectangle);
            }
            return regions;
        }

        public List<string> GetTextAll(List<IGuiNode> guiNodes)
        {
            List<string> textes = new List<string>();
            foreach (var item in guiNodes)
            {
                textes.Add(item.GetText());
            }
            return textes;
        }

        public List<string> GetHtmlAll(List<IGuiNode> guiNodes)
        {
            throw new NotImplementedException();
        }

        public List<string> GetAttributeAll(List<IGuiNode> guiNodes, string attributeName, bool isIntact, string apiVersion)
        {
            throw new NotImplementedException();
        }

        public void SetElementSelectWithMatchModel(int selectType, int matchModel, string setValue)
        {
            throw new NotImplementedException();
        }

        public string GetCleanHtml()
        {
            throw new NotImplementedException();
        }
    }
}
