using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility.Utils;
using II.RPA.Plugin.Core.Commons;
using Newtonsoft.Json.Linq;
using II.RPA.WebBrowser.Common.Basic;

namespace II.RPA.Plugin.GUI
{
    /// <summary>
    /// 先开一个新拾取组件，以便后续agent模式拾取的扩展，目前组件处理逻辑基本和现有普通拾取一致
    /// </summary>
    public class CaptureForAgent : OverlayCaptureAbilityBase<ParamCaptureModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.Capture;
        int isAutoSimilar = 0;

        protected override void ValidateParameter(ParamCaptureModel paramModel) {
            try
            {
                logger.LogDebug("Start Capture");
                originalCommonProps = paramModel.CommonProps;//必须每次赋值

                GlobleConfigurations.CaptureUniqueID = RandomHelper.RandomString(10);
                isAutoSimilar = paramModel.IsAutoSimilar;
                if (string.IsNullOrEmpty(paramModel.filepath))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "缺少文件地址参数".I18n("controlType_CaptureForAgent_98CDB9F7"));
                }
                else
                {
                    string path = Path.GetDirectoryName(paramModel.filepath);
                    string fileName = Path.GetFileName(paramModel.filepath);
                    paramModel.filepath = Path.Combine(path, GlobleConfigurations.CaptureUniqueID, fileName);
                }
                if (paramModel.isCVCapture == true && string.IsNullOrEmpty(paramModel.cvServer))
                {
                    throw new RPAException(eErrorMessageType.Error_ConfigCVSever);
                }
                return;
            }
            catch (Exception ex)
            {
                logger.LogInformation($"参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
        GuiElement elementCache = null;
        JMessage captureMessage = null;
        string globalSelector = null;
        CommonProps originalCommonProps = null;
        string outerHtml = null;

        protected override JMessage Execute(ParamCaptureModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            captureMessage = new JMessage();
            elementCache = null;
            globalSelector = string.Empty;
            outerHtml = string.Empty;

            logger.LogInformation($"Element当前应用域：{AppDomain.CurrentDomain.FriendlyName}");
            TimeTrackHelper timeTrackHelper = new TimeTrackHelper();
            timeTrackHelper.Start("CaptureExcute", TimeTrackLogLevel.info);
            try
            {
                logger.LogDebug($"开始拾取：CaptureForAgent.Excute");
                GuiElement.OnSelected = UIElement_OnSelected;
                GuiElement.OnCanceled = UiElement_OnCancel;
                GuiElement.OnSelectored = UIElement_OnSelectored;
                GuiElement.overlayForm.CaptureException = null;
                paramModel.cvServer = GlobleConfigurations.ZBrainServicePath + CVRouteConstFields.CV_ELEMENT_SERVICE_SETUP_DETECT;

                GuiElement.StartElementHighlight(paramModel.filepath, paramModel.isCVCapture, paramModel.cvServer, paramModel.InspectStatus, "", paramModel.CaptureMode, paramModel.IsCvSupport);
                GuiElement.overlayForm.Visible = false;
                GuiElement.overlayForm.IsAgentCapture = true;

                GuiElement.overlayForm.InitForStartCapture();
                timeTrackHelper.Pick("CaptureExcute InspectorStatusSetWndShow start", TimeTrackLogLevel.info);
                GuiElement.overlayForm.inspectorStatus.SetWndShow();
                timeTrackHelper.Pick("CaptureExcute InspectorStatusSetWndShow end", TimeTrackLogLevel.info);
                timeTrackHelper.Stop("CaptureExcute", TimeTrackLogLevel.info);
                GuiElement.overlayForm.ShowCaptureDialog();

                GuiElement.overlayForm.inspectorStatus.SetWndClose();
                /*****************需要在Chrome拾取完成之后，关闭Chrome***/
                GuiElement.ReleaseGuiResources();
                GuiElement.CVRelease();
                GuiElement.overlayForm.IsStartInspectUI = false;

                if (GuiElement.overlayForm.CaptureException != null)
                {
                    throw GuiElement.overlayForm.CaptureException;
                }
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
                    JObject uiSelector = JToken.Parse(globalSelector) as JObject;
                    var elementData = new { selector = uiSelector, outerHtml = outerHtml, webWndImage = GuiElement.overlayForm.BrowserWndImageFilePath };
                    captureMessage.Info = JsonConvert.SerializeObject(elementData);
                }
            }
            catch (COMException ex)
            {
                logger.LogError(ex);
            }
            catch (Exception ex)
            {
                if (GuiElement.overlayForm.Visible == true)
                {
                    GuiElement.overlayForm.Close();
                }
                //GuiElement.overlayForm.SetWndHide();//需要进一步修改 增加dispose

                if (GuiElement.overlayForm?.inspectorStatus?.Visibility == System.Windows.Visibility.Visible)
                {
                    //GuiElement.overlayForm?.inspectorStatus?.Close();
                    GuiElement.overlayForm.inspectorStatus.SetWndClose();
                }
                Exception(ex, eErrorMessageType.Error_ElementCaptureError, ref captureMessage);
            }
            finally
            {
                if (GuiElement.overlayForm.inspectorStatus != null && !GuiElement.overlayForm.inspectorStatus.IsClosed)
                {
                    //GuiElement.overlayForm.inspectorStatus.Close();
                    GuiElement.overlayForm.inspectorStatus.SetWndClose();
                }
                GuiElement.overlayForm.IsAgentCapture = false;
                GuiElement.overlayForm.BrowserWndImageFilePath = "";
                //GlobleConfigurations.CaptureUniqueID = string.Empty;
                timeTrackHelper.Stop("CaptureExcute", TimeTrackLogLevel.info);
            }
            return captureMessage;
        }

        private string UIElement_OnSelected(GuiElement element)
        {
            elementCache = element;
            globalSelector = AddFileAttrToSelector(isAutoSimilar == 1 ? element.GetSimilarSelectorByAuto() : element.Selector, element.screenShotPath);
            //只处理浏览器元素部分
            if (element?.uiNode is ElementGuiNode)
            {
                outerHtml = element.GetCleanHtml();
            }
            return globalSelector;
        }
        private string AddFileAttrToSelector(string orgJson, string filePath)
        {
            try
            {

                //JArray jArray = JArray.Parse(orgJson);
                var common = JsonConvert.DeserializeObject<RPACommon>(orgJson);
                //var item = jArray?.FirstOrDefault();
                //if (item != null)
                //{
                string fileContent = filePath;
                //如果是远程拾取服务端，传递图标的base64
                if (GlobleConfigurations.IsRemoteCaptureServer)
                {
                    if (File.Exists(filePath))
                    {
                        using (var img = new Bitmap(filePath))
                        {
                            fileContent = ImageHelper.Bitmap2Base64(img);
                        }
                    }
                }
                common.CommonProps.ModifyTime = DateTime.Now.ToLongTimeStamp();
                common.CommonProps.filePath = fileContent;
                common.CommonProps.captureMode = (int)GuiElement.overlayForm.CurrentCaptureMode;
                //统一设置Selector版本，selector的版本升级是跟着大版本统一处理，如后期有更细致的版本管理规则，改为在各依赖包内部指定版本号
                common.CommonProps.Version = GlobleConfigurations.CurrentSelectorVersion;
                //埋点数据
                GuiElement.overlayForm.CaptureOperationData.pick_mode = GuiElement.overlayForm.CurrentCaptureMode.ToString();
                GuiElement.overlayForm.CaptureOperationData.app_name = common.CommonProps.ProcessName;
                GuiElement.overlayForm.CaptureOperationData.element_type = common.CommonProps.GUIType;

                //(item as JObject).Add("filePath", fileContent);
                //(item as JObject).Add("captureMode", (int)GuiElement.overlayForm.CurrentCaptureMode);
                common.CommonProps.ResDirPath = $@"res\{GlobleConfigurations.CaptureUniqueID}";
                if (originalCommonProps != null)
                {
                    common.CommonProps.isFindbyIndex = originalCommonProps.isFindbyIndex;
                    common.CommonProps.isSmartControl = originalCommonProps.isSmartControl;
                    common.CommonProps.DiscernHidden = originalCommonProps.DiscernHidden;
                }
                try
                {
                    logger.LogDebug($"开始处理ico");
                    if (common.ElementDetail.CategoryName != "Windows桌面")
                    {
                        var icon = System.Drawing.Icon.ExtractAssociatedIcon(common.ElementDetail.CategoryIconFile);
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
                    logger.LogDebug($"处理ico结束");
                }
                catch (Exception e)
                {
                    common.ElementDetail.CategoryIcon = "";
                    common.ElementDetail.CategoryIconFile = "";
                    common.ElementDetail.CategoryName = "";
                    common.ElementDetail.WindowName = "";
                }
                if (GuiElement.overlayForm.UseCaptureMode == CaptureModeType.FloatWnd)
                {
                    common.CommonProps.Flag = "NoForeground";
                    //(item as JObject).Add("Flag", "NoForeground");
                }
                var originalSelector = JsonConvert.SerializeObject(common);
                SelectorFormat(originalSelector, out var selector);
                //}
                return selector;
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
            GuiElement.ReleaseGuiResources();
            GuiElement.overlayForm.CaptureOperationData.is_success = (resultMsg.Code == 0 ? 1 : 0);
            resultMsg.Extend = GuiElement.overlayForm.CaptureOperationData.ToJson();
            return base.HandlingErrorMessage(resultMsg);
        }
        private bool SelectorFormat(string aSelectorOriginal, out string aSelectorFormated)
        {
            string _picturePath;
            aSelectorFormated = aSelectorOriginal;
            if (string.IsNullOrEmpty(aSelectorOriginal))
            {
                return false;
            }
            try
            {
                JObject uiSelector = JToken.Parse(aSelectorOriginal) as JObject;
                if (uiSelector["commonProps"]["filePath"] != null)
                {
                    var picturePath = uiSelector["commonProps"]["filePath"].ToString();
                    _picturePath = Path.Combine("res", GlobleConfigurations.CaptureUniqueID, Path.GetFileName(picturePath));
                    uiSelector["commonProps"]["filePath"] = _picturePath;
                }
                if (uiSelector["elementDetail"] != null)
                {
                    var icon_name = uiSelector["elementDetail"]["categoryIcon"];
                    try
                    {
                        if (icon_name != null && !string.IsNullOrEmpty(icon_name.ToString()))
                        {
                            uiSelector["elementDetail"]["categoryIcon"] = Path.Combine("res", icon_name.ToString());
                        }
                        else
                        {
                            uiSelector["elementDetail"]["categoryIcon"] = icon_name?.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex);
                        aSelectorFormated = string.Empty;
                        return false;
                    }
                }
                aSelectorFormated = uiSelector.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return true;
        }
    }
}
