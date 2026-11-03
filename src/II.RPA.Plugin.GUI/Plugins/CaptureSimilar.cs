using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using Newtonsoft.Json;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class CaptureSimilar : OverlayCaptureAbilityBase<ParamCaptureSimilarModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.Capture;

        protected override void ValidateParameter(ParamCaptureSimilarModel paramModel) {
            try
            {
                logger.LogDebug("Start CaptureSimilar");
                if (string.IsNullOrEmpty(paramModel.selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ElementNotType_Error);
                }
                similarSelector = paramModel.selector;
                originalCommonProps = JsonConvert.DeserializeObject<RPACommon>(paramModel.selector)?.CommonProps;
                bool isReCapture = originalCommonProps != null;
                GlobleConfigurations.CaptureUniqueID = isReCapture ? originalCommonProps.GetRunUniqueID() : RandomHelper.RandomString(10);
                if (string.IsNullOrEmpty(paramModel.filepath))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "缺少文件地址参数".I18n("controlType_CaptureSimilar_98CDB9F7"));
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
        string similarSelector = null;
        string selectortype = null;
        CommonProps originalCommonProps = null;
        private const string SapSimilarUnsupportedMessage = "当前SAP获取相似元素仅支持同一Grid表格内单元格的同行或同列泛化，请选择同一SAP Grid表格中的同行或同列单元格作为相似元素。";

        protected override JMessage Execute(ParamCaptureSimilarModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            if (string.IsNullOrEmpty(paramModel.selector))
                throw new RPAException(eErrorMessageType.Error_ElementNotType_Error);
            selectortype = GuiElement.GetSelectorType(paramModel.selector);
            if (GuiElement.IsSupportSimilarSelectorType(selectortype))
                throw new RPAException(eErrorMessageType.Error_ElementNonsupport_Error);
            captureMessage = new JMessage();
            elementCache = null;
            globalSelector = string.Empty;
            try
            {
                logger.LogDebug($"开始拾取：CaptureSimilar.Excute");
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
                        captureMessage.Info = globalSelector;
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
            //应产品要求，拾取循环相似元素时返回的消息统一去除code码部分，
            //此处存在多处逻辑处理时直接返回了带code码的消息，
            //此处暂时先通过字符串截取返回
            if (captureMessage.Message.Count(a => a == '[') == 2 &&
                captureMessage.Message.Count(a => a == ']') == 2)
            {
                //层级结构和节点不同类型的异常
                if (captureMessage.Message.Contains("010400100000124") ||
                    captureMessage.Message.Contains("010400100000125"))
                {
                    //和前端约定的code码
                    captureMessage.Code = 3015;
                }
                captureMessage.Message = captureMessage.Message.Substring(captureMessage.Message.LastIndexOf(']') + 1);
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
            globalSelector = BuildCaptureSimilarSelector(element);
            return globalSelector;
        }
        private string BuildCaptureSimilarSelector(GuiElement element)
        {
            string orgJson = element.Selector;
            if (!string.IsNullOrEmpty(similarSelector)
                && selectortype.EqualIgnoreCase(eGuiType.SAP.ToString()))
            {
                try
                {
                    RPACommon srcCommon = similarSelector.ToObject<RPACommon>();
                    RPACommon currentCommon = orgJson.ToObject<RPACommon>();
                    var processedSelector = element.CompareBulidCollectDataSelectorV2(new ParamBulidCollectDataSelectorModel
                    {
                        OriginalSelector = srcCommon.Selector.ToObject<List<SelectorProps>>(),
                        CompareOriginalSelector = currentCommon.Selector.ToObject<List<SelectorProps>>()
                    });
                    if (processedSelector != null && processedSelector.Count > 0)
                    {
                        var result = SelectorShell.RepackWithSourceShell(JArray.FromObject(processedSelector), similarSelector, false);
                        return PreserveOriginalResourcePaths(result).ToJsonNoneFormating();
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "SAP CaptureSimilar build selector failed.");
                }
                throw new RPAException(eErrorMessageType.Error_ElementNonsupport_Error, SapSimilarUnsupportedMessage.I18n("controlType_CaptureSimilar_SapSimilarUnsupported"));
            }
            return AddFileAttrToSelector(orgJson);
        }
        private string AddFileAttrToSelector(string orgJson)
        {
            try
            {
                if (!string.IsNullOrEmpty(similarSelector))
                {
                    var result = SelectorShell.RepackWithSourceShell(orgJson, similarSelector, false);
                    orgJson = PreserveOriginalResourcePaths(result).ToJsonNoneFormating();
                }
                return orgJson;
            }
            catch (Exception ex)
            {
                NLogger.Default.Error(ex);
                return orgJson;
            }
        }
        private RPACommon PreserveOriginalResourcePaths(RPACommon result)
        {
            if (originalCommonProps == null)
                return result;
            result.CommonProps.filePath = originalCommonProps.filePath;
            result.CommonProps.ResDirPath = originalCommonProps.ResDirPath;
            return result;
        }
        private void UiElement_OnCancel()
        {
            if (captureMessage != null)
            {
                captureMessage.Code = 1;
                captureMessage.UnifyCode = "1";
                captureMessage.Message = eCommonMessageType.SDK_cs_YongHuQuXiao.GetMessage();
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

