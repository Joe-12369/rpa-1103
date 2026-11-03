using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.GuiBase;
using II.RPA.Plugin.GUI.Commons.MouseMotions;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic
{
    /// <summary>
    /// Web类元素节点，浏览器特有动作 派生自IGuiWebNode_XXX
    /// </summary>
    public abstract class ElementGuiNode : IGuiWebNode_690
    {
        #region 派生类要实现的抽象属性
        protected ILogger logger = Logger.OfContext();

        public eGuiType GuiType => this.PageElement.TabPage.GuiType;

        #endregion
        public virtual bool IsUseControlSend => false;
        public PageElement PageElement { get; set; }

        public ElementGuiNode(PageElement pageElement)
        {
            this.PageElement = pageElement;
        }
        public virtual Rectangle BoundingRectangle => this.PageElement.BoundingRectangle;

        public virtual Rectangle IntoViewPortRectangle => this.PageElement.TabPage.IntoViewPortRectangle;

        public virtual Rectangle BuildRelativeElementRectangle => this.PageElement.BuildRelativeElementRectangle;
        /// <summary>
        /// 相似元素的selector，拾取相似元素时使用
        /// </summary>
        public string SimilarSelector { get; set; }

        private int processID = 0;
        private string anonymousFunctionDeclRegex = @"^\s*function\s*\(";
        public virtual int ProcessID
        {
            get
            {
                var processWindowHandle = GetProcessWindowHandle();
                if (this.processID == 0 && processWindowHandle != IntPtr.Zero)
                {
                    UiCommon.GetWindowThreadProcessId(processWindowHandle, out var pId);
                    this.processID = (int)pId;
                }
                return this.processID;
            }
            set
            {
                this.processID = value;
            }
        }
        private IntPtr GetProcessWindowHandle()
        {
            var clientWndHandle = this.PageElement?.TabPage?.ClientWndHandle ?? IntPtr.Zero;
            if (clientWndHandle == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            var rootWnd = NativeMethods.GetAncestor(clientWndHandle, GetAncestorFlags.GetRoot);
            return rootWnd == IntPtr.Zero ? clientWndHandle : rootWnd;
        }
        public virtual string UserDefinedID => this.PageElement.UserDefinedID;
        public virtual string TableUserDefinedID { get; private set; }

        public virtual string ClassName => throw new NotImplementedException();
        public virtual string ControlType
        {
            get
            {
                if (!string.IsNullOrEmpty(Dropdown))
                {
                    return "select";
                }
                if (!string.IsNullOrEmpty(this.UserDefinedID))
                {
                    var strArr = this.UserDefinedID.Split('/');
                    if (strArr.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(this.Attributes))
                        {
                            return $"{strArr[strArr.Length - 1]}";
                        }
                        return strArr[strArr.Length - 1];
                    }
                    return "";
                }
                else
                    return "";
            }
        }
        private string cachedProcessFullPath = string.Empty;
        public virtual string ProcessFullPath
        {
            get
            {
                var processWindowHandle = GetProcessWindowHandle();
                if (cachedProcessFullPath.IsNullOrEmpty() && processWindowHandle != IntPtr.Zero)
                {
                    try
                    {
                        var path = UiCommon.GetProgramAddressByCache(processWindowHandle);
                        cachedProcessFullPath = path;
                    }
                    catch (Exception)
                    {
                        cachedProcessFullPath = string.Empty;
                    }
                }

                return cachedProcessFullPath;
            }
        }
        private string cachedProcessName = string.Empty;
        public virtual string ProcessName
        {
            get
            {
                if (cachedProcessName.IsNullOrEmpty())
                {
                    try
                    {
                        var name = Path.GetFileName(this.ProcessFullPath);
                        cachedProcessName = name;
                    }
                    catch (Exception)
                    {
                        cachedProcessName = string.Empty;
                    }
                }

                return cachedProcessName;
            }
        }
        public virtual ExtendedTags ExtendedTags { get; set; } = ExtendedTags.None;

        [Obsolete]
        public virtual void ScrollIntoViewIfNeeded(string location = "center")
        {
            this.PageElement.ScrollIntoViewIfNeeded(location);
        }
        public virtual void ScrollIntoViewIfNeeded_V2()
        {
            this.PageElement.ScrollIntoViewIfNeeded_V2();
        }
        public virtual void SetCaupureBounding(Rectangle boundingRectangle) => this.PageElement.SetCaupureBounding(boundingRectangle);
        public virtual Rectangle BuildElementBounding()
        {
            return this.PageElement.BuildElementBounding();
        }
        public virtual Rectangle BuildRelativeElementBounding()
        {
            return this.PageElement.BuildRelativeElementBounding();
        }
        public string Idx => string.Empty;

        public string AutomationId => string.Empty;

        public string Name
        {
            get
            {
                return this.PageElement.GetAttribute("", "");
            }
        }

        public string Role => string.Empty;
        /// <summary>
        /// 自定义下拉框
        /// </summary>
        public string Dropdown { get; set; }

        public string Description => throw new NotImplementedException();

        public IntPtr WindowHandle { get { return IntPtr.Zero; } }

        public IGuiNode Parent => throw new NotImplementedException();

        public IGuiNode AutomationElementParent => throw new NotImplementedException();

        public string Attributes { get; set; }

        public List<IGuiNode> Children => null;

        public bool IsTopLevelWindow => false;



        public bool IsShow => this.PageElement.IsDisplayed();

        public bool IsTable
        {
            get
            {
                try
                {
                    TableUserDefinedID = this.PageElement.GetIsTableName();
                    return !string.IsNullOrEmpty(TableUserDefinedID);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
        private string tableNodeSelector;
        public virtual string TableNodeSelector
        {
            get
            {
                if (string.IsNullOrEmpty(this.TableUserDefinedID))
                {
                    return string.Empty;
                }
                var tableNode = this.PageElement.TabPage.CreateElementGuiNode(this.TableUserDefinedID);
                if (string.IsNullOrEmpty(tableNodeSelector))
                {
                    tableNodeSelector = tableNode.PageElement.BuildSelector(false);
                }
                var path = ExtractViewData.GetMainModuleFilepath(ProcessID, out var processName);
                var cssSelector = tableNode.PageElement.BuildCssSelector();

                var common = ExtractViewData.SetCommonPropsAndDetail(tableNodeSelector, this.GuiType, path, cssSelector: cssSelector, processName: processName);
                SelectorShell.SetDiscernHidden(common);

                common.CommonProps.Version = GlobleConfigurations.CurrentSelectorVersion;
                return common.ToJsonNoneFormating();
            }
        }
        public virtual bool TryGetTableElement(out IGuiWebNode_690 tableNode)
        {
            tableNode = null;
            try
            {
                TableUserDefinedID = this.PageElement.GetIsTableName();
                if (!string.IsNullOrEmpty(TableUserDefinedID))
                {
                    tableNode = this.PageElement.TabPage.CreateElementGuiNode(this.TableUserDefinedID);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        private string nodeSelector;
        public virtual string NodeSelector
        {
            get
            {
                return GetSelector(false);
            }
        }
        public virtual string PinSelector
        {
            get
            {
                PinPath pinPath = new PinPath()
                {
                    MainWindowHandle = this.PageElement.TabPage.BrowserWindowInfo.Handle.ToInt64(),
                    TabId = this.PageElement.TabPage.TabId,
                    UserDefinedID = this.PageElement.UserDefinedID,
                    GuiType = this.GuiType
                };

                RPACommon rpaCommon = new RPACommon();
                rpaCommon.CommonProps.GUIType = this.GuiType.ToString();
                rpaCommon.CommonProps.PinPath = pinPath;
                rpaCommon.CommonProps.Version = GlobleConfigurations.CurrentSelectorVersion;
                rpaCommon.CommonProps.ExtendedTags |= ExtendedTags.PinPath;
                SelectorProps selectorProps = new SelectorProps
                {
                    Name = ConstFields.PINSELECTOR_TAG_NAME,
                    Accurate = ConstFields.SELECTOR_SELECTORPROPS_ACCURATE_TURE,
                    Props = new List<Props>() { new Props {
                                    Name=ConstFields.PINSELECTOR_PROP_UDID,
                                    Accurate = ConstFields.PROP_ACCURATE_TURE,
                                    Value = this.PageElement.UserDefinedID
                                }
                        }
                };
                rpaCommon.Selector = new JArray(JToken.FromObject(selectorProps));
                return rpaCommon.ToJsonNoneFormating();
            }
        }
        public bool IsIntoView
        {
            get
            {
                var eleRect = this.BuildRelativeElementRectangle;
                var intoViewRect = this.PageElement.TabPage.IntoViewPortRectangle;
                var leftTop = new Point(eleRect.X, eleRect.Y);
                var rightLower = new Point(eleRect.X + eleRect.Width, eleRect.Y + eleRect.Height);
                return intoViewRect.Contains(leftTop) && intoViewRect.Contains(rightLower);
            }
        }
        //
        public virtual string BuildSmartSelector(List<Dictionary<string, object>> validNodeAttributes)
        {
            try
            {
                ParamBuildSelectorModel paramModel = new ParamBuildSelectorModel
                {
                    ValidNodeAttributes = validNodeAttributes,
                };
                var smartSelector = this.PageElement.BuildSelector(paramModel);
                if (string.IsNullOrEmpty(smartSelector))
                {
                    throw new RPAException(eErrorMessageType.Error_SmartSelector_BuildFail);
                }
                return smartSelector;
            }
            catch (Exception ex)
            {
                //统一提示语
                if (ex is TimeoutException || ex.IsRPAException(eErrorMessageType.Error_PluginRun_TimeOut, out _))
                {
                    throw new RPAException(eErrorMessageType.Error_PluginRun_Element_TimeOut, ex);
                }
                throw;
            }
        }
        public virtual string GetSelector(bool innerTextFirst)
        {
            try
            {
                if (string.IsNullOrEmpty(nodeSelector))
                {
                    nodeSelector = this.PageElement.BuildSelector(innerTextFirst);
                    if (!string.IsNullOrEmpty(SimilarSelector))
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(nodeSelector))
                                throw new RPAException(eErrorMessageType.Error_ElementNotType_Error);
                            nodeSelector = this.BuildSimilarSelector(nodeSelector, SimilarSelector);
                        }
                        catch (RPAException e)
                        {
                            nodeSelector = SimilarSelector;
                            SimilarSelector = null;
                            throw e;
                        }
                    }
                    //注释，除此方法使用外未发现其他用途，注释理论不影响，观察
                    //SimilarSelector = null;
                }
                var path = ExtractViewData.GetMainModuleFilepath(ProcessID, out var processName);
                var cssSelector = this.PageElement.BuildCssSelector();

                var common = ExtractViewData.SetCommonPropsAndDetail(nodeSelector, this.GuiType, path, cssSelector: cssSelector, processName: processName);
                if (!string.IsNullOrEmpty(SimilarSelector))
                {
                    common = SelectorShell.RepackWithSourceShell(common, SimilarSelector);
                }
                else
                {
                    SelectorShell.SetDiscernHidden(common);
                }
                common.CommonProps.Version = GlobleConfigurations.CurrentSelectorVersion;
                common.CommonProps.ExtendedTags = this.ExtendedTags;

                if (!string.IsNullOrEmpty(Dropdown))
                {
                    common.ElementDetail.ElementName = $"{TargrtXmlHelper.CreateInstance().GetTranslatedNameByCode("select")}_";
                }
                return JsonConvert.SerializeObject(common);
            }
            catch (Exception ex)
            {
                //统一提示语
                if (ex is TimeoutException || ex.IsRPAException(eErrorMessageType.Error_PluginRun_TimeOut, out _))
                {
                    throw new RPAException(eErrorMessageType.Error_PluginRun_Element_TimeOut, ex);
                }
                throw;
            }
        }

        public virtual string BuildSimilarSelector(string selector, string similarselector, bool isAuto = false)
        {
            if (selector.Contains("fullSelector") && similarselector.Contains("fullSelector") || selector.Contains("webCtrl") && similarselector.Contains("webCtrl"))
            {
                try
                {
                    return BuildSimilarSelectorOld(selector, similarselector);
                }
                catch (Exception)
                {
                }
            }
            var selector1 = UiCommon.ExtractSelectors(selector);
            var selector2 = UiCommon.ExtractSelectors(similarselector);
            var s1 = JsonConvert.DeserializeObject<List<SelectorProps>>(selector1);
            var s2 = JsonConvert.DeserializeObject<List<SelectorProps>>(selector2);

            List<SelectorProps> selectorList = isAuto ? CompareBuildCollectDataSelectorV2(new ParamBulidCollectDataSelectorModel
            {
                OriginalSelector = s1,
                CompareOriginalSelector = s2
            }) : CompareBuildSimileSelector(s1, s2);
            var result = JsonConvert.SerializeObject(selectorList);
            return result;
        }

        public virtual string BuildSimilarSelectorOld(string selector, string similarselector)
        {
            JArray selectorArray = JArray.Parse(selector);
            JArray similarArray = JArray.Parse(similarselector);
            if (selectorArray[0]["fullSelector"] != null && similarArray[0]["fullSelector"] != null && !string.IsNullOrEmpty(selectorArray[0]["fullSelector"].ToString()) && !string.IsNullOrEmpty(similarArray[0]["fullSelector"].ToString()))
            {
                var s1 = JsonConvert.DeserializeObject<List<SelectorProps>>(JsonConvert.SerializeObject(selectorArray[0]["fullSelector"]));
                var s2 = JsonConvert.DeserializeObject<List<SelectorProps>>(JsonConvert.SerializeObject(similarArray[0]["fullSelector"]));
                if (s1.Count != s2.Count)
                    throw new RPAException(eErrorMessageType.Error_ElementLevel_Error);
                var selectorList = new List<SelectorProps>();
                for (int i = 0; i < s1.Count; i++)
                {
                    var selectorProps = new SelectorProps();
                    if (s1[i].Type != s2[i].Type)
                        throw new RPAException(eErrorMessageType.Error_ElementNonsupport_Error);
                    else
                        selectorProps.Type = s1[i].Type;
                    if (s1[i].Name != s2[i].Name)
                        throw new RPAException(eErrorMessageType.Error_ElementNode_Error);
                    else
                        selectorProps.Name = s1[i].Name;
                    var propsCount = 0;
                    if (s1[i].Props.Count <= s2[i].Props.Count)
                    {
                        propsCount = s1[i].Props.Count;
                    }
                    else
                    {
                        propsCount = s2[i].Props.Count;
                    }
                    selectorProps.Props = new List<Props>();
                    for (int j = 0; j < propsCount; j++)
                    {
                        if (s1[i].Props[j]?.Name == "title" && s2[i].Props[j]?.Name == "title")
                        {
                            if (s1[i].Props[j].Value == s2[i].Props[j].Value)
                                if (!(s1[i].Props[j].Accurate == "true"))
                                    s1[i].Props[j].Accurate = "true";
                            selectorProps.Props.Add(s1[i].Props[j]);
                        }
                        if (s1[i].Props[j]?.Name == "url" && s2[i].Props[j]?.Name == "url")
                        {
                            if (s1[i].Props[j].Value == s2[i].Props[j].Value)
                                if (!(s1[i].Props[j].Accurate == "true"))
                                    s1[i].Props[j].Accurate = "true";
                            selectorProps.Props.Add(s1[i].Props[j]);
                        }
                        if (s1[i].Props[j]?.Name == "id" && s2[i].Props[j]?.Name == "id")
                        {
                            if (s1[i].Props[j].Value == s2[i].Props[j].Value)
                                if (s1[i].Props[j].Accurate == "true")
                                    selectorProps.Props.Add(s1[i].Props[j]);
                        }
                        if (s1[i]?.Props[j].Name == "src" && s2[i].Props[j]?.Name == "src")
                        {
                            if (s1[i].Props[j].Value == s2[i].Props[j].Value)
                                if (s1[i].Props[j].Accurate == "true")
                                    selectorProps.Props.Add(s1[i].Props[j]);

                        }

                        if (i == s1.Count - 1)
                        {
                            if (s1[i].Props[j]?.Name == "class" && s2[i].Props[j]?.Name == "class")
                            {
                                if (s1[i].Props[j].Value == s2[i].Props[j].Value)
                                {
                                    if (!(s1[i].Props[j].Accurate == "true"))
                                        s1[i].Props[j].Accurate = "true";
                                    selectorProps.Props.Add(s1[i].Props[j]);
                                }
                            }
                            if ((s1[i].Props[j]?.Name == "nth-child" && s2[i].Props[j]?.Name == "nth-child")
                                || (s1[i].Props[j]?.Name == "@index" && s2[i].Props[j]?.Name == "@index"))
                            {
                                if (s1[i].Props[j].Value == s2[i].Props[j].Value)
                                {
                                    if (!(s1[i].Props[j].Accurate == "true"))
                                        s1[i].Props[j].Accurate = "true";
                                    selectorProps.Props.Add(s1[i].Props[j]);
                                }
                            }
                        }
                        else
                        {
                            if (s1[i].Props[j]?.Name == "class" && s2[i].Props[j]?.Name == "class")
                            {
                                if (s1[i].Props[j].Value == s2[i].Props[j].Value)
                                    selectorProps.Props.Add(s1[i].Props[j]);
                            }
                            if ((s1[i].Props[j]?.Name == "nth-child" && s2[i].Props[j]?.Name == "nth-child")
                            || (s1[i].Props[j]?.Name == "@index" && s2[i].Props[j]?.Name == "@index"))
                            {
                                if (s1[i].Props[j].Value == s2[i].Props[j].Value)
                                    selectorProps.Props.Add(s1[i].Props[j]);
                            }
                        }
                        if (s1[i]?.Props[j].Name == "type" && s2[i].Props[j]?.Name == "type")
                        {
                            if (s1[i].Props[j].Value == s2[i].Props[j].Value)
                                if (s1[i].Props[j].Accurate == "true")
                                    selectorProps.Props.Add(s1[i].Props[j]);

                        }
                        if (s1[i]?.Props[j].Name == "outertext" && s2[i].Props[j]?.Name == "outertext")
                        {
                            if (s1[i].Props[j].Value == s2[i].Props[j].Value)
                                if (s1[i].Props[j].Accurate == "true")
                                    selectorProps.Props.Add(s1[i].Props[j]);
                        }
                    }
                    selectorList.Add(selectorProps);
                }
                var result = JsonConvert.SerializeObject(selectorList);

                return BrowserSelectorItem.GetAllSelectorItem(JToken.Parse(result), this.GuiType.ToString());
            }
            else
            {
                int num = 0;

                for (int i = 0; i < selectorArray.Count; i++)
                {
                    if (selectorArray[i].Value<string>("tagName") != null || selectorArray[i].Value<string>("tagName") != null)
                        if (selectorArray[i].Value<string>("tagName").ToLower() == "iframe" || selectorArray[i].Value<string>("tagName").ToLower() == "frame")
                        {
                            num = i;
                        }
                }
                JArray jArr = new JArray();
                int arrayCount = selectorArray.Count;

                for (int i = 0; i < arrayCount; i++)
                {
                    JObject jObj = new JObject();
                    if (i == 0)
                    {
                        if (selectorArray[i].Value<string>("title") != similarArray[i].Value<string>("title"))
                        {
                            throw new RPAException(eErrorMessageType.Error_ElementPlacement_Error);
                        }
                        else
                        {
                            jObj.Add("title", selectorArray[i].Value<string>("title"));
                        }
                        if (selectorArray[i].Value<string>("url") != similarArray[i].Value<string>("url"))
                        {
                            throw new RPAException(eErrorMessageType.Error_ElementPlacement_Error);
                        }
                        else
                        {
                            jObj.Add("url", selectorArray[i].Value<string>("url"));
                        }
                        if (selectorArray[i].Value<string>("selector") != similarArray[i].Value<string>("selector"))
                        {
                            throw new RPAException(eErrorMessageType.Error_ElementNonsupport_Error);
                        }
                        else
                        {
                            jObj.Add("selector", selectorArray[i].Value<string>("selector"));
                        }
                        if (arrayCount != similarArray.Count)
                            throw new RPAException(eErrorMessageType.Error_ElementLevel_Error);
                    }
                    else
                    {
                        if (selectorArray[i].Value<string>("tagName") != similarArray[i].Value<string>("tagName"))
                        {
                            throw new RPAException(eErrorMessageType.Error_ElementNode_Error);
                        }
                        else if (num != 0 && i > num)
                        {
                            var cc = selectorArray[0].Value<string>("cssSelectorEx").ToString().Split('>');
                            var dd = similarArray[0].Value<string>("cssSelector").ToString().Split('>');
                            for (int k = 0; k < cc.Length; k++)
                            {
                                jObj = new JObject();
                                var aa = cc[k].Split(':');
                                var ff = dd[k].Split(':');
                                if (aa[0].Equals(ff[0]))
                                {
                                    if (aa[1].Equals(ff[1]))
                                    {
                                        jObj.Add("tagName", aa[0].ToString());
                                        var gg = aa[1].Remove(0, 10);
                                        var ee = gg.Remove(gg.IndexOf(')'), 1);
                                        jObj.Add("nth-child", ee);
                                    }
                                    else
                                    {
                                        jObj.Add("tagName", aa[0].ToString());
                                    }
                                }
                                else
                                {
                                    throw new RPAException(eErrorMessageType.Error_ElementLevel_Error);
                                }
                                jArr.Add(jObj);
                            }
                            return jArr.ToString();
                        }
                        else if (num == 0)
                        {
                            var cc = selectorArray[0].Value<string>("cssSelectorEx").ToString().Split('>');
                            var dd = similarArray[0].Value<string>("cssSelector").ToString().Split('>');
                            for (int k = 0; k < cc.Length; k++)
                            {
                                jObj = new JObject();
                                var aa = cc[k].Split(':');
                                var ff = dd[k].Split(':');
                                if (aa[0].Equals(ff[0]))
                                {
                                    if (aa[1].Equals(ff[1]))
                                    {
                                        jObj.Add("tagName", aa[0].ToString());
                                        var gg = aa[1].Remove(0, 10);

                                        var ee = gg.Remove(gg.IndexOf(')'), 1);
                                        jObj.Add("nth-child", ee);
                                    }
                                    else
                                    {
                                        jObj.Add("tagName", aa[0].ToString());
                                    }
                                }
                                else
                                {
                                    throw new RPAException(eErrorMessageType.Error_ElementLevel_Error);
                                }
                                jArr.Add(jObj);
                            }
                            return jArr.ToString();
                        }
                    }
                    jArr.Add(jObj);
                }
                return jArr.ToString();
            }
        }

        public virtual string ClickTableCell(string input)
        {
            throw new NotImplementedException();
        }

        public virtual Bitmap ElementToBitmap()
        {
            throw new NotImplementedException();
        }

        public virtual List<IGuiNode> FindAll(IGuiNode fromElement = null, int maxResult = 1, int milliseconds = 3000)
        {
            throw new NotImplementedException();
        }

        public virtual string FindElementWithPicPath(string strPicPath)
        {
            throw new NotImplementedException();
        }

        public virtual string FindRelativeElementsInfo(string strRelativeSelector, ref JArray context)
        {
            return "";
        }

        public virtual IGuiNode FindRelativeNode(int position, int offsetX, int offsetY)
        {
            throw new NotImplementedException();
        }

        public virtual void Focus()
        {
            throw new NotImplementedException();
        }

        public virtual IGuiNode GetChildByIdx(int idx)
        {
            throw new NotImplementedException();
        }

        public virtual Point GetClickablePoint()
        {
            var point = new Point();
            var rect = this.GetRegion(3);
            point.X = rect.X + rect.Width / 2;
            point.Y = rect.Y + rect.Height / 2;
            return point;
        }
        public virtual string GetElementAttribute_V1(string name, bool isIntact, string apiVersion)
        {
            //IsExecution();
            return this.PageElement.GetAttribute_V1(name, isIntact, apiVersion);
        }
        public virtual string GetElementAttribute(string name, string apiVersion)
        {
            //IsExecution();
            return this.PageElement.GetAttribute(name, apiVersion);
        }

        public virtual string GetElementCheck()
        {
            return this.PageElement.GetCheckStatus();
        }

        public virtual string GetElementSelect(int selectType = 2)
        {
            var result = selectType == 1 ? "text" : "value";
            return this.PageElement.GetSelectOption(result).ToString();
        }

        public virtual string GetEnableClick()
        {
            return (!this.PageElement.GetProperty("disabled")).ToString();
        }

        public virtual Rectangle GetRegion(int positionType)
        {
            //chromeTab.IsActive();
            Rectangle rectangle = Rectangle.Empty;
            if (positionType == 1)
                throw new RPAException(eErrorMessageType.Error_NotImplemented);
            else if (positionType == 2)
                rectangle = this.BuildRelativeElementBounding();
            else if (positionType == 3)
            {
                rectangle = this.BuildElementBounding();
            }
            return rectangle;
        }

        public virtual string GetSubElements()
        {
            throw new NotImplementedException();
        }

        public virtual string GetTableData(string input)
        {
            return this.PageElement.GetTableData();
        }

        public virtual string GetText()
        {
            return this.PageElement.GetText();
        }

        public virtual void IntoView()
        {
            this.ScrollIntoViewIfNeeded_V2();
        }

        public virtual void MouseClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            if (clickParams.operateType == 0 && offsetX == 0 && offsetY == 0 && clickParams.cursorPos == 0)
            {
                GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => this.PageElement.Click());
            }
            else
            {
                //this.PageElement.TabPage.ActiveTab();
                MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                Thread.Sleep(200);
                GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Left));
            }
        }

        public virtual void MouseConterClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            Thread.Sleep(500);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Middle));
        }

        public virtual void MouseConterDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            Thread.Sleep(500);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => Mouse.DoubleClick(MouseButton.Middle));
        }

        public virtual void MouseDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            if (clickParams.operateType == 0 && offsetX == 0 && offsetY == 0)
            {
                GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => this.PageElement.DoubleClick());
            }
            else
            {
                MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                Thread.Sleep(500);
                GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => Mouse.DoubleClick());
            }
        }

        public virtual void MouseHover(UiElementHoverParams hoverParams = null)
        {
            if (hoverParams == null)
                hoverParams = new UiElementHoverParams();
            MouseMove(hoverParams.cursorPos, hoverParams.offsetX, hoverParams.offsetY, hoverParams.isUsePerson);
        }

        public virtual void MouseRightClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            if (clickParams.operateType == 0 && offsetX == 0 && offsetY == 0)
            {
                GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => this.PageElement.RightClick());
            }
            else
            {
                MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                Thread.Sleep(500);
                GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () => GUIUtils.MouseAction(clickParams.RPAMousePresses, RPAMouseButtons.Right));
            }
        }

        public virtual void MouseRightDoubleClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            throw new RPAException(this.GuiType, eErrorMessageType.Error_NotImplemented);
        }

        public virtual void SetElementAttribute(string name, string value)
        {
            this.PageElement.SetAttribute(name, value);
        }

        public virtual void SetElementCheck(int check)
        {
            //this.PageElement.Focus();
            if (check == 0)
                this.PageElement.SetCheckStatus(false);
            else
                this.PageElement.SetCheckStatus(true);
        }

        public virtual void SetElementMultipleSelect(int selectType, string setValue)
        {
            //this.PageElement.Focus();
            var type = selectType == 1 ? "text" : "value";
            var selectOptions = this.PageElement.GetSelectOptions(type);
            List<int> idxList;

            if (selectType == 0)
            {
                idxList = setValue.ToObject<List<int>>((ex) =>
                {
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_NotArrayError, ex);
                });

                if (idxList.Any(index => index > selectOptions.Count))
                {
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_IndexOverflow);
                }
            }
            else
            {

                List<string> inputList = setValue.ToObject<List<string>>((ex) =>
                {
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_NotArrayError, ex);
                });

                idxList = inputList.Select(input => selectOptions.FindIndex(so => so.Equals(input))).Distinct().ToList();
                idxList.Remove(-1);
                if (idxList.Count() == 0 || idxList.Count() != inputList.Count)
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_SelectValueIsNotValid);

            }
            this.PageElement.SetSelectOptions(idxList);
        }

        public virtual void SetElementSelectWithMatchModel(int selectType, int matchModel, string setValue)
        {
            List<int> idxList;
            //按索引位置
            if (selectType == 0)
            {
                var selectOptions = this.PageElement.GetSelectOptions("value");
                idxList = setValue.ToObject<List<int>>((ex) =>
                {
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_NotArrayError, ex);
                });

                if (idxList.Any(index => index > selectOptions.Count))
                {
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_IndexOverflow);
                }
            }
            //按value
            else if (selectType == 2)
            {
                var selectOptions = this.PageElement.GetSelectOptions("value");
                List<string> inputList = setValue.ToObject<List<string>>((ex) =>
                {
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_NotArrayError, ex);
                });

                idxList = inputList.Select(input => selectOptions.FindIndex(so => so.Equals(input))).Distinct().ToList();
                idxList.Remove(-1);
                if (idxList.Count() == 0 || idxList.Count() != inputList.Count)
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_SelectValueIsNotValid);
            }
            //按选项内容
            else
            {
                var selectOptions = this.PageElement.GetSelectOptions("text");
                setValue = setValue.ToObject<List<string>>((ex) =>
                {
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_SelectValueIsNotValid, ex);
                }).First();
                //选项内容时，支持多匹配模式
                List<string> inputList = new List<string>();
                if (matchModel == 0)
                {
                    inputList = selectOptions.FindAll(so => so.Equals(setValue));
                    idxList = inputList.Select(input => selectOptions.FindIndex(so => so.Equals(input))).Distinct().ToList();
                }
                else if (matchModel == 1)
                {
                    inputList = selectOptions.FindAll(so => so.Contains(setValue));
                    idxList = inputList.Select(input => selectOptions.FindIndex(so => so.Equals(input))).Distinct().ToList();
                }
                else
                {
                    inputList = selectOptions.FindAll(so => Regex.IsMatch(so, setValue));
                    idxList = inputList.Select(input => selectOptions.FindIndex(so => so.Equals(input))).Distinct().ToList();
                }
                idxList.Remove(-1);
                if (idxList.Count() == 0 || idxList.Count() != inputList.Count)
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_SelectValueIsNotValid);

            }
            this.PageElement.SetSelectOptions(idxList);
        }

        public virtual void SetElementSelect(int selectType, string setValue)
        {

            this.SetElementMultipleSelect(selectType, new[] { setValue }.ToJson());
        }

        public virtual void SetForeground()
        {
            this.PageElement.TabPage.SetForeground();
        }
        [Obsolete]
        public virtual void SetText(string text, int isReplace, bool isSimulate)
        {
            throw new NotImplementedException();
        }
        public List<string> GetElementShot()
        {
            return this.PageElement.GetElementShot();
        }
        public Image GetTabPageShot(bool isNeedScroll)
        {
            if (isNeedScroll)
                return this.PageElement.TabPage.CaptureByDebugger(25000);
            else
                return this.PageElement.TabPage.CaptureByTab();
        }
        public virtual bool SetText(ParamSetTextModel param)
        {
            //this.PageElement.TabPage.ActiveTab();
            #region 点击输入前先获取元素焦点，防止输入区域被场景浮窗遮盖
            try
            {
                this.PageElement.Focus();
            }
            catch
            {
                //一般情况元素设置焦点不会异常，但一些禁用状态下的控件可能会焦点异常，但保险起见加try
                logger.LogWarning("设置元素焦点异常");
            }
            #endregion
            UiElementClickParams uiElementClickParams = new UiElementClickParams();
            uiElementClickParams.operateType = 1;
            uiElementClickParams.cursorPos = param.ElementPosition;
            MouseClick(param.OffsetX, param.OffsetY, uiElementClickParams);
            Thread.Sleep(param.FocusTimeout);
            if (!this.PageElement.IsEditable())
            {
                new RPAException(this.GuiType, eErrorMessageType.Error_ElementNotOperation);
            }
            var replace = param.isReplace == 0 ? true : false;
            if (param.inputType == 0)
            {
                this.PageElement.Input(param.text, replace);
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
                    logger.LogInformation("浏览器模拟输入");
                    if (param.Trigger == 1)
                    {
                        SetInputMethod();
                    }
                    if (IsUseControlSend)
                        AutoItHelper.ControlInputMessage(param.text, NativeMethods.GetForegroundWindow(), IntPtr.Zero, false, param.Interval);
                    else
                        AutoItHelper.Send(param.text, false, param.Interval);
                }
                else
                {
                    logger.LogInformation("浏览器剪切输入");
                    UiCommon.ClipboardPaste(param.text, param.isReplace, "");
                    logger.LogInformation("浏览器剪切结束");
                }
            }
            var verify = true;
            if (param.isVerify == 1)
            {
                if (replace)
                    verify = this.PageElement.GetText().Equals(param.text);
                else
                    verify = this.PageElement.GetText().EndsWith(param.text);
            }
            //if (replace && param.isVerify == 1 && !verify && param.inputType == 1)
            //{
            //    this.PageElement.Input(param.text, replace);
            //}
            return verify;
        }

        public virtual string GetSubElements(ParamGetSubElements param)
        {
            var ids = this.PageElement.GetChildrenIdList(param.Filter, param.IndexStr);
            var parentNodes = this.PageElement.TabPage.CreateElementGuiNodes(ids);
            return parentNodes.Select(node =>
                SelectorShell.RepackShell(node.PageElement.BuildSelector(false), this.GuiType).ToJsonNoneFormating()
            ).ToJson();
        }

        public virtual string GetParent(ParamGetParent param)
        {
            var ids = this.PageElement.GetParentIdList(param.Filter, param.Level);
            var parentNodes = this.PageElement.TabPage.CreateElementGuiNodes(ids);
            return parentNodes.Select(node =>
                SelectorShell.RepackShell(node.PageElement.BuildSelector(false), this.GuiType).ToJsonNoneFormating()
            ).ToJson();
        }
        public virtual IGuiNode GetParentNode()
        {
            var ids = this.PageElement.GetParentIdList("", 1);
            var parentNodes = this.PageElement.TabPage.CreateElementGuiNodes(ids);
            return parentNodes.FirstOrDefault();
        }
        public virtual IGuiNode GetChildNode()
        {
            var ids = this.PageElement.GetChildrenIdList("", "1");
            var parentNodes = this.PageElement.TabPage.CreateElementGuiNodes(ids);
            return parentNodes.FirstOrDefault();
        }

        public virtual string GetSiblings(ParamGetSiblings param)
        {
            var ids = this.PageElement.GetSiblingsIdList(param.Filter, param.Position);
            var parentNodes = this.PageElement.TabPage.CreateElementGuiNodes(ids);
            return parentNodes.Select(node =>
                SelectorShell.RepackShell(node.PageElement.BuildSelector(false), this.GuiType).ToJsonNoneFormating()
            ).ToJson();
        }

        public virtual List<SelectorProps> MatchNextButtonSelector(ParamDoElementEditorModel paramModel)
        {
            throw new NotImplementedException();
        }
        [Obsolete("690版本开始使用数据采集V2版本，此方法不再维护，仅用于兼容老版本")]
        public virtual List<SelectorProps> AutoBulidCollectDataSelector(ParamBulidCollectDataSelectorModel paramModel)
        {
            string selectors = this.PageElement.TabPage.GetElementFromSelectorAll(paramModel.OnlyContainsTagsSelector.ToJson(), null);
            List<string> elementIds = selectors.ToObject<List<string>>();
            if (elementIds.Count == 1)
            {
                return paramModel.OriginalSelector;
            }
            elementIds.RemoveAll(id => id == this.UserDefinedID);
            var similarSelectors = elementIds.Select(eleId =>
            {
                var node = this.PageElement.TabPage.CreateElementGuiNode(eleId);
                return node.PageElement.BuildSelector(false, true);
            }).ToList();
            return this.BuilidCollectDataSelector(paramModel.OriginalSelector, similarSelectors);
        }
        [Obsolete("690版本开始使用数据采集V2版本，此方法不再维护，仅用于兼容老版本")]
        private List<SelectorProps> BuilidCollectDataSelector(List<SelectorProps> originalSelector, List<string> structureSimilarSelectors)
        {
            int targetIndex = -1;
            //基础selector的副本，即原串副本
            List<SelectorProps> originalSelectorPropsList = originalSelector.ToObject<List<SelectorProps>>();
            //所有相似串的集合，
            var similarSelectorPropsLists = structureSimilarSelectors.Select(sss => sss.ToObject<List<SelectorProps>>()).ToList();
            //移除首节点，首节点是tab信息
            similarSelectorPropsLists.ForEach((item) => item.RemoveAt(0));
            //移除所有节点层数不同的selector ，理论上是不存在的，只是个保险
            similarSelectorPropsLists.RemoveAll(sps => sps.Count() != originalSelectorPropsList.Count());
            //用来存储每一个selector节点与基础selector对应节点的差异值
            Dictionary<int, List<int>> selectorDiffCountList = new Dictionary<int, List<int>>();
            //int propTotalCount = originalSelectorPropsList.Select(sp => sp.Props.Count).Sum();
            string[] efectivePropNameArr = { "class", "id", "@index" };//有效属性
            for (int i = 0; i < originalSelectorPropsList.Count; i++)//拾取原串所有的props
            {
                //移除 基础selector的副本 中的 无效元素，避免影响差异值准确性
                originalSelectorPropsList[i].Props.RemoveAll(osp => !efectivePropNameArr.Contains(osp.Name));
                for (int j = 0; j < similarSelectorPropsLists.Count; j++)//遍历每一个疑似元素串
                {
                    if (!selectorDiffCountList.ContainsKey(j))
                    {
                        selectorDiffCountList[j] = new List<int>();
                    }
                    //相同属性数量
                    int samePropCount = originalSelectorPropsList[i].Props.Count(osp => similarSelectorPropsLists[j][i].Props.Any(ssp => ssp.Name == osp.Name && ssp.Value == osp.Value));
                    //总数量 - 相同数量 = 差异值
                    int diffCount = originalSelectorPropsList[i].Props.Count - samePropCount;
                    selectorDiffCountList[j].Add(diffCount);
                }
            }
            //生成整体的差异数据，便于后边排除逻辑使用
            var selectorListDiffData = selectorDiffCountList.Select((diffCountList) => new
            {
                Index = diffCountList.Key,
                DiffSum = diffCountList.Value.Sum(),
                FirstDiffIndex = diffCountList.Value.FindIndex(v => v > 0),
                LastDiffIndex = diffCountList.Value.FindLastIndex(v => v > 0),
                NodeCount = diffCountList.Value.Count
            }).ToList();

            selectorListDiffData.RemoveAll(data => data.DiffSum == 0);//排除自己
            if (selectorListDiffData.Count() == 0)
            {
                return originalSelectorPropsList;
            }
            if (selectorListDiffData.Count(data => (data.FirstDiffIndex == data.LastDiffIndex && data.LastDiffIndex == data.NodeCount - 1)) < selectorListDiffData.Count)
            {
                selectorListDiffData.RemoveAll(data => (data.FirstDiffIndex == data.LastDiffIndex && data.LastDiffIndex == data.NodeCount - 1));
            }
            int mindiffSum = selectorListDiffData.Min(data => data.DiffSum);//最小差异值
                                                                            //优先使用差异值最小的元素
            var selectorListDiffData_diffMin = selectorListDiffData.Where(data => data.DiffSum == mindiffSum);
            if (selectorListDiffData_diffMin.Count() > 0)
            {
                var firstDiffMinData = selectorListDiffData_diffMin.OrderBy(data => data.FirstDiffIndex).First();
                var lastDiffMaxData = selectorListDiffData_diffMin.OrderBy(data => data.LastDiffIndex).Last();
                int similarCount_firstDiffMinData = selectorListDiffData_diffMin.Count(data => data.FirstDiffIndex == firstDiffMinData.FirstDiffIndex);
                int similarCount_lastDiffMinData = selectorListDiffData_diffMin.Count(data => data.LastDiffIndex == lastDiffMaxData.LastDiffIndex);
                if (similarCount_firstDiffMinData > similarCount_lastDiffMinData)
                {
                    targetIndex = firstDiffMinData.Index;
                }
                else
                {
                    targetIndex = lastDiffMaxData.Index;
                }
                //targetIndex = selectorListDiffData_diffMin.OrderBy(data => data.firstDiffIndex).First();//豆腐块优先
                //targetIndex = selectorListDiffData_diffMin.OrderBy(data => data.LastDiffIndex).Last().Index;//表格样式优先（tag非table）
            }
            if (targetIndex == -1)
            {
                return originalSelectorPropsList;
            }
            else
            {
                //一定是使用完整的原串 与 最相似元素 去异求同，尽可能多的保留相似属性
                //originalSelectorPropsList = originalSelector.ToObject<List<SelectorProps>>();
                List<SelectorProps> similarSelectorPropsList = similarSelectorPropsLists[targetIndex];
                return this.CompareBuildSimileSelector(originalSelector, similarSelectorPropsList);
            }
        }
        [Obsolete("690版本开始使用数据采集V2版本，此方法不再维护，仅用于兼容老版本")]
        public virtual List<SelectorProps> CompareBulidCollectDataSelector(ParamBulidCollectDataSelectorModel paramModel)
        {
            List<SelectorProps> originalSelectorPropsList = paramModel.OriginalSelector.ToObject<List<SelectorProps>>();
            List<SelectorProps> similarSelectorPropsList = paramModel.CompareOriginalSelector.ToObject<List<SelectorProps>>();
            //移除原串中 多余的节点
            if (originalSelectorPropsList.Count > similarSelectorPropsList.Count)
            {
                originalSelectorPropsList.RemoveRange(similarSelectorPropsList.Count, originalSelectorPropsList.Count - similarSelectorPropsList.Count);
            }
            //移除相似串中 多余的节点
            if (originalSelectorPropsList.Count < similarSelectorPropsList.Count)
            {
                similarSelectorPropsList.RemoveRange(originalSelectorPropsList.Count, similarSelectorPropsList.Count - originalSelectorPropsList.Count);
            }
            //判断节点类型是否都相同
            for (int i = 0; i < originalSelectorPropsList.Count; i++)
            {
                if (originalSelectorPropsList[i].Type != similarSelectorPropsList[i].Type || originalSelectorPropsList[i].Name != similarSelectorPropsList[i].Name)
                {
                    return new List<SelectorProps>();
                }
            }
            return this.CompareBuildSimileSelector(originalSelectorPropsList, similarSelectorPropsList);
        }

        private List<SelectorProps> CompareBuildSimileSelector(List<SelectorProps> s1, List<SelectorProps> s2)
        {
            if (s1.Count != s2.Count)
                throw new RPAException(eErrorMessageType.Error_ElementLevel_Error);
            List<int> removeIdNodeIndexs = new List<int>();
            for (int i = 0; i < s1.Count; i++)
            {
                if (s1[i].Type != s2[i].Type)
                    throw new RPAException(eErrorMessageType.Error_ElementNonsupport_Error);
                if (s1[i].Name != s2[i].Name)
                    throw new RPAException(eErrorMessageType.Error_ElementNode_Error);
                var propsCount = s1[i].Props.Count;
                for (int j = propsCount - 1; j >= 0; j--)
                {
                    var propName = s1[i].Props[j]?.Name;
                    if (!this.TryGetProps(s2[i].Props, propName, out var prop))
                    {
                        if (propName.Equals("id") && s1[i].Accurate == "true")
                        {
                            removeIdNodeIndexs.Add(i);
                        }
                        s1[i].Props[j].Accurate = "false";
                        continue;
                    }
                    var sameValue = s1[i].Props[j].Value == prop.Value;
                    //原属性名相同、值相同、且 勾选的属性 
                    if (",title,url,".IndexOf($",{propName},") != -1)
                    {
                        if (!sameValue)
                        {
                            s1[i].Props[j].Accurate = "false";
                        }
                    }
                    else if (",id,src,outertext,type,".IndexOf($",{propName},") != -1)
                    {
                        if (!sameValue || s1[i].Props[j].Accurate == "false")
                        {
                            s1[i].Props.Remove(s1[i].Props[j]);
                        }
                        if (!sameValue && propName.Equals("id") && s1[i].Accurate == "true")
                        {
                            removeIdNodeIndexs.Add(i);
                        }
                    }
                    else if (propName == "innertext")
                    {
                        s1[i].Props[j].Accurate = ConstFields.SELECTOR_SELECTORPROPS_ACCURATE_FALSE;
                    }
                    else if (",@index,nth-child,".IndexOf($",{propName},") != -1)
                    {
                        //index 相同启用  不相同不起
                        //sameValue && 原状态true && 非首次组合生成
                        if (!sameValue)
                        {
                            s1[i].Props.Remove(s1[i].Props[j]);
                        }
                        else
                        {
                            s1[i].Props[j].Accurate = "true";
                        }
                    }
                    else if (",class,".IndexOf($",{propName},") != -1)
                    {
                        if (!sameValue)
                        {
                            s1[i].Props.Remove(s1[i].Props[j]);
                        }
                        else if (i == s1.Count - 1)
                        {
                            s1[i].Props[j].Accurate = "true";
                        }
                    }
                    else
                    {
                        s1[i].Props[j].Accurate = "false";
                    }
                }
            }

            foreach (var nodeIndex in removeIdNodeIndexs)
            {
                for (int i = nodeIndex - 1; i >= 0; i--)
                {
                    if (s1[i].Accurate == "false")
                    {
                        s1[i].Accurate = "true";
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return s1;
        }
        public bool TryGetProps(List<Props> props, string propName, out Props prop)
        {
            prop = props.FirstOrDefault(s => s.Name == propName);
            return prop != null;
        }

        public List<SelectorProps> CompareBuildCollectDataSelectorV2(ParamBulidCollectDataSelectorModel paramModel)
        {
            List<SelectorProps> s1 = paramModel.OriginalSelector;
            List<SelectorProps> s2 = paramModel.CompareOriginalSelector;
            //移除原串中 多余的节点
            if (s1.Count > s2.Count)
            {
                for (int i = s2.Count; i < s1.Count; i++)
                {
                    s1[i].Accurate = "false";
                }
            }
            int _removeIdNodeIdx = -1;
            for (int i = 0; i < s1.Count; i++)
            {
                if (i >= s2.Count)
                    break;
                if (s1[i].Type != s2[i].Type)
                    throw new RPAException(eErrorMessageType.Error_ElementNonsupport_Error);
                if (s1[i].Name != s2[i].Name)
                    throw new RPAException(eErrorMessageType.Error_ElementNode_Error);
                for (int j = s1[i].Props.Count - 1; j >= 0; j--)
                {
                    if (s1[i].Props[j].Accurate == "false" && i != s1.Count - 1)
                    {
                        continue;
                    }
                    var propName = s1[i].Props[j]?.Name;
                    if (!this.TryGetProps(s2[i].Props, propName, out var prop) || s1[i].Props[j].Value != prop.Value)
                    {
                        if (propName.Equals("id") && s1[i].Accurate == "true" && _removeIdNodeIdx == -1)
                        {
                            _removeIdNodeIdx = i;
                        }
                        s1[i].Props[j].Accurate = "false";
                        continue;
                    }
                    //【7.0.1】【&& propName != "innertext"】兼容agent 自动匹配相似元素时，可能存在的 只采集innertext相同的元素
                    if (i == s1.Count - 1 && ",id,src,outertext,type,class,@index,nth-child,title,url,".IndexOf($",{propName},") != -1)
                    {
                        s1[i].Props[j].Accurate = "true";
                    }
                }
            }
            if (_removeIdNodeIdx > 0)
            {
                var anchorAncestor = s1.Take(_removeIdNodeIdx);
                if (!anchorAncestor.Any(item => item.Name != "tab" && item.Accurate == "true"))
                {
                    anchorAncestor.ForEach((item) => item.Accurate = "true");
                }
            }
            return s1;
        }


        public virtual string GetNextElementBuildSelector(List<string> cssSelectorList)
        {
            try
            {
                if (cssSelectorList.Count == 0)
                {
                    cssSelectorList.Add("");
                }
                foreach (var cssSelector in cssSelectorList)
                {
                    string btnSelector = this.PageElement.TabPage.GetNextElementBuildSelector(cssSelector, true);
                    if (!string.IsNullOrEmpty(btnSelector))
                    {
                        return btnSelector;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new RPAException(eErrorMessageType.Error_NotFoundElement, ex);
            }
        }
        public virtual string GetColletionColumnSelector()
        {
            if (string.IsNullOrEmpty(nodeSelector))
            {
                nodeSelector = this.PageElement.BuildSelector(innerTextFirst: false, excludeInnerText: true);
            }
            return nodeSelector;
        }
        public IGuiNode GetTurningPageElement(List<string> cssSelectors)
        {
            string userDefinedID = this.PageElement.GetTurningPageElement(cssSelectors);
            if (string.IsNullOrEmpty(userDefinedID))
            {
                return null;
            }
            return this.PageElement.TabPage.CreateElementGuiNode(userDefinedID);
        }
        public List<SelectorProps> GetSimilarElementSelector(List<SelectorProps> originalSelector)
        {
            string userDefinedID = this.PageElement.GetSimilarElement();
            if (string.IsNullOrEmpty(userDefinedID))
            {
                return originalSelector;
            }
            var node = this.PageElement.TabPage.CreateElementGuiNode(userDefinedID);
            logger.LogDebug($"自动采集的相似元素 Text:{node.GetText()},{node.GetRegion(3).ToString()}");

            var similarSelector = node.PageElement.BuildSelector(false, false, true)?.ToObject<List<SelectorProps>>();


            if (similarSelector == null || similarSelector.Count == 0)
            {
                return originalSelector;
            }
            similarSelector.RemoveAt(0);
            logger.LogDebug($"自动采集元素的originalSelector:{originalSelector.ToJsonNoneFormating()}");
            logger.LogDebug($"自动采集元素的similarSelector:{similarSelector.ToJsonNoneFormating()}");
            return this.CompareBuildCollectDataSelectorV2(new ParamBulidCollectDataSelectorModel
            {
                OriginalSelector = originalSelector,
                CompareOriginalSelector = similarSelector
            });
        }
        public virtual string GetHtml()
        {
            return this.PageElement.GetHtml();
        }

        public virtual string GetCleanHtml()
        {
            return this.PageElement.GetCleanHtml();
        }

        public virtual void MouseThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            if (clickParams.operateType == 0 && offsetX == 0 && offsetY == 0 && clickParams.cursorPos == 0)
            {
                GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
                {
                    this.PageElement.DoubleClick();
                    this.PageElement.Click();
                });
            }
            else
            {
                // this.PageElement.TabPage.ActiveTab();
                //this.ScrollIntoViewIfNeeded_V2();
                MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                Thread.Sleep(500);
                GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
                {
                    Mouse.LeftDoubleClick();
                    Mouse.LeftClick();
                });
            }
        }

        public virtual void MouseRightThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {

            if (clickParams.operateType == 0 && offsetX == 0 && offsetY == 0)
            {
                GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
                {
                    this.PageElement.RightDoubleClick();
                    this.PageElement.RightClick();
                });
            }
            else
            {
                MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
                Thread.Sleep(500);
                GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
                {
                    Mouse.RightDoubleClick();
                    Mouse.RightClick();
                });
            }

        }

        public virtual void MouseConterThreeClick(int offsetX, int offsetY, UiElementClickParams clickParams = null)
        {
            MouseMove(clickParams.cursorPos, offsetX, offsetY, clickParams.isUsePerson);
            Thread.Sleep(500);
            GUIUtils.HandleClickEventWithAidKey(clickParams.AidKeyType, () =>
            {
                Mouse.DoubleClick(FlaUI.Core.Input.MouseButton.Middle);
                Mouse.Click(FlaUI.Core.Input.MouseButton.Middle);
            });
        }

        public virtual void SetText(string text, int isReplace, int inputType, int interval)
        {
            throw new NotImplementedException();
        }

        public virtual string GetElementSelectEx(string strJson)
        {
            logger.LogDebug($"ElementGuiNode.GetElementSelectEx param:{strJson}");
            //chromeTab.IsActive();
            int selectMode = 0, selectType = 1;

            GetElementSelectExParamModel _GetElementSelectExParam = null;
            try
            {
                _GetElementSelectExParam = JsonConvert.DeserializeObject<GetElementSelectExParamModel>(strJson);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"Chrome GetElementSelectEx param Err. Json:{strJson}");
                throw new RPAException(this.GuiType, eErrorMessageType.Error_ParamsError);
            }
            finally
            {
                if (_GetElementSelectExParam == null || _GetElementSelectExParam.selectMode == -1 || _GetElementSelectExParam.selectType == -1)
                {
                    logger.LogError($"Chrome GetElementSelectEx param Err. Json:{strJson}");
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_ParamsError);
                }
                selectMode = _GetElementSelectExParam.selectMode;
                selectType = _GetElementSelectExParam.selectType;
            }

            string strContent = string.Empty;
            if (selectMode == 0)//获取当前下拉选项
            {
                switch (selectType)
                {
                    case 0:
                        var selectedItem = this.PageElement.GetSelectOptions("text", 1);
                        var strTextList = this.PageElement.GetSelectOptions("text");
                        var indexList = new List<int>();
                        foreach (var strText in selectedItem)
                        {
                            int _index = strTextList.IndexOf(strText);
                            indexList.Add(_index);
                        }
                        var IndexItem = new JsonArrayClass(indexList);
                        strContent = IndexItem.ToJson();
                        break;
                    case 1:
                        var selectedItems = this.PageElement.GetSelectOptions("text", 1);
                        var itemArray = new JsonArrayClass(selectedItems);
                        strContent = itemArray.ToJson();
                        break;
                    case 2:
                        var selectedValueItems = this.PageElement.GetSelectOptions("value", 1);
                        var valueArray = new JsonArrayClass(selectedValueItems);
                        strContent = valueArray.ToJson();
                        break;
                }
            }
            else //获取全部下拉选项
            {
                switch (selectType)
                {
                    case 0:
                        var strTextList = this.PageElement.GetSelectOptions("text");
                        var IndexItem = new JsonArrayClass(Enumerable.Range(0, strTextList.Count).ToList());
                        strContent = IndexItem.ToJson();
                        break;
                    case 1:
                        var strTextList2 = this.PageElement.GetSelectOptions("text");
                        var TextItem = new JsonArrayClass(strTextList2);
                        strContent = TextItem.ToJson();
                        break;
                    case 2:
                        var strValueList = this.PageElement.GetSelectOptions("value");
                        var ValueItem = new JsonArrayClass(strValueList);
                        strContent = ValueItem.ToJson();
                        break;
                }
            }
            return strContent;
        }
        public virtual void MouseMove(int cursorPos, int offsetX = 0, int offsetY = 0, bool isUsePerson = false)
        {
            this.PageElement.TabPage.ActiveTab();
            Point point = new Point(0, 0);
            Rectangle rect = this.PageElement.BuildRelativeElementBounding();
            var center = rect.Center();
            var docRect = this.PageElement.TabPage.GetIntoViewPortRect();
            if (cursorPos == MouseParamDef.CUR_POS_CENTER)
            {
                if (!docRect.Contains(center))
                {
                    this.PageElement.ScrollIntoViewIfNeeded_V2();
                    point = GetClickablePoint();
                }
                else
                {
                    center = GetRegion(3).Center();
                    point = center;
                }
                point = new Point(point.X + offsetX, point.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_LEFT_TOP)//左上
            {
                if (!docRect.Contains(new Point(rect.X, rect.Y)))
                {
                    this.PageElement.ScrollIntoViewIfNeeded_V2();
                }
                rect = GetRegion(3);
                point = new Point(rect.X + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RIGHT_TOP)//右上
            {
                if (!docRect.Contains(new Point(rect.X + rect.Width, rect.Y)))
                {
                    this.PageElement.ScrollIntoViewIfNeeded_V2();
                }
                rect = GetRegion(3);
                point = new Point(rect.X + rect.Width + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_LEFT_BOTTOM)//左下
            {
                if (!docRect.Contains(new Point(rect.X, rect.Y + rect.Height)))
                {
                    this.PageElement.ScrollIntoViewIfNeeded_V2();
                }
                rect = GetRegion(3);
                point = new Point(rect.X + offsetX, rect.Y + rect.Height + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RIGHT_BOTTOM)//右下
            {
                if (!docRect.Contains(new Point(rect.X + rect.Width, rect.Y + rect.Height)))
                {
                    this.PageElement.ScrollIntoViewIfNeeded_V2();
                }
                rect = GetRegion(3);
                point = new Point(rect.X + rect.Width + offsetX, rect.Y + rect.Height + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_RANDOM)//随机
            {
                if (!docRect.Contains(rect))
                {
                    this.PageElement.ScrollIntoViewIfNeeded_V2();
                }
                rect = GetRegion(3);
                point = rect.GetRandomPoint();
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_TOP)//上中
            {
                if (!docRect.Contains(new Point(rect.X + rect.Width / 2, rect.Y)))
                {
                    this.PageElement.ScrollIntoViewIfNeeded_V2();
                }
                rect = GetRegion(3);
                point = new Point(rect.X + rect.Width / 2 + offsetX, rect.Y + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_LEFT)//左中
            {
                if (!docRect.Contains(new Point(rect.X, rect.Y + rect.Height / 2)))
                {
                    this.PageElement.ScrollIntoViewIfNeeded_V2();
                }
                rect = GetRegion(3);
                point = new Point(rect.X + offsetX, rect.Y + rect.Height / 2 + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_RIGHT)//右中
            {
                if (!docRect.Contains(new Point(rect.X + rect.Width, rect.Y + rect.Height / 2)))
                {
                    this.PageElement.ScrollIntoViewIfNeeded_V2();
                }
                rect = GetRegion(3);
                point = new Point(rect.X + rect.Width + offsetX, rect.Y + rect.Height / 2 + offsetY);
            }
            else if (cursorPos == MouseParamDef.CUR_POS_CENTER_BOTTOM)//下中
            {
                if (!docRect.Contains(new Point(rect.X + rect.Width / 2, rect.Y + rect.Height)))
                {
                    this.PageElement.ScrollIntoViewIfNeeded_V2();
                }
                rect = GetRegion(3);
                point = new Point(rect.X + rect.Width / 2 + offsetX, rect.Y + rect.Height + offsetY);
            }
            logger.LogDebug($"rect:{rect.ToJson()} point: {point.ToJson()}");
            II.RPA.Plugin.GUI.Commons.MouseMotions.MouseMotionHelper.PersonMoveMouse(isUsePerson, point);
        }
        public virtual JObject GetInputMethod()
        {
            string result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "GetState"), "{}");
            var info = JObject.Parse(result)["info"];
            var state = JObject.Parse(info.ToString());
            return state;
        }
        public virtual void SetInputMethod(string model = "en_us")
        {
            JObject j = new JObject();
            j.Add("state", model);
            var result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "SetState"), j.ToString());
        }

        public virtual string ElementDownload(string saveFile = "", string fileName = "", int timeout = 10000)
        {
            //chromeTab.IsActive();
            //if (string.IsNullOrEmpty(userDefinedID))
            //{
            //    throw new ChromeException(eErrorMessageType.Error_NotFoundElement);
            //}
            //chromeElement = new ChromeElement(chromeTab, userDefinedID);
            this.ScrollIntoViewIfNeeded();
            //ChromeTab.GetMousePosition(getTitle);//获取一下窗口句柄 获得元素的相对坐标
            var rect = this.BuildElementBounding();
            Mouse.LeftClick(new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2));
            JToken traceItem = null;
            JArray array = null;
            var stop = new Stopwatch();
            stop.Start();
            do
            {
                //Thread.Sleep(100);去除延时，会存在下载文件过小，还没来得及检索下载列表就被过滤掉的情况
                if (stop.ElapsedMilliseconds <= timeout)
                {
                    try
                    {
                        traceItem = this.PageElement.TabPage.Downloading();
                        logger.LogInformation($"ElementDownload: {traceItem}");
                        array = JArray.Parse(traceItem.ToString());
                        Thread.Sleep(100);
                    }
                    catch (Exception e)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, e, "获取Trace失败");
                    }
                }
                else
                {
                    stop.Stop();
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_DownloadTimeOut);
                }
            } while (array.Count == 0);
            stop.Stop();
            string path = string.Empty;
            if (string.IsNullOrEmpty(fileName))
                path = Path.Combine(saveFile, Path.GetFileName(traceItem[0].Value<string>("filename")));
            else
                path = Path.Combine(saveFile, fileName);
            #region [等待另存为窗口出现]
            if (DownloadFileUtils.FindSaveAsFrom("另存为"))
            {
                if (!DownloadFileUtils.SaveAs(saveFile, "另存为", ref fileName))
                {
                    Thread.Sleep(200);
                    DownloadFileUtils.SaveAs(saveFile, "另存为".I18n("ElementGuiNode_1C4ECEB"), ref fileName);
                }
                if (DownloadFileUtils.FindSaveAsFrom("确认另存为"))
                {
                    DownloadFileUtils.NotarizeSaveAs();
                    return Path.Combine(saveFile, fileName).Replace("\\", "\\\\");
                }
                return Path.Combine(saveFile, fileName).Replace("\\", "\\\\");
            }
            #endregion
            if (traceItem[0] == null)
                throw new RPAException(this.GuiType, eErrorMessageType.Error_DownloadError);
            #region [等待下载]
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                var model = new ParamDownloadModel();
                while (stopWatch.ElapsedMilliseconds <= timeout)
                {
                    int id = 0;
                    int.TryParse(traceItem[0].Value<string>("id"), out id);
                    model.id = id;
                    var results = this.PageElement.TabPage.DownLoadSearch(model);
                    foreach (var item in results)
                    {
                        if (traceItem[0].Value<string>("id") == item.Value<string>("id").ToString())
                        {
                            if (path != null)
                            {
                                DownloadFileUtils.SelectedReserver(this.PageElement.TabPage.BrowserWindowInfo.Handle);

                                if (File.Exists(item.Value<string>("filename").ToString()))
                                {
                                    if (path != item.Value<string>("filename").ToString())
                                    {
                                        File.Move(item.Value<string>("filename").ToString(), path);
                                        //File.Copy(item.Value<string>("filename").ToString(), path, overwrite: true);
                                    }
                                    stopWatch.Stop();
                                    return path.Replace("\\", "\\\\");
                                }
                            }
                        }
                    }
                }
                stopWatch.Stop();
            }
            catch (Exception e)
            {
                throw new RPAException(this.GuiType, eErrorMessageType.Error_DownloadError, e);
            }
            #endregion
            throw new RPAException(this.GuiType, eErrorMessageType.Error_DownloadTimeOut);
        }
        public virtual string Executejs(string scriptCode, string @params, string executionEnvironment = "", bool isAsyncCode = false, float waitResultTimeout = 0, bool catchException = false)
        {
            var ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            long times = Convert.ToInt64(ts.TotalMilliseconds);
            if (string.IsNullOrEmpty(@params) || @params == "[]")
            {
                var jsCode = BuilderTargetJsCode(scriptCode, isAsyncCode, catchException, out string requestId, executionEnvironment: executionEnvironment);
                var targetCode = AddAsyncCodeIfNeeded(jsCode, isAsyncCode, out string windowObjName);
                return WaitJSResponseIfNeeded(targetCode, isAsyncCode, waitResultTimeout, windowObjName, requestId, executionEnvironment);
            }
            else
            {
                JArray param = null;
                try
                {
                    param = JArray.Parse(@params);
                }
                catch (Exception)
                {
                    throw new RPAException(this.GuiType, eErrorMessageType.Error_ParamsError);
                }
                int index = param.Count;

                string paras = string.Empty;
                foreach (var item in param)
                {
                    switch (item.Type)
                    {
                        case JTokenType.String:
                            paras += "'" + item.ToString() + "'";
                            break;
                        case JTokenType.Integer:
                            paras += Convert.ToInt32(item);
                            break;
                        case JTokenType.Float:
                            paras += Convert.ToDouble(item);
                            break;
                        case JTokenType.Boolean:
                            paras += Convert.ToBoolean(item);
                            break;
                        case JTokenType.Null:
                            paras += "null";
                            break;
                        default:
                            throw new RPAException(this.GuiType, eErrorMessageType.Error_ParamsError);
                    }

                    index--;
                    if (index > 0)
                        paras += ",";
                }

                var targetCode = AddAsyncCodeIfNeeded(BuilderTargetJsCode(scriptCode, isAsyncCode, catchException, out string requestId, param.Count(), paras, executionEnvironment), isAsyncCode, out string windowObjName);
                return WaitJSResponseIfNeeded(targetCode, isAsyncCode, waitResultTimeout, windowObjName, requestId, executionEnvironment);
            }
        }

        public string[] GetUsableAttributes()
        {
            return this.PageElement.GetUsableAttributes();
        }
        public string ElementDownloader(ParamDownloaderModel param)
        {
            string path = string.Empty;
            try
            {
                this.PageElement.TabPage.Downloader.VerifyFilePath(param.saveFile);
                this.PageElement.TabPage.Downloader.VerifyFileName(param.fileName);
                this.PageElement.TabPage.Downloader.AddCreatedEvent(param);
                UiElementClickParams uiElementClickParams = new UiElementClickParams();
                uiElementClickParams.operateType = param.clickMode;
                MouseClick(0, 0, uiElementClickParams);
                path = this.PageElement.TabPage.Downloader.Downloader(param.saveFile, param.fileName, param.interval, param.InputMode, param.SaveMode, param.timeout, param.waitDialogTime);
            }
            catch (RPAException rpaEx)
            {
                logger.LogDebug($"IsRunBrowser:{this.PageElement.TabPage.Browser.IsRunBrowser}");
                if (!this.PageElement.TabPage.Browser.IsRunBrowser)
                    throw new RPAException(eErrorMessageType.Error_BrowserQuit_Download, rpaEx);
                throw rpaEx;
            }
            finally
            {
                this.PageElement.TabPage.Downloader.RemoveCreatedEvent();
            }
            return path.Replace("\\", "\\\\");

        }

        public IGuiNode[] QueryTargetSelectorAll(ParamQueryTargetSelectorAllModel paramModel)
        {
            var result = this.PageElement.TabPage.QueryTargetSelectorAll(paramModel);
            return result;
        }
        public List<string> GetSelectorAll(List<IGuiNode> guiNodes, string sourceSelector)
        {
            try
            {
                List<string> elementIds = guiNodes.Select(node => { return node.UserDefinedID; }).ToList();
                var selector = UiCommon.ExtractSelectors(sourceSelector);
                JArray inputSelectorPropsArr = JArray.Parse(selector);
                var nodes = this.PageElement.TabPage.CreateElementGuiNodes(elementIds);
                var retData = nodes.Select(node =>
                {
                    var nodeSelector = node.PageElement.BuildSelector(false);
                    JArray selectorPropsArr = JArray.Parse(nodeSelector);
                    selectorPropsArr[0] = inputSelectorPropsArr[0];
                    return SelectorShell.RepackWithSourceShell(selectorPropsArr, sourceSelector, true).ToJsonNoneFormating();
                }).ToList();
                return retData;
            }
            catch (Exception ex)
            {
                //统一提示语
                if (ex is TimeoutException || ex.IsRPAException(eErrorMessageType.Error_PluginRun_TimeOut, out _))
                {
                    throw new RPAException(eErrorMessageType.Error_PluginRun_Element_TimeOut, ex);
                }
                throw;
            }
        }
        public List<Rectangle> GetRegionAll(List<IGuiNode> guiNodes)
        {
            List<string> elementId = guiNodes.Select(node => { return node.UserDefinedID; }).ToList();
            return this.PageElement.BuildElementBoundingAll(elementId);
        }

        public List<string> GetTextAll(List<IGuiNode> guiNodes)
        {
            List<string> elementIds = guiNodes.Select(node => { return node.UserDefinedID; }).ToList();
            return this.PageElement.GetTextAll(elementIds);
        }

        public List<string> GetHtmlAll(List<IGuiNode> guiNodes)
        {
            List<string> elementIds = guiNodes.Select(node => { return node.UserDefinedID; }).ToList();
            return this.PageElement.GetHtmlAll(elementIds);
        }
        public List<string> GetAttributeAll(List<IGuiNode> guiNodes, string attributeName, bool isIntact, string apiVersion)
        {
            List<string> elementIds = guiNodes.Select(node => { return node.UserDefinedID; }).ToList();
            return this.PageElement.GetAttributeAll(elementIds, attributeName, isIntact, apiVersion);
        }

        public string GetSimilarSelectorByAuto()
        {
            string similarSelectStr;
            string originSelector = "";
            try
            {
                originSelector = this.PageElement.BuildSelector(false, false, true);
                string userDefinedID = this.PageElement.GetSimilarElement();
                if (string.IsNullOrEmpty(userDefinedID))
                {
                    similarSelectStr = originSelector;
                }
                else
                {
                    var node = this.PageElement.TabPage.CreateElementGuiNode(userDefinedID);
                    logger.LogDebug($"自动采集的相似元素 Text:{node.GetText()},{node.GetRegion(3).ToString()}");

                    var similarSelector = node.PageElement.BuildSelector(false, false, true);
                    similarSelectStr = this.BuildSimilarSelector(originSelector, similarSelector, isAuto: true);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"自动生成相似元素失败:{ex}");
                similarSelectStr = originSelector;
            }
            var path = ExtractViewData.GetMainModuleFilepath(ProcessID, out var processName);
            var cssSelector = this.PageElement.BuildCssSelector();

            var common = ExtractViewData.SetCommonPropsAndDetail(similarSelectStr, this.GuiType, path, cssSelector: cssSelector, processName: processName);
            SelectorShell.SetDiscernHidden(common);

            common.CommonProps.Version = GlobleConfigurations.CurrentSelectorVersion;
            return JsonConvert.SerializeObject(common);
        }

        private string AddAsyncCodeIfNeeded(string sourceCode, bool isAsyncCode, out string windowObjName)
        {
            windowObjName = "";
            if (isAsyncCode)
            {
                var ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                long times = Convert.ToInt64(ts.TotalMilliseconds);
                windowObjName = $"window.jscomponent_result{times}";
                sourceCode = $"{sourceCode}\r\n" +
                            ".then(result=>{\r\n" +
                            $"  {windowObjName} = " + "{\"isComplete\": true, \"success\": true, \"response\": result};\r\n" +
                            "})" +
                            ".catch(error=>{\r\n" +
                            "   errorMsg = '未知异常';\r\n" +
                            "   if(typeof error === 'string'){\r\n" +
                            "       errorMsg = error;\r\n" +
                            "   }else if(error && typeof error.message === 'string'){" +
                            "       errorMsg = error.message;\r\n" +
                            "   }\r\n" +
                            $"  {windowObjName} = " + "{\"isComplete\": true, \"success\": false, \"response\": errorMsg};\r\n" +
                            "});";
            }
            return sourceCode;
        }

        private string WaitJSResponseIfNeeded(string code, bool isAsyncCode, float waitResultTimeout, string windowObjName, string requestId = "", string executionEnvironment = "")
        {
            var result = this.PageElement.Executejs(code, executionEnvironment);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            if (isAsyncCode)
            {
                var getResultCode = $"(function()" + "{return " + windowObjName + ";})()";
                var errorMsg = string.Empty;
                while (true)
                {
                    result = this.PageElement.Executejs(getResultCode, executionEnvironment);
                    try
                    {
                        var response = JObject.Parse(result);
                        if (response.ContainsKey("isComplete") && response.GetValue<bool>("isComplete"))
                        {
                            if (response.ContainsKey("response"))
                            {
                                if (response["response"].Type == JTokenType.String)
                                {
                                    result = response.GetValue<string>("response");
                                }
                                else
                                {
                                    result = response["response"]?.ToJson();
                                }
                            }
                            else
                            {
                                result = string.Empty;
                            }

                            if (response.GetValue<bool>("success"))
                            {
                                break;
                            }
                            errorMsg = $"{result}";
                        }
                    }
                    catch { }

                    if (!string.IsNullOrEmpty(errorMsg))
                    {
                        throw new Exception(errorMsg);
                    }

                    if (waitResultTimeout >= 0 && stopwatch.ElapsedMilliseconds >= (waitResultTimeout * 1000))
                    {
                        throw new Exception("等待JS执行结果超时".I18n("controlType_ElementGuiNode_6BDBCD57"));
                    }
                    Task.Delay(10).Wait();
                }
                return result;
            }
            else
            {
                var errorMsg = string.Empty;
                try
                {
                    var response = JObject.Parse(result);
                    if (response.GetValue<string>("requestId") == requestId && !response.GetValue<bool>("success"))
                    {
                        errorMsg = response.GetValue<string>("response");
                    }
                }
                catch { }

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    throw new Exception(errorMsg);
                }
                return result;
            }
        }

        private string BuilderTargetJsCode(string sourceCode, bool isAsyncCode, bool catchException, out string requestId, int paramsCount = 0, string inputParams = null, string executionEnvironment = null)
        {
            requestId = Guid.NewGuid().ToString("N");
            var targetParams = string.IsNullOrEmpty(inputParams) ? string.Empty : inputParams;
            if (isAsyncCode || !catchException)
            {
                // 因为一些历史包袱老版本的执行JS组件传过来的jscode可能是非标的，直接加上异常捕获的代码可能会有兼容性问题，
                // 此处使用catchException来区分新老版本的执行JS组件，两个版本走不同的代码拼接逻辑避免出现兼容性问题
                if (!catchException)
                {
                    var ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    long times = Convert.ToInt64(ts.TotalMilliseconds);
                    var code = $"scriptCode{times}={sourceCode}";
                    this.PageElement.Executejs(code, executionEnvironment);
                    Thread.Sleep(100);
                    return $"scriptCode{times}({targetParams})";
                }

                return $"({sourceCode})({targetParams})";
            }
            else
            {
                var codeStringBuilder = new StringBuilder();
                var resultMatch = Regex.Match(sourceCode, anonymousFunctionDeclRegex, RegexOptions.Multiline);

                var functionParamList = new List<string>();
                for (var index = 0; index < paramsCount; index++)
                {
                    functionParamList.Add($"dynamic_test_param_{index}");
                }
                var functionParam = paramsCount > 0 ? string.Join(",", functionParamList) : string.Empty;
                if (resultMatch.Success)
                {
                    var ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    long times = Convert.ToInt64(ts.TotalMilliseconds);
                    var targetFuncName = $"targetFunc_{times}";
                    sourceCode = Regex.Replace(sourceCode, anonymousFunctionDeclRegex, $"{targetFuncName} = function (", RegexOptions.Multiline);

                    var targetCode = $"function({functionParam})" + "{\r\n" +
                                     $"   {sourceCode}\r\n" +
                                      "   try{\r\n" +
                                     $"       return {targetFuncName}({functionParam});\r\n" +
                                      "   }catch(error){\r\n" +
                                      "       errorMsg = '未知错误';\r\n" +
                                      "       if(typeof error === 'string'){\r\n" +
                                      "           errorMsg = error;\r\n" +
                                      "       }else if(error && typeof error.message === 'string'){\r\n" +
                                      "           errorMsg = error.message;\r\n" +
                                      "       }\r\n" +
                                      "       return {\"isComplete\": true, \"success\": false, \"response\": errorMsg, \"requestId\": " + $"\"{requestId}\"" + "};\r\n" +
                                      "   }\r\n" +
                                      "}";
                    return $"({targetCode})({targetParams})";
                }
                else
                {
                    var targetCode = $"function({functionParam})" + "{\r\n" +
                                      "   try{\r\n" +
                                     $"       {sourceCode}\r\n" +
                                      "   }catch(error){\r\n" +
                                      "       errorMsg = '未知错误';\r\n" +
                                      "       if(typeof error === 'string'){\r\n" +
                                      "           errorMsg = error;\r\n" +
                                      "       }else if(error && typeof error.message === 'string'){\r\n" +
                                      "           errorMsg = error.message;\r\n" +
                                      "       }\r\n" +
                                      "       return {\"isComplete\": true, \"success\": false, \"response\": errorMsg, \"requestId\": " + $"\"{requestId}\"" + "};\r\n" +
                                      "   }\r\n" +
                                      "}";
                    return $"({targetCode})({targetParams})";
                }
            }
        }

        public void ElementFileUploader(ParamFileUploaderModel param)
        {
            UiElementClickParams uiElementClickParams = new UiElementClickParams();
            uiElementClickParams.operateType = 1;
            MouseClick(0, 0, uiElementClickParams);
            //和下载组件处理文件对话框逻辑部分通用，所以逻复用 Downloader 类库
            this.PageElement.TabPage.Downloader.FileUploader(param.uploadType, param.filePath, param.interval, param.InputMode, param.waitDialogTime);
        }

        public virtual string GetLineAndBarChartData(Plugin.GUI.Commons.Params.ParamGetChartDataModel param)
        {
            return this.PageElement.GetLineAndBarChartData(new Params.ParamGetChartDataModel
            {
                moveDirection = param.moveDirection,
                moveSpeed = param.moveSpeed,
                moveModel = param.moveModel,
                offset = param.offset,
                parentElementId = param.parentElementId
            });
        }
        public virtual string GetPieChartData(Plugin.GUI.Commons.Params.ParamGetChartDataModel param)
        {
            return this.PageElement.GetPieChartData(new Params.ParamGetChartDataModel
            {
                radiusRatio = param.radiusRatio,
                horOffset = param.horOffset,
                verOffset = param.verOffset,
                moveSpeed = param.moveSpeed
            });
        }
    }
}

