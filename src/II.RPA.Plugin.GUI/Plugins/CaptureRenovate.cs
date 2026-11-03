using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.Core.Commons;
using Newtonsoft.Json;
using System.IO;
using System.Drawing;
using II.RPA.WinAutomation.CommonUtility.Utils;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class CaptureRenovate : OverlayCaptureAbilityBase<ParamCaptureRenovateModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.Capture;

        protected override void ValidateParameter(ParamCaptureRenovateModel paramModel) {
            try
            {
                logger.LogDebug("Start CaptureRenovate");
                if (string.IsNullOrEmpty(paramModel.selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ElementNotType_Error);
                }
                originalCommonProps = JsonConvert.DeserializeObject<RPACommon>(paramModel.selector)?.CommonProps;
                bool isReCapture = originalCommonProps != null;
                GlobleConfigurations.CaptureUniqueID = isReCapture ? originalCommonProps.GetRunUniqueID() : RandomHelper.RandomString(10);
                if (string.IsNullOrEmpty(paramModel.filepath))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "缺少文件地址参数".I18n("controlType_CaptureRenovate_98CDB9F7"));
                }
                else
                {
                    string path = Path.GetDirectoryName(paramModel.filepath);
                    string fileName = isReCapture && !string.IsNullOrEmpty(originalCommonProps.filePath) ? Path.GetFileName(originalCommonProps.filePath) : Path.GetFileName(paramModel.filepath);
                    paramModel.filepath = Path.Combine(path, GlobleConfigurations.CaptureUniqueID, fileName);
                    logger.LogDebug($"元素图标保存路径：{paramModel.filepath}");
                }
                if (paramModel.isCVCapture == true && string.IsNullOrEmpty(paramModel.cvServer))
                {
                    throw new RPAException(eErrorMessageType.Error_ConfigCVSever);
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
        GuiElement elementCache = null;
        JMessage captureMessage = null;
        string globalSelector = null;
        string selectortype = null;
        CommonProps originalCommonProps = null;


        protected override JMessage Execute(ParamCaptureRenovateModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            if (string.IsNullOrEmpty(paramModel.selector))
                throw new RPAException(eErrorMessageType.Error_ElementRenovateNotType_Error);
            selectortype = GuiElement.GetSelectorType(paramModel.selector);
            if (GuiElement.IsSupportSimilarSelectorType(selectortype))
                throw new RPAException(eErrorMessageType.Error_ElementRenovateNonsupport_Error);
            #region [结构兼容方案]
            string nativeSelectorCopy = paramModel.selector;
            paramModel.selector = string.Empty;
            #endregion
            captureMessage = new JMessage();
            elementCache = null;
            globalSelector = string.Empty;
            try
            {
                logger.LogDebug($"开始拾取：CaptureRenovate.Excute");
                GuiElement.OnSelected = UIElement_OnSelected;
                GuiElement.OnCanceled = UiElement_OnCancel;
                paramModel.cvServer = GlobleConfigurations.ZBrainServicePath + CVRouteConstFields.CV_ELEMENT_SERVICE_SETUP_DETECT;
                GuiElement.StartElementHighlight(paramModel.filepath, paramModel.isCVCapture, paramModel.cvServer, paramModel.InspectStatus, RecoverViewData.RestoreViewData(paramModel.selector));
                GuiElement.overlayForm.Visible = false;
                if (!paramModel.isCVCapture)
                    GuiElement.overlayForm.inspectorStatus.SetWndShow();
                GuiElement.overlayForm.ShowCaptureDialog();
                if (!paramModel.isCVCapture)
                    //GuiElement.overlayForm.inspectorStatus.Close();
                    GuiElement.overlayForm.inspectorStatus.SetWndClose();
                /*****************需要在Chrome拾取完成之后，关闭Chrome***/
                GuiElement.ReleaseGuiResources ();
                GuiElement.overlayForm.IsStartInspectUI = false;
                if (!string.IsNullOrEmpty(GuiElement.overlayForm.ExceptionError))
                {
                    captureMessage.Code = 1;
                    captureMessage.UnifyCode = "1";
                    captureMessage.Message = GuiElement.overlayForm.ExceptionError;
                }
                else if (!string.IsNullOrEmpty(GuiElement.overlayForm.FindError))
                {
                    captureMessage.Code = 2;
                    captureMessage.UnifyCode = "2";
                    captureMessage.Message = GuiElement.overlayForm.FindError;
                }
                else if (!string.IsNullOrEmpty(GuiElement.overlayForm.SelectorError))
                {
                    captureMessage.Code = 1;
                    captureMessage.UnifyCode = "1";
                    captureMessage.Message = GuiElement.overlayForm.SelectorError;
                    GuiElement.overlayForm.SelectorError = null;
                }
                else if (!string.IsNullOrEmpty(GuiElement.overlayForm.IsPause))
                {
                    captureMessage.Message = GuiElement.overlayForm.IsPause;
                }
                else
                {
                    if (captureMessage.Code != 1)
                    {
                        var inspecttype = GuiElement.GetSelectorType(globalSelector);
                        if (inspecttype != selectortype)
                        {
                            captureMessage.Info = "";
                            throw new RPAException(eErrorMessageType.Error_ElementRenovatePlacement_Error);
                        }
                        captureMessage.Info = BulidSimileSelector(nativeSelectorCopy, globalSelector);
                    }
                }

            }
            catch (Exception ex)
            {
                if (GuiElement.overlayForm.Visible == true)
                {
                    GuiElement.overlayForm.Close();
                }
                //GuiElement.overlayForm.SetWndHide();
                Exception(ex, eErrorMessageType.Error_ElementCaptureError, ref captureMessage);
            }
            return captureMessage;
        }

        private string UIElement_OnSelected(GuiElement element)
        {
            if (!element.IsGuiType(selectortype))
            {
                if (!GuiElement.selectorType_Web.Contains(selectortype, StringComparer.OrdinalIgnoreCase) || !element.IsWebType())
                {
                    throw new RPAException(eErrorMessageType.Error_ElementPlacement_Error);
                }
            }
            elementCache = element;
            globalSelector = AddFileAttrToSelector(element.Selector, element.screenShotPath);
            return globalSelector;
        }
        /// <summary>
        /// 过滤转义 ‘前端’传入selector都需要过滤 ’engine‘ 传入无需过滤
        /// </summary>
        /// <param name="buildSelector"></param>
        private void UnescapeValue(List<SelectorProps> buildSelector)
        {
            foreach (var nodeSelector in buildSelector)
            {
                foreach (var prop in nodeSelector.Props)
                {
                    prop.Value = Substring(prop.Value);
                }
            }
        }
        private string Substring(string value)
        {
            return value.Trim('"');
        }

        internal string BulidSimileSelector(string nativeSelector, string buildSelector)
        {
            var nativeEleSelector = UiCommon.ExtractSelectors(nativeSelector);
            var selectorAll = JsonConvert.DeserializeObject<RPACommon>(buildSelector);

            var nativeListSelector = JsonConvert.DeserializeObject<List<SelectorProps>>(nativeEleSelector);
            UnescapeValue(nativeListSelector);
            var buildListSelector = JsonConvert.DeserializeObject<List<SelectorProps>>(selectorAll.Selector.ToString());
            if (nativeListSelector.Count != buildListSelector.Count)
                throw new RPAException(eErrorMessageType.Error_ElementRenovateLevel_Error);
            var selectorList = new List<SelectorProps>();
            for (int i = 0; i < nativeListSelector.Count; i++)
            {
                var selectorProps = new SelectorProps();

                if (nativeListSelector[i].Type != buildListSelector[i].Type)
                    throw new RPAException(eErrorMessageType.Error_ElementRenovateNonsupport_Error);
                else
                    selectorProps.Type = nativeListSelector[i].Type;
                if (nativeListSelector[i].Name != buildListSelector[i].Name)
                    throw new RPAException(eErrorMessageType.Error_ElementRenovateNode_Error);
                else
                    selectorProps.Name = nativeListSelector[i].Name;
                var propsCount = 0;
                if (nativeListSelector[i].Props.Count <= buildListSelector[i].Props.Count)
                {
                    propsCount = nativeListSelector[i].Props.Count;
                }
                else
                {
                    propsCount = buildListSelector[i].Props.Count;
                }
                selectorProps.Props = new List<Props>();
                for (int j = 0; j < propsCount; j++)
                {
                    for (int z = 0; z < propsCount; z++)
                    {
                        var nativeValue = nativeListSelector[i].Props[j].Value;
                        if (nativeListSelector[i].Props[j]?.Name == "@index"
                       && buildListSelector[i].Props[z]?.Name == "@index")
                        {
                            if (nativeListSelector[i].Accurate.As<bool>())
                            {
                                if (nativeValue != buildListSelector[i].Props[z].Value)
                                {
                                    if (nativeListSelector[i].Props[j].Accurate.As<bool>())
                                    {
                                        buildListSelector[i].Props[z].Accurate = "false";
                                    }
                                }
                            }
                            else
                            {
                                //非选中层级保留原始值
                                buildListSelector[i].Props[z] = nativeListSelector[i].Props[j];
                            }
                            selectorProps.Props.Add(buildListSelector[i].Props[z]);
                        }
                        else
                        {
                            if (nativeListSelector[i].Props[j]?.Name == buildListSelector[i].Props[z]?.Name)
                            {
                                //同步选中状态
                                buildListSelector[i].Props[z].Accurate = nativeListSelector[i].Props[j].Accurate;
                                if (nativeValue != buildListSelector[i].Props[z].Value)
                                {
                                    //选中层级修复
                                    if (nativeListSelector[i].Accurate.As<bool>())
                                    {
                                        //选中属性修复
                                        if (nativeListSelector[i].Props[j].Accurate.As<bool>())
                                        {
                                            buildListSelector[i].Props[z].ExpressionValue = buildListSelector[i].Props[z].Value;
                                            buildListSelector[i].Props[z].Value = BuildRegular(buildListSelector[i].Props[z].Value, nativeValue);
                                            buildListSelector[i].Props[z].TextValue = buildListSelector[i].Props[z].Value;
                                            buildListSelector[i].Props[z].Pattern = "regular";
                                        }
                                    }
                                    else
                                    {
                                        //非选中层级保留原始值
                                        buildListSelector[i].Props[z] = nativeListSelector[i].Props[j];
                                    }
                                }
                                selectorProps.Props.Add(buildListSelector[i].Props[z]);
                            }
                        }
                    }
                }
                selectorProps.Accurate = nativeListSelector[i].Accurate;
                selectorList.Add(selectorProps);
            }
            var result = JsonConvert.SerializeObject(selectorList);
            selectorAll.Selector = JArray.Parse(result);
            return JsonConvert.SerializeObject(selectorAll); ;
        }
        private string BuildRegular(string buildString, string nativeString)
        {
            string regular = string.Empty;
            if (nativeString != null && buildString != null)
            {
                int minLength = nativeString.Length < buildString.Length ? nativeString.Length : buildString.Length;
                int maxLength = nativeString.Length > buildString.Length ? nativeString.Length : buildString.Length;
                int num = maxLength - minLength;
                string[] values = new string[maxLength];
                int left = 0;
                int right = maxLength - 1;
                while (true)
                {
                    if (minLength == left)
                        break;
                    if (right <= num)
                        break;
                    if (left == right)
                        break;
                    if (nativeString[left] == buildString[left])
                    {
                        values[left] = buildString[left].ToString();
                        left++;
                    }
                    else
                    {
                        if (nativeString.Length - 1 >= buildString.Length - 1)
                        {
                            if (nativeString[right] == buildString[right - num])
                            {
                                values[right] = buildString[right - num].ToString();
                            }
                        }
                        else
                        {
                            if (nativeString[right - num] == buildString[right])
                            {
                                values[right] = buildString[right - num].ToString();
                            }
                        }
                        right--;
                    }
                }
                List<string> strings = new List<string>();
                bool isSuperaddition = false;
                foreach (var item in values)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        strings.Add(item);
                        isSuperaddition = false;
                    }
                    else
                    {
                        if (!isSuperaddition)
                        {
                            strings.Add(".*");
                            isSuperaddition = true;
                        }
                    }
                }
                List<string> matches = strings.ToList();
                if (minLength != maxLength && matches[matches.Count - 1] != ".*")
                    matches.Add("*");
                regular = string.Join("", matches);
            }
            return regular;
        }

        private string AddFileAttrToSelector(string orgJson, string filePath)
        {
            try
            {
                var common = JsonConvert.DeserializeObject<RPACommon>(orgJson);
                if (GuiElement.IsWebSelectorType(common.CommonProps.GUIType))
                {
                    common.CommonProps.XpathSelector = SelectorPathToXPath.Build(JsonConvert.DeserializeObject<List<SelectorProps>>(common.Selector.ToString()));
                }
                common.CommonProps.ModifyTime = DateTime.Now.ToLongTimeStamp();
                common.CommonProps.filePath = filePath;
                common.CommonProps.captureMode = (int)GuiElement.overlayForm.CurrentCaptureMode;
                common.CommonProps.Version = GlobleConfigurations.CurrentSelectorVersion;
                common.CommonProps.ResDirPath = $@"res\{GlobleConfigurations.CaptureUniqueID}";
                if (originalCommonProps != null)
                {
                    common.CommonProps.isFindbyIndex = originalCommonProps.isFindbyIndex;
                    common.CommonProps.isSmartControl = originalCommonProps.isSmartControl;
                    common.CommonProps.DiscernHidden = originalCommonProps.DiscernHidden;
                }
                else
                {
                    common.CommonProps.isSmartControl = "false";
                }

                GuiElement.overlayForm.CaptureOperationData.pick_mode = GuiElement.overlayForm.CurrentCaptureMode.ToString();
                GuiElement.overlayForm.CaptureOperationData.app_name = common.CommonProps.ProcessName;
                GuiElement.overlayForm.CaptureOperationData.element_type = common.CommonProps.GUIType;
                try
                {
                    if (common.ElementDetail.CategoryName != "Windows桌面")
                    {
                        var icon = Icon.ExtractAssociatedIcon(common.ElementDetail.CategoryIconFile);
                        Bitmap bmp = icon.ToBitmap();
                        var iconfile = Path.GetDirectoryName(Path.GetDirectoryName(filePath));
                        var iconfileName = Path.GetFileNameWithoutExtension(common.ElementDetail.CategoryIconFile);
                        iconfile = Path.Combine(iconfile, iconfileName + ".ico");
                        bmp.Save(iconfile, System.Drawing.Imaging.ImageFormat.Icon);
                        common.ElementDetail.CategoryIcon = iconfileName + ".ico";
                    }
                    else
                    {
                        common.ElementDetail.CategoryIcon = "";
                    }
                    common.ElementDetail.ElementId = GlobleConfigurations.CaptureUniqueID;
                }
                catch (Exception)
                {
                    common.ElementDetail.CategoryIcon = "";
                    common.ElementDetail.CategoryIconFile = "";
                    common.ElementDetail.CategoryName = "";
                    common.ElementDetail.WindowName = "";
                }
                if (GuiElement.overlayForm.UseCaptureMode == CaptureModeType.FloatWnd)
                {
                    common.CommonProps.Flag = "NoForeground";
                }
                return JsonConvert.SerializeObject(common);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return orgJson;
            }
        }
        private void UiElement_OnCancel()
        {
            if (captureMessage != null)
            {
                captureMessage.Code = 1;
                captureMessage.UnifyCode = "1";
                captureMessage.Message = "用户取消".I18n("CaptureRenovate_455826F4");
            }
            globalSelector = string.Empty;
        }
        private void UIElement_OnSelectored(string selector)
        {
            globalSelector = selector;
        }
        protected override string HandlingErrorMessage(JMessage resultMsg)
        {
            GuiElement.ReleaseGuiResources ();
            return base.HandlingErrorMessage(resultMsg);
        }
    }
}

